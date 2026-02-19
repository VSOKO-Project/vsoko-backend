using Application.Common.DTOs;
using Application.Common.Interfaces;
using Application.Interfaces.DataManager.Repositories;
using MediatR;

namespace Application.Features.FeedbackFeatures.Query;

public record GetFeedbackByIdQuery(string Id) : IRequest<FeedbackDto>;

public class GetFeedbackByIdQueryHandler : IRequestHandler<GetFeedbackByIdQuery, FeedbackDto>
{
    private readonly IFeedbackRepository _feedbackRepository;
    private readonly IUserContext _userContext;

    public GetFeedbackByIdQueryHandler(IFeedbackRepository feedbackRepository, IUserContext userContext)
    {
        _feedbackRepository = feedbackRepository;
        _userContext = userContext;
    }

    public async Task<FeedbackDto> Handle(GetFeedbackByIdQuery request, CancellationToken cancellationToken)
    {
        return await _feedbackRepository.GetFeedbackById(
            request.Id,
            _userContext.UserId ?? throw new UnauthorizedAccessException(),
            cancellationToken
        );
    }
}
