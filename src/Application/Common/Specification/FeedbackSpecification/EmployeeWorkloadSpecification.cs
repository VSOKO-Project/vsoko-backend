using System.Linq.Expressions;
using Domain.Entities;

namespace Application.Common.Specification.FeedbackSpecification;

public class EmployeeFeedbackSpecification : Specification<Feedback>
{
    public override Expression<Func<Feedback, bool>> ToExpression() => w => true;
}
