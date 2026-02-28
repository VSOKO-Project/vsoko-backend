using Application.Common.DTOs;
using Application.Common.Results;
using Application.Features.FeedbackFeatures.Command;
using Application.Features.FeedbackFeatures.Query;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Common.Base;
using Presentation.Common.DTOs;
using static Microsoft.AspNetCore.Http.StatusCodes;

namespace Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FeedbackController : BaseApiController
{
    public FeedbackController(ISender sender) : base(sender) { }

    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(ApiSuccessResult<FeedbackDto>), Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), Status500InternalServerError)]
    public async Task<ApiSuccessResult<FeedbackDto>> Post([FromBody] PostFeedbackRequest request)
    {
        var result = await _sender.Send(request);

        return new ApiSuccessResult<FeedbackDto>
        {
            Code = Status201Created,
            Message = "Success",
            Data = result,
        };
    }

    [HttpGet]
    [Authorize]
    [ProducesResponseType(typeof(ApiSuccessResult<PagedResultDto<FeedbackDto>>), Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), Status500InternalServerError)]
    public async Task<ApiSuccessResult<PagedResultDto<FeedbackDto>>> GetAll([FromQuery] GetAllFeedbackQuery request)
    {
        var result = await _sender.Send(request);

        return new ApiSuccessResult<PagedResultDto<FeedbackDto>>
        {
            Code = Status200OK,
            Message = "Success",
            Data = result,
        };
    }

    [HttpGet("{id}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiSuccessResult<FeedbackDto>), Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), Status500InternalServerError)]
    public async Task<ApiSuccessResult<FeedbackDto>> GetById(string id)
    {
        var result = await _sender.Send(new GetFeedbackByIdQuery(id));

        return new ApiSuccessResult<FeedbackDto>
        {
            Code = Status200OK,
            Message = "Success",
            Data = result,
        };
    }

    [HttpPut("{id}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiSuccessResult<FeedbackDto>), Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), Status500InternalServerError)]
    public async Task<ApiSuccessResult<FeedbackDto>> Put(string id, [FromBody] PutFeedbackRequest request)
    {
        if (id != request.Id)
        {
            throw new ArgumentException("Id mismatch");
        }
        var result = await _sender.Send(request);

        return new ApiSuccessResult<FeedbackDto>
        {
            Code = Status200OK,
            Message = "Success",
            Data = result,
        };
    }

    [HttpDelete("{id}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiSuccessResult<Unit>), Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), Status500InternalServerError)]
    public async Task<ApiSuccessResult<Unit>> Delete(string id)
    {
        await _sender.Send(new DeleteFeedbackRequest { Id = id });

        return new ApiSuccessResult<Unit>
        {
            Code = Status204NoContent,
            Message = "Success",
            Data = Unit.Value,
        };
    }
}
