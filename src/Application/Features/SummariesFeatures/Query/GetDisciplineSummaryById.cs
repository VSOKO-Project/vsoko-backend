using Application.Common.CQRS;
using Application.Common.DTOs;
using Application.Common.Periods;
using Application.Interfaces.AIManager;
using MediatR;

namespace Application.Features.SummariesFeatures.Query;

public record GetDisciplineSummaryByIdQuery(string Id, string? PeriodId = null, int? StartYear = null) : IRequest<FeedbackReportDto>, IQuery;

// Coverage changes as the background worker progresses: do not cache the report.
public class GetDisciplineSummaryByIdQueryHandler(IFeedbackAnalysisService analysis) : IRequestHandler<GetDisciplineSummaryByIdQuery, FeedbackReportDto>
{
    public Task<FeedbackReportDto> Handle(GetDisciplineSummaryByIdQuery request, CancellationToken ct) =>
        analysis.GetReportAsync(request.Id, false, new PeriodFilter(request.PeriodId, request.StartYear), ct);
}
