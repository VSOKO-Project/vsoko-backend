using Application.Common.DTOs;
using Application.Features.FeedbackFeatures.Command;
using Application.Features.ReportFeatures.Query;
using Application.Interfaces.FileManager;
using Application.Common.Results;
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
public class ReportController : BaseApiController
{
    private readonly IReportHistoryService _history;
    public ReportController(ISender sender, IReportHistoryService history) : base(sender) { _history = history; }

    [HttpGet("history")]
    public async Task<ApiSuccessResult<PagedResultDto<ReportHistoryDto>>> History([FromQuery] int page = 1, CancellationToken cancellationToken = default) =>
        new() { Code = Status200OK, Message = "Success", Data = await _history.GetHistoryAsync(page, cancellationToken) };

    [HttpGet("history/{id}")]
    public async Task<FileContentResult> Download(string id, CancellationToken cancellationToken)
    {
        var report = await _history.GetFileAsync(id, cancellationToken);
        return File(report.Content, "application/pdf", report.FileName);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(string), Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), Status500InternalServerError)]
    public async Task<FileContentResult> Post([FromQuery] string? periodId, [FromQuery] int? startYear, CancellationToken cancellationToken)
    {
        var doc = await _sender.Send(new ReportQuery(periodId, startYear), cancellationToken);

        return File(doc, "application/pdf", $"RatingVsoko-{DateTime.Now:dd-MM-yyyy}.pdf");
    }
}
