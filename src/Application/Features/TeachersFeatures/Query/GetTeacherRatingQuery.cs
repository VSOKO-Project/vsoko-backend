using System.ComponentModel;
using System.Data;
using Application.Common.Caching;
using Application.Common.CQRS;
using Application.Common.DTOs;
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
        var key = CacheKeys.Teacher.GetPaged(request.Page, request.PageSize, request.Query ?? "");
        var tag = CacheKeys.Teacher.ListTag;

        return (await _cacheService.GetOrCreateAsync(key, async (ct) => await _teacherRepository.GetRatingAsync(request.Page, request.Query ?? "", request.PageSize, cancellationToken), [tag], cancellationToken))!;
    }
}
