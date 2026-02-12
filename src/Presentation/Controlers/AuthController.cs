using Application.Common.ResultsDto;
using Application.Features.SecurityFeatures.Query;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Common.Base;
using Presentation.Common.DTOs;

namespace Presentation.Controllers;

[Route("api/[controller]")]
public class SecurityController : BaseApiController
{
    public SecurityController(ISender sender)
        : base(sender) { }

    [AllowAnonymous]
    [HttpPost("Login")]
    public async Task<ApiSuccessResult<LoginResultDto>> Login(
        LoginQuery query,
        CancellationToken cancellationToken
    )
    {
        var result = await _sender.Send(query, cancellationToken);

        return new ApiSuccessResult<LoginResultDto>
        {
            Code = StatusCodes.Status200OK,
            Message = "Success",
            Data = result,
        };
    }

    [AllowAnonymous]
    [HttpPost("Refresh")]
    public async Task<ApiSuccessResult<LoginResultDto>> Refresh(
        RefreshQuery query,
        CancellationToken cancellationToken
    )
    {
        var result = await _sender.Send(query, cancellationToken);

        return new ApiSuccessResult<LoginResultDto>
        {
            Code = StatusCodes.Status200OK,
            Message = "Success",
            Data = result,
        };
    }
}
