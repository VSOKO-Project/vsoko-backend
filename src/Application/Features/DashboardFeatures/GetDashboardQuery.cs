using Application.Common.CQRS;
using Application.Common.DTOs;
using Application.Common.Periods;
using Application.Interfaces.DataManager.Repositories;
using FluentValidation;
using MediatR;

namespace Application.Features.DashboardFeatures;

public record GetDashboardQuery(string? PeriodId = null, int? StartYear = null) : IRequest<DashboardDto>, IQuery;

public class GetDashboardQueryValidator : AbstractValidator<GetDashboardQuery>
{
    public GetDashboardQueryValidator()
    {
        RuleFor(q => q.StartYear).InclusiveBetween(1900, 9998).When(q => q.StartYear.HasValue);
        RuleFor(q => q).Must(q => string.IsNullOrWhiteSpace(q.PeriodId) || q.StartYear is null)
            .WithMessage("Выберите семестр или учебный год");
    }
}

public class GetDashboardQueryHandler(IDashboardRepository repository) : IRequestHandler<GetDashboardQuery, DashboardDto>
{
    public Task<DashboardDto> Handle(GetDashboardQuery request, CancellationToken cancellationToken) =>
        repository.GetAsync(new PeriodFilter(request.PeriodId, request.StartYear), cancellationToken);
}
