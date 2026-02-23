using System.Linq.Expressions;
using Domain.Entities;

namespace Application.Common.Specification.FeedbackSpecification;

public class StudentFeedbackSpecification : Specification<Feedback>
{
    private readonly string _id;
    public StudentFeedbackSpecification(string id)
    {
        _id = id;
    }

    public override Expression<Func<Feedback, bool>> ToExpression() =>
        w => w.StudentId == _id;
}
