using Application.Common.ResultsDto;

namespace Application.Interfaces.SecurityManager;

public interface ISecurityService
{
    public Task<LoginResultDto> LoginAsync(
        string login,
        string password,
        CancellationToken cancellationToken
    );

    public Task<LoginResultDto> RefreshToken(string refresh, CancellationToken cancellationToken);

    public Task LogOut(string refresh, CancellationToken cancellationToken);

    public Task ChangePasswordAsync(
        string userId,
        string currentPassword,
        string newPassword,
        CancellationToken cancellationToken
    );
}
