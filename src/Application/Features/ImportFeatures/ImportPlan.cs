using Application.Common.DTOs;
using Application.Common.Periods;
using System.Security.Cryptography;
using System.Text.Json;

namespace Application.Features.ImportFeatures;

public readonly record struct WorkloadKey(string TeacherId, string DisciplineId, string GroupId);

public record ExistingWorkload(string Id, int FeedbackCount);

/// <summary>Срез выгрузки со скрытого листа. Без TeacherIds — вся нагрузка семестра.</summary>
public record ExportScope(PeriodKey Period, IReadOnlySet<string>? TeacherIds);

/// <summary>Существующая учётная запись: у студента есть группа, у сотрудника нет.</summary>
public record ExistingAccount(bool IsStudent, string? GroupName, string? Surname = null, string? Name = null, string? Patronymic = null, string? Login = null);

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
    public Dictionary<string, string> TeacherNamesById { get; init; } = [];

    /// <summary>Название дисциплины → id.</summary>
    public Dictionary<string, string> DisciplineIds { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, string> DisciplineNamesById { get; init; } = [];

    /// <summary>Нагрузка выбранного периода.</summary>
    public Dictionary<WorkloadKey, ExistingWorkload> Workloads { get; init; } = [];
}

public record PlannedStudent(string GroupName, string Surname, string Name, string? Patronymic, string StudentNumber);
public record PlannedStudentUpdate(PlannedStudent Student);
public record PlannedTeacherUpdate(string Id, PlannedTeacher Teacher);
public record PlannedDisciplineUpdate(string Id, string Name);

public record PlannedTeacher(string Surname, string Name, string Patronymic)
{
    public string Key => ImportPlanBuilder.TeacherKey(Surname, Name, Patronymic);
}

public record PlannedWorkload(string TeacherKey, string DisciplineName, string GroupName, string? TeacherId = null, string? DisciplineId = null);

public record PlannedWorkloadDeletion(string Id, string TeacherName, string DisciplineName, string GroupName);

/// <summary>
/// Что импорт создаст и что пропустит. Строится из файла и справочников, в БД ничего не пишет.
/// </summary>
public class ImportPlan
{
    public ImportMode Mode { get; init; }
    public bool CanSynchronize { get; set; }
    public string ScopeDescription { get; set; } = "";
    public List<ImportChangeDto> Changes { get; } = [];
    public List<PlannedStudentUpdate> StudentsToUpdate { get; } = [];
    public List<PlannedTeacherUpdate> TeachersToUpdate { get; } = [];
    public List<PlannedDisciplineUpdate> DisciplinesToUpdate { get; } = [];
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

    /// <summary>Нагрузка из среза выгрузки, которую убрали из файла.</summary>
    public List<PlannedWorkloadDeletion> WorkloadsToDelete { get; } = [];

    /// <summary>Группы с листа «Студенты», которые будут в БД после импорта.</summary>
    public List<string> StudentGroupNames { get; } = [];

    public List<ImportIssueDto> Errors { get; } = [];
    public List<ImportIssueDto> Warnings { get; } = [];

    public bool HasErrors => Errors.Count > 0;

    public ImportReportDto ToReport()
    {
        var report = new ImportReportDto
        {
            Mode = Mode,
            CanSynchronize = CanSynchronize,
            ScopeDescription = ScopeDescription,
            Changes = [.. Changes],
            NewTeachers = TeachersToCreate.Select(t => t.Key).ToList(),
            NewDisciplines = [.. DisciplinesToCreate],
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
                Students = new() { Create = StudentsToCreate.Count, Update = StudentsToUpdate.Count, Existing = ExistingStudents - StudentsToUpdate.Count },
                Teachers = new() { Create = TeachersToCreate.Count, Update = TeachersToUpdate.Count, Existing = ExistingTeachers - TeachersToUpdate.Count },
                Disciplines = new() { Create = DisciplinesToCreate.Count, Update = DisciplinesToUpdate.Count, Existing = ExistingDisciplines - DisciplinesToUpdate.Count },
                Workloads = new() { Create = WorkloadsToCreate.Count, Existing = ExistingWorkloads, Delete = WorkloadsToDelete.Count },
            },
            WorkloadsToDelete = WorkloadsToDelete
                .Select(w => new ImportWorkloadDto(w.TeacherName, w.DisciplineName, w.GroupName))
                .ToList(),
            Errors = [.. Errors],
            Warnings = [.. Warnings],
        };
        // Bind confirmation to the exact operations, identities and visible differences.
        report.PreviewToken = Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(new
        {
            Report = report, GroupsToCreate, StudentsToCreate, StudentsToUpdate,
            TeachersToCreate, TeachersToUpdate, DisciplinesToCreate, DisciplinesToUpdate,
            WorkloadsToCreate, WorkloadsToDelete,
        })));
        return report;
    }
}
