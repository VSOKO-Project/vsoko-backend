using Application.Common.DTOs;
using Application.Common.Results;
using Application.Features.TeachersFeatures.Query;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Common.Base;
using Presentation.Common.DTOs;
using static Microsoft.AspNetCore.Http.StatusCodes;

namespace Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TeachersController : BaseApiController
{
    public TeachersController(ISender sender) : base(sender) { }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiSuccessResult<PagedResultDto<TeacherDto>>), Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), Status500InternalServerError)]
    public async Task<ApiSuccessResult<PagedResultDto<TeacherDto>>> GetAll([FromQuery] GetAllTeachersRequest request)
    {
        var result = await _sender.Send(request);

        return new ApiSuccessResult<PagedResultDto<TeacherDto>>
        {
            Code = Status200OK,
            Message = "Success",
            Data = result
        };
    }

    [HttpGet("rating")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiSuccessResult<PagedResultDto<RatingDto>>), Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), Status500InternalServerError)]
    public async Task<ApiSuccessResult<PagedResultDto<RatingDto>>> GetRating([FromQuery] GetTeachersRatingRequest request)
    {
        var result = await _sender.Send(request);

        return new ApiSuccessResult<PagedResultDto<RatingDto>>
        {
            Code = Status200OK,
            Message = "Success",
            Data = result
        };
    }
}
