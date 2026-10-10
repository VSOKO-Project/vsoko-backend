using Application.Common.DTOs;
using Application.Common.Mappings;
using Application.Common.Extension;
using Application.Common.Results;
using Application.Common.Exceptions;
using Application.Interfaces.DataManager.Repositories;
using Infrastructure.DataManager.Contexts;
using Microsoft.EntityFrameworkCore;
using Domain.Entities;
using Application.Common.Specification.WorkloadSpecification;

namespace Infrastructure.DataManager.Repositories;

public class WorkloadRepository : IWorkloadRepository
{
    private readonly AppDbContext _dbContext;
    private readonly IWorkloadAccessService _accessService;
    private readonly WorkloadMapper _mapper;

    public WorkloadRepository(AppDbContext dbContext, IWorkloadAccessService accessService, WorkloadMapper mapper)
    {
        _dbContext = dbContext;
        _accessService = accessService;
        _mapper = mapper;
    }

    public async Task<PagedResultDto<WorkloadDto>> GetPagedWorkload(
        int page,
        string query,
        int pageSize,
        string? periodId,
        bool openPeriodOnly,
        CancellationToken cancellationToken
    )
    {
        var spec = _accessService.GetSpecification();

        var qury = spec.Apply(
            _dbContext.Workloads.Include(w => w.TeacherRef).Include(w => w.DisciplineRef)
        );

        if (periodId is not null)
            qury = qury.Where(w => w.PeriodId == periodId);
        else if (openPeriodOnly)
            qury = qury.Where(w => w.PeriodRef!.IsFeedbackOpen);

        var baseQuery = qury.WhereNameOrTeacherContains(query);

        var totalCount = await baseQuery.CountAsync(cancellationToken);

        var items = await _mapper.WorkloadToDto(baseQuery)
            .OrderBy(x => x.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResultDto<WorkloadDto>
        {
            Items = items,
            TotalPages = (int)Math.Ceiling(totalCount / (decimal)pageSize),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
        };
    }

    public async Task<WorkloadDto> GetWorkloadById(string id, CancellationToken cancellationToken)
    {
        var spec = _accessService.GetSpecification();

        var query = spec.Apply(
            _dbContext.Workloads.Include(w => w.TeacherRef).Include(w => w.DisciplineRef).Include(w => w.GroupRef)
        );

        var workload = await _mapper.WorkloadToDto(query)
            .FirstOrDefaultAsync(w => w.Id == id, cancellationToken);

        if (workload is null)
            throw new NotFoundException(nameof(Workload), id);

        return workload;
    }

    public async Task<bool> IsFeedbackPeriodOpenAsync(string workloadId, CancellationToken cancellationToken)
    {
        var spec = _accessService.GetSpecification();

        var isOpen = await spec.Apply(_dbContext.Workloads)
            .Where(w => w.Id == workloadId)
            .Select(w => (bool?)w.PeriodRef!.IsFeedbackOpen)
            .FirstOrDefaultAsync(cancellationToken);

        return isOpen ?? throw new NotFoundException(nameof(Workload), workloadId);
    }
}
