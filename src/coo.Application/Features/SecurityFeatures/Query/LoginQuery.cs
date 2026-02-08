using System.ComponentModel;
using coo.Application.Common.Results;
using coo.Application.Common.Interfaces.SecurityManager;
using FluentValidation;
using MediatR;
using coo.Application.Common.CQRS;

namespace coo.Application.Features.SecurityFeatures;

public class LoginQuery : IRequest<LoginResultDto>, IQuery
{
    public string? Login { get; init;}
    public string? Password { get; init; }
}

public class LoginQueryValidator : AbstractValidator<LoginQuery>
{
    public LoginQueryValidator()
    {
        RuleFor(w => w.Login).NotEmpty();
        RuleFor(w => w.Password).NotEmpty();
    }
}

public class LoginQueryHandler : IRequestHandler<LoginQuery, LoginResultDto>
{
    private readonly ISecurityManager _securityManager;

    public LoginQueryHandler(ISecurityManager securityManager)
    {
        _securityManager = securityManager;
    }

    public async Task<LoginResultDto> Handle(LoginQuery request, CancellationToken cancellationToken)
    {
        var result = await _securityManager.LoginAsync(request.Login ?? "", request.Password ?? "", cancellationToken);

        return new LoginResultDto
        {
            AccessToken = result.AccessToken,
            Expires = result.Expires,
            UserId = result.UserId,
            IsAdmin = result.IsAdmin
        };
    }
}