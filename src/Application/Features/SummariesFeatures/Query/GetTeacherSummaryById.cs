using Application.Common.CQRS;
using Application.Common.DTOs;
using Application.Common.Periods;
using Application.Interfaces.AIManager;
using MediatR;

namespace Application.Features.SummariesFeatures.Query;

public record GetTeacherSummaryByIdQuery(string Id, string? PeriodId = null, int? StartYear = null) : IRequest<FeedbackReportDto>, IQuery;

// Coverage changes as the background worker progresses: do not cache the report.
public class GetTeacherSummaryByIdQueryHandler(IFeedbackAnalysisService analysis) : IRequestHandler<GetTeacherSummaryByIdQuery, FeedbackReportDto>
{
    public Task<FeedbackReportDto> Handle(GetTeacherSummaryByIdQuery request, CancellationToken ct) =>
        analysis.GetReportAsync(request.Id, true, new PeriodFilter(request.PeriodId, request.StartYear), ct);
}
