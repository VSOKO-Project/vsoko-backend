using Application.Common.DTOs;
using Application.Common.Results;
using Application.Features.TeachersFeatures.Query;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Common.Base;
using static Microsoft.AspNetCore.Http.StatusCodes;

namespace Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TeachersController : BaseApiController
{
    public TeachersController(ISender sender) : base(sender) { }

    [HttpGet("rating")]
    [Authorize]
    [ProducesResponseType(typeof(PagedResultDto<RatingDto>), Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), Status500InternalServerError)]
    public async Task<PagedResultDto<RatingDto>> GetRating([FromQuery] GetTeachersRatingRequest request)
    {
        return await _sender.Send(request);
    }
}
