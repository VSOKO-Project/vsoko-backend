using Application.Common.DTOs;
using Application.Common.Interfaces;
using Application.Interfaces.DataManager.Repositories;
using MediatR;

namespace Application.Features.FeedbackFeatures.Query;

public record GetAllFeedbackQuery : IRequest<List<FeedbackDto>>;

public class GetAllFeedbackQueryHandler : IRequestHandler<GetAllFeedbackQuery, List<FeedbackDto>>
{
    private readonly IFeedbackRepository _feedbackRepository;
    private readonly IUserContext _userContext;

    public GetAllFeedbackQueryHandler(IFeedbackRepository feedbackRepository, IUserContext userContext)
    {
        _feedbackRepository = feedbackRepository;
        _userContext = userContext;
    }

    public async Task<List<FeedbackDto>> Handle(GetAllFeedbackQuery request, CancellationToken cancellationToken)
    {
        return await _feedbackRepository.GetFeedbacksByStudentId(
            _userContext.UserId ?? throw new UnauthorizedAccessException(),
            cancellationToken
        );
    }
}
