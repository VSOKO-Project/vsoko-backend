using Application.Common.Caching;
using Application.Common.CQRS;
using Application.Common.DTOs;
using Application.Common.Exceptions;
using Application.Interfaces.CachingManager;
using Application.Interfaces.DataManager.Repositories;
using MediatR;

namespace Application.Features.ImportFeatures;

public class ApplyImportCommand : ImportFileRequest, IRequest<ImportReportDto>, ICommand;

public class ApplyImportCommandValidator : ImportFileRequestValidator<ApplyImportCommand>;

public class ApplyImportCommandHandler : IRequestHandler<ApplyImportCommand, ImportReportDto>
{
    private readonly ImportPlanner _planner;
    private readonly IImportRepository _importRepository;
    private readonly ICacheService _cacheService;

    public ApplyImportCommandHandler(
        ImportPlanner planner,
        IImportRepository importRepository,
        ICacheService cacheService
    )
    {
        _planner = planner;
        _importRepository = importRepository;
        _cacheService = cacheService;
    }

    public async Task<ImportReportDto> Handle(ApplyImportCommand request, CancellationToken cancellationToken)
    {
        var (plan, refs) = await _planner.PlanAsync(request, cancellationToken);
        var report = plan.ToReport();

        if (plan.HasErrors)
            throw new ImportValidationException(report);

        var result = await _importRepository.ApplyAsync(plan, refs, cancellationToken);

        await _cacheService.RemoveByTagAsync(CacheKeys.Workload.ListTag, cancellationToken);
        await _cacheService.RemoveByTagAsync(CacheKeys.Teacher.ListTag, cancellationToken);
        await _cacheService.RemoveByTagAsync(CacheKeys.Discipline.ListTag, cancellationToken);

        report.Period.Id = result.PeriodId;
        report.CreatedGroupIds = result.CreatedGroupIds;
        report.StudentGroupIds = result.StudentGroupIds;
        return report;
    }
}
