using Application.Common.DTOs;
using Application.Common.Results;
using Application.Features.WorkloadFeatures;
using Application.Features.WorkloadFeatures.Query;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Common.Base;
using static Microsoft.AspNetCore.Http.StatusCodes;

namespace Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WorkloadController : BaseApiController
{
    public WorkloadController(ISender sender) : base(sender) { }

    [HttpGet]
    [Authorize]
    [ProducesResponseType(typeof(PagedResultDto<WorkloadDto>), Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), Status500InternalServerError)]
    public async Task<PagedResultDto<WorkloadDto>> GetAll([FromQuery] GetAllWorkloadRequest request)
    {
        return await _sender.Send(request);
    }

    [HttpGet("{id}")]
    [Authorize]
    [ProducesResponseType(typeof(WorkloadDto), Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), Status500InternalServerError)]
    public async Task<WorkloadDto> GetById(string id)
    {
        return await _sender.Send(new GetWorkloadByIdQuery(id));
    }
}
