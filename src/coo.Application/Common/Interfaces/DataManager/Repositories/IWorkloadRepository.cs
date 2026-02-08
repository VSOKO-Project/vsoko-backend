using coo.Domain.Entities;
using coo.Application.Common.Interfaces.DataManager.Repositories;
using coo.Application.Common.Results;
using coo.Application.Common.DTOs;

namespace coo.Application.Common.Interfaces.DataManager;

public interface IWorkloadRepository : IRepository<Workload>
{
    public Task<PagedResultDto<WorkloadDto>> GetPagedWorkload(int page, string query, int pageSize, CancellationToken cancellationToken);
}