using System.Linq.Expressions;
using coo.Domain.Entities;

namespace coo.Application.Common.Specification.WorkloadSpecification;

public class EmployeeWorkloadSpecification : Specification<Workload>
{
    public override Expression<Func<Workload, bool>> ToExpression() =>
        w => true;
}