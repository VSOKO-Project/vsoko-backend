using Application.Common.DTOs;
using Application.Common.Results;
using Application.Features.WorkloadFeatures;
using Application.Features.WorkloadFeatures.Query;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Common.Base;
using Presentation.Common.DTOs;
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
    [ProducesResponseType(typeof(ProblemDetails), Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), Status500InternalServerError)]
    public async Task<ApiSuccessResult<PagedResultDto<WorkloadDto>>> GetAll([FromQuery] GetAllWorkloadRequest request)
    {
        var result = await _sender.Send(request);

        return new ApiSuccessResult<PagedResultDto<WorkloadDto>>
        {
            Code = Status200OK,
            Message = "Success",
            Data = result
        };
    }

    [HttpGet("{id}")]
    [Authorize]
    [ProducesResponseType(typeof(WorkloadDto), Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), Status500InternalServerError)]
    public async Task<ApiSuccessResult<WorkloadDto>> GetById(string id)
    {
        var result = await _sender.Send(new GetWorkloadByIdQuery(id));

        return new ApiSuccessResult<WorkloadDto>
        {
            Code = Status200OK,
            Message = "Success",
            Data = result
        };
    }
}
