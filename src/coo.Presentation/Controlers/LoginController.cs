using coo.Application.Common.Results;
using coo.Application.Features.Security;
using coo.Presentation.Common.Base;
using coo.Presentation.Common.DTOs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace coo.Presentation.Controllers;

[Route("api/[controller]")]
public class SecurityController : BaseApiController
{
    public SecurityController(ISender sender) : base(sender)
    { }

    [AllowAnonymous]
    [HttpPost("Login")]
    public async Task<ApiSuccessResult<LoginResultDto>> LoginAsync(LoginQuery query, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(query, cancellationToken);

        return new ApiSuccessResult<LoginResultDto>
        {
            Code = StatusCodes.Status200OK,
            Message = "Success",
            Data = result
        };
    }
}