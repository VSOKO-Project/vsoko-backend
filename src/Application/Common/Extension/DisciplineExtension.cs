using Application.Common.Periods;
using Domain.Entities;

namespace Application.Common.Extension;

public static class DisciplineQueryExtensions
{
    public static IQueryable<Discipline> WhereHasWorkloadIn(this IQueryable<Discipline> query, PeriodFilter period)
    {
        if (period.IsEmpty)
            return query;

        var inPeriod = period.ToWorkloadExpression();
        return query.Where(d => d.WorkloadRefs!.AsQueryable().Any(inPeriod));
    }

    public static IQueryable<Discipline> WhereNameOrTeacherContains(
        this IQueryable<Discipline> query,
        string? searchTerm
    )
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return query;

        var term = searchTerm.Trim().ToLower();

        return query.Where(d =>
            d.Name.ToLower().Contains(term)
            || d.WorkloadRefs!.Any(w =>
                w.TeacherRef!.Name.ToLower().Contains(term)
                || w.TeacherRef.Surname.ToLower().Contains(term)
                || w.TeacherRef.Patronymic.ToLower().Contains(term)
                || (w.TeacherRef.Surname + " " + w.TeacherRef.Name).ToLower().Contains(term)
                || (w.TeacherRef.Surname + " " + w.TeacherRef.Name + " " + w.TeacherRef.Patronymic)
                    .ToLower()
                    .Contains(term)
            )
        );
    }
}
