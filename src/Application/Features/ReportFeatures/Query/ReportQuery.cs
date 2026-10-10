using Application.Common.CQRS;
using Application.Common.Periods;
using Application.Interfaces.FileManager;
using MediatR;

namespace Application.Features.ReportFeatures.Query;

public record ReportQuery(string? PeriodId = null, int? StartYear = null) : IRequest<byte[]>, IQuery;

public class ReportQueryHandler : IRequestHandler<ReportQuery, byte[]>
{
    private readonly IReportService _reportService;

    public ReportQueryHandler(IReportService reportService)
    {
        _reportService = reportService;
    }

    public async Task<byte[]> Handle(ReportQuery request, CancellationToken cancellationToken)
    {
        return await _reportService.GenerateReportAsync(new PeriodFilter(request.PeriodId, request.StartYear), cancellationToken);
    }
}