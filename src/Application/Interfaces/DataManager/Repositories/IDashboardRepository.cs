using Application.Common.DTOs;
using Application.Common.Periods;

namespace Application.Interfaces.DataManager.Repositories;

public interface IDashboardRepository
{
    Task<DashboardDto> GetAsync(PeriodFilter period, CancellationToken cancellationToken);
}
