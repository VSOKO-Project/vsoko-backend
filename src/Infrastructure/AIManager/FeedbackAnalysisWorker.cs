using Infrastructure.DataManager.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.AIManager;

public class FeedbackAnalysisWorker(IServiceScopeFactory scopes, AiSettings settings,
    ILogger<FeedbackAnalysisWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!settings.AnalysisEnabled) return;
        while (!stoppingToken.IsCancellationRequested)
        {
            var processed = false;
            try
            {
                using var scope = scopes.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                await db.Database.OpenConnectionAsync(stoppingToken);
                // A session lock prevents duplicate model calls across API replicas. Released on crash/disconnect.
                await using var command = db.Database.GetDbConnection().CreateCommand();
                command.CommandText = "SELECT pg_try_advisory_lock(728351902)";
                var locked = (bool)(await command.ExecuteScalarAsync(stoppingToken))!;
                if (locked)
                {
                    try { processed = await scope.ServiceProvider.GetRequiredService<FeedbackAnalysisProcessor>().ProcessNextAsync(stoppingToken); }
                    finally
                    {
                        command.CommandText = "SELECT pg_advisory_unlock(728351902)";
                        await command.ExecuteScalarAsync(CancellationToken.None);
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex) { logger.LogWarning("Feedback analysis worker unavailable ({ExceptionType})", ex.GetType().Name); }
            if (!processed)
                await Task.Delay(TimeSpan.FromSeconds(settings.AnalysisPollSeconds), stoppingToken);
        }
    }
}
