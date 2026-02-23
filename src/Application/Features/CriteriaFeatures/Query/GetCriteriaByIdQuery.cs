using Application.Common.DTOs;
using Application.Interfaces.CachingManager;
using Application.Interfaces.DataManager.Repositories;
using MediatR;
using Application.Common.Caching;
using Application.Common.CQRS;

namespace Application.Features.CriteriaFeatures.Query;

public record GetCriteriaByIdQuery(string Id) : IRequest<CriteriaDto>, IQuery;

public class GetCriteriaByIdQueryHandler : IRequestHandler<GetCriteriaByIdQuery, CriteriaDto>
{
    private readonly ICriteriaRepository _criteriaRepository;
    private readonly ICacheService _cacheService;

    public GetCriteriaByIdQueryHandler(ICriteriaRepository criteriaRepository, ICacheService cacheService)
    {
        _criteriaRepository = criteriaRepository;
        _cacheService = cacheService;
    }

    public async Task<CriteriaDto> Handle(GetCriteriaByIdQuery request, CancellationToken cancellationToken)
    {
        var key = CacheKeys.Criteria.GetById(request.Id);


        return (await _cacheService.GetOrCreateAsync(key,
            async (ct) => await _criteriaRepository.GetCriteriaById(request.Id, ct),
            cancellationToken: cancellationToken))!;
    }
}
