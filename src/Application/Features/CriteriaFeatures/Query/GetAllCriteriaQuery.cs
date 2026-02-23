using Application.Common.DTOs;
using Application.Interfaces.DataManager.Repositories;
using MediatR;
using Application.Common.Caching;
using Application.Interfaces.CachingManager;
using Application.Common.CQRS;

namespace Application.Features.CriteriaFeatures.Query;

public class GetAllCriteriaQuery : IRequest<List<CriteriaDto>>, IQuery;

public class GetAllCriteriaQueryHandler : IRequestHandler<GetAllCriteriaQuery, List<CriteriaDto>>
{
    private readonly ICriteriaRepository _criteriaRepository;
    private readonly ICacheService _cacheService;

    public GetAllCriteriaQueryHandler(ICriteriaRepository criteriaRepository, ICacheService cacheService)
    {
        _criteriaRepository = criteriaRepository;
        _cacheService = cacheService;
    }

    public async Task<List<CriteriaDto>> Handle(
        GetAllCriteriaQuery query,
        CancellationToken cancellationToken
    )
    {
        var key = CacheKeys.Criteria.All;
        var tag = CacheKeys.Criteria.ListTag;
        return (await _cacheService.GetOrCreateAsync(
            key,
            async (ct) => await _criteriaRepository.GetAllCriteria(ct),
            [tag],
            cancellationToken))!;
    }
}
