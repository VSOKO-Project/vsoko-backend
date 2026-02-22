using Application.Common.ResultsDto;
using Application.Features.SecurityFeatures.Query;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Common.Base;
using Presentation.Common.DTOs;
using static Microsoft.AspNetCore.Http.StatusCodes;

namespace Presentation.Controllers;

[Route("api/[controller]")]
public class SecurityController : BaseApiController
{
    public SecurityController(ISender sender)
        : base(sender) { }

    [AllowAnonymous]
    [HttpPost("Login")]
    [ProducesResponseType(typeof(ApiSuccessResult<LoginResultDto>), Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), Status500InternalServerError)]
    public async Task<ApiSuccessResult<LoginResultDto>> Login(
        LoginQuery query,
        CancellationToken cancellationToken
    )
    {
        var result = await _sender.Send(query, cancellationToken);

        return new ApiSuccessResult<LoginResultDto>
        {
            Code = Status200OK,
            Message = "Success",
            Data = result,
        };
    }

    [AllowAnonymous]
    [HttpPost("Refresh")]
    [ProducesResponseType(typeof(ApiSuccessResult<LoginResultDto>), Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), Status500InternalServerError)]
    public async Task<ApiSuccessResult<LoginResultDto>> Refresh(
        RefreshQuery query,
        CancellationToken cancellationToken
    )
    {
        var result = await _sender.Send(query, cancellationToken);

        return new ApiSuccessResult<LoginResultDto>
        {
            Code = Status200OK,
            Message = "Success",
            Data = result,
        };
    }

    [AllowAnonymous]
    [HttpPost("LogOut")]
    [ProducesResponseType(typeof(ApiSuccessResult<Unit>), Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), Status500InternalServerError)]
    public async Task<ApiSuccessResult<Unit>> LogOut(
        LogOutQuery query,
        CancellationToken cancellationToken
    )
    {
        await _sender.Send(query, cancellationToken);

        return new ApiSuccessResult<Unit>
        {
            Code = Status204NoContent,
            Message = "Success"
        };
    }   
}
