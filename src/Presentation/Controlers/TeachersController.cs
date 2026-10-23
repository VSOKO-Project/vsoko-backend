using Application.Common.DTOs;
using Application.Common.Results;
using Application.Features.TeachersFeatures.Query;
using Application.Features.TeachersFeatures.Command;
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

    /// <summary>Исправляет ФИО. Если такое ФИО уже есть, отвечает 400: записи нужно объединить.</summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiSuccessResult<TeacherDto>), Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), Status404NotFound)]
    public async Task<ApiSuccessResult<TeacherDto>> Rename(string id, RenameTeacherForm form, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new RenameTeacherCommand(id, form.Surname, form.Name, form.Patronymic), cancellationToken);

        return new ApiSuccessResult<TeacherDto>
        {
            Code = Status200OK,
            Message = "Success",
            Data = result
        };
    }

    /// <summary>Переносит нагрузку и отзывы преподавателя {id} на targetId и удаляет {id}.</summary>
    [HttpPost("{id}/merge")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiSuccessResult<TeacherDto>), Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), Status404NotFound)]
    public async Task<ApiSuccessResult<TeacherDto>> Merge(string id, TeacherMergeForm form, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new MergeTeachersCommand(id, form.TargetId), cancellationToken);

        return new ApiSuccessResult<TeacherDto>
        {
            Code = Status200OK,
            Message = "Success",
            Data = result
        };
    }

    public record RenameTeacherForm(string Surname, string Name, string Patronymic);

    public record TeacherMergeForm(string TargetId);
}
