using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Api.IntegrationTests.Support;
using Application.Common.DTOs;
using Application.Common.Results;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Api.IntegrationTests;

/// <summary>Админские выборки: преподаватели, дисциплины, нагрузка, периоды, отчёт, сводки.</summary>
[Collection(ApiCollection.Name)]
public class CatalogTests(ApiFactory factory)
{
    private Task<string> OpenPeriodId() => factory.WithDbAsync(db => db.AcademicPeriods.Where(p => p.IsFeedbackOpen).Select(p => p.Id).SingleAsync());

    [Fact]
    public async Task Teachers_ListSearchAndRating()
    {
        var admin = await factory.AdminAsync();

        var page = await (await admin.GetAsync("/api/Teachers?page=1&pageSize=3")).DataAsync<PagedResultDto<TeacherDto>>();
        Assert.Equal(3, page.Items!.Count);
        Assert.True(page.TotalPages >= 3);

        // В сиде имя и фамилия перепутаны местами: Surname = «Алексей», Name = «Иванов».
        var found = await (await admin.GetAsync("/api/Teachers?query=ИВАНОВ")).DataAsync<PagedResultDto<TeacherDto>>();
        Assert.Contains(found.Items!, t => t.Name == "Иванов");

        var rating = await (await admin.GetAsync("/api/Teachers/rating?pageSize=50")).DataAsync<PagedResultDto<RatingDto>>();
        Assert.Contains(rating.Items!, r => r.Grade > 0);

        var periodId = await OpenPeriodId();
        var inPeriod = await (await admin.GetAsync($"/api/Teachers/rating?periodId={periodId}&query=Алексей")).DataAsync<PagedResultDto<RatingDto>>();
        Assert.Single(inPeriod.Items!);

        var nextYear = await (await admin.GetAsync("/api/Teachers/rating?startYear=2090")).DataAsync<PagedResultDto<RatingDto>>();
        Assert.Empty(nextYear.Items!);
    }

    [Fact]
    public async Task Disciplines_ListSearchAndRating()
    {
        var admin = await factory.AdminAsync();

        var page = await (await admin.GetAsync("/api/Disciplines?query=физ")).DataAsync<PagedResultDto<DisciplineDto>>();
        Assert.Equal("Физика", Assert.Single(page.Items!).Name);

        var all = await (await admin.GetAsync("/api/Disciplines?pageSize=100")).DataAsync<PagedResultDto<DisciplineDto>>();
        Assert.True(all.TotalCount >= 10);

        var rating = await (await admin.GetAsync("/api/Disciplines/rating?query=Сидоров")).DataAsync<PagedResultDto<RatingDto>>();
        Assert.Equal("Физика", Assert.Single(rating.Items!).Name);

        var byYear = await (await admin.GetAsync("/api/Disciplines/rating?startYear=2026&pageSize=100")).DataAsync<PagedResultDto<RatingDto>>();
        Assert.NotEmpty(byYear.Items!);

        var student = await factory.LoginClientAsync("student4", ApiFactory.StudentPassword);
        Assert.Equal(HttpStatusCode.Forbidden, (await student.GetAsync("/api/Disciplines")).StatusCode);
    }

    [Fact]
    public async Task Workload_AdminListAndById()
    {
        var admin = await factory.AdminAsync();
        var periodId = await OpenPeriodId();

        var all = await (await admin.GetAsync("/api/Workload?pageSize=100")).DataAsync<PagedResultDto<WorkloadDto>>();
        Assert.True(all.TotalCount >= 16);

        var search = await (await admin.GetAsync($"/api/Workload?periodId={periodId}&query=философия")).DataAsync<PagedResultDto<WorkloadDto>>();
        var item = Assert.Single(search.Items!);

        var byId = await (await admin.GetAsync($"/api/Workload/{item.Id}")).DataAsync<WorkloadDto>();
        Assert.Equal("Философия", byId.Discipline!.Name);
        Assert.NotNull(byId.Group);

        await (await admin.GetAsync("/api/Workload/missing")).ProblemAsync(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Periods_ListSuggestAndToggle()
    {
        var admin = await factory.AdminAsync();

        var list = await (await admin.GetAsync("/api/Periods")).DataAsync<List<PeriodListItemDto>>();
        var open = Assert.Single(list, p => p.IsFeedbackOpen);
        Assert.True(open.WorkloadCount >= 16);
        Assert.True(open.FeedbackCount > 0);

        var suggested = await (await admin.GetAsync("/api/Periods/suggested")).DataAsync<List<SuggestedPeriodDto>>();
        Assert.Equal(3, suggested.Count);
        Assert.Single(suggested, p => p.IsCurrent);

        var other = await factory.WithDbAsync(async db =>
        {
            var year = await db.AcademicPeriods.IgnoreQueryFilters().MaxAsync(p => p.StartYear) + 1;
            var period = new AcademicPeriod { StartYear = year, Term = Term.Autumn };
            db.AcademicPeriods.Add(period);
            await db.SaveChangesAsync();
            return period;
        });

        try
        {
            var opened = await (await admin.PutAsJsonAsync($"/api/Periods/{other.Id}/feedback", new { isOpen = true })).DataAsync<PeriodDto>();
            Assert.True(opened.IsFeedbackOpen);
            var after = await (await admin.GetAsync("/api/Periods")).DataAsync<List<PeriodListItemDto>>();
            Assert.Equal(other.Id, Assert.Single(after, p => p.IsFeedbackOpen).Id);

            var closed = await (await admin.PutAsJsonAsync($"/api/Periods/{other.Id}/feedback", new { isOpen = false })).DataAsync<PeriodDto>();
            Assert.False(closed.IsFeedbackOpen);
        }
        finally
        {
            await admin.PutAsJsonAsync($"/api/Periods/{open.Id}/feedback", new { isOpen = true });
        }

        Assert.Equal(open.Id, await OpenPeriodId());
        await (await admin.PutAsJsonAsync("/api/Periods/missing/feedback", new { isOpen = true })).ProblemAsync(HttpStatusCode.NotFound);

        var student = await factory.LoginClientAsync("student5", ApiFactory.StudentPassword);
        Assert.NotEmpty(await (await student.GetAsync("/api/Periods")).DataAsync<List<PeriodListItemDto>>());
        Assert.Equal(HttpStatusCode.Forbidden, (await student.GetAsync("/api/Periods/suggested")).StatusCode);
    }

    [Theory]
    [InlineData("")]
    [InlineData("?startYear=2026")]
    [InlineData("?periodId=OPEN")]
    public async Task Report_IsPdf(string query)
    {
        var admin = await factory.AdminAsync();
        query = query.Replace("OPEN", await OpenPeriodId());

        var response = await admin.PostAsync($"/api/Report{query}", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/pdf", response.Content.Headers.ContentType!.MediaType);
        var bytes = await response.Content.ReadAsByteArrayAsync();
        Assert.Equal("%PDF"u8.ToArray(), bytes[..4]);
    }

    [Fact]
    public async Task Report_UnknownPeriod_Is404()
    {
        var admin = await factory.AdminAsync();

        await (await admin.PostAsync("/api/Report?periodId=missing", null)).ProblemAsync(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Summaries_UseAiWithStudentComments()
    {
        var admin = await factory.AdminAsync();
        var (teacherId, disciplineId) = await factory.WithDbAsync(async db =>
        {
            var w = await db.Workloads.Include(x => x.TeacherRef).FirstAsync(x => x.FeedbackRefs!.Any());
            return (w.TeacherId, w.DisciplineId);
        });
        factory.Chat.Requests.Clear();

        var teacher = await (await admin.GetAsync($"/api/Summaries/teacher/{teacherId}?startYear=2026")).DataAsync<string>();
        var discipline = await (await admin.GetAsync($"/api/Summaries/discipline/{disciplineId}")).DataAsync<string>();

        Assert.Equal("Сводка от ИИ", teacher);
        Assert.Equal("Сводка от ИИ", discipline);
        Assert.Equal(2, factory.Chat.Requests.Count);
        Assert.Contains("ОТЗЫВЫ СТУДЕНТОВ", factory.Chat.Requests[0].Last().Content);

        await (await admin.GetAsync("/api/Summaries/teacher/missing")).ProblemAsync(HttpStatusCode.NotFound);
        await (await admin.GetAsync("/api/Summaries/discipline/missing")).ProblemAsync(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task HealthAndSwagger()
    {
        var client = factory.CreateClient();

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health/live")).StatusCode);

        var ready = await client.GetAsync("/health/ready");
        Assert.Equal(HttpStatusCode.OK, ready.StatusCode);
        var body = JsonDocument.Parse(await ready.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal("Healthy", body.GetProperty("Status").GetString());
        Assert.Equal(2, body.GetProperty("Checks").GetArrayLength());

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/swagger/v1/swagger.json")).StatusCode);
    }
}
