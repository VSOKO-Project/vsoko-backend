using Application.Common.DTOs;
using Domain.Entities;

namespace Application.Interfaces.DataManager.Repositories;

public interface IPeriodRepository : IRepository<AcademicPeriod>
{
    Task<List<PeriodListItemDto>> GetAllAsync(CancellationToken cancellationToken);

    Task<PeriodDto> GetByIdAsync(string id, CancellationToken cancellationToken);

    Task<PeriodDto?> GetOpenAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Открывает или закрывает сбор отзывов. При открытии закрывает его у остальных периодов.
    /// </summary>
    Task<PeriodDto> SetFeedbackOpenAsync(string id, bool isOpen, CancellationToken cancellationToken);
}
