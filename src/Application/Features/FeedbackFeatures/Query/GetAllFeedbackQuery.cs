using Application.Common.Sorting;
using Application.Common.Periods;
using FluentValidation;
using Application.Common.Caching;
using Application.Common.CQRS;
using Application.Common.DTOs;
using Application.Common.Interfaces;
using Application.Common.Results;
using Application.Interfaces.CachingManager;
using Application.Interfaces.DataManager.Repositories;
using MediatR;

namespace Application.Features.FeedbackFeatures.Query;

public class GetAllFeedbackQuery : IRequest<PagedResultDto<FeedbackDto>>, IQuery
{
    public FeedbackSortOrder Sort { get; init; } = FeedbackSortOrder.Newest;
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public string? DisciplineId { get; init; }
    public string? TeacherId { get; init; }
    public string? PeriodId { get; init; }
    public int? StartYear { get; init; }
    public string? WorkloadId { get; init; }
}

public class GetAllFeedbackQueryValidator : AbstractValidator<GetAllFeedbackQuery>
{
    public GetAllFeedbackQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 1000);
        RuleFor(x => x.Sort).IsInEnum();
    }
}

public class GetAllFeedbackQueryHandler : IRequestHandler<GetAllFeedbackQuery, PagedResultDto<FeedbackDto>>
{
    private readonly IFeedbackRepository _feedbackRepository;
    private readonly IUserContext _userContext;
    private readonly ICacheService _cacheService;

    public GetAllFeedbackQueryHandler(IFeedbackRepository feedbackRepository, IUserContext userContext, ICacheService cacheService)
    {
        _feedbackRepository = feedbackRepository;
        _userContext = userContext;
        _cacheService = cacheService;
    }

    public async Task<PagedResultDto<FeedbackDto>> Handle(GetAllFeedbackQuery request, CancellationToken cancellationToken)
    {
        var userId = _userContext.UserId ?? throw new UnauthorizedAccessException();
        var role = _userContext.Role;

        var key = role == "student"
        ? CacheKeys.Feedback.GetPagedForStudent(request.Page, request.PageSize, userId!, request.DisciplineId, request.TeacherId, request.WorkloadId, request.Sort)
        : CacheKeys.Feedback.GetPaged(request.Page, request.PageSize, request.DisciplineId, request.TeacherId, request.WorkloadId, request.Sort);

        var period = new PeriodFilter(request.PeriodId, request.StartYear);
        if (!period.IsEmpty) key += $":{period.CacheKey}";
        return (await _cacheService.GetOrCreateAsync(
            key,
            async (ct) => await _feedbackRepository.GetPagedFeedbacks(request.Page, request.PageSize, request.DisciplineId, request.TeacherId, request.WorkloadId, ct, request.Sort, period.IsEmpty ? null : period),
            [CacheKeys.Feedback.Tag],
            cancellationToken))!;
    }
}
