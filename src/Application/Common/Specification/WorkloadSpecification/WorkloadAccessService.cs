using System.Collections;
using System.Security.Claims;
using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Common.Specification;
using Application.Common.Specification.WorkloadSpecification;
using Domain.Entities;

namespace Application.Common.Specification.WorkloadSpecification;

public interface IWorkloadAccessService
{
    Specification<Workload> GetSpecification();
}

public class WorkloadAccessService : IWorkloadAccessService
{
    private readonly IUserContext _context;

    public WorkloadAccessService(IUserContext context)
    {
        _context = context;
    }

    public Specification<Workload> GetSpecification()
    {
        var role = _context.Role;

        return role switch
        {
            "student" => new StudentWorkloadSpecification(
                _context.UserId ?? "",
                _context.StudentGroup ?? ""
            ),
            "admin" => new EmployeeWorkloadSpecification(),
            _ => throw new UnauthorizationException("non correct role in claims"),
        };
    }
}
