using Application.Common.Caching;
using Application.Common.CQRS;
using Application.Common.DTOs;
using Application.Common.Results;
using Application.Interfaces.CachingManager;
using Application.Interfaces.DataManager.Repositories;
using FluentValidation;
using MediatR;

namespace Application.Features.DisciplineFeatures.Query;

public class GetAllDisciplinesRequest : IRequest<PagedResultDto<DisciplineDto>>, IQuery
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public string? Query { get; init; }
}

public class GetAllDisciplinesRequestValidator : AbstractValidator<GetAllDisciplinesRequest>
{
    public GetAllDisciplinesRequestValidator()
    {
        RuleFor(w => w.Page).NotNull();
        RuleFor(w => w.PageSize).NotNull();
    }
}

public class GetAllDisciplinesRequestHandler
    : IRequestHandler<GetAllDisciplinesRequest, PagedResultDto<DisciplineDto>>
{
    private readonly IDisciplineRepository _disciplineRepository;
    private readonly ICacheService _cacheService;

    public GetAllDisciplinesRequestHandler(IDisciplineRepository disciplineRepository, ICacheService cacheService)
    {
        _disciplineRepository = disciplineRepository;
        _cacheService = cacheService;
    }

    public async Task<PagedResultDto<DisciplineDto>> Handle(
        GetAllDisciplinesRequest request,
        CancellationToken cancellationToken
    )
    {
        var key = CacheKeys.Discipline.GetPaged(request.Page, request.PageSize, request.Query ?? "");
        var tag = CacheKeys.Discipline.ListTag;

        return (await _cacheService.GetOrCreateAsync(key, async (ct) => await _disciplineRepository.GetAllAsync(request.Page, request.Query ?? "", request.PageSize, cancellationToken), [tag], cancellationToken))!;
    }
}
