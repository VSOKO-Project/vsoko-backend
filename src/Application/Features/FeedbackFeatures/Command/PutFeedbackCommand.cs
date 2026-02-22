using Application.Common.Caching;
using Application.Common.DTOs;
using Application.Common.Interfaces;
using Application.Interfaces.CachingManager;
using Application.Interfaces.DataManager.Repositories;
using FluentValidation;
using MediatR;

namespace Application.Features.FeedbackFeatures.Command;

public class PutFeedbackRequest : IRequest<FeedbackDto>
{
    public string? Id { get; init; }
    public string? Comment { get; init; }
    public List<Grades>? Feedback { get; init; }
}

public class PutFeedbackRequestValidator : AbstractValidator<PutFeedbackRequest>
{
    public PutFeedbackRequestValidator()
    {
        RuleFor(w => w.Id).NotEmpty();
    }
}

public class PutFeedbackRequestHandler : IRequestHandler<PutFeedbackRequest, FeedbackDto>
{
    private readonly IFeedbackRepository _feedbackRepository;
    private readonly IUserContext _userContext;
    private readonly ICacheService _cacheService;

    public PutFeedbackRequestHandler(IFeedbackRepository feedbackRepository, IUserContext userContext, ICacheService cacheService)
    {
        _feedbackRepository = feedbackRepository;
        _userContext = userContext;
        _cacheService = cacheService;
    }

    public async Task<FeedbackDto> Handle(PutFeedbackRequest request, CancellationToken cancellationToken)
    {
        var userId = _userContext.UserId ?? throw new UnauthorizedAccessException();
        var key = CacheKeys.Feedback.GetById(request.Id!, userId);

        await _cacheService.RemoveByTagAsync(CacheKeys.Feedback.ListTag(userId), cancellationToken);
        await _cacheService.RemoveAsync(key);

        return await _feedbackRepository.PutFeedback(
            request.Id!,
            _userContext.UserId ?? throw new UnauthorizedAccessException(),
            request.Comment,
            request.Feedback,
            cancellationToken
        );
    }
}
