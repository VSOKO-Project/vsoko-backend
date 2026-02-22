using Application.Common.Caching;
using Application.Common.DTOs;
using Application.Common.Interfaces;
using Application.Interfaces.CachingManager;
using Application.Interfaces.DataManager.Repositories;
using MediatR;

namespace Application.Features.FeedbackFeatures.Query;

public record GetAllFeedbackQuery : IRequest<List<FeedbackDto>>;

public class GetAllFeedbackQueryHandler : IRequestHandler<GetAllFeedbackQuery, List<FeedbackDto>>
{
    private readonly IFeedbackRepository _feedbackRepository;
    private readonly IUserContext _userContext;
    private readonly ICacheService _cacheService;

    public GetAllFeedbackQueryHandler(IFeedbackRepository feedbackRepository, IUserContext userContext, ICacheService cacheService)
    {
        _feedbackRepository = feedbackRepository;
        _userContext = userContext;
        _cacheService = cacheService;
    }

    public async Task<List<FeedbackDto>> Handle(GetAllFeedbackQuery request, CancellationToken cancellationToken)
    {
        var userId = _userContext.UserId ?? throw new UnauthorizedAccessException();
        var key = CacheKeys.Feedback.GetByStudent(userId);
        var tag = CacheKeys.Feedback.ListTag(userId);

        return (await _cacheService.GetOrCreateAsync(
            key,
            async (ct) => await _feedbackRepository.GetFeedbacksByStudentId(userId, ct),
            [tag],
            cancellationToken))!;
    }
}
