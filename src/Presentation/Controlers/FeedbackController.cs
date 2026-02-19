using Application.Common.DTOs;
using Application.Features.FeedbackFeatures.Command;
using Application.Features.FeedbackFeatures.Query;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Common.Base;
using static Microsoft.AspNetCore.Http.StatusCodes;

namespace Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FeedbackController : BaseApiController
{
    public FeedbackController(ISender sender) : base(sender) { }

    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(string), Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), Status500InternalServerError)]
    public async Task<string> Post([FromBody] PostFeedbackRequest request)
    {
        return await _sender.Send(request);
    }

    [HttpGet]
    [Authorize]
    [ProducesResponseType(typeof(List<FeedbackDto>), Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), Status500InternalServerError)]
    public async Task<List<FeedbackDto>> GetAll()
    {
        return await _sender.Send(new GetAllFeedbackQuery());
    }

    [HttpGet("{id}")]
    [Authorize]
    [ProducesResponseType(typeof(FeedbackDto), Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), Status500InternalServerError)]
    public async Task<FeedbackDto> GetById(string id)
    {
        return await _sender.Send(new GetFeedbackByIdQuery(id));
    }

    [HttpPut("{id}")]
    [Authorize]
    [ProducesResponseType(typeof(FeedbackDto), Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), Status500InternalServerError)]
    public async Task<FeedbackDto> Put(string id, [FromBody] PutFeedbackRequest request)
    {
        if (id != request.Id)
        {
            throw new ArgumentException("Id mismatch");
        }
        return await _sender.Send(request);
    }

    [HttpDelete("{id}")]
    [Authorize]
    [ProducesResponseType(typeof(Unit), Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), Status500InternalServerError)]
    public async Task<Unit> Delete(string id)
    {
        return await _sender.Send(new DeleteFeedbackRequest { Id = id });
    }
}
