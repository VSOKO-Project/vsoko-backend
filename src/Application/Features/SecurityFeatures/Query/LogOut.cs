using System.Reflection.Metadata;
using Application.Common.ResultsDto;
using Application.Interfaces.SecurityManager;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication;

namespace Application.Features.SecurityFeatures.Query;

public class LogOutQuery : IRequest<Unit>
{
    public string? Refresh { get; init; }
}

public class LogOutQueryValidator : AbstractValidator<LogOutQuery>
{
    public LogOutQueryValidator()
    {
        RuleFor(w => w.Refresh).NotEmpty();
    }
}

public class LogOutQueryHandler : IRequestHandler<LogOutQuery, Unit>
{
    private readonly ISecurityService _securityService;

    public LogOutQueryHandler(ISecurityService securityService)
    {
        _securityService = securityService;
    }

    public async Task<Unit> Handle(
        LogOutQuery request,
        CancellationToken cancellationToken
    )
    {
        await _securityService.LogOut(request.Refresh!, cancellationToken);

        return Unit.Value;
    }
}
