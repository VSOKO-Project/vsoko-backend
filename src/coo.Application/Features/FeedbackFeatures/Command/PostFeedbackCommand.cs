using System.Data;
using coo.Application.Common.Interfaces;
using coo.Application.Common.Interfaces.DataManager;
using FluentValidation;
using MediatR;

namespace coo.Application.Features.FeedbackFeatures.Command;

public class PostFeedbackRequest : IRequest<string>
{
    public IDictionary<string, int>? Feedback { get; init; }
    public string? Comment { get; init; }
    public string? workloadId { get; init; }
}

public class PostFeedbackRequestValidator : AbstractValidator<PostFeedbackRequest>
{
    public PostFeedbackRequestValidator()
    {
        RuleFor(w => w.Feedback).NotEmpty();
        RuleFor(w => w.workloadId).NotEmpty();
    }
}

public class PostFeedbackRequestHandler : IRequestHandler<PostFeedbackRequest, string>
{
    private readonly IFeedbackRepository _feedbackRepository;
    private readonly IUserContext _userContext;

    public PostFeedbackRequestHandler(IFeedbackRepository feedbackRepository, IUserContext userContext)
    {
        _feedbackRepository = feedbackRepository;
        _userContext = userContext;
    }

    public async Task<string> Handle(PostFeedbackRequest request, CancellationToken cancellationToken)
    {
        return await _feedbackRepository.PostFeedback(request.Feedback!, request.Comment, request.workloadId!, _userContext.UserId, cancellationToken);
    }
}