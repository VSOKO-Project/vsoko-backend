using System.Reflection.Metadata;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Common.Behaviors;

public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken
    )
    {
        var requestName = typeof(TRequest).Name;

        _logger.LogInformation("Start executing {RequestName} at {DateTime}: {@Request}", 
            requestName, DateTime.UtcNow, request);

        try
        {
            var response = await next();
            
            _logger.LogInformation("Finished executing {RequestName} at {DateTime}", 
                requestName, DateTime.UtcNow);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing {RequestName} at {DateTime}. Data: {@Request}", 
                requestName, DateTime.UtcNow, request);

            throw;
        }
    }
}
