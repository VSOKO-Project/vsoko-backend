using Application.Common.Exceptions;
using Application.Common.ResultsDto;
using Application.Interfaces.SecurityManager;
using Domain.Entities;
using Infrastructure.DataManager.Contexts;
using Infrastructure.SecurityManager.Tokens;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.SecurityManager.AspNetCoreIdentity;

public class SecurityService
    (UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager,
     TokenService tokenService, AppDbContext context) : ISecurityService
{
    public async Task<LoginResultDto> LoginAsync(string login, string password, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByNameAsync(login);
        EnsureActive(user);
        // JWT login does not need to issue an additional Identity cookie.
        var result = await signInManager.CheckPasswordSignInAsync(user!, password, lockoutOnFailure: true);
        if (!result.Succeeded) throw new UnauthorizationException("Invalid login credentials");
        return await CreateSession(user!, cancellationToken);
    }

    public async Task<LoginResultDto> RefreshToken(string refresh, CancellationToken cancellationToken)
    {
        var session = await context.Refreshes.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Token == refresh && s.ExpiresAt > DateTime.UtcNow, cancellationToken);
        if (session is null) throw new UnauthorizationException("Invalid Refresh");
        var user = await userManager.FindByIdAsync(session.UserId);
        EnsureActive(user);
        if (session.SecurityStamp is null || session.SecurityStamp != user!.SecurityStamp)
            throw new UnauthorizationException("Session revoked");

        var (newRefresh, expires) = tokenService.GenerateRefreshToken();
        // Compare-and-swap consumes this token exactly once, even across tabs/servers.
        // The session ID stays stable so concurrent requests using its JWT remain valid.
        var consumed = await context.Refreshes
            .Where(s => s.Id == session.Id && s.Token == refresh && s.ExpiresAt > DateTime.UtcNow)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(s => s.Token, newRefresh)
                .SetProperty(s => s.ExpiresAt, expires), cancellationToken);
        if (consumed != 1) throw new UnauthorizationException("Refresh already consumed or revoked");
        session.Token = newRefresh;
        return await SessionResult(user!, session, cancellationToken);
    }

    public async Task LogOut(string refresh, CancellationToken cancellationToken)
    {
        // Idempotent; revoking the session also invalidates its access tokens.
        await context.Refreshes.Where(s => s.Token == refresh)
            .ExecuteUpdateAsync(setters => setters.SetProperty(s => s.IsDeleted, true), cancellationToken);
    }

    public async Task<LoginResultDto> ChangePasswordAsync(
        string userId, string currentPassword, string newPassword, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(userId);
        EnsureActive(user);
        var result = await userManager.ChangePasswordAsync(user!, currentPassword, newPassword);
        if (!result.Succeeded)
            throw new ValidationException(string.Join("; ", result.Errors.Select(e => e.Description)));
        user!.MustChangePassword = false;
        result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
            throw new InvalidOperationException("Could not save password change state");

        await context.Refreshes.Where(s => s.UserId == userId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(s => s.IsDeleted, true), cancellationToken);
        // Return the replacement session in the same transaction as password change.
        return await CreateSession(user, cancellationToken);
    }

    private static void EnsureActive(ApplicationUser? user)
    {
        if (user is null || user.IsDeleted == true || user.IsBlocked == true)
            throw new UnauthorizationException("Account unavailable");
    }

    private async Task<LoginResultDto> CreateSession(ApplicationUser user, CancellationToken cancellationToken)
    {
        var (refresh, expires) = tokenService.GenerateRefreshToken();
        var session = new Refresh {
            UserId = user.Id, Token = refresh, ExpiresAt = expires, SecurityStamp = user.SecurityStamp
        };
        // Generate claims before persisting a session for an invalid profile.
        var result = await SessionResult(user, session, cancellationToken);
        context.Refreshes.Add(session);
        await context.SaveChangesAsync(cancellationToken);
        return result;
    }

    private async Task<LoginResultDto> SessionResult(ApplicationUser user, Refresh session, CancellationToken cancellationToken)
    {
        var (token, expires) = await tokenService.GenerateJwtToken(user, session.Id, cancellationToken);
        return new LoginResultDto {
            AccessToken = token, RefreshToken = session.Token, Expires = expires, UserId = user.Id,
            IsAdmin = user.Type == Domain.Enums.UserType.Employee && await userManager.IsInRoleAsync(user, "Admin"),
            MustChangePassword = user.MustChangePassword
        };
    }
}
