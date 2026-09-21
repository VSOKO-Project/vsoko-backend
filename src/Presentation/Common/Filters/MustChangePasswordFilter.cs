using Infrastructure.SecurityManager.Tokens;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using static Microsoft.AspNetCore.Http.StatusCodes;

namespace Presentation.Common.Filters;

public class MustChangePasswordFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var endpoint = context.HttpContext.GetEndpoint();

        var isExempt =
            endpoint?.Metadata.GetMetadata<IAllowAnonymous>() is not null
            || endpoint?.Metadata.GetMetadata<AllowWithPendingPasswordChangeAttribute>() is not null;

        if (!isExempt)
        {
            var mustChangePassword = context.HttpContext.User.Claims.FirstOrDefault(
                c => c.Type == ClaimService.MustChangePasswordClaimType
            )?.Value;

            if (mustChangePassword == "true")
            {
                context.Result = new ObjectResult(
                    new ProblemDetails
                    {
                        Title = "Password Change Required",
                        Status = Status403Forbidden,
                        Detail = "Password must be changed before accessing this resource.",
                    }
                )
                {
                    StatusCode = Status403Forbidden,
                    ContentTypes = { "application/problem+json" },
                };
                return;
            }
        }

        await next();
    }
}
