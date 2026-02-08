using coo.Application.Common.DTOs;
using coo.Application.Common.Interfaces.DataManager;
using MediatR;

namespace coo.Application.Features.CriteriaFeatures.Query;

public class GetAllCriteriaQuery : IRequest<List<CriteriaDto>>
{ }

public class GetAllCriteriaQueryHandler : IRequestHandler<GetAllCriteriaQuery, List<CriteriaDto>>
{
    private readonly ICriteriaRepository _criteriaRepository;

    public GetAllCriteriaQueryHandler(ICriteriaRepository criteriaRepository)
    {
        _criteriaRepository = criteriaRepository;
    }

    public async Task<List<CriteriaDto>> Handle(GetAllCriteriaQuery query, CancellationToken cancellationToken)
    {
        return await _criteriaRepository.GetAllCriteria(cancellationToken);
    }
}