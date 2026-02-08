using coo.Domain.Entities;

namespace coo.Application.Common.Extension;
public static class DisciplineQueryExtensions
{
    public static IQueryable<Discipline> WhereNameOrTeacherContains(this IQueryable<Discipline> query, string? searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return query;

        var term = searchTerm.Trim().ToLower();

        return query.Where(d =>
            d.Name.ToLower().Contains(term) ||
            d.WorkloadRefs!.Any(w =>
                w.TeacherRef!.Name.ToLower().Contains(term) ||
                w.TeacherRef.Surname.ToLower().Contains(term) ||
                w.TeacherRef.Patronymic.ToLower().Contains(term) ||
                (w.TeacherRef.Surname + " " + w.TeacherRef.Name).ToLower().Contains(term) ||
                (w.TeacherRef.Surname + " " + w.TeacherRef.Name + " " + w.TeacherRef.Patronymic).ToLower().Contains(term))
        );
    }
}