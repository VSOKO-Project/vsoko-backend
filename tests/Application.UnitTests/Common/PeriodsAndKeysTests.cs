using Application.Common.Caching;
using Application.Common.DTOs;
using Application.Common.Periods;
using Domain.Entities;
using Domain.Enums;

namespace Application.UnitTests.Common;

public class PeriodsAndKeysTests
{
    private static readonly AcademicPeriod P2025 = new() { Id = "p25", StartYear = 2025, Term = Term.Spring };
    private static readonly AcademicPeriod P2026 = new() { Id = "p26", StartYear = 2026, Term = Term.Autumn };

    private static Workload WorkloadIn(AcademicPeriod p) => new() { PeriodId = p.Id, PeriodRef = p };

    [Theory]
    [InlineData(null, null, true)]
    [InlineData(" ", null, true)]
    [InlineData("p26", null, false)]
    [InlineData(null, 2026, false)]
    public void IsEmpty(string? periodId, int? startYear, bool expected)
    {
        Assert.Equal(expected, new PeriodFilter(periodId, startYear).IsEmpty);
    }

    [Fact]
    public void All_IsEmptyAndHasStableCacheKey()
    {
        Assert.True(PeriodFilter.All.IsEmpty);
        Assert.Equal("per:y", PeriodFilter.All.CacheKey);
        Assert.Equal("perp1:y2026", new PeriodFilter("p1", 2026).CacheKey);
    }

    [Theory]
    [InlineData(null, null, true, true)]
    [InlineData("", null, true, true)]
    [InlineData("p26", null, false, true)]
    [InlineData(null, 2025, true, false)]
    [InlineData("p26", 2025, false, false)]
    public void Expressions_FilterByPeriodAndYear(string? periodId, int? startYear, bool matches2025, bool matches2026)
    {
        var filter = new PeriodFilter(periodId, startYear);
        var w25 = WorkloadIn(P2025);
        var w26 = WorkloadIn(P2026);

        var workload = filter.ToWorkloadExpression().Compile();
        Assert.Equal(matches2025, workload(w25));
        Assert.Equal(matches2026, workload(w26));

        var feedback = filter.ToFeedbackExpression().Compile();
        Assert.Equal(matches2025, feedback(new Feedback { WorkloadRef = w25 }));
        Assert.Equal(matches2026, feedback(new Feedback { WorkloadRef = w26 }));

        var criteria = filter.ToCriteriaFeedbackExpression().Compile();
        Assert.Equal(matches2025, criteria(new CriteriaFeedback { FeedbackRef = new Feedback { WorkloadRef = w25 } }));
        Assert.Equal(matches2026, criteria(new CriteriaFeedback { FeedbackRef = new Feedback { WorkloadRef = w26 } }));
    }

    [Theory]
    [InlineData(2026, Term.Autumn, "2026/27, осенний семестр")]
    [InlineData(2099, Term.Spring, "2099/00, весенний семестр")]
    public void Titles(int startYear, Term term, string expected)
    {
        Assert.Equal(expected, PeriodCalculator.Title(startYear, term));
        Assert.Equal(expected, new PeriodDto { StartYear = startYear, Term = term }.Title);
        Assert.Equal(expected, new SuggestedPeriodDto { StartYear = startYear, Term = term }.Title);
        Assert.Equal(expected, new ImportPeriodDto { StartYear = startYear, Term = term }.Title);
    }

    [Fact]
    public void TeacherDto_FullName_IsTrimmed()
    {
        Assert.Equal("Иванов Иван Иванович", new TeacherDto { Surname = "Иванов", Name = "Иван", Patronymic = "Иванович" }.FullName);
        Assert.Equal("Иванов Иван", new TeacherDto { Surname = "Иванов", Name = "Иван", Patronymic = "" }.FullName);
    }

    [Fact]
    public void CacheKeys_AreDistinctPerParameters()
    {
        var period = new PeriodFilter("p1", 2026);

        Assert.Equal("workload:id:w1", CacheKeys.Workload.GetById("w1"));
        Assert.Equal("workload:id:w1:group:g1:student:s1", CacheKeys.Workload.GetByIdForStudent("w1", "g1", "s1"));
        Assert.Equal("workload:list:p1:s10:perall:qmath", CacheKeys.Workload.GetPaged(1, 10, "math", "all"));
        Assert.Equal("workload:list:p1:s10:group:g1:student:s1:peropen:q", CacheKeys.Workload.GetPagedForStudent(1, 10, "g1", "s1", "", "open"));
        Assert.Equal("workload-list", CacheKeys.Workload.ListTag);

        Assert.Equal("criteria:all", CacheKeys.Criteria.All);
        Assert.Equal("criteria:id:c1", CacheKeys.Criteria.GetById("c1"));
        Assert.Equal("criteria-list", CacheKeys.Criteria.ListTag);

        Assert.Equal("discipline-list", CacheKeys.Discipline.ListTag);
        Assert.Equal("discipline:list:p2:s5:qx", CacheKeys.Discipline.GetPaged(2, 5, "x"));
        Assert.Equal("discipline:rating:p2:s5:perp1:y2026:qx", CacheKeys.Discipline.GetRating(2, 5, "x", period));
        Assert.Equal("discipline:summary:d1:perp1:y2026", CacheKeys.Discipline.GetSummary("d1", period));

        Assert.Equal("feedback", CacheKeys.Feedback.Tag);
        Assert.Equal("feedback:student:s1", CacheKeys.Feedback.GetByStudent("s1"));
        Assert.Equal("feedback:id:f1:student:s1", CacheKeys.Feedback.GetById("f1", "s1"));
        Assert.Equal("feedback:list:p1:s10:dd1:tt1:ww1", CacheKeys.Feedback.GetPaged(1, 10, "d1", "t1", "w1"));
        Assert.Equal("feedback:list:p1:s10:student:s1:d:t:w", CacheKeys.Feedback.GetPagedForStudent(1, 10, "s1"));

        Assert.Equal("teacher-list", CacheKeys.Teacher.ListTag);
        Assert.Equal("teacher:list:p1:s10:q", CacheKeys.Teacher.GetPaged(1, 10, ""));
        Assert.Equal("teacher:rating:p1:s10:perp1:y2026:qx", CacheKeys.Teacher.GetRating(1, 10, "x", period));
        Assert.Equal("teacher:summary:t1:perp1:y2026", CacheKeys.Teacher.GetSummary("t1", period));
    }
}
