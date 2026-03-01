using Application.Common.Caching;
using Application.Common.CQRS;
using Application.Common.DTOs;
using Application.Common.Interfaces;
using Application.Common.Results;
using Application.Interfaces.CachingManager;
using Application.Interfaces.DataManager.Repositories;
using MediatR;

namespace Application.Features.FeedbackFeatures.Query;

public class GetAllFeedbackQuery : IRequest<PagedResultDto<FeedbackDto>>, IQuery
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public string? DisciplineId { get; init; }
    public string? TeacherId { get; init; }
    public string? WorkloadId { get; init; }
}

public class GetAllFeedbackQueryHandler : IRequestHandler<GetAllFeedbackQuery, PagedResultDto<FeedbackDto>>
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

    public async Task<PagedResultDto<FeedbackDto>> Handle(GetAllFeedbackQuery request, CancellationToken cancellationToken)
    {
        var userId = _userContext.UserId ?? throw new UnauthorizedAccessException();
        var role = _userContext.Role;

        var key = role == "student" 
        ? CacheKeys.Feedback.GetPagedForStudent(request.Page, request.PageSize, userId!, request.DisciplineId, request.TeacherId, request.WorkloadId)
        : CacheKeys.Feedback.GetPaged(request.Page, request.PageSize, request.DisciplineId, request.TeacherId, request.WorkloadId);

        var tag = CacheKeys.Feedback.ListTag(userId);

        return (await _cacheService.GetOrCreateAsync(
            key,
            async (ct) => await _feedbackRepository.GetPagedFeedbacks(request.Page, request.PageSize, request.DisciplineId, request.TeacherId, request.WorkloadId, ct),
            [tag],
            cancellationToken))!;
    }
}
