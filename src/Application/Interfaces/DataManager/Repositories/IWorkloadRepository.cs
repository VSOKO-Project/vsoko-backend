using Application.Common.DTOs;
using Application.Common.Results;
using Domain.Entities;

namespace Application.Interfaces.DataManager.Repositories;

public interface IWorkloadRepository : IRepository<Workload>
{
    public Task<PagedResultDto<WorkloadDto>> GetPagedWorkload(
        int page,
        string query,
        int pageSize,
        string? periodId,
        bool openPeriodOnly,
        CancellationToken cancellationToken
    );

    public Task<WorkloadDto> GetWorkloadById(string id, CancellationToken cancellationToken);

    /// <summary>
    /// Открыт ли сбор отзывов в периоде нагрузки.
    /// </summary>
    public Task<bool> IsFeedbackPeriodOpenAsync(string workloadId, CancellationToken cancellationToken);
}
