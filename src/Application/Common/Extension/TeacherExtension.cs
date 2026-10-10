using Application.Common.Periods;
using Domain.Entities;

namespace Application.Common.Extension;

public static class TeacherQueryExtensions
{
    public static IQueryable<Teacher> WhereHasWorkloadIn(this IQueryable<Teacher> query, PeriodFilter period)
    {
        if (period.IsEmpty)
            return query;

        var inPeriod = period.ToWorkloadExpression();
        return query.Where(t => t.WorkloadsRefs!.AsQueryable().Any(inPeriod));
    }

    public static IQueryable<Teacher> WhereNameContains(
        this IQueryable<Teacher> query,
        string? searchTerm
    )
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return query;

        var term = searchTerm.Trim().ToLower();

        return query.Where(t =>
            t.Name.ToLower().Contains(term)
            || t.Surname.ToLower().Contains(term)
            || t.Patronymic.ToLower().Contains(term)
            || (t.Name + " " + t.Patronymic).ToLower().Contains(term)
            || (t.Surname + " " + t.Name).ToLower().Contains(term)
            || (t.Surname + " " + t.Name + " " + t.Patronymic).ToLower().Contains(term)
        );
    }
}
