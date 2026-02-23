using System.Security.Cryptography.X509Certificates;
using Application.Common.DTOs;
using Application.Common.Results;
using Application.Interfaces.CachingManager;
using Application.Interfaces.DataManager.Repositories;
using FluentValidation;
using MediatR;
using Application.Common.Caching;
using Application.Common.CQRS;

namespace Application.Features.DisciplineFeatures.Query;

public class GetDisciplineRatingRequest : IRequest<PagedResultDto<RatingDto>>, IQuery
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public string? Query { get; init; }
}

public class GetDisciplineRatingRequestValidator : AbstractValidator<GetDisciplineRatingRequest>
{
    public GetDisciplineRatingRequestValidator()
    {
        RuleFor(w => w.Page).NotNull();
        RuleFor(w => w.PageSize).NotNull();
    }
}

public class GetDisciplineRatingRequestHandler
    : IRequestHandler<GetDisciplineRatingRequest, PagedResultDto<RatingDto>>
{
    private readonly IDisciplineRepository _disciplineRepository;
    private readonly ICacheService _cacheService;

    public GetDisciplineRatingRequestHandler(IDisciplineRepository disciplineRepository, ICacheService cacheService)
    {
        _disciplineRepository = disciplineRepository;
        _cacheService = cacheService;
    }

    public async Task<PagedResultDto<RatingDto>> Handle(
        GetDisciplineRatingRequest request,
        CancellationToken cancellationToken
    )
    {
        var key = CacheKeys.Discipline.GetPaged(request.Page, request.PageSize, request.Query ?? "");
        var tag = CacheKeys.Discipline.ListTag;

        return (await _cacheService.GetOrCreateAsync(key, async (ct) => await _disciplineRepository.GetRatingAsync(request.Page, request.Query ?? "", request.PageSize, cancellationToken), [tag], cancellationToken))!;
    }
}
