using Application.Common.DTOs;
using Application.Common.Results;
using Application.Features.DashboardFeatures;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Common.Base;
using Presentation.Common.DTOs;

namespace Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class DashboardController(ISender sender) : BaseApiController(sender)
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiSuccessResult<DashboardDto>), StatusCodes.Status200OK)]
    public async Task<ApiSuccessResult<DashboardDto>> Get([FromQuery] GetDashboardQuery request, CancellationToken cancellationToken) =>
        new() { Code = StatusCodes.Status200OK, Message = "Success", Data = await _sender.Send(request, cancellationToken) };
}
