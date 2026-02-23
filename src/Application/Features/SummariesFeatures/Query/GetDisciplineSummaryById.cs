using Application.Common.Caching;
using Application.Common.CQRS;
using Application.Interfaces.AIManager;
using Application.Interfaces.CachingManager;
using Application.Interfaces.DataManager.Repositories;
using MediatR;

namespace Application.Features.SummariesFeatures.Query;

public record GetDisciplineSummaryByIdQuery(string Id) : IRequest<string>, IQuery;

public class GetDisciplineSummaryByIdQueryHandler : IRequestHandler<GetDisciplineSummaryByIdQuery, string>
{
    private readonly IDisciplineRepository _disciplineRepository;
    private readonly IFeedbackRepository _feedbackRepository;
    private readonly IFeedbackSummarizer _feedbackSummarizer;
    private readonly ICacheService _cacheService;

    public GetDisciplineSummaryByIdQueryHandler(
        IDisciplineRepository disciplineRepository,
        IFeedbackRepository feedbackRepository,
        IFeedbackSummarizer feedbackSummarizer,
        ICacheService cacheService)
    {
        _disciplineRepository = disciplineRepository;
        _feedbackRepository = feedbackRepository;
        _feedbackSummarizer = feedbackSummarizer;
        _cacheService = cacheService;
    }

    public async Task<string> Handle(GetDisciplineSummaryByIdQuery request, CancellationToken cancellationToken)
    {
        var key = CacheKeys.Discipline.GetSummary(request.Id);
        var tag = CacheKeys.Discipline.ListTag;

        return (await _cacheService.GetOrCreateAsync(
            key,
            async (ct) =>
            {
                var discipline = await _disciplineRepository.GetByIdAsync(request.Id, ct);
                var comments = await _feedbackRepository.GetCommentByDisciplineId(request.Id, ct);
                return await _feedbackSummarizer.SummarizeDisciplineAsync(discipline.Name, comments, ct);
            },
            [tag],
            cancellationToken))!;
    }
}
