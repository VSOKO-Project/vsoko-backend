using Application.Common.DTOs;
using Application.Common.Periods;

namespace Application.Features.ImportFeatures;

public readonly record struct WorkloadKey(string TeacherId, string DisciplineId, string GroupId);

/// <summary>Существующая учётная запись: у студента есть группа, у сотрудника нет.</summary>
public record ExistingAccount(bool IsStudent, string? GroupName);

/// <summary>
/// Справочники из БД, загруженные один раз перед построением плана.
/// Ключи сравниваются без учёта регистра.
/// </summary>
public class ImportReferenceData
{
    public string? PeriodId { get; init; }

    /// <summary>Название группы → id.</summary>
    public Dictionary<string, string> GroupIds { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Логин (номер зачётки) → учётная запись.</summary>
    public Dictionary<string, ExistingAccount> Accounts { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>«Фамилия Имя Отчество» → id.</summary>
    public Dictionary<string, string> TeacherIds { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Название дисциплины → id.</summary>
    public Dictionary<string, string> DisciplineIds { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Нагрузка выбранного периода.</summary>
    public HashSet<WorkloadKey> Workloads { get; init; } = [];
}

public record PlannedStudent(string GroupName, string Surname, string Name, string? Patronymic, string StudentNumber);

public record PlannedTeacher(string Surname, string Name, string Patronymic)
{
    public string Key => ImportPlanBuilder.TeacherKey(Surname, Name, Patronymic);
}

public record PlannedWorkload(string TeacherKey, string DisciplineName, string GroupName);

/// <summary>
/// Что импорт создаст и что пропустит. Строится из файла и справочников, в БД ничего не пишет.
/// </summary>
public class ImportPlan
{
    public PeriodKey Period { get; init; }
    public string? ExistingPeriodId { get; init; }

    public List<string> GroupsToCreate { get; } = [];
    public int ExistingGroups { get; set; }

    public List<PlannedStudent> StudentsToCreate { get; } = [];
    public int ExistingStudents { get; set; }

    public List<PlannedTeacher> TeachersToCreate { get; } = [];
    public int ExistingTeachers { get; set; }

    public List<string> DisciplinesToCreate { get; } = [];
    public int ExistingDisciplines { get; set; }

    public List<PlannedWorkload> WorkloadsToCreate { get; } = [];
    public int ExistingWorkloads { get; set; }

    /// <summary>Группы с листа «Студенты», которые будут в БД после импорта.</summary>
    public List<string> StudentGroupNames { get; } = [];

    public List<ImportIssueDto> Errors { get; } = [];
    public List<ImportIssueDto> Warnings { get; } = [];

    public bool HasErrors => Errors.Count > 0;

    public ImportReportDto ToReport() =>
        new()
        {
            Period = new ImportPeriodDto
            {
                Id = ExistingPeriodId,
                StartYear = Period.StartYear,
                Term = Period.Term,
                WillBeCreated = ExistingPeriodId is null,
            },
            Summary = new ImportSummaryDto
            {
                Groups = new() { Create = GroupsToCreate.Count, Existing = ExistingGroups },
                Students = new() { Create = StudentsToCreate.Count, Existing = ExistingStudents },
                Teachers = new() { Create = TeachersToCreate.Count, Existing = ExistingTeachers },
                Disciplines = new() { Create = DisciplinesToCreate.Count, Existing = ExistingDisciplines },
                Workloads = new() { Create = WorkloadsToCreate.Count, Existing = ExistingWorkloads },
            },
            Errors = [.. Errors],
            Warnings = [.. Warnings],
        };
}
