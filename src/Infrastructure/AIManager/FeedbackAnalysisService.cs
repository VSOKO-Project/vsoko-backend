using System.Text.Json;
using Application.Common.DTOs;
using Application.Common.Exceptions;
using Application.Common.Periods;
using Application.Interfaces.AIManager;
using Domain.Entities;
using Infrastructure.DataManager.Contexts;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Infrastructure.AIManager;

public class FeedbackAnalysisService(AppDbContext db, AiSettings settings) : IFeedbackAnalysisService
{
    public Task<List<FeedbackCategoryDto>> GetCategoriesAsync(CancellationToken ct) =>
        db.FeedbackCategories.AsNoTracking().OrderBy(c => c.Id)
            .Select(c => new FeedbackCategoryDto(c.Id, c.Name, c.Description, c.RequiresAttention)).ToListAsync(ct);

    public async Task<FeedbackCategoryDto> AddCategoryAsync(AddFeedbackCategoryRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Length > 120
            || string.IsNullOrWhiteSpace(request.Description) || request.Description.Length > 600)
            throw new ValidationException("Укажите название категории (до 120 символов) и описание (до 600 символов).");
        var normalized = FeedbackCategoryCatalog.Normalize(request.Name);
        if (await db.FeedbackCategories.AnyAsync(c => c.NormalizedName == normalized, ct))
            throw new ValidationException("Категория с таким названием уже существует.");
        var category = new FeedbackCategory
        {
            Id = Guid.NewGuid().ToString(), Name = request.Name.Trim(), NormalizedName = normalized,
            Description = request.Description.Trim(), RequiresAttention = request.RequiresAttention
        };
        db.FeedbackCategories.Add(category);
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "23505" })
        { throw new ValidationException("Категория с таким названием уже существует."); }
        return new(category.Id, category.Name, category.Description, category.RequiresAttention);
    }

    public async Task<FeedbackReportDto> GetReportAsync(string id, bool teacher, PeriodFilter period, CancellationToken ct)
    {
        var name = teacher
            ? await db.Teachers.Where(t => t.Id == id).Select(t => t.Surname + " " + t.Name + " " + t.Patronymic).FirstOrDefaultAsync(ct)
            : await db.Disciplines.Where(d => d.Id == id).Select(d => d.Name).FirstOrDefaultAsync(ct);
        if (name is null) throw new NotFoundException(teacher ? "Teacher" : "Discipline", id);

        var categories = await GetCategoriesAsync(ct);
        var version = FeedbackAnalysisProcessor.Version(settings.Model, categories);
        var query = db.Feedbacks.AsNoTracking().Where(period.ToFeedbackExpression());
        query = teacher ? query.Where(f => f.WorkloadRef!.TeacherId == id) : query.Where(f => f.WorkloadRef!.DisciplineId == id);
        var sources = await query.Select(f => new FeedbackReportSource(f.Id, f.Comment)).ToListAsync(ct);
        var ids = sources.Select(f => f.Id).ToArray();
        var analyses = await db.FeedbackAnalyses.AsNoTracking().Where(a => ids.Contains(a.FeedbackId)).ToDictionaryAsync(a => a.FeedbackId, ct);
        var reviews = await db.FeedbackSignalReviews.AsNoTracking().Where(r => ids.Contains(r.FeedbackId)).ToListAsync(ct);
        return FeedbackReportBuilder.Build(name, sources, analyses, reviews, categories, version);
    }

    public async Task ReviewSignalAsync(string feedbackId, string signalKey, ReviewFeedbackSignalRequest request, CancellationToken ct)
    {
        if (request.Status is not ("Pending" or "InReview" or "Reviewed") || request.Note is null || request.Note.Length > 2000)
            throw new ValidationException("Недопустимый статус или слишком длинная заметка.");
        if (request.Status == "Reviewed" && string.IsNullOrWhiteSpace(request.Note))
            throw new ValidationException("Укажите результат рассмотрения.");
        var source = await db.Feedbacks.AsNoTracking().FirstOrDefaultAsync(f => f.Id == feedbackId, ct)
            ?? throw new NotFoundException("Feedback", feedbackId);
        var analysis = await db.FeedbackAnalyses.AsNoTracking().FirstOrDefaultAsync(a => a.FeedbackId == feedbackId, ct);
        var findings = analysis is not null && analysis.SourceComment == source.Comment
            ? JsonSerializer.Deserialize<List<ExtractedFeedbackFinding>>(analysis.FindingsJson, FeedbackAnalysisModel.JsonOptions) ?? [] : [];
        // Resolve a newly approved proposal the same way as the report does, even before the worker revisits it.
        FeedbackAnalysisModel.Validate(findings, source.Comment, await GetCategoriesAsync(ct), false);
        if (!findings.Any(f => f.RequiresAttention && FeedbackAnalysisModel.SignalKey(source.Comment, f) == signalKey))
            throw new NotFoundException("Signal", signalKey);
        var review = await db.FeedbackSignalReviews.FindAsync([feedbackId, signalKey], ct);
        if (review is null)
        {
            review = new FeedbackSignalReview { FeedbackId = feedbackId, SignalKey = signalKey };
            db.FeedbackSignalReviews.Add(review);
        }
        review.Status = request.Status;
        review.Note = request.Note.Trim();
        review.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }

}
