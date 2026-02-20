using Application.Common.DTOs;
using Application.Common.Interfaces;
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

    public PutFeedbackRequestHandler(IFeedbackRepository feedbackRepository, IUserContext userContext)
    {
        _feedbackRepository = feedbackRepository;
        _userContext = userContext;
    }

    public async Task<FeedbackDto> Handle(PutFeedbackRequest request, CancellationToken cancellationToken)
    {
        return await _feedbackRepository.PutFeedback(
            request.Id!,
            _userContext.UserId ?? throw new UnauthorizedAccessException(),
            request.Comment,
            request.Feedback,
            cancellationToken
        );
    }
}
