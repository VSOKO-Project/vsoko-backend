using Application.Features.SummariesFeatures.Query;
using Application.Common.DTOs;
using Application.Interfaces.AIManager;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Common.Base;
using Presentation.Common.DTOs;
using static Microsoft.AspNetCore.Http.StatusCodes;

namespace Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SummariesController : BaseApiController
{
    private readonly IFeedbackAnalysisService _analysis;

    public SummariesController(ISender sender, IFeedbackAnalysisService analysis) : base(sender) { _analysis = analysis; }

    [HttpGet("teacher/{id}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiSuccessResult<FeedbackReportDto>), Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), Status500InternalServerError)]
    public async Task<ApiSuccessResult<FeedbackReportDto>> GetTeacherSummary(string id, [FromQuery] string? periodId, [FromQuery] int? startYear, CancellationToken ct)
    {
        var result = await _sender.Send(new GetTeacherSummaryByIdQuery(id, periodId, startYear), ct);

        return new ApiSuccessResult<FeedbackReportDto>
        {
            Code = Status200OK,
            Message = "Success",
            Data = result
        };
    }

    [HttpGet("discipline/{id}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiSuccessResult<FeedbackReportDto>), Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), Status500InternalServerError)]
    public async Task<ApiSuccessResult<FeedbackReportDto>> GetDisciplineSummary(string id, [FromQuery] string? periodId, [FromQuery] int? startYear, CancellationToken ct)
    {
        var result = await _sender.Send(new GetDisciplineSummaryByIdQuery(id, periodId, startYear), ct);

        return new ApiSuccessResult<FeedbackReportDto>
        {
            Code = Status200OK,
            Message = "Success",
            Data = result
        };
    }
    [HttpGet("categories")]
    [Authorize(Roles = "Admin")]
    public async Task<ApiSuccessResult<List<FeedbackCategoryDto>>> GetCategories(CancellationToken ct) =>
        new() { Code = Status200OK, Message = "Success", Data = await _analysis.GetCategoriesAsync(ct) };

    [HttpPost("categories")]
    [Authorize(Roles = "Admin")]
    public async Task<ApiSuccessResult<FeedbackCategoryDto>> AddCategory(AddFeedbackCategoryRequest request, CancellationToken ct) =>
        new() { Code = Status200OK, Message = "Success", Data = await _analysis.AddCategoryAsync(request, ct) };

    [HttpPut("signals/{feedbackId}/{signalKey}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ReviewSignal(string feedbackId, string signalKey, ReviewFeedbackSignalRequest request, CancellationToken ct)
    {
        await _analysis.ReviewSignalAsync(feedbackId, signalKey, request, ct);
        return NoContent();
    }
}