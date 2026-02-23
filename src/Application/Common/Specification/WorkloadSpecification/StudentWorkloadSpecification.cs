using System.Linq.Expressions;
using Domain.Entities;

namespace Application.Common.Specification.WorkloadSpecification;

public class StudentWorkloadSpecification : Specification<Workload>
{
    private readonly string _id;
    private readonly string _groupId;

    public StudentWorkloadSpecification(string id, string groupId)
    {
        _id = id;
        _groupId = groupId;
    }

    public override Expression<Func<Workload, bool>> ToExpression() =>
        w => w.GroupId == _groupId && !w.FeedbackRefs.Any(w => w.StudentId == _id);
}
