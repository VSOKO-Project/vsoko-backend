using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Api.IntegrationTests.Support;
using Application.Common.DTOs;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.AIManager;
using Infrastructure.SecurityManager.AspNetCoreIdentity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Api.IntegrationTests;

[Collection(ApiCollection.Name)]
public class FeedbackAnalysisIntegrationTests(ApiFactory factory)
{
    [Fact]
    public async Task ReportPreservesRareSourceReviewAndPeriodBoundaries()
    {
        var seed = await factory.WithScopeAsync(async sp =>
        {
            var db = sp.GetRequiredService<Infrastructure.DataManager.Contexts.AppDbContext>();
            var categories = await db.FeedbackCategories.OrderBy(c => c.Id)
                .Select(c => new FeedbackCategoryDto(c.Id, c.Name, c.Description, c.RequiresAttention)).ToListAsync();
            var version = FeedbackAnalysisProcessor.Version(sp.GetRequiredService<AiSettings>().Model, categories);
            var workload = new Workload
            {
                PeriodId = await db.AcademicPeriods.Select(p => p.Id).FirstAsync(),
                TeacherRef = new Teacher { Name = "Тест", Surname = ApiFactory.Unique("Анализ"), Patronymic = "Тестович" },
                DisciplineRef = new Domain.Entities.Discipline { Name = ApiFactory.Unique("Курс") },
                GroupRef = new StudentGroup { Name = ApiFactory.Unique("Анализ-"), Semester = 1 }
            };
            db.Workloads.Add(workload);
            await db.SaveChangesAsync();
            Feedback? rare = null;
            for (var i = 0; i < 200; i++)
            {
                // One student can leave only one active review per workload.
                // These accounts are report sources and never authenticate.
                var studentId = Guid.NewGuid().ToString();
                var login = ApiFactory.Unique("analysis-");
                db.Users.Add(new ApplicationUser
                {
                    Id = studentId, UserName = login, NormalizedUserName = login.ToUpperInvariant(),
                    Name = "Тест", Surname = "Анализ", Type = UserType.Student,
                    StudentRef = new Student { Id = studentId, GroupId = workload.GroupId }
                });
                var comment = i == 199 ? "Объясняет понятно. Перед группой назвал меня тупым." : "Объясняет понятно.";
                var feedback = new Feedback { StudentId = studentId, WorkloadId = workload.Id, Comment = comment };
                db.Feedbacks.Add(feedback);
                var findings = new List<ExtractedFeedbackFinding>
                {
                    new() { CategoryId = "explanation", Quote = "Объясняет понятно.", Sentiment = "Positive", Basis = "Personal" }
                };
                if (i == 199)
                {
                    rare = feedback;
                    findings.Add(new() { CategoryId = "humiliation", Quote = "Перед группой назвал меня тупым.", Sentiment = "Negative", Basis = "Personal", RequiresAttention = true });
                }
                db.FeedbackAnalyses.Add(new()
                {
                    FeedbackId = feedback.Id, SourceComment = comment, Version = version,
                    Model = "test", Status = "Completed", UpdatedAtUtc = DateTime.UtcNow,
                    FindingsJson = JsonSerializer.Serialize(findings, FeedbackAnalysisModel.JsonOptions)
                });
            }
            await db.SaveChangesAsync();
            return (workload.TeacherId, workload.DisciplineId, workload.PeriodId, RareId: rare!.Id);
        });
        var admin = await factory.AdminAsync();
        var path = $"/api/Summaries/teacher/{seed.TeacherId}?periodId={seed.PeriodId}";
        var report = await (await admin.GetAsync(path)).DataAsync<FeedbackReportDto>();
        Assert.Equal(200, report.ProcessedCount);
        Assert.Equal(200, report.Topics.Single(t => t.CategoryId == "explanation").Count);
        var signal = Assert.Single(report.Signals);
        Assert.Equal(seed.RareId, signal.FeedbackId);
        var source = await (await admin.GetAsync($"/api/Feedback/{signal.FeedbackId}")).DataAsync<FeedbackDto>();
        Assert.Contains(signal.Quote, source.Comment);

        await (await admin.PutAsJsonAsync($"/api/Summaries/signals/{signal.FeedbackId}/{signal.SignalKey}",
            new { status = "Reviewed", note = "" })).ProblemAsync(HttpStatusCode.BadRequest);
        var reviewed = await admin.PutAsJsonAsync($"/api/Summaries/signals/{signal.FeedbackId}/{signal.SignalKey}",
            new { status = "Reviewed", note = "Результат проверки сотрудником" });
        Assert.Equal(HttpStatusCode.NoContent, reviewed.StatusCode);
        report = await (await admin.GetAsync(path)).DataAsync<FeedbackReportDto>();
        Assert.Equal("Reviewed", Assert.Single(report.Signals).Status);

        var otherYear = await (await admin.GetAsync($"/api/Summaries/teacher/{seed.TeacherId}?startYear=2099")).DataAsync<FeedbackReportDto>();
        Assert.Equal(0, otherYear.TotalFeedbackCount);
        var discipline = await (await admin.GetAsync($"/api/Summaries/discipline/{seed.DisciplineId}")).DataAsync<FeedbackReportDto>();
        Assert.Equal(seed.RareId, Assert.Single(discipline.Signals).FeedbackId);

        await factory.WithDbAsync(async db =>
        {
            (await db.Feedbacks.SingleAsync(f => f.Id == seed.RareId)).Comment = "Комментарий изменён";
            await db.SaveChangesAsync();
        });
        report = await (await admin.GetAsync(path)).DataAsync<FeedbackReportDto>();
        Assert.Empty(report.Signals);
        Assert.Equal(199, report.ProcessedCount);
        Assert.Equal(1, report.PendingCount);
        await factory.WithDbAsync(async db =>
        {
            (await db.Feedbacks.SingleAsync(f => f.Id == seed.RareId)).IsDeleted = true;
            await db.SaveChangesAsync();
        });
        report = await (await admin.GetAsync(path)).DataAsync<FeedbackReportDto>();
        Assert.Equal(199, report.TotalFeedbackCount);
        Assert.Equal(0, report.PendingCount);
    }

    [Fact]
    public async Task CategoryCreationRequiresAdminAndPreventsExactDuplicates()
    {
        var admin = await factory.AdminAsync();
        var request = new { name = ApiFactory.Unique("Категория"), description = "Описание новой категории", requiresAttention = false };
        var created = await (await admin.PostAsJsonAsync("/api/Summaries/categories", request)).DataAsync<FeedbackCategoryDto>();
        Assert.Equal(request.name, created.Name);
        var categories = await (await admin.GetAsync("/api/Summaries/categories")).DataAsync<List<FeedbackCategoryDto>>();
        Assert.Contains(categories, c => c.Id == created.Id);
        await (await admin.PostAsJsonAsync("/api/Summaries/categories", request)).ProblemAsync(HttpStatusCode.BadRequest);
        var student = await factory.LoginClientAsync("student4", ApiFactory.StudentPassword);
        Assert.Equal(HttpStatusCode.Forbidden, (await student.GetAsync("/api/Summaries/categories")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await student.PostAsJsonAsync("/api/Summaries/categories", request)).StatusCode);
    }
}
