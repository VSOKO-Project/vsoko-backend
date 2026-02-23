using Application.Features.SummariesFeatures.Query;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Common.Base;
using Presentation.Common.DTOs;
using static Microsoft.AspNetCore.Http.StatusCodes;

namespace Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SummariesController : BaseApiController
{
    public SummariesController(ISender sender) : base(sender) { }

    [HttpGet("teacher/{id}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiSuccessResult<string>), Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), Status500InternalServerError)]
    public async Task<ApiSuccessResult<string>> GetTeacherSummary(string id)
    {
        var result = await _sender.Send(new GetTeacherSummaryByIdQuery(id));

        return new ApiSuccessResult<string>
        {
            Code = Status200OK,
            Message = "Success",
            Data = result
        };
    }

    [HttpGet("discipline/{id}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiSuccessResult<string>), Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), Status500InternalServerError)]
    public async Task<ApiSuccessResult<string>> GetDisciplineSummary(string id)
    {
        var result = await _sender.Send(new GetDisciplineSummaryByIdQuery(id));

        return new ApiSuccessResult<string>
        {
            Code = Status200OK,
            Message = "Success",
            Data = result
        };
    }
}