using coo.Application.Common.DTOs;
using coo.Application.Common.Interfaces.DataManager;
using coo.Application.Common.Results;
using coo.Domain.Entities;
using coo.Domain.Enums;
using coo.Infrastructure.DataManager.Contexts;
using Microsoft.EntityFrameworkCore;
using static coo.Domain.Entities.Workload;
using coo.Application.Common.Extension;

namespace coo.Infrastructure.DataManager.Repositories;

public class WorkloadRepository : IWorkloadRepository
{
    private readonly AppDbContext _dbContext;
    private readonly IWorkloadAccessService _accessService;
    public WorkloadRepository(AppDbContext dbContext, IWorkloadAccessService accessService)
    {
        _dbContext = dbContext;
        _accessService = accessService;
    }
    public async Task<PagedResultDto<WorkloadDto>> GetPagedWorkload(int page, string query, int pageSize, CancellationToken cancellationToken)
    {
        var spec = _accessService.GetSpecification();

        var qury = spec.Apply(_dbContext.Workloads
        .Include(w => w.TeacherRef)
        .Include(w => w.DisciplineRef));
        
        var baseQuery = qury
        .WhereNameOrTeacherContains(query);

        var totalCount = await baseQuery.CountAsync(cancellationToken);

        var items = await baseQuery
            .OrderBy(x => x.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new WorkloadDto
            {
                Id = t.Id,
                Name = t.DisciplineRef.Name,
                Teacher = (t.TeacherRef.Surname + " " + t.TeacherRef.Name + " " + t.TeacherRef.Patronymic).Trim()
            }).ToListAsync(cancellationToken);

        return new PagedResultDto<WorkloadDto>
        {
            Items = items,
            TotalPages = (int)Math.Ceiling(totalCount / (decimal)pageSize),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }
}