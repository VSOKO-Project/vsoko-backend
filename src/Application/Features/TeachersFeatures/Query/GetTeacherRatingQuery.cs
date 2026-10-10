using System.ComponentModel;
using System.Data;
using Application.Common.Caching;
using Application.Common.CQRS;
using Application.Common.DTOs;
using Application.Common.Periods;
using Application.Common.Results;
using Application.Interfaces.CachingManager;
using Application.Interfaces.DataManager.Repositories;
using FluentValidation;
using MediatR;

namespace Application.Features.TeachersFeatures.Query;

public class GetTeachersRatingRequest : IRequest<PagedResultDto<RatingDto>>, IQuery
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public string? Query { get; init; }
    public string? PeriodId { get; init; }
    public int? StartYear { get; init; }
}

public class GetTeachersRatingRequestValidator : AbstractValidator<GetTeachersRatingRequest>
{
    public GetTeachersRatingRequestValidator()
    {
        RuleFor(w => w.Page).NotNull();
        RuleFor(W => W.PageSize).NotNull();
    }
}

public class GetTeachersRatingRequestHandler
    : IRequestHandler<GetTeachersRatingRequest, PagedResultDto<RatingDto>>
{
    private readonly ITeacherRepository _teacherRepository;
    private readonly ICacheService _cacheService;

    public GetTeachersRatingRequestHandler(ITeacherRepository teacherRepository, ICacheService cacheService)
    {
        _teacherRepository = teacherRepository;
        _cacheService = cacheService;
    }

    public async Task<PagedResultDto<RatingDto>> Handle(
        GetTeachersRatingRequest request,
        CancellationToken cancellationToken
    )
    {
        var period = new PeriodFilter(request.PeriodId, request.StartYear);
        var key = CacheKeys.Teacher.GetRating(request.Page, request.PageSize, request.Query ?? "", period);
        var tag = CacheKeys.Teacher.ListTag;

        return (await _cacheService.GetOrCreateAsync(key, async (ct) => await _teacherRepository.GetRatingAsync(request.Page, request.Query ?? "", request.PageSize, period, ct), [tag], cancellationToken))!;
    }
}
