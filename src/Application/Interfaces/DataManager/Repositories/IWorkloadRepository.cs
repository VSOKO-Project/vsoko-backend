using Domain.Entities;
using Application.Common.Results;
using Application.Common.DTOs;

namespace Application.Interfaces.DataManager.Repositories;

public interface IWorkloadRepository : IRepository<Workload>
{
    public Task<PagedResultDto<WorkloadDto>> GetPagedWorkload(int page, string query, int pageSize, CancellationToken cancellationToken);
}