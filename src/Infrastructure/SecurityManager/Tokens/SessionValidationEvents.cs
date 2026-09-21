using System.Security.Claims;
using Domain.Common.Exceptions;
using Infrastructure.DataManager.Contexts;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.SecurityManager.Tokens;

public class SessionValidationEvents(AppDbContext db, ClaimService claims) : JwtBearerEvents
{
    public override async Task TokenValidated(TokenValidatedContext context)
    {
        var userId = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
        var sessionId = context.Principal?.FindFirstValue("sid");
        var ct = context.HttpContext.RequestAborted;
        if (userId is null || sessionId is null) { context.Fail("Session required"); return; }
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null || user.IsDeleted == true || user.IsBlocked == true ||
            user.LockoutEnd > DateTimeOffset.UtcNow || string.IsNullOrEmpty(user.SecurityStamp) ||
            !await db.Refreshes.AsNoTracking().AnyAsync(s => s.Id == sessionId && s.UserId == userId &&
                s.ExpiresAt > DateTime.UtcNow && s.SecurityStamp == user.SecurityStamp, ct))
        {
            context.Fail("Session revoked");
            return;
        }
        try
        {
            // Re-evaluate permissions and group membership instead of trusting stale claims.
            var currentClaims = await claims.GetClaimsForUserAsync(user, ct);
            currentClaims.Add(new Claim("sid", sessionId));
            context.Principal = new ClaimsPrincipal(new ClaimsIdentity(currentClaims,
                context.Scheme.Name, ClaimTypes.Name, ClaimTypes.Role));
        }
        catch (DomainException)
        {
            context.Fail("Profile unavailable");
        }
    }
}
