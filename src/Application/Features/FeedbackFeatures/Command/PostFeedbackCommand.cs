using System.Data;
using Application.Common.DTOs;
using Application.Common.Interfaces;
using Application.Interfaces.DataManager.Repositories;
using FluentValidation;
using MediatR;
using Application.Common.Exceptions;

namespace Application.Features.FeedbackFeatures.Command;

public class PostFeedbackRequest : IRequest<FeedbackDto>
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

    public PostFeedbackRequestHandler(
        IFeedbackRepository feedbackRepository,
        IUserContext userContext
    )
    {
        _feedbackRepository = feedbackRepository;
        _userContext = userContext;
    }

    public async Task<FeedbackDto> Handle(
        PostFeedbackRequest request,
        CancellationToken cancellationToken
    )
    {
        var hasFeedback = await _feedbackRepository.HasFeedbackAsync(
            _userContext.UserId, 
            request.workloadId!, 
            cancellationToken);

        if (hasFeedback)
        {
            throw new Application.Common.Exceptions.ValidationException("Вы уже оставляли отзыв на эту дисциплину.");
        }

        return await _feedbackRepository.PostFeedback(
            request.Feedback!,
            request.Comment,
            request.workloadId!,
            _userContext.UserId,
            cancellationToken
        );
    }
}
