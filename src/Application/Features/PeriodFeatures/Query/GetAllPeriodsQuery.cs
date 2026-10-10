using Application.Common.CQRS;
using Application.Common.DTOs;
using Application.Interfaces.DataManager.Repositories;
using MediatR;

namespace Application.Features.PeriodFeatures.Query;

public record GetAllPeriodsQuery : IRequest<List<PeriodListItemDto>>, IQuery;

public class GetAllPeriodsQueryHandler : IRequestHandler<GetAllPeriodsQuery, List<PeriodListItemDto>>
{
    private readonly IPeriodRepository _periodRepository;

    public GetAllPeriodsQueryHandler(IPeriodRepository periodRepository)
    {
        _periodRepository = periodRepository;
    }

    public async Task<List<PeriodListItemDto>> Handle(GetAllPeriodsQuery request, CancellationToken cancellationToken)
    {
        return await _periodRepository.GetAllAsync(cancellationToken);
    }
}
