using Application.Common.CQRS;
using Application.Common.DTOs;
using MediatR;

namespace Application.Features.ImportFeatures;

public class PreviewImportQuery : ImportFileRequest, IRequest<ImportReportDto>, IQuery;

public class PreviewImportQueryValidator : ImportFileRequestValidator<PreviewImportQuery>;

public class PreviewImportQueryHandler : IRequestHandler<PreviewImportQuery, ImportReportDto>
{
    private readonly ImportPlanner _planner;

    public PreviewImportQueryHandler(ImportPlanner planner)
    {
        _planner = planner;
    }

    public async Task<ImportReportDto> Handle(PreviewImportQuery request, CancellationToken cancellationToken)
    {
        var (plan, _) = await _planner.PlanAsync(request, cancellationToken);
        return plan.ToReport();
    }
}
