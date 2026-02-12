using System.Reflection.Metadata;
using Application.Common.ResultsDto;
using Application.Interfaces.SecurityManager;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication;

namespace Application.Features.SecurityFeatures.Query;

public class RefreshQuery : IRequest<LoginResultDto>
{
    public string? Refresh { get; init; }
}

public class RefreshQueryValidator : AbstractValidator<RefreshQuery>
{
    public RefreshQueryValidator()
    {
        RuleFor(w => w.Refresh).NotEmpty();
    }
}

public class RefreshQueryHandler : IRequestHandler<RefreshQuery, LoginResultDto>
{
    private readonly ISecurityService _securityService;

    public RefreshQueryHandler(ISecurityService securityService)
    {
        _securityService = securityService;
    }

    public async Task<LoginResultDto> Handle(
        RefreshQuery request,
        CancellationToken cancellationToken
    )
    {
        var result = await _securityService.RefreshToken(request.Refresh!, cancellationToken);

        return result;
    }
}
