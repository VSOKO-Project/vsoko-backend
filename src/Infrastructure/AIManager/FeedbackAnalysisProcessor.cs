using System.Text.Json;
using Application.Common.DTOs;
using Domain.Entities;
using Infrastructure.DataManager.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.AIManager;

public class FeedbackAnalysisProcessor(AppDbContext db, IFeedbackAnalysisModel model,
    AiSettings settings, ILogger<FeedbackAnalysisProcessor> logger)
{
    public static string Version(string modelName, IReadOnlyList<FeedbackCategoryDto> categories) =>
        FeedbackAnalysisModel.Hash(FeedbackAnalysisModel.PromptVersion + modelName
            + JsonSerializer.Serialize(categories.OrderBy(c => c.Id), FeedbackAnalysisModel.JsonOptions));

    public async Task<bool> ProcessNextAsync(CancellationToken ct)
    {
        var categories = await db.FeedbackCategories.AsNoTracking().OrderBy(c => c.Id)
            .Select(c => new FeedbackCategoryDto(c.Id, c.Name, c.Description, c.RequiresAttention)).ToListAsync(ct);
        var version = Version(settings.Model, categories);
        var now = DateTime.UtcNow;
        // The persisted result is also the queue. Changes and old records are picked up after a restart.
        var source = await db.Feedbacks.AsNoTracking()
            .Where(f => f.Comment != null && f.Comment.Trim() != "")
            .Where(f => !db.FeedbackAnalyses.Any(a => a.FeedbackId == f.Id
                && a.SourceComment == f.Comment && a.Version == version
                && (a.Status == "Completed" || a.RetryAfterUtc > now)))
            .OrderBy(f => f.CreatedAtUtc).ThenBy(f => f.Id)
            .Select(f => new { f.Id, f.Comment }).FirstOrDefaultAsync(ct);
        if (source is null) return false;

        var analysis = await db.FeedbackAnalyses.FindAsync([source.Id], ct);
        if (analysis is null)
        {
            analysis = new FeedbackAnalysis { FeedbackId = source.Id };
            db.FeedbackAnalyses.Add(analysis);
        }
        var sameComment = analysis.SourceComment == source.Comment;
        var sameVersion = sameComment && analysis.Version == version;
        analysis.Attempts = sameVersion ? analysis.Attempts + 1 : 1;
        analysis.SourceComment = source.Comment;
        analysis.Version = version;
        analysis.Model = settings.Model;
        analysis.Status = "Processing";
        analysis.RetryAfterUtc = now.AddMinutes(10);
        if (!sameComment) analysis.FindingsJson = "[]";
        await db.SaveChangesAsync(ct);

        var findings = JsonSerializer.Deserialize<List<ExtractedFeedbackFinding>>(analysis.FindingsJson,
            FeedbackAnalysisModel.JsonOptions) ?? [];
        FeedbackAnalysisModel.Validate(findings, source.Comment, categories, false);
        var failed = false;
        foreach (var chunk in FeedbackAnalysisModel.SplitComment(source.Comment))
        {
            // Each pass runs even if the other one failed. Valid signals remain visible on partial failure.
            foreach (var risksOnly in new[] { false, true })
            {
                try
                {
                    using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
                    timeout.CancelAfter(TimeSpan.FromSeconds(settings.RequestTimeoutSeconds));
                    var extracted = await model.ExtractAsync(chunk, categories, risksOnly, timeout.Token);
                    FeedbackAnalysisModel.Validate(extracted, chunk, categories, risksOnly);
                    findings.AddRange(extracted);
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
                catch (Exception ex)
                {
                    failed = true;
                    // Never log comment text or provider response bodies.
                    logger.LogWarning("Feedback analysis failed ({ExceptionType}); pass={RiskPass}", ex.GetType().Name, risksOnly);
                }
            }
        }
        // Multiple passes and overlapping chunks may return the same evidence.
        findings = findings.GroupBy(f => new { f.CategoryId, f.ProposedCategoryName, f.Quote, f.Sentiment, f.Basis })
            .Select(g => { var first = g.First(); first.RequiresAttention = g.Any(f => f.RequiresAttention); return first; }).ToList();
        analysis.FindingsJson = JsonSerializer.Serialize(findings, FeedbackAnalysisModel.JsonOptions);
        analysis.Status = failed ? "Failed" : "Completed";
        analysis.UpdatedAtUtc = DateTime.UtcNow;
        analysis.RetryAfterUtc = DateTime.UtcNow.AddMinutes(Math.Min(60, Math.Pow(2, Math.Min(analysis.Attempts, 6))));
        await db.SaveChangesAsync(ct);
        return true;
    }
}
