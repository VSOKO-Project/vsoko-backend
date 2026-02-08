using coo.Domain.Entities;

namespace coo.Application.Common.Extension;
public static class TeacherQueryExtensions
{
    public static IQueryable<Teacher> WhereNameContains(this IQueryable<Teacher> query, string? searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return query;

        var term = searchTerm.Trim().ToLower();

        return query.Where(t =>
            t.Name.ToLower().Contains(term) ||
            t.Surname.ToLower().Contains(term) ||
            t.Patronymic.ToLower().Contains(term) ||
            (t.Name + " " + t.Patronymic).ToLower().Contains(term) ||
            (t.Surname + " " + t.Name).ToLower().Contains(term) ||
            (t.Surname + " " + t.Name + " " + t.Patronymic).ToLower().Contains(term)
        );
    }
}