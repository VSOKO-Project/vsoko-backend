using Application.Common.ResultsDto;
using Application.Common.Exceptions;
using Application.Interfaces.SecurityManager;
using Infrastructure.SecurityManager.Tokens;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Identity;

namespace Infrastructure.SecurityManager.AspNetCoreIdentity;

public class SecurityService : ISecurityService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly TokenService _tokenService;

    public SecurityService(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, TokenService tokenService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _tokenService = tokenService;
    }
    public async Task<LoginResultDto> LoginAsync(string login, string password, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByNameAsync(login);

        if (user is null)
            throw new NotFoundException(nameof(ApplicationUser));

        if (user.IsDeleted == true)
            throw new UnauthorizationException("Deleted!");

        if (user.IsBlocked == true)
            throw new UnauthorizationException("Blocked!");

        var result = await _signInManager.PasswordSignInAsync(user, password, true, false);

        if (result.IsLockedOut)
        {
            throw new UnauthorizationException("Invalid login cerdinals. IsLockedOut.");
        }

        if (!result.Succeeded)
        {
            throw new UnauthorizationException("Invalid login cerdinals. Not Succeeded.");
        }

        var (token, expires) = await _tokenService.GenerateJwtToken(user, cancellationToken);

        return new LoginResultDto
        {
            AccessToken = token,
            Expires = expires,
            UserId = user.Id,
            IsAdmin = await _userManager.IsInRoleAsync(user, "Admin")
        };
    }
}