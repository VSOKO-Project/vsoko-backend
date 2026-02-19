using Application.Common.Interfaces;
using Application.Interfaces.DataManager.Repositories;
using FluentValidation;
using MediatR;

namespace Application.Features.FeedbackFeatures.Command;

public class DeleteFeedbackRequest : IRequest<Unit>
{
    public string? Id { get; init; }
}

public class DeleteFeedbackRequestValidator : AbstractValidator<DeleteFeedbackRequest>
{
    public DeleteFeedbackRequestValidator()
    {
        RuleFor(w => w.Id).NotEmpty();
    }
}

public class DeleteFeedbackRequestHandler : IRequestHandler<DeleteFeedbackRequest, Unit>
{
    private readonly IFeedbackRepository _feedbackRepository;
    private readonly IUserContext _userContext;

    public DeleteFeedbackRequestHandler(IFeedbackRepository feedbackRepository, IUserContext userContext)
    {
        _feedbackRepository = feedbackRepository;
        _userContext = userContext;
    }

    public async Task<Unit> Handle(DeleteFeedbackRequest request, CancellationToken cancellationToken)
    {
        await _feedbackRepository.DeleteFeedback(
            request.Id!,
            _userContext.UserId ?? throw new UnauthorizedAccessException(),
            cancellationToken
        );
        return Unit.Value;
    }
}
