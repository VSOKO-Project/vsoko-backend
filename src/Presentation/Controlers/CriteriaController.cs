using Application.Features.CriteriaFeatures.Command;
using Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Common.Base;

namespace Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CriteriaController : BaseApiController
{
    public CriteriaController(ISender sender) : base(sender) {}

    [Authorize(Roles = "admin")]
    [HttpPost]
    public async Task<PostCriteriaCommandRequest
}