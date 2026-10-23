using System.Linq.Expressions;
using Domain.Entities;
using Infrastructure.DataManager.Contexts;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.DataManager.Repositories;

/// <summary>
/// Перенос нагрузки при объединении преподавателей или дисциплин.
/// </summary>
internal static class WorkloadMerger
{
    /// <summary>
    /// Нагрузка источника переходит к цели. Если у цели уже есть такая же нагрузка (тот же ключ),
    /// отзывы переносятся на неё, а нагрузка источника удаляется. Отзыв студента, который оценил обе,
    /// удаляется: остаётся его отзыв у цели. Удаление мягкое, как у остальных сущностей.
    /// </summary>
    public static async Task MergeAsync<TKey>(
        AppDbContext dbContext,
        Expression<Func<Workload, bool>> isSource,
        Expression<Func<Workload, bool>> isTarget,
        Func<Workload, TKey> key,
        Action<Workload> reassign,
        CancellationToken cancellationToken
    ) where TKey : notnull
    {
        var sourceWorkloads = await dbContext.Workloads
            .Include(w => w.FeedbackRefs)
            .Where(isSource)
            .ToListAsync(cancellationToken);

        var targetWorkloads = (await dbContext.Workloads
            .Include(w => w.FeedbackRefs)
            .Where(isTarget)
            .ToListAsync(cancellationToken))
            .ToDictionary(key);

        foreach (var workload in sourceWorkloads)
        {
            if (!targetWorkloads.TryGetValue(key(workload), out var twin))
            {
                reassign(workload);
                continue;
            }

            var reviewed = twin.FeedbackRefs!.Select(f => f.StudentId).ToHashSet();
            foreach (var feedback in workload.FeedbackRefs!)
            {
                if (reviewed.Contains(feedback.StudentId))
                    feedback.IsDeleted = true;
                else
                    feedback.WorkloadId = twin.Id;
            }

            workload.IsDeleted = true;
        }
    }
}
