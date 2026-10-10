using Application.Common.DTOs;
using Application.Features.ImportFeatures;
using Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Common.Base;
using Presentation.Common.DTOs;
using static Microsoft.AspNetCore.Http.StatusCodes;

namespace Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class ImportController : BaseApiController
{
    private const string XlsxContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    // Запас сверх лимита файла на поля формы и multipart-обвязку.
    private const long RequestLimit = ImportFormat.MaxFileSize + 64 * 1024;

    public ImportController(ISender sender) : base(sender) { }

    [HttpGet("template")]
    [ProducesResponseType(typeof(FileContentResult), Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), Status403Forbidden)]
    public async Task<FileContentResult> GetTemplate(CancellationToken cancellationToken)
    {
        var file = await _sender.Send(new GetImportTemplateQuery(), cancellationToken);

        return File(file, XlsxContentType, "vsoko-import-template.xlsx");
    }

    [HttpPost("preview")]
    [RequestSizeLimit(RequestLimit)]
    [RequestFormLimits(MultipartBodyLengthLimit = RequestLimit)]
    [ProducesResponseType(typeof(ApiSuccessResult<ImportReportDto>), Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), Status500InternalServerError)]
    public async Task<ApiSuccessResult<ImportReportDto>> Preview([FromForm] ImportForm form, CancellationToken cancellationToken)
    {
        await using var content = form.File?.OpenReadStream();

        var result = await _sender.Send(new PreviewImportQuery
        {
            Content = content,
            FileName = form.File?.FileName,
            Length = form.File?.Length ?? 0,
            StartYear = form.StartYear,
            Term = form.Term,
        }, cancellationToken);

        return new ApiSuccessResult<ImportReportDto>
        {
            Code = Status200OK,
            Message = "Success",
            Data = result
        };
    }

    /// <summary>
    /// Повторяет разбор и проверку и применяет импорт в одной транзакции.
    /// Если в файле есть ошибки, отвечает 400 с отчётом в поле report и ничего не пишет.
    /// </summary>
    [HttpPost("apply")]
    [RequestSizeLimit(RequestLimit)]
    [RequestFormLimits(MultipartBodyLengthLimit = RequestLimit)]
    [ProducesResponseType(typeof(ApiSuccessResult<ImportReportDto>), Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), Status500InternalServerError)]
    public async Task<ApiSuccessResult<ImportReportDto>> Apply([FromForm] ImportForm form, CancellationToken cancellationToken)
    {
        await using var content = form.File?.OpenReadStream();

        var result = await _sender.Send(new ApplyImportCommand
        {
            Content = content,
            FileName = form.File?.FileName,
            Length = form.File?.Length ?? 0,
            StartYear = form.StartYear,
            Term = form.Term,
        }, cancellationToken);

        return new ApiSuccessResult<ImportReportDto>
        {
            Code = Status200OK,
            Message = "Success",
            Data = result
        };
    }

    [HttpGet("credentials")]
    [ProducesResponseType(typeof(FileContentResult), Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), Status403Forbidden)]
    public async Task<FileContentResult> GetCredentials([FromQuery] List<string>? groupIds, CancellationToken cancellationToken)
    {
        var file = await _sender.Send(new GetCredentialsQuery(groupIds), cancellationToken);

        return File(file, XlsxContentType, $"vsoko-credentials-{DateTime.Now:dd-MM-yyyy}.xlsx");
    }

    [HttpGet("groups")]
    [ProducesResponseType(typeof(ApiSuccessResult<List<StudentGroupDto>>), Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), Status403Forbidden)]
    public async Task<ApiSuccessResult<List<StudentGroupDto>>> GetGroups(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetImportGroupsQuery(), cancellationToken);

        return new ApiSuccessResult<List<StudentGroupDto>>
        {
            Code = Status200OK,
            Message = "Success",
            Data = result
        };
    }

    public class ImportForm
    {
        public IFormFile? File { get; init; }
        public int StartYear { get; init; }
        public Term Term { get; init; }
    }
}
