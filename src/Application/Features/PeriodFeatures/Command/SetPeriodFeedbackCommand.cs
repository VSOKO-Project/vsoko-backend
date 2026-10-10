using Application.Common.Caching;
using Application.Common.CQRS;
using Application.Common.DTOs;
using Application.Interfaces.CachingManager;
using Application.Interfaces.DataManager.Repositories;
using FluentValidation;
using MediatR;

namespace Application.Features.PeriodFeatures.Command;

public class SetPeriodFeedbackRequest : IRequest<PeriodDto>, ICommand
{
    public string? Id { get; init; }
    public bool IsOpen { get; init; }
}

public class SetPeriodFeedbackRequestValidator : AbstractValidator<SetPeriodFeedbackRequest>
{
    public SetPeriodFeedbackRequestValidator()
    {
        RuleFor(w => w.Id).NotEmpty();
    }
}

public class SetPeriodFeedbackRequestHandler : IRequestHandler<SetPeriodFeedbackRequest, PeriodDto>
{
    private readonly IPeriodRepository _periodRepository;
    private readonly ICacheService _cacheService;

    public SetPeriodFeedbackRequestHandler(IPeriodRepository periodRepository, ICacheService cacheService)
    {
        _periodRepository = periodRepository;
        _cacheService = cacheService;
    }

    public async Task<PeriodDto> Handle(SetPeriodFeedbackRequest request, CancellationToken cancellationToken)
    {
        var result = await _periodRepository.SetFeedbackOpenAsync(request.Id!, request.IsOpen, cancellationToken);

        // Студенту без periodId отдаётся нагрузка открытого периода.
        await _cacheService.RemoveByTagAsync(CacheKeys.Workload.ListTag, cancellationToken);

        return result;
    }
}
