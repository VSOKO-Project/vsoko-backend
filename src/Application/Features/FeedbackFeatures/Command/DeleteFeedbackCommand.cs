using Application.Common.Caching;
using Application.Common.Interfaces;
using Application.Interfaces.CachingManager;
using Application.Interfaces.DataManager.Repositories;
using FluentValidation;
using MediatR;
using Application.Common.CQRS;

namespace Application.Features.FeedbackFeatures.Command;

public class DeleteFeedbackRequest : IRequest<Unit>, ICommand
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
    private readonly ICacheService _cacheService;

    public DeleteFeedbackRequestHandler(IFeedbackRepository feedbackRepository, IUserContext userContext, ICacheService cacheService)
    {
        _feedbackRepository = feedbackRepository;
        _userContext = userContext;
        _cacheService = cacheService;
    }

    public async Task<Unit> Handle(DeleteFeedbackRequest request, CancellationToken cancellationToken)
    {
        _ = _userContext.UserId ?? throw new UnauthorizedAccessException();

        await _cacheService.RemoveByTagAsync(CacheKeys.Feedback.Tag, cancellationToken);
        await _cacheService.RemoveByTagAsync(CacheKeys.Teacher.ListTag, cancellationToken);
        await _cacheService.RemoveByTagAsync(CacheKeys.Discipline.ListTag, cancellationToken);
        await _cacheService.RemoveByTagAsync(CacheKeys.Workload.ListTag, cancellationToken);

        await _feedbackRepository.DeleteFeedback(
            request.Id!,
            cancellationToken
        );
        return Unit.Value;
    }
}
