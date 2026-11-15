using Application.Common.DTOs;
using Application.Common.Exceptions;
using Application.Common.Mappings;
using Application.Common.Periods;
using Application.Interfaces.DataManager.Repositories;
using Domain.Enums;
using Infrastructure.DataManager.Contexts;
using Microsoft.EntityFrameworkCore;
using static Application.Common.Extension.DisciplineQueryExtensions;

namespace Infrastructure.DataManager.Repositories;

/// <summary>Returns bounded, aggregated dashboard data; never transfers raw feedback to the browser.</summary>
public class DashboardRepository(AppDbContext db, DisciplineMapper mapper) : IDashboardRepository
{
    public async Task<DashboardDto> GetAsync(PeriodFilter filter, CancellationToken cancellationToken)
    {
        var semesters = db.AcademicPeriods.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(filter.PeriodId))
        {
            var selected = await semesters.FirstOrDefaultAsync(p => p.Id == filter.PeriodId, cancellationToken)
                ?? throw new NotFoundException("AcademicPeriod", filter.PeriodId);
            semesters = semesters.Where(p => p.StartYear < selected.StartYear ||
                (p.StartYear == selected.StartYear && p.Term <= selected.Term));
        }
        else if (filter.StartYear is not null)
            semesters = semesters.Where(p => p.StartYear == filter.StartYear);

        var periods = await semesters.OrderByDescending(p => p.StartYear).ThenByDescending(p => p.Term)
            .Take(6)
            .Select(p => new { p.Id, p.StartYear, p.Term, FeedbackCount = p.WorkloadRefs!.SelectMany(w => w.FeedbackRefs!).Count() })
            .ToListAsync(cancellationToken);
        periods.Reverse();
        var ids = periods.Select(p => p.Id).ToArray();

        // PostgreSQL groups scores before materialization. At most six semesters of per-discipline averages.
        var semesterGrades = await db.criteriaFeedbacks.AsNoTracking()
            .Where(c => c.CriteriaRef.Object == CriteriaObject.Discipline && !c.FeedbackRef!.WorkloadRef!.DisciplineRef!.IsDeleted && ids.Contains(c.FeedbackRef!.WorkloadRef!.PeriodId))
            .GroupBy(c => new { c.FeedbackRef!.WorkloadRef!.PeriodId, c.FeedbackRef.WorkloadRef.DisciplineId })
            .Select(g => new { g.Key.PeriodId, Grade = g.Average(c => (double)c.CriteriaScore), FeedbackCount = g.Select(c => c.FeedbackId).Distinct().Count() })
            .ToListAsync(cancellationToken);
        // Compare adjacent academic semesters only; never bridge a missing semester.
        DashboardComparisonDto? comparison = null;
        var current = periods.LastOrDefault();
        if (current is not null)
        {
            var previousKey = PeriodCalculator.Previous(new PeriodKey(current.StartYear, current.Term));
            var previous = periods.FirstOrDefault(p => p.StartYear == previousKey.StartYear && p.Term == previousKey.Term);
            var currentGrades = semesterGrades.Where(g => g.PeriodId == current.Id).ToList();
            var previousGrades = semesterGrades.Where(g => g.PeriodId == previous?.Id).ToList();
            if (currentGrades.Count > 0 && previousGrades.Count > 0)
                comparison = new DashboardComparisonDto(PeriodCalculator.Title(current.StartYear, current.Term),
                    PeriodCalculator.Title(previous!.StartYear, previous.Term),
                    Math.Round(currentGrades.Average(g => g.Grade) - previousGrades.Average(g => g.Grade), 2),
                    currentGrades.Sum(g => g.FeedbackCount), previousGrades.Sum(g => g.FeedbackCount));
        }
        var periodId = string.IsNullOrWhiteSpace(filter.PeriodId) ? null : filter.PeriodId;
        var relevantWorkloads = db.Workloads.AsNoTracking().Where(w => ids.Contains(w.PeriodId) ||
            ((periodId == null || w.PeriodId == periodId) && (filter.StartYear == null || w.PeriodRef!.StartYear == filter.StartYear)));
        var eligibleStudents = db.Students.Where(s => db.Users.Any(u => u.Id == s.Id && u.IsDeleted != true && u.IsBlocked != true));
        var assignments = from w in relevantWorkloads
                          join student in eligibleStudents on w.GroupId equals student.GroupId
                          select new { WorkloadId = w.Id, StudentId = student.Id, w.PeriodId };
        var possibleCounts = await assignments.GroupBy(a => a.PeriodId)
            .Select(g => new { PeriodId = g.Key, Count = g.LongCount() }).ToDictionaryAsync(g => g.PeriodId, g => g.Count, cancellationToken);
        var submittedCounts = await (from feedback in db.Feedbacks
                                     join assignment in assignments on new { feedback.WorkloadId, feedback.StudentId }
                                         equals new { assignment.WorkloadId, assignment.StudentId }
                                     select assignment.PeriodId)
            .GroupBy(id => id).Select(g => new { PeriodId = g.Key, Count = g.LongCount() })
            .ToDictionaryAsync(g => g.PeriodId, g => g.Count, cancellationToken);
        static DashboardCoverageDto Coverage(long submitted, long possible) =>
            new(submitted, possible, possible > 0 ? Math.Round(100d * submitted / possible, 1) : null);
        var selectedIds = await db.AcademicPeriods.Where(p =>
            (periodId == null || p.Id == periodId) && (filter.StartYear == null || p.StartYear == filter.StartYear))
            .Select(p => p.Id).ToListAsync(cancellationToken);
        var coverage = Coverage(selectedIds.Sum(id => submittedCounts.GetValueOrDefault(id)), selectedIds.Sum(id => possibleCounts.GetValueOrDefault(id)));
        var timeline = periods.Select(p => {
            var grades = semesterGrades.Where(g => g.PeriodId == p.Id).ToList();
            return new DashboardSemesterDto(p.Id, PeriodCalculator.Title(p.StartYear, p.Term), p.FeedbackCount,
                grades.Count > 0 ? grades.Average(g => g.Grade) : null, grades.Count,
                Coverage(submittedCounts.GetValueOrDefault(p.Id), possibleCounts.GetValueOrDefault(p.Id)));
        }).ToList();

        var ratings = await mapper.ProjectToRating(db.Disciplines.AsNoTracking().WhereHasWorkloadIn(filter), filter)
            .ToListAsync(cancellationToken);
        var rated = ratings.Where(r => r.Grade > 0).ToList();
        var low = rated.Where(r => r.Grade < 3.5).OrderBy(r => r.Grade).ThenBy(r => r.Name).ToList();
        var workloadFilter = filter.ToWorkloadExpression();
        var teacherCount = await db.Teachers.CountAsync(t => t.WorkloadsRefs!.AsQueryable().Where(workloadFilter).Any(), cancellationToken);
        var feedbackCount = await db.Feedbacks.Where(filter.ToFeedbackExpression()).CountAsync(cancellationToken);
        var distribution = new[] { "1–2", "2–3", "3–4", "4–5" }.Select((label, index) =>
            new DashboardBucketDto(label, rated.Count(r => Math.Min(3, (int)Math.Floor(r.Grade) - 1) == index))).ToList();

        return new DashboardDto(feedbackCount, teacherCount, ratings.Count,
            rated.Count > 0 ? rated.Average(r => (double)r.Grade) : null, low.Count, distribution,
            low.Take(5).ToList(), rated.Where(r => r.Grade >= 4).OrderByDescending(r => r.Grade).ThenBy(r => r.Name).Take(5).ToList(), timeline, coverage, rated.Sum(r => r.FeedbackCount), comparison);
    }
}
