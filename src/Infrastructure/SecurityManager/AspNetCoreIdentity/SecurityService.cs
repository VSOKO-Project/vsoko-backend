using Application.Common.Exceptions;
using Application.Common.ResultsDto;
using Application.Interfaces.SecurityManager;
using Domain.Entities;
using Infrastructure.DataManager.Contexts;
using Infrastructure.SecurityManager.Tokens;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.SecurityManager.AspNetCoreIdentity;

public class SecurityService : ISecurityService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly TokenService _tokenService;
    private readonly AppDbContext _context;

    public SecurityService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        TokenService tokenService,
        AppDbContext context
    )
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _tokenService = tokenService;
        _context = context;
    }

    public async Task<LoginResultDto> LoginAsync(
        string login,
        string password,
        CancellationToken cancellationToken
    )
    {
        var user = await _userManager.FindByNameAsync(login);

        if (user is null)
            throw new UnauthorizationException("Bad creds");

        if (user.IsDeleted == true)
            throw new UnauthorizationException("Deleted!");

        if (user.IsBlocked == true)
            throw new UnauthorizationException("Blocked!");

        var result = await _signInManager.PasswordSignInAsync(user, password, true, false);

        if (result.IsLockedOut)
            throw new UnauthorizationException("Invalid login cerdinals. IsLockedOut.");

        if (!result.Succeeded)
            throw new UnauthorizationException("Invalid login cerdinals. Not Succeeded.");

        var (token, expires) = await _tokenService.GenerateJwtToken(user, cancellationToken);
        var (refreshToken, refreshExpires) = _tokenService.GenerateRefreshToken();

        await _context.AddAsync(
            new Refresh
            {
                UserId = user.Id,
                Token = refreshToken,
                ExpiresAt = refreshExpires,
            }
        );

        await _context.SaveChangesAsync(cancellationToken);

        return new LoginResultDto
        {
            AccessToken = token,
            RefreshToken = refreshToken,
            Expires = expires,
            UserId = user.Id,
            IsAdmin = await _userManager.IsInRoleAsync(user, "Admin"),
            MustChangePassword = user.MustChangePassword,
        };
    }

    public async Task<LoginResultDto> RefreshToken(
        string refresh,
        CancellationToken cancellationToken
    )
    {
        var oldRefesh = await _context.Refreshes.FirstOrDefaultAsync(w => w.Token.Equals(refresh) && w.ExpiresAt > DateTime.UtcNow, cancellationToken);

        if (oldRefesh is null)
            throw new UnauthorizationException("Invalid Refresh");

        oldRefesh.IsDeleted = true;

        var user = await _userManager.FindByIdAsync(oldRefesh.UserId);

        if (user is null)
            throw new UnauthorizationException("Bad creds");

        if (user.IsDeleted == true)
            throw new UnauthorizationException("Deleted!");

        if (user.IsBlocked == true)
            throw new UnauthorizationException("Blocked!");

        var (token, expires) = await _tokenService.GenerateJwtToken(user, cancellationToken);
        var (refreshToken, refreshExpires) = _tokenService.GenerateRefreshToken();

        await _context.AddAsync(
            new Refresh
            {
                UserId = user.Id,
                Token = refreshToken,
                ExpiresAt = refreshExpires,
            }
        );

        await _context.SaveChangesAsync(cancellationToken);

        return new LoginResultDto
        {
            AccessToken = token,
            RefreshToken = refreshToken,
            Expires = expires,
            UserId = user.Id,
            IsAdmin = await _userManager.IsInRoleAsync(user, "Admin"),
            MustChangePassword = user.MustChangePassword,
        };
    }

    public async Task LogOut(string refreshToken, CancellationToken cancellationToken)
    {
        var oldRefesh = await _context.Refreshes.FirstOrDefaultAsync(w => w.Token.Equals(refreshToken));

        if (oldRefesh is null)
            throw new UnauthorizationException("Invalid Refresh");

        oldRefesh.IsDeleted = true;

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task ChangePasswordAsync(
        string userId,
        string currentPassword,
        string newPassword,
        CancellationToken cancellationToken
    )
    {
        var user = await _userManager.FindByIdAsync(userId);

        if (user is null)
            throw new UnauthorizationException("Bad creds");

        var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);

        if (!result.Succeeded)
            throw new UnauthorizationException(
                string.Join("; ", result.Errors.Select(e => e.Description))
            );

        user.MustChangePassword = false;
        await _userManager.UpdateAsync(user);
    }
}
