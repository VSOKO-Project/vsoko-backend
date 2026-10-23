using Application.Common.DTOs;
using Application.Common.Periods;
using Application.Features.ImportFeatures;

namespace Application.Interfaces.DataManager.Repositories;

public record ImportApplyResult(string PeriodId, List<string> CreatedGroupIds, List<string> StudentGroupIds);

public record ExportWorkloadRow(string TeacherFio, string Discipline, string Group);

public record ExportStudentRow(string Group, string Surname, string Name, string? Patronymic, string StudentNumber);

public record PeriodExport(List<ExportWorkloadRow> Workloads, List<ExportStudentRow> Students);

public interface IImportRepository
{
    /// <summary>Загружает справочники для плана одним набором запросов.</summary>
    Task<ImportReferenceData> LoadReferenceDataAsync(PeriodKey period, CancellationToken cancellationToken);

    /// <summary>
    /// Применяет план: период → группы → студенты → преподаватели → дисциплины → нагрузка.
    /// Транзакцию открывает TransactionBehavior.
    /// </summary>
    Task<ImportApplyResult> ApplyAsync(ImportPlan plan, ImportReferenceData refs, CancellationToken cancellationToken);

    /// <summary>Студенты групп для выгрузки логинов. Без groupIds — все группы.</summary>
    Task<List<StudentCredentialDto>> GetStudentCredentialsAsync(
        IReadOnlyCollection<string>? groupIds,
        CancellationToken cancellationToken
    );

    Task<List<StudentGroupDto>> GetGroupsAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Нагрузка семестра и студенты её групп для выгрузки. Без teacherIds — вся нагрузка.
    /// Если семестра нет, возвращает null.
    /// </summary>
    Task<PeriodExport?> GetPeriodExportAsync(
        PeriodKey period,
        IReadOnlyCollection<string>? teacherIds,
        CancellationToken cancellationToken
    );

    /// <summary>Преподаватели с нагрузкой в семестре, по ФИО.</summary>
    Task<List<TeacherDto>> GetPeriodTeachersAsync(PeriodKey period, CancellationToken cancellationToken);
}
