using Application.Common.Caching;
using Application.Common.CQRS;
using Application.Interfaces.AIManager;
using Application.Interfaces.CachingManager;
using Application.Interfaces.DataManager.Repositories;
using MediatR;

namespace Application.Features.SummariesFeatures.Query;

public record GetTeacherSummaryByIdQuery(string Id) : IRequest<string>, IQuery;

public class GetTeacherSummaryByIdQueryHandler : IRequestHandler<GetTeacherSummaryByIdQuery, string>
{
    private readonly ITeacherRepository _teacherRepository;
    private readonly IFeedbackRepository _feedbackRepository;
    private readonly IFeedbackSummarizer _feedbackSummarizer;
    private readonly ICacheService _cacheService;

    public GetTeacherSummaryByIdQueryHandler(
        ITeacherRepository teacherRepository,
        IFeedbackRepository feedbackRepository,
        IFeedbackSummarizer feedbackSummarizer,
        ICacheService cacheService)
    {
        _teacherRepository = teacherRepository;
        _feedbackRepository = feedbackRepository;
        _feedbackSummarizer = feedbackSummarizer;
        _cacheService = cacheService;
    }

    public async Task<string> Handle(GetTeacherSummaryByIdQuery request, CancellationToken cancellationToken)
    {
        var key = CacheKeys.Teacher.GetSummary(request.Id);
        var tag = CacheKeys.Teacher.ListTag;

        return (await _cacheService.GetOrCreateAsync(
            key,
            async (ct) =>
            {
                var teacher = await _teacherRepository.GetByIdAsync(request.Id, ct);
                var comments = await _feedbackRepository.GetCommentByTeacherId(request.Id, ct);
                return await _feedbackSummarizer.SummarizeTeacherAsync(teacher.FullName, comments, ct);
            },
            [tag],
            cancellationToken))!;
    }
}
