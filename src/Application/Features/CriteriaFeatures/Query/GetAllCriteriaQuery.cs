using Application.Common.DTOs;
using Application.Interfaces.DataManager.Repositories;
using MediatR;

namespace Application.Features.CriteriaFeatures.Query;

public class GetAllCriteriaQuery : IRequest<List<CriteriaDto>> { }

public class GetAllCriteriaQueryHandler : IRequestHandler<GetAllCriteriaQuery, List<CriteriaDto>>
{
    private readonly ICriteriaRepository _criteriaRepository;

    public GetAllCriteriaQueryHandler(ICriteriaRepository criteriaRepository)
    {
        _criteriaRepository = criteriaRepository;
    }

    public async Task<List<CriteriaDto>> Handle(
        GetAllCriteriaQuery query,
        CancellationToken cancellationToken
    )
    {
        return await _criteriaRepository.GetAllCriteria(cancellationToken);
    }
}
