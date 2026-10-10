using System.Linq.Expressions;
using Domain.Entities;

namespace Application.Common.Periods;

/// <summary>
/// Фильтр по периоду для админских выборок: конкретный семестр, весь учебный год или всё время.
/// </summary>
public record PeriodFilter(string? PeriodId = null, int? StartYear = null)
{
    public static PeriodFilter All { get; } = new();

    public bool IsEmpty => string.IsNullOrWhiteSpace(PeriodId) && StartYear is null;

    public string CacheKey => $"per{PeriodId}:y{StartYear}";

    public Expression<Func<Workload, bool>> ToWorkloadExpression()
    {
        var periodId = string.IsNullOrWhiteSpace(PeriodId) ? null : PeriodId;
        var startYear = StartYear;

        return w => (periodId == null || w.PeriodId == periodId)
            && (startYear == null || w.PeriodRef!.StartYear == startYear);
    }

    public Expression<Func<Feedback, bool>> ToFeedbackExpression()
    {
        var periodId = string.IsNullOrWhiteSpace(PeriodId) ? null : PeriodId;
        var startYear = StartYear;

        return f => (periodId == null || f.WorkloadRef!.PeriodId == periodId)
            && (startYear == null || f.WorkloadRef!.PeriodRef!.StartYear == startYear);
    }

    public Expression<Func<CriteriaFeedback, bool>> ToCriteriaFeedbackExpression()
    {
        var periodId = string.IsNullOrWhiteSpace(PeriodId) ? null : PeriodId;
        var startYear = StartYear;

        return cf => (periodId == null || cf.FeedbackRef!.WorkloadRef!.PeriodId == periodId)
            && (startYear == null || cf.FeedbackRef!.WorkloadRef!.PeriodRef!.StartYear == startYear);
    }
}
