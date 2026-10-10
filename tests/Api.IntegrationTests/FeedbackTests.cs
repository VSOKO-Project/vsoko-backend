using System.Net;
using System.Net.Http.Json;
using Api.IntegrationTests.Support;
using Application.Common.DTOs;
using Application.Common.Results;
using Application.Features.FeedbackFeatures.Command;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Api.IntegrationTests;

[Collection(ApiCollection.Name)]
public class FeedbackTests(ApiFactory factory)
{
    /// <summary>Студент в новой группе и нагрузка этой группы в указанном (или открытом) периоде.</summary>
    private async Task<(HttpClient Client, string StudentId, Workload Workload)> StudentWithWorkload(bool openPeriod = true)
    {
        var workload = await factory.WithDbAsync(async db =>
        {
            AcademicPeriod period;
            if (openPeriod)
            {
                period = await db.AcademicPeriods.SingleAsync(p => p.IsFeedbackOpen);
            }
            else
            {
                var year = await db.AcademicPeriods.IgnoreQueryFilters().MaxAsync(p => p.StartYear) + 1;
                period = new AcademicPeriod { StartYear = year, Term = Term.Spring };
                db.AcademicPeriods.Add(period);
            }

            var w = new Workload
            {
                PeriodId = period.Id,
                TeacherRef = new Teacher { Surname = "Тестова", Name = "Анна", Patronymic = ApiFactory.Unique("И") },
                DisciplineRef = new Discipline { Name = ApiFactory.Unique("Дисц ") },
                GroupRef = new StudentGroup { Name = ApiFactory.Unique("F-"), Semester = 1 },
            };
            db.Workloads.Add(w);
            await db.SaveChangesAsync();
            return w;
        });

        var user = await factory.CreateStudentAsync(workload.GroupId);
        var client = await factory.LoginClientAsync(user.UserName!, ApiFactory.StudentPassword);
        return (client, user.Id, workload);
    }

    private async Task<List<CriteriaDto>> Criteria(HttpClient client) =>
        await (await client.GetAsync("/api/Criteria")).DataAsync<List<CriteriaDto>>();

    private static object Grades(IEnumerable<CriteriaDto> criteria, int grade) =>
        criteria.Select(c => new { criteriaId = c.Id, grade }).ToList();

    [Fact]
    public async Task Student_FullLifecycle()
    {
        var (client, studentId, workload) = await StudentWithWorkload();
        var criteria = await Criteria(client);

        var available = await (await client.GetAsync("/api/Workload")).DataAsync<PagedResultDto<WorkloadDto>>();
        Assert.Equal(workload.Id, Assert.Single(available.Items!).Id);
        Assert.Equal(workload.Id, (await (await client.GetAsync($"/api/Workload/{workload.Id}")).DataAsync<WorkloadDto>()).Id);

        var created = await (await client.PostAsJsonAsync("/api/Feedback",
            new { feedback = Grades(criteria, 4), comment = "Хорошо", workloadId = workload.Id })).DataAsync<FeedbackDto>();
        Assert.Equal("Хорошо", created.Comment);
        Assert.Equal(criteria.Count, created.CriteriaFeedback!.Count);
        Assert.Equal(workload.Id, created.Workload!.Id);

        // Оценённая нагрузка пропадает из списка студента.
        Assert.Empty((await (await client.GetAsync("/api/Workload")).DataAsync<PagedResultDto<WorkloadDto>>()).Items!);
        await (await client.GetAsync($"/api/Workload/{workload.Id}")).ProblemAsync(HttpStatusCode.NotFound);

        var again = await client.PostAsJsonAsync("/api/Feedback", new { feedback = Grades(criteria, 5), workloadId = workload.Id });
        await again.ProblemAsync(HttpStatusCode.BadRequest);

        var mine = await (await client.GetAsync("/api/Feedback")).DataAsync<PagedResultDto<FeedbackDto>>();
        Assert.Equal(created.Id, Assert.Single(mine.Items!).Id);
        Assert.Equal(1, mine.TotalPages);

        var byId = await (await client.GetAsync($"/api/Feedback/{created.Id}")).DataAsync<FeedbackDto>();
        Assert.Equal(studentId, byId.Student!.Id);

        var updated = await (await client.PutAsJsonAsync($"/api/Feedback/{created.Id}",
            new { id = created.Id, comment = "Отлично", feedback = Grades(criteria.Take(1), 5) })).DataAsync<FeedbackDto>();
        Assert.Equal("Отлично", updated.Comment);
        Assert.Equal([5], updated.CriteriaFeedback!.Select(c => c.CriteriaScore));

        var commentOnly = await (await client.PutAsJsonAsync($"/api/Feedback/{created.Id}", new { id = created.Id })).DataAsync<FeedbackDto>();
        Assert.Equal("Отлично", commentOnly.Comment);

        Assert.Equal(HttpStatusCode.OK, (await client.DeleteAsync($"/api/Feedback/{created.Id}")).StatusCode);
        await (await client.GetAsync($"/api/Feedback/{created.Id}")).ProblemAsync(HttpStatusCode.NotFound);
        await (await client.DeleteAsync($"/api/Feedback/{created.Id}")).ProblemAsync(HttpStatusCode.NotFound);
        Assert.Single((await (await client.GetAsync("/api/Workload")).DataAsync<PagedResultDto<WorkloadDto>>()).Items!);
    }

    [Fact]
    public async Task Student_CannotSeeOrChangeOthersFeedback()
    {
        var (owner, _, workload) = await StudentWithWorkload();
        var criteria = await Criteria(owner);
        var created = await (await owner.PostAsJsonAsync("/api/Feedback",
            new { feedback = Grades(criteria, 3), workloadId = workload.Id })).DataAsync<FeedbackDto>();

        var stranger = (await StudentWithWorkload()).Client;

        await (await stranger.GetAsync($"/api/Feedback/{created.Id}")).ProblemAsync(HttpStatusCode.NotFound);
        await (await stranger.PutAsJsonAsync($"/api/Feedback/{created.Id}", new { id = created.Id, comment = "x" })).ProblemAsync(HttpStatusCode.NotFound);
        await (await stranger.DeleteAsync($"/api/Feedback/{created.Id}")).ProblemAsync(HttpStatusCode.NotFound);
        // Чужая нагрузка недоступна студенту.
        await (await stranger.PostAsJsonAsync("/api/Feedback", new { feedback = Grades(criteria, 3), workloadId = workload.Id }))
            .ProblemAsync(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Admin_SeesAllFeedbackWithFilters()
    {
        var (student, _, workload) = await StudentWithWorkload();
        var criteria = await Criteria(student);
        var created = await (await student.PostAsJsonAsync("/api/Feedback",
            new { feedback = Grades(criteria, 2), comment = "для админа", workloadId = workload.Id })).DataAsync<FeedbackDto>();
        var admin = await factory.AdminAsync();

        foreach (var filter in new[]
                 {
                     $"workloadId={workload.Id}",
                     $"teacherId={workload.TeacherId}",
                     $"disciplineId={workload.DisciplineId}",
                 })
        {
            var page = await (await admin.GetAsync($"/api/Feedback?{filter}")).DataAsync<PagedResultDto<FeedbackDto>>();
            Assert.Equal(created.Id, Assert.Single(page.Items!).Id);
        }

        var all = await (await admin.GetAsync("/api/Feedback?page=1&pageSize=5")).DataAsync<PagedResultDto<FeedbackDto>>();
        Assert.Equal(5, all.Items!.Count);
        Assert.True(all.TotalCount > 5);

        Assert.Equal(created.Id, (await (await admin.GetAsync($"/api/Feedback/{created.Id}")).DataAsync<FeedbackDto>()).Id);
    }

    [Fact]
    public async Task Admin_SeesStudentChangesDespiteCache()
    {
        var (student, _, workload) = await StudentWithWorkload();
        var criteria = await Criteria(student);
        var admin = await factory.AdminAsync();
        var listUrl = $"/api/Feedback?workloadId={workload.Id}";

        Assert.Empty((await (await admin.GetAsync(listUrl)).DataAsync<PagedResultDto<FeedbackDto>>()).Items!);

        var created = await (await student.PostAsJsonAsync("/api/Feedback",
            new { feedback = Grades(criteria, 4), comment = "первый", workloadId = workload.Id })).DataAsync<FeedbackDto>();
        Assert.Equal(created.Id, Assert.Single((await (await admin.GetAsync(listUrl)).DataAsync<PagedResultDto<FeedbackDto>>()).Items!).Id);
        Assert.Equal("первый", (await (await admin.GetAsync($"/api/Feedback/{created.Id}")).DataAsync<FeedbackDto>()).Comment);

        await student.PutAsJsonAsync($"/api/Feedback/{created.Id}", new { id = created.Id, comment = "второй" });
        Assert.Equal("второй", (await (await admin.GetAsync($"/api/Feedback/{created.Id}")).DataAsync<FeedbackDto>()).Comment);
        Assert.Equal("второй", Assert.Single((await (await admin.GetAsync(listUrl)).DataAsync<PagedResultDto<FeedbackDto>>()).Items!).Comment);

        await student.DeleteAsync($"/api/Feedback/{created.Id}");
        Assert.Empty((await (await admin.GetAsync(listUrl)).DataAsync<PagedResultDto<FeedbackDto>>()).Items!);
        await (await admin.GetAsync($"/api/Feedback/{created.Id}")).ProblemAsync(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ClosedPeriod_RejectsPostAndPut()
    {
        var (client, studentId, workload) = await StudentWithWorkload(openPeriod: false);
        var criteria = await Criteria(client);

        // Нагрузка закрытого периода видна только по явному periodId.
        Assert.Empty((await (await client.GetAsync("/api/Workload")).DataAsync<PagedResultDto<WorkloadDto>>()).Items!);
        var inPeriod = await (await client.GetAsync($"/api/Workload?periodId={workload.PeriodId}")).DataAsync<PagedResultDto<WorkloadDto>>();
        Assert.Single(inPeriod.Items!);

        var post = await client.PostAsJsonAsync("/api/Feedback", new { feedback = Grades(criteria, 4), workloadId = workload.Id });
        Assert.Equal(FeedbackMessages.PeriodClosed, (await post.ProblemAsync(HttpStatusCode.BadRequest)).GetProperty("detail").GetString());

        var feedbackId = await factory.WithDbAsync(async db =>
        {
            var feedback = new Feedback { StudentId = studentId, WorkloadId = workload.Id, Comment = "старый" };
            db.Feedbacks.Add(feedback);
            await db.SaveChangesAsync();
            return feedback.Id;
        });

        var put = await client.PutAsJsonAsync($"/api/Feedback/{feedbackId}", new { id = feedbackId, comment = "новый" });
        Assert.Equal(FeedbackMessages.PeriodClosed, (await put.ProblemAsync(HttpStatusCode.BadRequest)).GetProperty("detail").GetString());
    }

    [Fact]
    public async Task InvalidGrades_AreRejectedAndRolledBack()
    {
        var (client, studentId, workload) = await StudentWithWorkload();
        var criteria = await Criteria(client);

        var noCriteria = await client.PostAsJsonAsync("/api/Feedback",
            new { feedback = new[] { new { criteriaId = (string?)null, grade = 3 } }, workloadId = workload.Id });
        await noCriteria.ProblemAsync(HttpStatusCode.BadRequest);
        Assert.False(await factory.WithDbAsync(db => db.Feedbacks.AnyAsync(f => f.StudentId == studentId)));

        await (await client.PostAsJsonAsync("/api/Feedback", new { workloadId = workload.Id })).ProblemAsync(HttpStatusCode.BadRequest);

        var created = await (await client.PostAsJsonAsync("/api/Feedback",
            new { feedback = Grades(criteria, 4), workloadId = workload.Id })).DataAsync<FeedbackDto>();

        var badPut = await client.PutAsJsonAsync($"/api/Feedback/{created.Id}",
            new { id = created.Id, feedback = new[] { new { criteriaId = (string?)null, grade = 1 } } });
        await badPut.ProblemAsync(HttpStatusCode.BadRequest);

        var mismatch = await client.PutAsJsonAsync($"/api/Feedback/{created.Id}", new { id = "other" });
        await mismatch.ProblemAsync(HttpStatusCode.BadRequest);

        await (await client.PostAsJsonAsync("/api/Feedback", new { feedback = Grades(criteria, 4), workloadId = "missing" }))
            .ProblemAsync(HttpStatusCode.NotFound);
    }
}
