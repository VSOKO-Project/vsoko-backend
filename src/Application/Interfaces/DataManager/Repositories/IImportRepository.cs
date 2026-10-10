using Application.Common.DTOs;
using Application.Common.Periods;
using Application.Features.ImportFeatures;

namespace Application.Interfaces.DataManager.Repositories;

public record ImportApplyResult(string PeriodId, List<string> CreatedGroupIds, List<string> StudentGroupIds);

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
}
