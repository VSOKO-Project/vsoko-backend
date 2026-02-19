using Application.Common.DTOs;
using Application.Interfaces.DataManager.Repositories;
using MediatR;

namespace Application.Features.CriteriaFeatures.Query;

public record GetCriteriaByIdQuery(string Id) : IRequest<CriteriaDto>;

public class GetCriteriaByIdQueryHandler : IRequestHandler<GetCriteriaByIdQuery, CriteriaDto>
{
    private readonly ICriteriaRepository _criteriaRepository;

    public GetCriteriaByIdQueryHandler(ICriteriaRepository criteriaRepository)
    {
        _criteriaRepository = criteriaRepository;
    }

    public async Task<CriteriaDto> Handle(GetCriteriaByIdQuery request, CancellationToken cancellationToken)
    {
        return await _criteriaRepository.GetCriteriaById(request.Id, cancellationToken);
    }
}
