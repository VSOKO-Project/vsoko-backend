using System.Linq.Expressions;

namespace coo.Application.Common.Specification;

public abstract class Specification<T>
{
    public abstract Expression<Func<T, bool>> ToExpression();

    public IQueryable<T> Apply(IQueryable<T> query) =>
    query.Where(ToExpression());
}