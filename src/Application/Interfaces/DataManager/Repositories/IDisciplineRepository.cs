using Application.Common.DTOs;
using Application.Common.Periods;
using Application.Common.Results;
using Application.Interfaces.DataManager.Repositories;
using Domain.Entities;

namespace Application.Interfaces.DataManager.Repositories;

public interface IDisciplineRepository : IRepository<Teacher>
{
    public Task<PagedResultDto<RatingDto>> GetRatingAsync(
        int page,
        string query,
        int pageSize,
        PeriodFilter period,
        CancellationToken cancellationToken
    );
    public Task<List<RatingDto>> GetAllRatingAsync(
        PeriodFilter period,
        CancellationToken cancellationToken
    );

    public Task<DisciplineDto> GetByIdAsync(
        string id,
        CancellationToken cancellationToken
    );

    /// <summary>Без фильтра периода — все записи, иначе только с нагрузкой в периоде.</summary>
    public Task<PagedResultDto<DisciplineDto>> GetAllAsync(
        int page,
        string query,
        int pageSize,
        PeriodFilter period,
        CancellationToken cancellationToken
    );

    /// <summary>Переименовывает. Если название занято другой дисциплиной, бросает ValidationException.</summary>
    public Task<DisciplineDto> RenameAsync(string id, string name, CancellationToken cancellationToken);

    /// <summary>
    /// Переносит нагрузку и отзывы источника на цель и удаляет источник.
    /// Совпавшая нагрузка сливается; если студент оставил отзыв на обе, остаётся отзыв цели.
    /// </summary>
    public Task<DisciplineDto> MergeAsync(string sourceId, string targetId, CancellationToken cancellationToken);
}
