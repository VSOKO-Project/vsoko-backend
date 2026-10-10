using Application.Common.Caching;
using Application.Common.DTOs;
using Application.Common.Periods;
using Application.Common.Results;
using Application.Features.DisciplineFeatures.Query;
using Application.Features.ReportFeatures.Query;
using Application.Features.SummariesFeatures.Query;
using Application.Features.TeachersFeatures.Query;
using Application.Features.WorkloadFeatures;
using Application.Features.WorkloadFeatures.Query;
using Application.Interfaces.AIManager;
using Application.Interfaces.DataManager.Repositories;
using Application.Interfaces.FileManager;
using Application.UnitTests.TestDoubles;
using NSubstitute;

namespace Application.UnitTests.Features;

/// <summary>Чтение преподавателей, дисциплин, нагрузки, сводок и отчёта.</summary>
public class CatalogHandlerTests
{
    private readonly ITeacherRepository _teachers = Substitute.For<ITeacherRepository>();
    private readonly IDisciplineRepository _disciplines = Substitute.For<IDisciplineRepository>();
    private readonly IWorkloadRepository _workloads = Substitute.For<IWorkloadRepository>();
    private readonly IFeedbackRepository _feedback = Substitute.For<IFeedbackRepository>();
    private readonly IFeedbackSummarizer _summarizer = Substitute.For<IFeedbackSummarizer>();
    private readonly FakeCacheService _cache = new();
    private readonly CancellationToken _ct = CancellationToken.None;

    private static readonly PeriodFilter Period = new("p1", 2026);

    [Theory]
    [InlineData(null, "")]
    [InlineData("анна", "анна")]
    public async Task Teachers_GetAll(string? query, string expectedQuery)
    {
        var page = new PagedResultDto<TeacherDto>();
        _teachers.GetAllAsync(2, expectedQuery, 5, Arg.Any<CancellationToken>()).Returns(page);

        var result = await new GetAllTeachersRequestHandler(_teachers, _cache)
            .Handle(new GetAllTeachersRequest { Page = 2, PageSize = 5, Query = query }, _ct);

        Assert.Same(page, result);
        Assert.Equal((CacheKeys.Teacher.GetPaged(2, 5, expectedQuery), CacheKeys.Teacher.ListTag), Assert.Single(_cache.Requests));
    }

    [Fact]
    public async Task Teachers_Rating_PassesPeriod()
    {
        var page = new PagedResultDto<RatingDto>();
        _teachers.GetRatingAsync(1, "", 10, Period, Arg.Any<CancellationToken>()).Returns(page);

        var result = await new GetTeachersRatingRequestHandler(_teachers, _cache)
            .Handle(new GetTeachersRatingRequest { PeriodId = "p1", StartYear = 2026 }, _ct);

        Assert.Same(page, result);
        Assert.Equal(CacheKeys.Teacher.GetRating(1, 10, "", Period), Assert.Single(_cache.Requests).Key);
    }

    [Theory]
    [InlineData(null, "")]
    [InlineData("физ", "физ")]
    public async Task Disciplines_GetAll(string? query, string expectedQuery)
    {
        var page = new PagedResultDto<DisciplineDto>();
        _disciplines.GetAllAsync(1, expectedQuery, 10, Arg.Any<CancellationToken>()).Returns(page);

        var result = await new GetAllDisciplinesRequestHandler(_disciplines, _cache)
            .Handle(new GetAllDisciplinesRequest { Query = query }, _ct);

        Assert.Same(page, result);
        Assert.Equal((CacheKeys.Discipline.GetPaged(1, 10, expectedQuery), CacheKeys.Discipline.ListTag), Assert.Single(_cache.Requests));
    }

    [Fact]
    public async Task Disciplines_Rating_PassesPeriod()
    {
        var page = new PagedResultDto<RatingDto>();
        _disciplines.GetRatingAsync(3, "x", 7, Period, Arg.Any<CancellationToken>()).Returns(page);

        var result = await new GetDisciplineRatingRequestHandler(_disciplines, _cache)
            .Handle(new GetDisciplineRatingRequest { Page = 3, PageSize = 7, Query = "x", PeriodId = "p1", StartYear = 2026 }, _ct);

        Assert.Same(page, result);
        Assert.Equal(CacheKeys.Discipline.GetRating(3, 7, "x", Period), Assert.Single(_cache.Requests).Key);
    }

    [Fact]
    public async Task TeacherSummary_SummarizesCommentsForPeriod()
    {
        List<string> comments = ["a", "b"];
        _teachers.GetByIdAsync("t1", Arg.Any<CancellationToken>())
            .Returns(new TeacherDto { Id = "t1", Surname = "Петрова", Name = "Анна", Patronymic = "Ивановна" });
        _feedback.GetCommentByTeacherId("t1", Period, Arg.Any<CancellationToken>()).Returns(comments);
        _summarizer.SummarizeTeacherAsync("Петрова Анна Ивановна", comments, Arg.Any<CancellationToken>()).Returns("summary");

        var result = await new GetTeacherSummaryByIdQueryHandler(_teachers, _feedback, _summarizer, _cache)
            .Handle(new GetTeacherSummaryByIdQuery("t1", "p1", 2026), _ct);

        Assert.Equal("summary", result);
        Assert.Equal((CacheKeys.Teacher.GetSummary("t1", Period), CacheKeys.Teacher.ListTag), Assert.Single(_cache.Requests));
    }

    [Fact]
    public async Task DisciplineSummary_SummarizesCommentsForPeriod()
    {
        List<string> comments = ["a"];
        _disciplines.GetByIdAsync("d1", Arg.Any<CancellationToken>()).Returns(new DisciplineDto { Id = "d1", Name = "Физика" });
        _feedback.GetCommentByDisciplineId("d1", PeriodFilter.All, Arg.Any<CancellationToken>()).Returns(comments);
        _summarizer.SummarizeDisciplineAsync("Физика", comments, Arg.Any<CancellationToken>()).Returns("summary");

        var result = await new GetDisciplineSummaryByIdQueryHandler(_disciplines, _feedback, _summarizer, _cache)
            .Handle(new GetDisciplineSummaryByIdQuery("d1"), _ct);

        Assert.Equal("summary", result);
        Assert.Equal((CacheKeys.Discipline.GetSummary("d1", PeriodFilter.All), CacheKeys.Discipline.ListTag), Assert.Single(_cache.Requests));
    }

    [Fact]
    public async Task Report_PassesPeriodFilter()
    {
        var reports = Substitute.For<IReportService>();
        reports.GenerateReportAsync(Period, _ct).Returns([1, 2]);

        Assert.Equal([1, 2], await new ReportQueryHandler(reports).Handle(new ReportQuery("p1", 2026), _ct));
    }

    [Fact]
    public async Task Workloads_StudentWithoutPeriod_GetsOpenPeriodOnly()
    {
        var page = new PagedResultDto<WorkloadDto>();
        _workloads.GetPagedWorkload(1, "", 10, null, true, Arg.Any<CancellationToken>()).Returns(page);

        var result = await new GetAllWorkloadRequestHandler(_workloads, _cache, FakeUserContext.Student("s1", "g1"))
            .Handle(new GetAllWorkloadRequest { PeriodId = " " }, _ct);

        Assert.Same(page, result);
        Assert.Equal((CacheKeys.Workload.GetPagedForStudent(1, 10, "g1", "s1", "", "open"), CacheKeys.Workload.ListTag),
            Assert.Single(_cache.Requests));
    }

    [Fact]
    public async Task Workloads_StudentWithPeriod_GetsThatPeriod()
    {
        await new GetAllWorkloadRequestHandler(_workloads, _cache, FakeUserContext.Student("s1", "g1"))
            .Handle(new GetAllWorkloadRequest { PeriodId = "p1", Query = "физ" }, _ct);

        await _workloads.Received(1).GetPagedWorkload(1, "физ", 10, "p1", false, Arg.Any<CancellationToken>());
        Assert.Equal(CacheKeys.Workload.GetPagedForStudent(1, 10, "g1", "s1", "физ", "p1"), Assert.Single(_cache.Requests).Key);
    }

    [Fact]
    public async Task Workloads_AdminWithoutPeriod_GetsAllTime()
    {
        await new GetAllWorkloadRequestHandler(_workloads, _cache, FakeUserContext.Admin())
            .Handle(new GetAllWorkloadRequest(), _ct);

        await _workloads.Received(1).GetPagedWorkload(1, "", 10, null, false, Arg.Any<CancellationToken>());
        Assert.Equal(CacheKeys.Workload.GetPaged(1, 10, "", "all"), Assert.Single(_cache.Requests).Key);
    }

    [Fact]
    public async Task WorkloadById_Student_UsesPersonalKey()
    {
        var dto = new WorkloadDto { Id = "w1" };
        _workloads.GetWorkloadById("w1", Arg.Any<CancellationToken>()).Returns(dto);

        var result = await new GetWorkloadByIdQueryHandler(_workloads, _cache, FakeUserContext.Student("s1", "g1"))
            .Handle(new GetWorkloadByIdQuery("w1"), _ct);

        Assert.Same(dto, result);
        Assert.Equal((CacheKeys.Workload.GetByIdForStudent("w1", "g1", "s1"), CacheKeys.Workload.ListTag),
            Assert.Single(_cache.Requests));
    }

    [Fact]
    public async Task WorkloadById_Admin_UsesSharedKey()
    {
        await new GetWorkloadByIdQueryHandler(_workloads, _cache, FakeUserContext.Admin())
            .Handle(new GetWorkloadByIdQuery("w1"), _ct);

        Assert.Equal(CacheKeys.Workload.GetById("w1"), Assert.Single(_cache.Requests).Key);
    }
}
