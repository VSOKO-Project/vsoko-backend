using Application.Common.CQRS;
using Application.Interfaces.FileManager;
using MediatR;
using Microsoft.AspNetCore.DataProtection.KeyManagement.Internal;

namespace Application.Features.ReportFeatures.Query;

public class ReportQuery : IRequest<byte[]>, IQuery;

public class ReportQueryHandler : IRequestHandler<ReportQuery, byte[]>
{
    private readonly IReportService _reportService;

    public ReportQueryHandler(IReportService reportService)
    {
        _reportService = reportService;
    }

    public async Task<byte[]> Handle(ReportQuery request, CancellationToken cancellationToken)
    {
        return await _reportService.GenerateReportAsync(cancellationToken);
    }
}