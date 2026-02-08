using coo.Application.Common.DTOs;

namespace coo.Application.Common.Interfaces.SecurityManager;

public interface ISecurityManager
{
    public Task<LoginResultDto> LoginAsync(
        string login,
        string password,
        CancellationToken cancellationToken
    );
}