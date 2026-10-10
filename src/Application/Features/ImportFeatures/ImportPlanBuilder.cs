using System.Globalization;
using System.Text.RegularExpressions;
using Application.Common.DTOs;
using Application.Common.Periods;
using Application.Interfaces.FileManager;

namespace Application.Features.ImportFeatures;

/// <summary>
/// Строит план импорта из листов файла и справочников БД.
/// Используется и в preview, и в apply, поэтому их результаты не расходятся.
/// </summary>
public static partial class ImportPlanBuilder
{
    // Ограничения длины из конфигураций сущностей.
    private const int GroupNameMaxLength = 20;
    private const int StudentNameMaxLength = 60;
    private const int StudentSurnameMaxLength = 70;
    private const int StudentPatronymicMaxLength = 70;
    private const int StudentNumberMaxLength = 64;
    private const int TeacherPartMaxLength = 255;
    private const int DisciplineNameMaxLength = 100;

    // Символы, разрешённые Identity в логине по умолчанию.
    private const string AllowedLoginChars =
        "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";

    public static ImportPlan Build(
        IReadOnlyList<RosterSheet> sheets,
        PeriodKey period,
        ImportReferenceData refs
    )
    {
        var plan = new ImportPlan { Period = period, ExistingPeriodId = refs.PeriodId };

        var studentsSheet = FindSheet(sheets, ImportFormat.StudentsSheet);
        var workloadsSheet = FindSheet(sheets, ImportFormat.WorkloadsSheet);

        if (studentsSheet is null && workloadsSheet is null)
        {
            plan.Errors.Add(new("", 0,
                $"В файле нет листов «{ImportFormat.StudentsSheet}» и «{ImportFormat.WorkloadsSheet}»"));
            return plan;
        }

        if ((studentsSheet?.Rows.Count ?? 0) == 0 && (workloadsSheet?.Rows.Count ?? 0) == 0)
        {
            plan.Errors.Add(new("", 0, "В файле нет строк с данными"));
            return plan;
        }

        // Группы, которые встречаются на листе «Студенты»: нагрузка может на них ссылаться.
        var sheetGroups = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        // Группы, которые понадобятся после импорта: для новых студентов и нагрузки.
        var neededGroups = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var referencedExistingGroups = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (studentsSheet is not null)
            BuildStudents(studentsSheet, refs, plan, sheetGroups, neededGroups, referencedExistingGroups);

        if (workloadsSheet is not null)
            BuildWorkloads(workloadsSheet, refs, plan, sheetGroups, neededGroups, referencedExistingGroups);

        plan.GroupsToCreate.AddRange(neededGroups.Values.Where(g => !refs.GroupIds.ContainsKey(g)));
        plan.ExistingGroups = referencedExistingGroups.Count;

        plan.StudentGroupNames.AddRange(sheetGroups.Values.Where(g =>
            refs.GroupIds.ContainsKey(g) || neededGroups.ContainsKey(g)));

        return plan;
    }

    private static void BuildStudents(
        RosterSheet sheet,
        ImportReferenceData refs,
        ImportPlan plan,
        Dictionary<string, string> sheetGroups,
        Dictionary<string, string> neededGroups,
        HashSet<string> referencedExistingGroups
    )
    {
        var columns = MapColumns(
            sheet,
            [ImportFormat.Group, ImportFormat.Surname, ImportFormat.Name, ImportFormat.StudentNumber],
            [ImportFormat.Patronymic],
            plan
        );

        if (columns is null || !CheckRowLimit(sheet, plan))
            return;

        // Номер зачётки → (строка, нормализованное содержимое строки).
        var seen = new Dictionary<string, (int Row, string Content)>(StringComparer.OrdinalIgnoreCase);

        foreach (var row in sheet.Rows)
        {
            var values = ReadRow(row, columns);
            if (values.Values.All(string.IsNullOrEmpty))
                continue;

            var errors = new List<string>();
            RequireFilled(values, errors, ImportFormat.Group, ImportFormat.Surname, ImportFormat.Name, ImportFormat.StudentNumber);

            var group = values[ImportFormat.Group];
            var surname = values[ImportFormat.Surname];
            var name = values[ImportFormat.Name];
            var patronymic = values.GetValueOrDefault(ImportFormat.Patronymic) is { Length: > 0 } p ? p : null;
            var number = values[ImportFormat.StudentNumber];

            CheckLength(errors, ImportFormat.Group, group, GroupNameMaxLength);
            CheckLength(errors, ImportFormat.Surname, surname, StudentSurnameMaxLength);
            CheckLength(errors, ImportFormat.Name, name, StudentNameMaxLength);
            CheckLength(errors, ImportFormat.Patronymic, patronymic, StudentPatronymicMaxLength);
            CheckLength(errors, ImportFormat.StudentNumber, number, StudentNumberMaxLength);

            if (number.Length > 0 && number.Any(c => !AllowedLoginChars.Contains(c)))
                errors.Add($"Номер зачётки «{number}» может содержать только латинские буквы, цифры и символы -._@+");

            if (AddErrors(plan, sheet.Name, row.RowNumber, errors))
                continue;

            var content = string.Join('\u001f', values.Values).ToLowerInvariant();
            if (seen.TryGetValue(number, out var first))
            {
                if (first.Content == content)
                    plan.Warnings.Add(new(sheet.Name, row.RowNumber, $"Строка повторяет строку {first.Row}, пропущена"));
                else
                    plan.Errors.Add(new(sheet.Name, row.RowNumber, $"Номер зачётки {number} уже встречается в строке {first.Row}"));
                continue;
            }
            seen[number] = (row.RowNumber, content);

            group = CanonicalGroup(group, refs, sheetGroups);
            sheetGroups.TryAdd(group, group);

            if (refs.Accounts.TryGetValue(number, out var account))
            {
                if (!account.IsStudent)
                {
                    plan.Errors.Add(new(sheet.Name, row.RowNumber, $"Логин {number} уже занят другой учётной записью"));
                    continue;
                }

                plan.ExistingStudents++;
                if (refs.GroupIds.ContainsKey(group))
                    referencedExistingGroups.Add(group);

                if (!string.Equals(account.GroupName, group, StringComparison.OrdinalIgnoreCase))
                {
                    plan.Warnings.Add(new(sheet.Name, row.RowNumber,
                        $"Студент {number} уже числится в группе {account.GroupName}, оставлен там"));
                }
                continue;
            }

            if (refs.GroupIds.ContainsKey(group))
                referencedExistingGroups.Add(group);
            neededGroups.TryAdd(group, group);

            plan.StudentsToCreate.Add(new PlannedStudent(group, surname, name, patronymic, number));
        }
    }

    private static void BuildWorkloads(
        RosterSheet sheet,
        ImportReferenceData refs,
        ImportPlan plan,
        Dictionary<string, string> sheetGroups,
        Dictionary<string, string> neededGroups,
        HashSet<string> referencedExistingGroups
    )
    {
        var columns = MapColumns(
            sheet,
            [ImportFormat.TeacherFio, ImportFormat.Discipline, ImportFormat.Group],
            [],
            plan
        );

        if (columns is null || !CheckRowLimit(sheet, plan))
            return;

        var seen = new Dictionary<(string, string, string), int>();
        var teachersToCreate = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var existingTeachers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var disciplines = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var existingDisciplines = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var row in sheet.Rows)
        {
            var values = ReadRow(row, columns);
            if (values.Values.All(string.IsNullOrEmpty))
                continue;

            var errors = new List<string>();
            RequireFilled(values, errors, ImportFormat.TeacherFio, ImportFormat.Discipline, ImportFormat.Group);

            var fio = values[ImportFormat.TeacherFio];
            var discipline = values[ImportFormat.Discipline];
            var group = values[ImportFormat.Group];

            PlannedTeacher? teacher = null;
            if (fio.Length > 0)
            {
                teacher = ParseTeacher(fio);
                if (teacher is null)
                    errors.Add($"ФИО преподавателя «{fio}» должно содержать фамилию, имя и отчество");
                else if (new[] { teacher.Surname, teacher.Name, teacher.Patronymic }.Any(x => x.Length > TeacherPartMaxLength))
                    errors.Add($"ФИО преподавателя длиннее {TeacherPartMaxLength} символов");
            }

            CheckLength(errors, ImportFormat.Discipline, discipline, DisciplineNameMaxLength);

            if (group.Length > 0 && !refs.GroupIds.ContainsKey(group) && !sheetGroups.ContainsKey(group))
                errors.Add($"Группа «{group}» не найдена ни в базе, ни на листе «{ImportFormat.StudentsSheet}»");

            if (AddErrors(plan, sheet.Name, row.RowNumber, errors))
                continue;

            group = CanonicalGroup(group, refs, sheetGroups);
            discipline = refs.DisciplineIds.Keys.FirstOrDefault(d => string.Equals(d, discipline, StringComparison.OrdinalIgnoreCase))
                ?? disciplines.GetValueOrDefault(discipline)
                ?? discipline;

            var key = (teacher!.Key.ToLowerInvariant(), discipline.ToLowerInvariant(), group.ToLowerInvariant());
            if (seen.TryGetValue(key, out var firstRow))
            {
                plan.Warnings.Add(new(sheet.Name, row.RowNumber, $"Строка повторяет строку {firstRow}, пропущена"));
                continue;
            }
            seen[key] = row.RowNumber;

            if (refs.TeacherIds.ContainsKey(teacher.Key))
                existingTeachers.Add(teacher.Key);
            else if (teachersToCreate.Add(teacher.Key))
                plan.TeachersToCreate.Add(teacher);

            if (refs.DisciplineIds.ContainsKey(discipline))
                existingDisciplines.Add(discipline);
            else if (disciplines.TryAdd(discipline, discipline))
                plan.DisciplinesToCreate.Add(discipline);

            if (refs.GroupIds.ContainsKey(group))
                referencedExistingGroups.Add(group);
            neededGroups.TryAdd(group, group);

            var exists = refs.TeacherIds.TryGetValue(teacher.Key, out var teacherId)
                && refs.DisciplineIds.TryGetValue(discipline, out var disciplineId)
                && refs.GroupIds.TryGetValue(group, out var groupId)
                && refs.Workloads.Contains(new WorkloadKey(teacherId, disciplineId, groupId));

            if (exists)
                plan.ExistingWorkloads++;
            else
                plan.WorkloadsToCreate.Add(new PlannedWorkload(teacher.Key, discipline, group));
        }

        plan.ExistingTeachers = existingTeachers.Count;
        plan.ExistingDisciplines = existingDisciplines.Count;
    }

    public static string TeacherKey(string surname, string name, string patronymic) =>
        $"{surname} {name} {patronymic}";

    /// <summary>
    /// Разбирает «Фамилия Имя Отчество»: первое слово — фамилия, второе — имя, остальное — отчество.
    /// Регистр приводится к виду «Иванов Иван Иванович».
    /// </summary>
    public static PlannedTeacher? ParseTeacher(string fio)
    {
        var parts = Normalize(fio).Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 3)
            return null;

        return new PlannedTeacher(
            TitleCase(parts[0]),
            TitleCase(parts[1]),
            string.Join(' ', parts[2..].Select(TitleCase))
        );
    }

    /// <summary>Обрезает пробелы по краям и схлопывает повторные пробелы внутри.</summary>
    public static string Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "" : Whitespace().Replace(value.Trim(), " ");

    private static string NormalizeHeader(string header) =>
        Whitespace().Replace(header, "").ToLowerInvariant().Replace('ё', 'е');

    private static string TitleCase(string word) =>
        string.Join('-', word.Split('-').Select(part =>
            part.Length == 0
                ? part
                : char.ToUpper(part[0], CultureInfo.InvariantCulture) + part[1..].ToLower(CultureInfo.InvariantCulture)));

    private static RosterSheet? FindSheet(IReadOnlyList<RosterSheet> sheets, string name) =>
        sheets.FirstOrDefault(s => NormalizeHeader(s.Name) == NormalizeHeader(name));

    /// <summary>Сопоставляет колонки по заголовку. Если нет обязательной колонки, добавляет ошибку и возвращает null.</summary>
    private static Dictionary<string, int>? MapColumns(
        RosterSheet sheet,
        string[] required,
        string[] optional,
        ImportPlan plan
    )
    {
        var byHeader = new Dictionary<string, int>();
        for (var i = 0; i < sheet.Headers.Count; i++)
            byHeader.TryAdd(NormalizeHeader(sheet.Headers[i]), i);

        var columns = new Dictionary<string, int>();
        var missing = new List<string>();

        foreach (var column in required.Concat(optional))
        {
            if (byHeader.TryGetValue(NormalizeHeader(column), out var index))
                columns[column] = index;
            else if (required.Contains(column))
                missing.Add(column);
        }

        foreach (var column in missing)
            plan.Errors.Add(new(sheet.Name, 1, $"Нет колонки «{column}»"));

        return missing.Count == 0 ? columns : null;
    }

    private static bool CheckRowLimit(RosterSheet sheet, ImportPlan plan)
    {
        if (sheet.Rows.Count <= ImportFormat.MaxRowsPerSheet)
            return true;

        plan.Errors.Add(new(sheet.Name, 0,
            $"На листе {sheet.Rows.Count} строк, допускается не больше {ImportFormat.MaxRowsPerSheet}"));
        return false;
    }

    private static Dictionary<string, string> ReadRow(RosterRow row, Dictionary<string, int> columns) =>
        columns.ToDictionary(
            c => c.Key,
            c => c.Value < row.Values.Count ? Normalize(row.Values[c.Value]) : ""
        );

    private static void RequireFilled(Dictionary<string, string> values, List<string> errors, params string[] columns)
    {
        foreach (var column in columns)
        {
            if (values[column].Length == 0)
                errors.Add($"Не заполнена колонка «{column}»");
        }
    }

    private static void CheckLength(List<string> errors, string column, string? value, int maxLength)
    {
        if (value is not null && value.Length > maxLength)
            errors.Add($"Значение в колонке «{column}» длиннее {maxLength} символов");
    }

    private static bool AddErrors(ImportPlan plan, string sheet, int row, List<string> errors)
    {
        foreach (var error in errors)
            plan.Errors.Add(new ImportIssueDto(sheet, row, error));

        return errors.Count > 0;
    }

    /// <summary>Название группы как в БД, иначе как при первом упоминании в файле.</summary>
    private static string CanonicalGroup(string group, ImportReferenceData refs, Dictionary<string, string> sheetGroups) =>
        refs.GroupIds.Keys.FirstOrDefault(g => string.Equals(g, group, StringComparison.OrdinalIgnoreCase))
            ?? sheetGroups.GetValueOrDefault(group)
            ?? group;

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();
}
