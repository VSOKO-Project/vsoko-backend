using System.Text.Json;
using Application.Common.DTOs;
using Domain.Entities;

namespace Infrastructure.AIManager;

public record FeedbackReportSource(string Id, string Comment);

public static class FeedbackReportBuilder
{
    public static FeedbackReportDto Build(string name, IReadOnlyList<FeedbackReportSource> sources,
        IReadOnlyDictionary<string, FeedbackAnalysis> analyses, IReadOnlyList<FeedbackSignalReview> reviews,
        IReadOnlyList<FeedbackCategoryDto> categories, string version)
    {
        var report = new FeedbackReportDto
        {
            Name = name.Trim(), TotalFeedbackCount = sources.Count,
            CommentCount = sources.Count(f => !string.IsNullOrWhiteSpace(f.Comment)),
            GeneratedAtUtc = DateTime.UtcNow, Version = version
        };
        var evidence = new List<(string Id, string Comment, ExtractedFeedbackFinding Finding)>();
        foreach (var source in sources.Where(s => !string.IsNullOrWhiteSpace(s.Comment)))
        {
            // Never show evidence from an edited comment or a soft-deleted feedback record.
            if (!analyses.TryGetValue(source.Id, out var analysis) || analysis.SourceComment != source.Comment) continue;
            if (analysis.Version == version)
            {
                if (analysis.Status == "Completed") report.ProcessedCount++;
                if (analysis.Status == "Failed") report.FailedCount++;
            }
            // Valid results from the previous catalogue and partial failures remain visible while updating.
            var findings = JsonSerializer.Deserialize<List<ExtractedFeedbackFinding>>(analysis.FindingsJson, FeedbackAnalysisModel.JsonOptions) ?? [];
            foreach (var finding in findings.Where(f => f.CategoryId is null && f.ProposedCategoryName is not null))
            {
                var approved = categories.FirstOrDefault(c => FeedbackCategoryCatalog.Normalize(c.Name)
                    == FeedbackCategoryCatalog.Normalize(finding.ProposedCategoryName!));
                if (approved is null) continue;
                finding.CategoryId = approved.Id;
                finding.ProposedCategoryName = null;
                finding.ProposedCategoryDescription = null;
                finding.RequiresAttention |= approved.RequiresAttention;
            }
            foreach (var finding in findings.Where(f => !string.IsNullOrWhiteSpace(f.Quote) && source.Comment.Contains(f.Quote, StringComparison.Ordinal)))
                evidence.Add((source.Id, source.Comment, finding));
        }
        foreach (var category in categories)
        {
            var matching = evidence.Where(e => e.Finding.CategoryId == category.Id).ToList();
            if (matching.Count == 0) continue;
            report.Topics.Add(new(category.Id, category.Name, matching.Select(e => e.Id).Distinct().Count(),
                Count("Positive"), Count("Negative"), Count("Neutral"),
                matching.Select(e => Evidence(e.Id, e.Finding)).Distinct().ToList()));
            int Count(string sentiment) => matching.Where(e => e.Finding.Sentiment == sentiment).Select(e => e.Id).Distinct().Count();
        }
        report.Topics = report.Topics.OrderByDescending(t => t.Count).ThenBy(t => t.CategoryId).ToList();
        foreach (var entry in evidence.Where(e => e.Finding.RequiresAttention)
                     .DistinctBy(e => new { e.Id, Key = FeedbackAnalysisModel.SignalKey(e.Comment, e.Finding) }))
        {
            var key = FeedbackAnalysisModel.SignalKey(entry.Comment, entry.Finding);
            var review = reviews.FirstOrDefault(r => r.FeedbackId == entry.Id && r.SignalKey == key);
            var categoryName = categories.FirstOrDefault(c => c.Id == entry.Finding.CategoryId)?.Name
                ?? entry.Finding.ProposedCategoryName ?? "Требует проверки";
            report.Signals.Add(new(entry.Id, key, categoryName, entry.Finding.Quote, entry.Finding.Basis,
                review?.Status ?? "Pending", review?.Note ?? ""));
        }
        report.ProposedCategories = evidence.Where(e => e.Finding.CategoryId is null && e.Finding.ProposedCategoryName is not null)
            .GroupBy(e => FeedbackCategoryCatalog.Normalize(e.Finding.ProposedCategoryName!))
            .Select(g => new ProposedFeedbackCategoryDto(g.First().Finding.ProposedCategoryName!,
                g.First().Finding.ProposedCategoryDescription!, g.Any(e => e.Finding.RequiresAttention),
                g.Select(e => Evidence(e.Id, e.Finding)).Distinct().ToList())).ToList();

        // The overview is rendered from counted topics; every substantive statement has evidence below.
        report.Summary = report.CommentCount == 0 ? "За выбранный период нет текстовых комментариев."
            : report.FailedCount > 0 && evidence.Count == 0 ? "Анализ части комментариев завершился ошибкой. Система повторит обработку."
            : report.ProcessedCount == 0 && evidence.Count == 0 ? "Комментарии ожидают анализа. Обновите отчёт после обработки."
            : report.Topics.Count == 0 && report.ProposedCategories.Count == 0 ? "В обработанных комментариях конкретные темы не выделены."
            : report.Topics.Count == 0 ? "Найдены темы, для которых предложены новые категории."
            : "Наиболее часто упоминаются: " + string.Join("; ", report.Topics.Take(3).Select(t => $"{t.Name} — {t.Count}")) + ".";
        return report;
    }

    private static FeedbackEvidenceDto Evidence(string id, ExtractedFeedbackFinding f) => new(id, f.Quote, f.Sentiment, f.Basis);
}
