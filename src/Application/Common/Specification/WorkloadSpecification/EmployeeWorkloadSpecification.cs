using System.Linq.Expressions;
using Domain.Entities;

namespace Application.Common.Specification.WorkloadSpecification;

public class EmployeeWorkloadSpecification : Specification<Workload>
{
    public override Expression<Func<Workload, bool>> ToExpression() =>
        w => true;
}