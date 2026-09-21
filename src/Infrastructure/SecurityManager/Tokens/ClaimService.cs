using System.ComponentModel;
using System.Security.Claims;
using Domain.Common.Exceptions;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.DataManager.Contexts;
using Infrastructure.SecurityManager.AspNetCoreIdentity;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.SecurityManager.Tokens;

public class ClaimService
{
    public const string MustChangePasswordClaimType = "must_change_password";

    private readonly AppDbContext _dbContext;

    public ClaimService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<Claim>> GetClaimsForUserAsync(
        ApplicationUser user,
        CancellationToken cancellationToken
    )
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, user.UserName!),
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(MustChangePasswordClaimType, user.MustChangePassword ? "true" : "false"),
        };

        var additionalClaims = user.Type switch
        {
            UserType.Student => await GetClaimsForStudentAsync(user, cancellationToken),
            UserType.Employee => await GetClaimsForEmployeeAsync(user, cancellationToken),
            _ => throw new DomainException("UserType", new[] { "Null or not set" }),
        };

        claims.AddRange(additionalClaims);

        return claims;
    }

    private async Task<List<Claim>> GetClaimsForStudentAsync(
        ApplicationUser user,
        CancellationToken cancellationToken
    )
    {
        var student = await _dbContext.Students.FindAsync(user.Id, cancellationToken);

        if (student is null)
            throw new DomainException($"Student {user.Id}", new[] { "null or not set" });

        return new List<Claim>
        {
            new Claim("group", student.GroupId),
            new Claim(ClaimTypes.Role, "student"),
        };
    }

    private async Task<List<Claim>> GetClaimsForEmployeeAsync(
        ApplicationUser user,
        CancellationToken cancellationToken
    )
    {
        var employee = await _dbContext
            .Employees.Include(w => w.RoleRef)
            .FirstOrDefaultAsync(w => w.Id.Equals(user.Id), cancellationToken);

        if (employee is null)
            throw new DomainException($"Employee {user.Id}", new[] { "null or not set" });

        return new List<Claim>
        {
            new Claim(ClaimTypes.Role, employee.RoleRef!.Name),
            new Claim(ClaimTypes.Role, "admin"),
        };
    }
}
