using Application.Common.DTOs;
using Application.Features.CriteriaFeatures.Command;
using Application.Features.CriteriaFeatures.Query;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Common.Base;
using static Microsoft.AspNetCore.Http.StatusCodes;

namespace Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CriteriaController : BaseApiController
{
    public CriteriaController(ISender sender) : base(sender) {}

    [Authorize(Roles = "admin")]
    [HttpPost]
    [ProducesResponseType(typeof(CriteriaDto), Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), Status500InternalServerError)]
    public async Task<CriteriaDto> Post(PostCriteriaCommandRequest request)
    {
        return await _sender.Send(request);
    }

    [Authorize(Roles = "admin")]
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(CriteriaDto), Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), Status500InternalServerError)]
    public async Task<CriteriaDto> Put(string id, PutCriteriaCommandRequest request)
    {
        if (id != request.Id)
        {
            throw new ArgumentException("Id mismatch");
        }
        return await _sender.Send(request);
    }

    [Authorize(Roles = "admin")]
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(Unit), Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), Status500InternalServerError)]
    public async Task<Unit> Delete(string id)
    {
        return await _sender.Send(new DeleteCriteriaCommandRequest { Id = id });
    }

    [HttpGet]
    [Authorize]
    [ProducesResponseType(typeof(List<CriteriaDto>), Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), Status500InternalServerError)]
    public async Task<List<CriteriaDto>> GetAll()
    {
        return await _sender.Send(new GetAllCriteriaQuery());
    }

    [HttpGet("{id}")]
    [Authorize]
    [ProducesResponseType(typeof(CriteriaDto), Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), Status500InternalServerError)]
    public async Task<CriteriaDto> GetById(string id)
    {
        return await _sender.Send(new GetCriteriaByIdQuery(id));
    }
}
