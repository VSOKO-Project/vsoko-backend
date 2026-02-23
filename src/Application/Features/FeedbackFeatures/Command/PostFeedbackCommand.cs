using System.Data;
using Application.Common.Caching;
using Application.Common.DTOs;
using Application.Common.Interfaces;
using Application.Interfaces.CachingManager;
using Application.Interfaces.DataManager.Repositories;
using FluentValidation;
using MediatR;
using Application.Common.Exceptions;
using Application.Common.CQRS;

namespace Application.Features.FeedbackFeatures.Command;

public class PostFeedbackRequest : IRequest<FeedbackDto>, ICommand
{
    public List<Grades>? Feedback { get; init; }
    public string? Comment { get; init; }
    public string? workloadId { get; init; }
}

public class Grades
{
    public string? CriteriaId { get; init; }
    public int Grade { get; init; }
}

public class PostFeedbackRequestValidator : AbstractValidator<PostFeedbackRequest>
{
    public PostFeedbackRequestValidator()
    {
        RuleFor(w => w.Feedback).NotEmpty();
        RuleFor(w => w.workloadId).NotEmpty();
    }
}

public class PostFeedbackRequestHandler : IRequestHandler<PostFeedbackRequest, FeedbackDto>
{
    private readonly IFeedbackRepository _feedbackRepository;
    private readonly IUserContext _userContext;
    private readonly ICacheService _cacheService;

    public PostFeedbackRequestHandler(
        IFeedbackRepository feedbackRepository,
        IUserContext userContext,
        ICacheService cacheService
    )
    {
        _feedbackRepository = feedbackRepository;
        _userContext = userContext;
        _cacheService = cacheService;
    }

    public async Task<FeedbackDto> Handle(
        PostFeedbackRequest request,
        CancellationToken cancellationToken
    )
    {
        var hasFeedback = await _feedbackRepository.HasFeedbackAsync(
            request.workloadId!, 
            cancellationToken);

        if (hasFeedback)
            throw new Common.Exceptions.ValidationException("Вы уже оставляли отзыв на эту дисциплину.");

        var result = await _feedbackRepository.PostFeedback(
            request.Feedback!,
            request.Comment,
            request.workloadId!,
            _userContext.UserId,
            cancellationToken
        );

        await _cacheService.RemoveByTagAsync(CacheKeys.Feedback.ListTag(_userContext.UserId ?? throw new UnauthorizationException("Invalid token")), cancellationToken);
        await _cacheService.RemoveByTagAsync(CacheKeys.Teacher.ListTag, cancellationToken);
        await _cacheService.RemoveByTagAsync(CacheKeys.Discipline.ListTag, cancellationToken);

        return result;
    }
}
