using coo.Domain.Entities;

namespace coo.Application.Common.Extension;
public static class WorkloadQueryExtensions
{
    public static IQueryable<Workload> WhereNameOrTeacherContains(this IQueryable<Workload> query, string? searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return query;

        var term = searchTerm.Trim().ToLower();

        return query.Where(d =>
            d.DisciplineRef.Name.ToLower().Contains(term) ||
            d.TeacherRef!.Name.ToLower().Contains(term) ||
            d.TeacherRef.Surname.ToLower().Contains(term) ||
            d.TeacherRef.Patronymic.ToLower().Contains(term) ||
            (d.TeacherRef.Surname + " " + d.TeacherRef.Name).ToLower().Contains(term) ||
            (d.TeacherRef.Surname + " " + d.TeacherRef.Name + " " + d.TeacherRef.Patronymic).ToLower().Contains(term));
    }
}