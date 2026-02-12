using System.ComponentModel;
using Application.Common.CQRS;
using Application.Common.ResultsDto;
using Application.Interfaces.SecurityManager;
using FluentValidation;
using MediatR;

namespace Application.Features.SecurityFeatures.Query;

public class LoginQuery : IRequest<LoginResultDto>, IQuery
{
    public string? Login { get; init; }
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
    private readonly ISecurityService _securityManager;

    public LoginQueryHandler(ISecurityService securityManager)
    {
        _securityManager = securityManager;
    }

    public async Task<LoginResultDto> Handle(
        LoginQuery request,
        CancellationToken cancellationToken
    )
    {
        var result = await _securityManager.LoginAsync(
            request.Login ?? "",
            request.Password ?? "",
            cancellationToken
        );

        return result;
    }
}
