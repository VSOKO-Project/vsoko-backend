using System.Globalization;
using System.Text.RegularExpressions;
using Application.Common.DTOs;
using Application.Common.Periods;
using Application.Interfaces.FileManager;
using Domain.Enums;

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
        ImportReferenceData refs,
        ImportMode mode = ImportMode.AddOnly
    )
    {
        var plan = new ImportPlan { Period = period, ExistingPeriodId = refs.PeriodId, Mode = mode };
        var scope = ReadScope(sheets);
        plan.CanSynchronize = scope?.Period == period &&
            (scope.TeacherIds is null || scope.TeacherIds.All(id => refs.TeacherIds.ContainsValue(id) || refs.TeacherNamesById.ContainsKey(id)));
        plan.ScopeDescription = scope is null
            ? "В файле нет корректных данных выгрузки. Доступны добавление и обновление без удаления нагрузки."
            : $"Выгрузка: {PeriodCalculator.Title(scope.Period.StartYear, scope.Period.Term)}. " +
                (scope.TeacherIds is null ? "Вся нагрузка семестра." :
                    $"Преподаватели: {string.Join(", ", scope.TeacherIds.Order().Select(id => refs.TeacherIds.FirstOrDefault(t => t.Value == id).Key ?? id))}.");
        if (mode == ImportMode.SynchronizeWorkloads && !plan.CanSynchronize)
            plan.Errors.Add(new("", 0, "Для синхронизации нужна исходная выгрузка выбранного семестра с сохранённым служебным листом. Выберите добавление/обновление или загрузите подходящую выгрузку."));

        var studentsSheet = FindSheet(sheets, ImportFormat.StudentsSheet);
        var workloadsSheet = FindSheet(sheets, ImportFormat.WorkloadsSheet);

        if (studentsSheet is null && workloadsSheet is null)
        {
            plan.Errors.Add(new("", 0,
                $"В файле нет листов «{ImportFormat.StudentsSheet}» и «{ImportFormat.WorkloadsSheet}»"));
            return plan;
        }

        if (mode == ImportMode.SynchronizeWorkloads && workloadsSheet is null)
            plan.Errors.Add(new("", 0, "Для синхронизации сохраните лист «Нагрузка». Для удаления всего среза оставьте на нём только заголовки."));

        if (!(studentsSheet?.Rows.Any(r => r.Values.Any(v => !string.IsNullOrWhiteSpace(v))) ?? false)
            && !(workloadsSheet?.Rows.Any(r => r.Values.Any(v => !string.IsNullOrWhiteSpace(v))) ?? false)
            && !(mode == ImportMode.SynchronizeWorkloads && plan.CanSynchronize && workloadsSheet is not null))
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

        HashSet<WorkloadKey>? keptWorkloads = null;
        if (workloadsSheet is not null)
            keptWorkloads = BuildWorkloads(workloadsSheet, refs, plan, sheetGroups, neededGroups, referencedExistingGroups);

        if (scope is not null && scope.Period != period)
        {
            plan.Warnings.Add(new("", 0,
                $"Файл выгружен из семестра «{PeriodCalculator.Title(scope.Period.StartYear, scope.Period.Term)}»: " +
                "в выбранном семестре удаления недоступны"));
        }
        else if (mode == ImportMode.SynchronizeWorkloads && scope is not null && keptWorkloads is not null && !plan.HasErrors)
        {
            PlanDeletions(scope, keptWorkloads, refs, plan);
        }

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
                var groupChanged = !string.Equals(account.GroupName, group, StringComparison.OrdinalIgnoreCase);
                var nameChanged = account.Surname is not null &&
                    (account.Surname != surname || account.Name != name || (account.Patronymic ?? "") != (patronymic ?? ""));
                if ((groupChanged || nameChanged) && plan.Mode != ImportMode.AddOnly)
                {
                    neededGroups.TryAdd(group, group);
                    plan.StudentsToUpdate.Add(new(new(group, surname, name, patronymic, account.Login ?? number)));
                    if (groupChanged)
                        plan.Changes.Add(new("Студент", number, "Группа", account.GroupName ?? "", group));
                    if (nameChanged)
                        plan.Changes.Add(new("Студент", number, "ФИО",
                            string.Join(' ', new[] { account.Surname, account.Name, account.Patronymic }.Where(s => !string.IsNullOrEmpty(s))),
                            string.Join(' ', new[] { surname, name, patronymic }.Where(s => !string.IsNullOrEmpty(s)))));
                }
                else if (groupChanged || nameChanged)
                {
                    plan.Warnings.Add(new(sheet.Name, row.RowNumber,
                        $"Изменения студента {number} не применятся в режиме добавления. " +
                        $"Текущая группа: {account.GroupName}. Для исправления ФИО или группы выберите режим обновления."));
                }
                continue;
            }

            if (refs.GroupIds.ContainsKey(group))
                referencedExistingGroups.Add(group);
            neededGroups.TryAdd(group, group);

            plan.StudentsToCreate.Add(new PlannedStudent(group, surname, name, patronymic, number));
        }
    }

    /// <summary>
    /// Нагрузка среза, которой нет в файле, удаляется. Нагрузку с отзывами не трогаем:
    /// удаление унесло бы отзывы студентов.
    /// </summary>
    private static void PlanDeletions(
        ExportScope scope,
        HashSet<WorkloadKey> keptWorkloads,
        ImportReferenceData refs,
        ImportPlan plan
    )
    {
        var teacherNames = refs.TeacherIds.ToDictionary(t => t.Value, t => t.Key);
        var disciplineNames = refs.DisciplineIds.ToDictionary(d => d.Value, d => d.Key);
        foreach (var pair in refs.TeacherNamesById) teacherNames[pair.Key] = pair.Value;
        foreach (var pair in refs.DisciplineNamesById) disciplineNames[pair.Key] = pair.Value;
        var groupNames = refs.GroupIds.ToDictionary(g => g.Value, g => g.Key);

        // Не удаляем запись, если её справочная сущность отсутствует в срезе данных.
        var removed = refs.Workloads
            .Where(w => scope.TeacherIds is null || scope.TeacherIds.Contains(w.Key.TeacherId))
            .Where(w => !keptWorkloads.Contains(w.Key))
            .Where(w => teacherNames.ContainsKey(w.Key.TeacherId)
                && disciplineNames.ContainsKey(w.Key.DisciplineId)
                && groupNames.ContainsKey(w.Key.GroupId))
            .Select(w => (
                w.Value,
                Deletion: new PlannedWorkloadDeletion(
                    w.Value.Id,
                    teacherNames[w.Key.TeacherId],
                    disciplineNames[w.Key.DisciplineId],
                    groupNames[w.Key.GroupId])))
            .OrderBy(w => w.Deletion.TeacherName)
            .ThenBy(w => w.Deletion.DisciplineName)
            .ThenBy(w => w.Deletion.GroupName);

        foreach (var (existing, deletion) in removed)
        {
            if (existing.FeedbackCount > 0)
            {
                plan.Warnings.Add(new(ImportFormat.WorkloadsSheet, 0,
                    $"Нагрузка «{deletion.TeacherName} — {deletion.DisciplineName} — {deletion.GroupName}» не удалится: " +
                    $"по ней уже есть отзывы ({existing.FeedbackCount})"));
                continue;
            }

            plan.WorkloadsToDelete.Add(deletion);
        }
    }

    /// <summary>Читает скрытый лист выгрузки. Если его нет или он испорчен — null: импорт только добавляет.</summary>
    private static ExportScope? ReadScope(IReadOnlyList<RosterSheet> sheets)
    {
        var sheet = FindSheet(sheets, ImportFormat.ScopeSheet);
        if (sheet is null || sheet.Headers.Count < 2)
            return null;

        PeriodKey? period = null;
        var teacherIds = new HashSet<string>();
        int? teacherCount = null;

        foreach (var row in sheet.Rows.Where(r => r.Values.Count >= 2))
        {
            var (key, value) = (row.Values[0].Trim(), row.Values[1].Trim());

            if (key == ImportFormat.ScopePeriod)
            {
                if (period is not null) return null;
                period = ParsePeriod(value);
            }
            else if (key == ImportFormat.ScopeTeacher && value.Length > 0)
                teacherIds.Add(value);
            else if (key == ImportFormat.ScopeTeacherCount)
            {
                if (teacherCount is not null || !int.TryParse(value, out var count) || count < 0) return null;
                teacherCount = count;
            }
        }

        if (teacherCount is { } expected && teacherIds.Count != expected) return null;
        return period is { } p ? new ExportScope(p, teacherIds.Count > 0 ? teacherIds : null) : null;
    }

    private static PeriodKey? ParsePeriod(string value)
    {
        var parts = value.Split(':');
        return parts.Length == 2
            && int.TryParse(parts[0], out var startYear)
            && startYear is >= 2000 and <= 2100
            && Enum.TryParse<Term>(parts[1], out var term)
            && Enum.IsDefined(term)
                ? new PeriodKey(startYear, term)
                : null;
    }

    /// <summary>Возвращает существующую нагрузку, которая осталась в файле, или null, если лист не разобран.</summary>
    private static HashSet<WorkloadKey>? BuildWorkloads(
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
            [ImportFormat.TeacherId, ImportFormat.DisciplineId],
            plan
        );

        if (columns is null || !CheckRowLimit(sheet, plan))
            return null;

        var kept = new HashSet<WorkloadKey>();
        var seen = new Dictionary<(string, string, string), int>();
        var teachersToCreate = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var existingTeachers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var disciplines = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var existingDisciplines = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var teacherNames = refs.TeacherIds.ToDictionary(t => t.Value, t => t.Key);
        var disciplineNames = refs.DisciplineIds.ToDictionary(d => d.Value, d => d.Key);
        foreach (var pair in refs.TeacherNamesById) teacherNames[pair.Key] = pair.Value;
        foreach (var pair in refs.DisciplineNamesById) disciplineNames[pair.Key] = pair.Value;
        var teacherEdits = new Dictionary<string, string>();
        var disciplineEdits = new Dictionary<string, string>();
        var ambiguousTeachers = teacherNames.Values.GroupBy(n => n, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1).Select(g => g.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var ambiguousDisciplines = disciplineNames.Values.GroupBy(n => n, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1).Select(g => g.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);

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
            var teacherId = values.GetValueOrDefault(ImportFormat.TeacherId, "");
            var disciplineId = values.GetValueOrDefault(ImportFormat.DisciplineId, "");
            if (teacherId.Length > 0 && !teacherNames.ContainsKey(teacherId))
                errors.Add("ID преподавателя не найден. Скачайте свежую выгрузку; для нового преподавателя оставьте ID пустым.");
            if (disciplineId.Length > 0 && !disciplineNames.ContainsKey(disciplineId))
                errors.Add("ID дисциплины не найден. Скачайте свежую выгрузку; для новой дисциплины оставьте ID пустым.");
            if (teacherId.Length == 0 && teacher is not null &&
                ambiguousTeachers.Contains(teacher.Key))
                errors.Add("Найдено несколько преподавателей с таким ФИО. Используйте выгрузку с ID преподавателя.");
            if (disciplineId.Length == 0 &&
                ambiguousDisciplines.Contains(discipline))
                errors.Add("Найдено несколько дисциплин с таким названием. Используйте выгрузку с ID дисциплины.");

            if (group.Length > 0 && !refs.GroupIds.ContainsKey(group) && !sheetGroups.ContainsKey(group))
                errors.Add($"Группа «{group}» не найдена ни в базе, ни на листе «{ImportFormat.StudentsSheet}»");

            if (AddErrors(plan, sheet.Name, row.RowNumber, errors))
                continue;

            group = CanonicalGroup(group, refs, sheetGroups);
            var teacherKey = teacherId.Length > 0 ? teacherNames[teacherId] : teacher!.Key;
            if (teacherId.Length == 0)
                teacherId = refs.TeacherIds.GetValueOrDefault(teacherKey, "");

            discipline = refs.DisciplineIds.Keys.FirstOrDefault(d => string.Equals(d, discipline, StringComparison.OrdinalIgnoreCase))
                ?? disciplines.GetValueOrDefault(discipline)
                ?? discipline;
            var disciplineKey = disciplineId.Length > 0 ? disciplineNames[disciplineId] : discipline;
            if (disciplineId.Length == 0)
                disciplineId = refs.DisciplineIds.GetValueOrDefault(disciplineKey, "");

            if (teacherId.Length > 0)
                PlanRename(ImportFormat.TeacherFio, "Преподаватель", teacherId, teacherNames[teacherId], teacher!.Key,
                    teacherEdits, refs.TeacherIds, plan, sheet.Name, row.RowNumber,
                    () => plan.TeachersToUpdate.Add(new(teacherId, teacher!)));
            if (disciplineId.Length > 0)
                PlanRename(ImportFormat.Discipline, "Дисциплина", disciplineId, disciplineNames[disciplineId], discipline,
                    disciplineEdits, refs.DisciplineIds, plan, sheet.Name, row.RowNumber,
                    () => plan.DisciplinesToUpdate.Add(new(disciplineId, discipline)));

            var key = ((teacherId.Length > 0 ? teacherId : teacherKey).ToLowerInvariant(),
                (disciplineId.Length > 0 ? disciplineId : disciplineKey).ToLowerInvariant(), group.ToLowerInvariant());
            if (seen.TryGetValue(key, out var firstRow))
            {
                plan.Warnings.Add(new(sheet.Name, row.RowNumber, $"Строка повторяет строку {firstRow}, пропущена"));
                continue;
            }
            seen[key] = row.RowNumber;

            if (teacherId.Length > 0)
                existingTeachers.Add(teacherId);
            else if (teachersToCreate.Add(teacher!.Key))
                plan.TeachersToCreate.Add(teacher!);

            if (disciplineId.Length > 0)
                existingDisciplines.Add(disciplineId);
            else if (disciplines.TryAdd(discipline, discipline))
                plan.DisciplinesToCreate.Add(discipline);

            if (refs.GroupIds.ContainsKey(group))
                referencedExistingGroups.Add(group);
            neededGroups.TryAdd(group, group);

            var existingKey = teacherId.Length > 0 && disciplineId.Length > 0
                && refs.GroupIds.TryGetValue(group, out var groupId)
                    ? new WorkloadKey(teacherId, disciplineId, groupId)
                    : (WorkloadKey?)null;

            if (existingKey is { } k && refs.Workloads.ContainsKey(k))
            {
                plan.ExistingWorkloads++;
                kept.Add(k);
            }
            else
            {
                plan.WorkloadsToCreate.Add(new PlannedWorkload(teacherKey, disciplineKey, group,
                    teacherId.Length > 0 ? teacherId : null, disciplineId.Length > 0 ? disciplineId : null));
            }
        }

        plan.ExistingTeachers = existingTeachers.Count;
        plan.ExistingDisciplines = existingDisciplines.Count;
        foreach (var rename in plan.TeachersToUpdate)
            if (teachersToCreate.Contains(rename.Teacher.Key))
                plan.Errors.Add(new(sheet.Name, 0, $"ФИО «{rename.Teacher.Key}» одновременно используется для нового и существующего преподавателя. Укажите одинаковый ID во всех его строках."));
        foreach (var rename in plan.DisciplinesToUpdate)
            if (disciplines.ContainsKey(rename.Name))
                plan.Errors.Add(new(sheet.Name, 0, $"Дисциплина «{rename.Name}» одновременно используется для новой и существующей записи. Укажите одинаковый ID во всех её строках."));
        return kept;
    }

    private static void PlanRename(string field, string entity, string id, string before, string after,
        Dictionary<string, string> edits, Dictionary<string, string> existing, ImportPlan plan,
        string sheet, int row, Action addUpdate)
    {
        if (edits.TryGetValue(id, out var previous))
        {
            if (!string.Equals(previous, after, StringComparison.Ordinal))
                plan.Errors.Add(new(sheet, row, $"Для {entity.ToLowerInvariant()} с ID {id} указаны разные названия. Исправьте все строки с этим ID одинаково."));
            return;
        }
        edits[id] = after;
        if (string.Equals(before, after, StringComparison.OrdinalIgnoreCase)) return;
        if (plan.Mode == ImportMode.AddOnly)
        {
            plan.Warnings.Add(new(sheet, row, $"{entity}: «{before}» → «{after}» не применится в режиме добавления. Выберите режим обновления."));
            return;
        }
        if ((existing.TryGetValue(after, out var otherId) && otherId != id)
            || edits.Any(e => e.Key != id && string.Equals(e.Value, after, StringComparison.OrdinalIgnoreCase)))
        {
            plan.Errors.Add(new(sheet, row, $"{entity} «{after}» уже существует. Объедините записи в справочнике, затем скачайте свежую выгрузку."));
            return;
        }
        addUpdate();
        plan.Changes.Add(new(entity, id, field, before, after));
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
