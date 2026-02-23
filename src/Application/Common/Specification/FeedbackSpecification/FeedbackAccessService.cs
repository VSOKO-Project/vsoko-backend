using System.Collections;
using System.Security.Claims;
using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Common.Specification;
using Application.Common.Specification.WorkloadSpecification;
using Domain.Entities;

namespace Application.Common.Specification.FeedbackSpecification;

public interface IFeedbackAccessService
{
    Specification<Feedback> GetSpecification();
}

public class FeedbackAccessService : IFeedbackAccessService
{
    private readonly IUserContext _context;

    public FeedbackAccessService(IUserContext context)
    {
        _context = context;
    }

    public Specification<Feedback> GetSpecification()
    {
        var role = _context.Role;

        return role switch
        {
            "student" => new StudentFeedbackSpecification(
                _context.UserId ?? ""
            ),
            "admin" => new EmployeeFeedbackSpecification(),
            _ => throw new UnauthorizationException("non correct role in claims"),
        };
    }
}
