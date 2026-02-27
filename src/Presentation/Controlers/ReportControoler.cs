using Application.Common.DTOs;
using Application.Features.FeedbackFeatures.Command;
using Application.Features.ReportFeatures.Query;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Common.Base;
using Presentation.Common.DTOs;
using static Microsoft.AspNetCore.Http.StatusCodes;

namespace Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReportController : BaseApiController
{
    public ReportController(ISender sender) : base(sender) { }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(string), Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), Status500InternalServerError)]
    public async Task<FileContentResult> Post(CancellationToken cancellationToken)
    {
        var doc = await _sender.Send(new ReportQuery(), cancellationToken);

        return File(doc, "application/pdf", $"RatingVsoko-{DateTime.Now:dd-MM-yyyy}.pdf");
    }
}
