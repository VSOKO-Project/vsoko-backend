using Application.Common.DTOs;
using Application.Common.Periods;
using Application.Common.Results;
using Domain.Entities;

namespace Application.Interfaces.DataManager.Repositories;

public interface ITeacherRepository : IRepository<Teacher>
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

    public Task<TeacherDto> GetByIdAsync(
        string id,
        CancellationToken cancellationToken
    );

    /// <summary>Без фильтра периода — все записи, иначе только с нагрузкой в периоде.</summary>
    public Task<PagedResultDto<TeacherDto>> GetAllAsync(
        int page,
        string query,
        int pageSize,
        PeriodFilter period,
        CancellationToken cancellationToken
    );

    /// <summary>Переименовывает. Если ФИО занято другим преподавателем, бросает ValidationException.</summary>
    public Task<TeacherDto> RenameAsync(
        string id,
        string surname,
        string name,
        string patronymic,
        CancellationToken cancellationToken
    );

    /// <summary>
    /// Переносит нагрузку и отзывы источника на цель и удаляет источник.
    /// Совпавшая нагрузка сливается; если студент оставил отзыв на обе, остаётся отзыв цели.
    /// </summary>
    public Task<TeacherDto> MergeAsync(string sourceId, string targetId, CancellationToken cancellationToken);
}
