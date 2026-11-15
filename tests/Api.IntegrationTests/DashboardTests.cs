using System.Net;
using Api.IntegrationTests.Support;
using Application.Common.DTOs;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Api.IntegrationTests;

[Collection(ApiCollection.Name)]
public class DashboardTests(ApiFactory factory)
{
    [Fact]
    public async Task AggregatesDisciplineMeansAndLeavesEmptySemestersNull()
    {
        var ids = await factory.WithDbAsync(async db =>
        {
            var year = await db.AcademicPeriods.MaxAsync(p => p.StartYear) + 5;
            var first = new AcademicPeriod { StartYear = year, Term = Term.Autumn };
            var empty = new AcademicPeriod { StartYear = year, Term = Term.Spring };
            db.AcademicPeriods.AddRange(first, empty);
            var teacher = await db.Teachers.FirstAsync();
            var groupId = await db.Students.GroupBy(s => s.GroupId).Where(g => g.Count() >= 2).Select(g => g.Key).FirstAsync();
            var students = await db.Students.Where(s => s.GroupId == groupId).Take(2).ToListAsync();
            var groupSize = await db.Students.CountAsync(s => s.GroupId == groupId && db.Users.Any(u => u.Id == s.Id && u.IsDeleted != true && u.IsBlocked != true));
            var criterion = await db.Criterias.FirstAsync(c => c.Object == CriteriaObject.Discipline);
            var teacherCriterion = await db.Criterias.FirstAsync(c => c.Object == CriteriaObject.Teacher);
            var a = new Discipline { Name = ApiFactory.Unique("DashboardA") };
            var b = new Discipline { Name = ApiFactory.Unique("DashboardB") };
            db.Disciplines.AddRange(a, b);
            foreach (var (discipline, scores) in new[] { (a, new[] { 5, 5 }), (b, new[] { 1 }) })
            {
                var workload = new Workload { PeriodId = first.Id, TeacherId = teacher.Id, DisciplineId = discipline.Id, GroupId = students[0].GroupId };
                db.Workloads.Add(workload);
                for (var i = 0; i < scores.Length; i++)
                {
                    var feedback = new Feedback { StudentId = students[i].Id, WorkloadId = workload.Id, Comment = "dashboard test" };
                    db.Feedbacks.Add(feedback);
                    db.criteriaFeedbacks.Add(new CriteriaFeedback { CriteriaId = criterion.Id, CriteriaRef = criterion, FeedbackId = feedback.Id, CriteriaScore = scores[i] });
                    db.criteriaFeedbacks.Add(new CriteriaFeedback { CriteriaId = teacherCriterion.Id, CriteriaRef = teacherCriterion, FeedbackId = feedback.Id, CriteriaScore = 2 });
                }
            }
            await db.SaveChangesAsync();
            return (First: first.Id, Empty: empty.Id, Year: year, GroupSize: groupSize, Disciplines: new[] { a.Id, b.Id });
        });
        try
        {
            using var admin = await factory.AdminAsync();
            var result = await (await admin.GetAsync($"/api/Dashboard?startYear={ids.Year}")).DataAsync<DashboardDto>();
            Assert.Equal(3, result.FeedbackCount);
            Assert.Equal(3, result.Coverage.SubmittedCount);
            Assert.Equal(2 * ids.GroupSize, result.Coverage.PossibleCount);
            Assert.Equal(Math.Round(300d / (2 * ids.GroupSize), 1), result.Coverage.CompletionPercent);
            Assert.Equal(result.Coverage, result.Timeline[0].Coverage);
            Assert.Equal(3, result.RatedFeedbackCount);
            Assert.Null(result.Comparison);
            Assert.Equal(1, Assert.Single(result.LowGrades).FeedbackCount);
            Assert.Equal(2, Assert.Single(result.HighGrades).FeedbackCount);
            Assert.Equal(1, result.TeacherCount);
            Assert.Equal(2, result.DisciplineCount);
            Assert.Equal(3d, result.AverageGrade);
            Assert.Equal(1, result.LowGradeCount);
            Assert.Equal(1f, Assert.Single(result.LowGrades).Grade);
            Assert.Equal(5f, Assert.Single(result.HighGrades).Grade);
            Assert.Equal(new[] { 1, 0, 0, 1 }, result.Distribution.Select(b => b.Value));
            Assert.Equal(new[] { ids.First, ids.Empty }, result.Timeline.Select(p => p.PeriodId));
            Assert.Equal(3d, result.Timeline[0].AverageGrade);
            Assert.Equal(2, result.Timeline[0].DisciplineCount);
            Assert.Null(result.Timeline[1].AverageGrade);
            var empty = await (await admin.GetAsync($"/api/Dashboard?periodId={ids.Empty}")).DataAsync<DashboardDto>();
            Assert.Null(empty.AverageGrade);
            Assert.Equal(0, empty.Coverage.PossibleCount);
            Assert.Equal(0, empty.Coverage.SubmittedCount);
            Assert.Null(empty.Coverage.CompletionPercent);
            Assert.Null(result.Timeline[1].Coverage.CompletionPercent);
            Assert.Equal(0, empty.FeedbackCount);
            Assert.InRange(empty.Timeline.Count, 1, 6);
            Assert.Equal(ids.Empty, empty.Timeline.Last().PeriodId);
            var prior = await (await admin.GetAsync($"/api/Dashboard?periodId={ids.First}")).DataAsync<DashboardDto>();
            Assert.DoesNotContain(prior.Timeline, p => p.PeriodId == ids.Empty);
            await factory.WithDbAsync(async db =>
            {
                var firstWorkload = await db.Workloads.FirstAsync(w => w.PeriodId == ids.First && w.DisciplineId == ids.Disciplines[0]);
                var studentId = await db.Students.Where(s => s.GroupId == firstWorkload.GroupId).Select(s => s.Id).FirstAsync();
                var workload = new Workload { PeriodId = ids.Empty, TeacherId = firstWorkload.TeacherId, GroupId = firstWorkload.GroupId, DisciplineId = ids.Disciplines[0] };
                var feedback = new Feedback { WorkloadId = workload.Id, StudentId = studentId, Comment = "next semester" };
                var criterion = await db.Criterias.FirstAsync(c => c.Object == CriteriaObject.Discipline);
                db.Workloads.Add(workload);
                db.Feedbacks.Add(feedback);
                db.criteriaFeedbacks.Add(new CriteriaFeedback { CriteriaId = criterion.Id, CriteriaRef = criterion, FeedbackId = feedback.Id, CriteriaScore = 4 });
                await db.SaveChangesAsync();
            });
            var next = await (await admin.GetAsync($"/api/Dashboard?periodId={ids.Empty}")).DataAsync<DashboardDto>();
            Assert.NotNull(next.Comparison);
            Assert.Equal(1d, next.Comparison.Delta);
            Assert.Equal(1, next.Comparison.CurrentFeedbackCount);
            Assert.Equal(3, next.Comparison.PreviousFeedbackCount);
            var catalog = await (await admin.GetAsync($"/api/Disciplines/{ids.Disciplines[0]}")).DataAsync<DisciplineDto>();
            Assert.Equal(ids.Disciplines[0], catalog.Id);
            foreach (var (periodId, count) in new[] { (ids.First, 2), (ids.Empty, 1), (ids.First, 2) })
            {
                var feedbacks = await (await admin.GetAsync($"/api/Feedback?disciplineId={ids.Disciplines[0]}&periodId={periodId}")).DataAsync<Application.Common.Results.PagedResultDto<FeedbackDto>>();
                Assert.Equal(count, feedbacks.TotalCount);
                Assert.All(feedbacks.Items!, f => Assert.Equal(periodId, f.Workload!.Period!.Id));
            }
            await (await admin.GetAsync("/api/Dashboard?periodId=missing")).ProblemAsync(HttpStatusCode.NotFound);
            await (await admin.GetAsync($"/api/Dashboard?periodId={ids.First}&startYear={ids.Year}")).ProblemAsync(HttpStatusCode.BadRequest);
        }
        finally
        {
            await factory.WithDbAsync(async db =>
            {
                var workloadIds = await db.Workloads.Where(w => (w.PeriodId == ids.First || w.PeriodId == ids.Empty)).Select(w => w.Id).ToListAsync();
                var feedbackIds = await db.Feedbacks.Where(f => workloadIds.Contains(f.WorkloadId)).Select(f => f.Id).ToListAsync();
                await db.criteriaFeedbacks.Where(c => feedbackIds.Contains(c.FeedbackId)).ExecuteDeleteAsync();
                await db.Feedbacks.Where(f => feedbackIds.Contains(f.Id)).ExecuteDeleteAsync();
                await db.Workloads.Where(w => workloadIds.Contains(w.Id)).ExecuteDeleteAsync();
                await db.Disciplines.Where(d => ids.Disciplines.Contains(d.Id)).ExecuteDeleteAsync();
                await db.AcademicPeriods.Where(p => p.Id == ids.First || p.Id == ids.Empty).ExecuteDeleteAsync();
            });
        }
    }

    [Fact]
    public async Task RequiresAdmin()
    {
        using var anonymous = factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/Dashboard")).StatusCode);
        using var student = await factory.LoginClientAsync("student4", ApiFactory.StudentPassword);
        Assert.Equal(HttpStatusCode.Forbidden, (await student.GetAsync("/api/Dashboard")).StatusCode);
    }
}
