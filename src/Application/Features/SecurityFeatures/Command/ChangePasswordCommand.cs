using Application.Common.Interfaces;
using Application.Interfaces.SecurityManager;
using FluentValidation;
using MediatR;

namespace Application.Features.SecurityFeatures.Command;

public class ChangePasswordCommand : IRequest<Unit>
{
    public string? CurrentPassword { get; init; }
    public string? NewPassword { get; init; }
}

public class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordCommandValidator()
    {
        RuleFor(w => w.CurrentPassword).NotEmpty();
        RuleFor(w => w.NewPassword).NotEmpty().MinimumLength(6);
    }
}

public class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand, Unit>
{
    private readonly ISecurityService _securityManager;
    private readonly IUserContext _userContext;

    public ChangePasswordCommandHandler(ISecurityService securityManager, IUserContext userContext)
    {
        _securityManager = securityManager;
        _userContext = userContext;
    }

    public async Task<Unit> Handle(
        ChangePasswordCommand request,
        CancellationToken cancellationToken
    )
    {
        await _securityManager.ChangePasswordAsync(
            _userContext.UserId!,
            request.CurrentPassword ?? "",
            request.NewPassword ?? "",
            cancellationToken
        );

        return Unit.Value;
    }
}
