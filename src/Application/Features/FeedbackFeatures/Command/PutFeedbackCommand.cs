using Application.Common.Caching;
using Application.Common.DTOs;
using Application.Common.Interfaces;
using Application.Interfaces.CachingManager;
using Application.Interfaces.DataManager.Repositories;
using FluentValidation;
using MediatR;
using Application.Common.CQRS;

namespace Application.Features.FeedbackFeatures.Command;

public class PutFeedbackRequest : IRequest<FeedbackDto>, ICommand
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
        await _cacheService.RemoveByTagAsync(CacheKeys.Teacher.ListTag, cancellationToken);
        await _cacheService.RemoveByTagAsync(CacheKeys.Discipline.ListTag, cancellationToken);

        return await _feedbackRepository.PutFeedback(
            request.Id!,
            request.Comment,
            request.Feedback,
            cancellationToken
        );
    }
}
