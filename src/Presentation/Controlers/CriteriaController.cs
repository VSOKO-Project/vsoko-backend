using Application.Common.DTOs;
using Application.Features.CriteriaFeatures.Command;
using Application.Features.CriteriaFeatures.Query;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Common.Base;
using Presentation.Common.DTOs;
using static Microsoft.AspNetCore.Http.StatusCodes;

namespace Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CriteriaController : BaseApiController
{
    public CriteriaController(ISender sender) : base(sender) {}

    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ProducesResponseType(typeof(CriteriaDto), Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), Status500InternalServerError)]
    public async Task<ApiSuccessResult<CriteriaDto>> Post(PostCriteriaCommandRequest request)
    {
        var result = await _sender.Send(request);

        return new ApiSuccessResult<CriteriaDto>
        {
            Code = Status201Created,
            Message = "Success",
            Data = result,
        };
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(CriteriaDto), Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), Status500InternalServerError)]
    public async Task<ApiSuccessResult<CriteriaDto>> Put(string id, PutCriteriaCommandRequest request)
    {
        if (id != request.Id)
        {
            throw new ArgumentException("Id mismatch");
        }
        var result = await _sender.Send(request);

        return new ApiSuccessResult<CriteriaDto>
        {
            Code = Status200OK,
            Message = "Success",
            Data = result,
        };
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(Unit), Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), Status500InternalServerError)]
    public async Task<ApiSuccessResult<Unit>> Delete(string id)
    {
        await _sender.Send(new DeleteCriteriaCommandRequest { Id = id });

        return new ApiSuccessResult<Unit>
        {
            Code = Status204NoContent,
            Message = "Success",
            Data = Unit.Value,
        };
    }

    [HttpGet]
    [Authorize]
    [ProducesResponseType(typeof(List<CriteriaDto>), Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), Status500InternalServerError)]
    public async Task<ApiSuccessResult<List<CriteriaDto>>> GetAll()
    {
        var result = await _sender.Send(new GetAllCriteriaQuery());

        return new ApiSuccessResult<List<CriteriaDto>>
        {
            Code = Status200OK,
            Message = "Success",
            Data = result,
        };
    }

    [HttpGet("{id}")]
    [Authorize]
    [ProducesResponseType(typeof(CriteriaDto), Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), Status403Forbidden)] 
    [ProducesResponseType(typeof(ProblemDetails), Status500InternalServerError)]
    public async Task<ApiSuccessResult<CriteriaDto>> GetById(string id)
    {
        var result = await _sender.Send(new GetCriteriaByIdQuery(id));

        return new ApiSuccessResult<CriteriaDto>
        {
            Code = Status200OK,
            Message = "Success",
            Data = result,
        };
    }
}
