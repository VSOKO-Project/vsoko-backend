using Application.Common.CQRS;
using Application.Common.DTOs;
using Application.Common.Periods;
using Application.Interfaces.DataManager.Repositories;
using MediatR;

namespace Application.Features.PeriodFeatures.Query;

public record GetSuggestedPeriodsQuery : IRequest<List<SuggestedPeriodDto>>, IQuery;

public class GetSuggestedPeriodsQueryHandler : IRequestHandler<GetSuggestedPeriodsQuery, List<SuggestedPeriodDto>>
{
    private readonly IPeriodRepository _periodRepository;
    private readonly TimeProvider _timeProvider;

    public GetSuggestedPeriodsQueryHandler(IPeriodRepository periodRepository, TimeProvider timeProvider)
    {
        _periodRepository = periodRepository;
        _timeProvider = timeProvider;
    }

    public async Task<List<SuggestedPeriodDto>> Handle(GetSuggestedPeriodsQuery request, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(_timeProvider.GetLocalNow().DateTime);
        var current = PeriodCalculator.Current(today);
        var existing = await _periodRepository.GetAllAsync(cancellationToken);

        return new[] { PeriodCalculator.Previous(current), current, PeriodCalculator.Next(current) }
            .Select(key =>
            {
                var period = existing.FirstOrDefault(p => p.StartYear == key.StartYear && p.Term == key.Term);
                return new SuggestedPeriodDto
                {
                    Id = period?.Id,
                    StartYear = key.StartYear,
                    Term = key.Term,
                    Exists = period is not null,
                    IsCurrent = key == current,
                };
            })
            .ToList();
    }
}
