using System.Security.Claims;
using Application.Common.Interfaces;
using Application.Common.Exceptions;

namespace Presentation.Services;

public class UserContext : IUserContext
{
    private readonly IHttpContextAccessor _accessor;
    private ClaimsPrincipal? User => _accessor.HttpContext?.User;

    public UserContext(IHttpContextAccessor accessor) => _accessor = accessor;

    public string? UserName => User?.FindFirst(ClaimTypes.Name)?.Value;
    public string? UserId => User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    public string? Role
    {
        get
        {
            var roles = User?.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();

            if (roles is null || roles.Count == 0)
                return null;

            if (roles.Contains("Admin"))
                return "admin";

            if (roles.Contains("student"))
                return "student";

            throw new UnauthorizationException("This account has no access to student or administrator resources");
        }
    }
    public string? StudentGroup => User?.FindFirst("group")?.Value ?? null;
}
