using Application.Common.DTOs;
using Application.Features.PeriodFeatures.Command;
using Application.Features.PeriodFeatures.Query;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Common.Base;
using Presentation.Common.DTOs;
using static Microsoft.AspNetCore.Http.StatusCodes;

namespace Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PeriodsController : BaseApiController
{
    public PeriodsController(ISender sender) : base(sender) { }

    [HttpGet]
    [Authorize]
    [ProducesResponseType(typeof(ApiSuccessResult<List<PeriodListItemDto>>), Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), Status500InternalServerError)]
    public async Task<ApiSuccessResult<List<PeriodListItemDto>>> GetAll()
    {
        var result = await _sender.Send(new GetAllPeriodsQuery());

        return new ApiSuccessResult<List<PeriodListItemDto>>
        {
            Code = Status200OK,
            Message = "Success",
            Data = result
        };
    }

    [HttpGet("suggested")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiSuccessResult<List<SuggestedPeriodDto>>), Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), Status500InternalServerError)]
    public async Task<ApiSuccessResult<List<SuggestedPeriodDto>>> GetSuggested()
    {
        var result = await _sender.Send(new GetSuggestedPeriodsQuery());

        return new ApiSuccessResult<List<SuggestedPeriodDto>>
        {
            Code = Status200OK,
            Message = "Success",
            Data = result
        };
    }

    [HttpPut("{id}/feedback")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiSuccessResult<PeriodDto>), Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), Status500InternalServerError)]
    public async Task<ApiSuccessResult<PeriodDto>> SetFeedback(string id, [FromBody] SetPeriodFeedbackBody body)
    {
        var result = await _sender.Send(new SetPeriodFeedbackRequest { Id = id, IsOpen = body.IsOpen });

        return new ApiSuccessResult<PeriodDto>
        {
            Code = Status200OK,
            Message = "Success",
            Data = result
        };
    }

    public record SetPeriodFeedbackBody(bool IsOpen);
}
