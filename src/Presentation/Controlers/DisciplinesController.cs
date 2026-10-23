using Application.Common.DTOs;
using Application.Common.Results;
using Application.Features.DisciplineFeatures.Query;
using Application.Features.DisciplineFeatures.Command;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Common.Base;
using Presentation.Common.DTOs;
using static Microsoft.AspNetCore.Http.StatusCodes;

namespace Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DisciplinesController : BaseApiController
{
    public DisciplinesController(ISender sender) : base(sender) { }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiSuccessResult<PagedResultDto<DisciplineDto>>), Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), Status500InternalServerError)]
    public async Task<ApiSuccessResult<PagedResultDto<DisciplineDto>>> GetAll([FromQuery] GetAllDisciplinesRequest request)
    {
        var result = await _sender.Send(request);

        return new ApiSuccessResult<PagedResultDto<DisciplineDto>>
        {
            Code = Status200OK,
            Message = "Success",
            Data = result
        };
    }

    [HttpGet("rating")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiSuccessResult<PagedResultDto<RatingDto>>), Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), Status500InternalServerError)]
    public async Task<ApiSuccessResult<PagedResultDto<RatingDto>>> GetRating([FromQuery] GetDisciplineRatingRequest request)
    {
        var result = await _sender.Send(request);

        return new ApiSuccessResult<PagedResultDto<RatingDto>>
        {
            Code = Status200OK,
            Message = "Success",
            Data = result
        };
    }

    /// <summary>Исправляет название. Если такое название уже есть, отвечает 400: записи нужно объединить.</summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiSuccessResult<DisciplineDto>), Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), Status404NotFound)]
    public async Task<ApiSuccessResult<DisciplineDto>> Rename(string id, RenameDisciplineForm form, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new RenameDisciplineCommand(id, form.Name), cancellationToken);

        return new ApiSuccessResult<DisciplineDto>
        {
            Code = Status200OK,
            Message = "Success",
            Data = result
        };
    }

    /// <summary>Переносит нагрузку и отзывы дисциплины {id} на targetId и удаляет {id}.</summary>
    [HttpPost("{id}/merge")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiSuccessResult<DisciplineDto>), Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), Status404NotFound)]
    public async Task<ApiSuccessResult<DisciplineDto>> Merge(string id, DisciplineMergeForm form, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new MergeDisciplinesCommand(id, form.TargetId), cancellationToken);

        return new ApiSuccessResult<DisciplineDto>
        {
            Code = Status200OK,
            Message = "Success",
            Data = result
        };
    }

    public record RenameDisciplineForm(string Name);

    public record DisciplineMergeForm(string TargetId);
}
