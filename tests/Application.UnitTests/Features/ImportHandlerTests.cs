using Application.Common.Caching;
using Application.Common.DTOs;
using Application.Common.Exceptions;
using Application.Common.Periods;
using Application.Features.ImportFeatures;
using Application.Interfaces.DataManager.Repositories;
using Application.Interfaces.FileManager;
using Application.UnitTests.TestDoubles;
using Domain.Enums;
using NSubstitute;

namespace Application.UnitTests.Features;

public class ImportHandlerTests
{
    private readonly IRosterFileReader _reader = Substitute.For<IRosterFileReader>();
    private readonly IRosterFileWriter _writer = Substitute.For<IRosterFileWriter>();
    private readonly IImportRepository _repo = Substitute.For<IImportRepository>();
    private readonly FakeCacheService _cache = new();
    private readonly CancellationToken _ct = CancellationToken.None;

    private static readonly PeriodKey Period = new(2026, Term.Autumn);

    private ImportPlanner Planner => new(_reader, _repo);

    private void FileWith(params string[][] studentRows)
    {
        _reader.Read(Arg.Any<Stream>()).Returns([
            new RosterSheet(
                ImportFormat.StudentsSheet,
                ImportFormat.StudentColumns,
                studentRows.Select((r, i) => new RosterRow(i + 2, r)).ToList()),
        ]);
        _repo.LoadReferenceDataAsync(Period, _ct).Returns(new ImportReferenceData { PeriodId = "p1" });
    }

    private static T Request<T>(string? token = null, ImportMode mode = ImportMode.AddOnly) where T : ImportFileRequest, new() =>
        new() { Content = new MemoryStream([1]), FileName = "a.xlsx", Length = 1, StartYear = 2026, Term = Term.Autumn, PreviewToken = token, Mode = mode };

    [Fact]
    public async Task Preview_ReturnsPlanReport()
    {
        FileWith(["ИВТ-21", "Иванов", "Пётр", "", "210001"]);

        var report = await new PreviewImportQueryHandler(Planner).Handle(Request<PreviewImportQuery>(), _ct);

        Assert.Equal(("p1", false), (report.Period.Id, report.Period.WillBeCreated));
        Assert.Equal(1, report.Summary.Students.Create);
        Assert.Equal(1, report.Summary.Groups.Create);
        Assert.Null(report.CreatedGroupIds);
    }

    [Fact]
    public async Task Apply_WritesPlanAndInvalidatesCatalogs()
    {
        FileWith(["ИВТ-21", "Иванов", "Пётр", "", "210001"]);
        _repo.ApplyAsync(Arg.Any<ImportPlan>(), Arg.Any<ImportReferenceData>(), _ct)
            .Returns(new ImportApplyResult("p1", ["g1"], ["g1"]));

        var preview = await new PreviewImportQueryHandler(Planner).Handle(Request<PreviewImportQuery>(), _ct);
        var report = await new ApplyImportCommandHandler(Planner, _repo, _cache).Handle(Request<ApplyImportCommand>(preview.PreviewToken), _ct);

        Assert.Equal("p1", report.Period.Id);
        Assert.Equal(["g1"], report.CreatedGroupIds);
        Assert.Equal(["g1"], report.StudentGroupIds);
        Assert.Equal([CacheKeys.Teacher.ListTag, CacheKeys.Discipline.ListTag, CacheKeys.Workload.ListTag, CacheKeys.Feedback.Tag], _cache.RemovedTags);
    }

    [Fact]
    public async Task Apply_ReportsActualDeletionCount_WhenFeedbackAppearsDuringApply()
    {
        _reader.Read(Arg.Any<Stream>()).Returns([
            new RosterSheet(ImportFormat.WorkloadsSheet, ImportFormat.WorkloadColumns,
                [new RosterRow(2, ["Петров Пётр Петрович", "Физика", "Г-1"])]),
            new RosterSheet(ImportFormat.ScopeSheet, ImportFormat.ScopeColumns,
                [new RosterRow(2, [ImportFormat.ScopePeriod, "2026:Autumn"])]),
        ]);
        _repo.LoadReferenceDataAsync(Period, _ct).Returns(new ImportReferenceData
        {
            PeriodId = "p1", GroupIds = { ["Г-1"] = "g1" },
            TeacherIds = { ["Петров Пётр Петрович"] = "t1" },
            DisciplineIds = { ["Физика"] = "d1", ["Химия"] = "d2" },
            Workloads = { [new("t1", "d1", "g1")] = new("w1", 0), [new("t1", "d2", "g1")] = new("w2", 0) },
        });
        _repo.ApplyAsync(Arg.Any<ImportPlan>(), Arg.Any<ImportReferenceData>(), _ct)
            .Returns(new ImportApplyResult("p1", [], [], 0));
        var preview = await new PreviewImportQueryHandler(Planner).Handle(Request<PreviewImportQuery>(mode: ImportMode.SynchronizeWorkloads), _ct);
        Assert.Equal(1, preview.Summary.Workloads.Delete);
        var report = await new ApplyImportCommandHandler(Planner, _repo, _cache)
            .Handle(Request<ApplyImportCommand>(preview.PreviewToken, ImportMode.SynchronizeWorkloads), _ct);
        Assert.Equal(0, report.Summary.Workloads.Delete);
        Assert.Contains(report.Warnings, w => w.Message.Contains("во время импорта"));
    }

    [Fact]
    public async Task Apply_RequiresPreviewAndReturnsNewPlanWithoutWriting()
    {
        FileWith(["ИВТ-21", "Иванов", "Пётр", "", "210001"]);
        var handler = new ApplyImportCommandHandler(Planner, _repo, _cache);
        var missing = await Assert.ThrowsAsync<ImportPlanChangedException>(() => handler.Handle(Request<ApplyImportCommand>(), _ct));
        Assert.NotEmpty(missing.Report.PreviewToken);
        var token = missing.Report.PreviewToken;

        // The database now contains this account: confirmation must be repeated.
        _repo.LoadReferenceDataAsync(Period, _ct).Returns(new ImportReferenceData
        {
            PeriodId = "p1", GroupIds = { ["ИВТ-21"] = "g1" },
            Accounts = { ["210001"] = new(true, "ИВТ-21", "Иванов", "Пётр") },
        });
        var changed = await Assert.ThrowsAsync<ImportPlanChangedException>(() => handler.Handle(Request<ApplyImportCommand>(token), _ct));
        Assert.NotEqual(token, changed.Report.PreviewToken);
        Assert.Equal(0, changed.Report.Summary.Students.Create);
        await _repo.DidNotReceiveWithAnyArgs().ApplyAsync(default!, default!, default);
        Assert.Empty(_cache.RemovedTags);
    }

    [Fact]
    public async Task Apply_PlanWithErrors_ThrowsWithReportAndWritesNothing()
    {
        FileWith(["ИВТ-21", "", "Пётр", "", "210001"]);

        var ex = await Assert.ThrowsAsync<ImportValidationException>(() =>
            new ApplyImportCommandHandler(Planner, _repo, _cache).Handle(Request<ApplyImportCommand>(), _ct));

        Assert.Single(ex.Report.Errors);
        await _repo.DidNotReceiveWithAnyArgs().ApplyAsync(default!, default!, default);
        Assert.Empty(_cache.RemovedTags);
    }

    [Fact]
    public async Task Template_ExamplesAreOutsideImportSheets()
    {
        IReadOnlyList<SpreadsheetSheet>? sheets = null;
        _writer.Write(Arg.Do<IReadOnlyList<SpreadsheetSheet>>(s => sheets = s)).Returns([9]);

        var file = await new GetImportTemplateQueryHandler(_writer).Handle(new GetImportTemplateQuery(), _ct);

        Assert.Equal([9], file);
        Assert.Equal([ImportFormat.StudentsSheet, ImportFormat.WorkloadsSheet, "Инструкция"], sheets!.Select(s => s.Name));
        Assert.All(sheets!.Take(2), s => Assert.Empty(s.Rows));
        Assert.NotEmpty(sheets![2].Rows);

        // Пустой шаблон не должен импортировать примеры из инструкции.
        var plan = ImportPlanBuilder.Build(
            sheets!.Select(s => new RosterSheet(s.Name, s.Headers, s.Rows.Select((r, i) => new RosterRow(i + 2, r)).ToList())).ToList(),
            Period,
            new ImportReferenceData());
        Assert.NotEmpty(plan.Errors);
        Assert.Empty(plan.StudentsToCreate);
        Assert.Empty(plan.WorkloadsToCreate);
    }

    [Fact]
    public async Task Groups_ReturnsRepositoryList()
    {
        List<StudentGroupDto> groups = [new() { Id = "g1", Name = "ИВТ-21" }];
        _repo.GetGroupsAsync(_ct).Returns(groups);

        Assert.Same(groups, await new GetImportGroupsQueryHandler(_repo).Handle(new GetImportGroupsQuery(), _ct));
    }

    [Fact]
    public async Task Credentials_SheetPerGroup_PasswordOnlyUntilChanged()
    {
        _repo.GetStudentCredentialsAsync(null, _ct).Returns([
            new StudentCredentialDto { GroupName = "ИВТ-22", FullName = "Яковлев", Login = "3", MustChangePassword = true },
            new StudentCredentialDto { GroupName = "ИВТ-21", FullName = "Петров", Login = "2", MustChangePassword = false },
            new StudentCredentialDto { GroupName = "ИВТ-21", FullName = "Иванов", Login = "1", MustChangePassword = true },
        ]);
        IReadOnlyList<SpreadsheetSheet>? sheets = null;
        _writer.Write(Arg.Do<IReadOnlyList<SpreadsheetSheet>>(s => sheets = s)).Returns([7]);

        var file = await new GetCredentialsQueryHandler(_repo, _writer).Handle(new GetCredentialsQuery([]), _ct);

        Assert.Equal([7], file);
        Assert.Equal(["ИВТ-21", "ИВТ-22"], sheets!.Select(s => s.Name));
        Assert.Equal(
            [
                ["ИВТ-21", "Иванов", "1", ImportFormat.TemporaryPassword("1"), ""],
                ["ИВТ-21", "Петров", "2", "", "да"],
            ],
            sheets![0].Rows);
    }

    [Fact]
    public async Task Credentials_PassesSelectedGroups()
    {
        string[] ids = ["g1"];
        _repo.GetStudentCredentialsAsync(ids, _ct).Returns([]);

        await new GetCredentialsQueryHandler(_repo, _writer).Handle(new GetCredentialsQuery(ids), _ct);

        await _repo.Received(1).GetStudentCredentialsAsync(ids, _ct);
    }

    [Fact]
    public async Task Export_SortsRows_AndPassesTeacherFilter()
    {
        string[] ids = ["t1"];
        _repo.GetPeriodExportAsync(Period, ids, _ct).Returns(new PeriodExport(
            [new("Яковлев Яков Яковлевич", "Физика", "ИВТ-21"), new("Андреев Андрей Андреевич", "Физика", "ИВТ-22")],
            [new("ИВТ-22", "Борисов", "Борис", null, "2"), new("ИВТ-21", "Петров", "Пётр", "Петрович", "1")]));
        IReadOnlyList<SpreadsheetSheet>? sheets = null;
        _writer.Write(Arg.Do<IReadOnlyList<SpreadsheetSheet>>(s => sheets = s)).Returns([7]);

        var file = await new ExportPeriodQueryHandler(_repo, _writer).Handle(new ExportPeriodQuery(2026, Term.Autumn, ids), _ct);

        Assert.Equal([7], file);
        Assert.Equal([ImportFormat.StudentsSheet, ImportFormat.WorkloadsSheet, ImportFormat.ScopeSheet], sheets!.Select(s => s.Name));
        Assert.True(sheets![2].Hidden);
        Assert.Equal([[ImportFormat.ScopePeriod, "2026:Autumn"], [ImportFormat.ScopeTeacher, "t1"], [ImportFormat.ScopeTeacherCount, "1"]], sheets[2].Rows);
        Assert.Equal(
            [["ИВТ-21", "Петров", "Пётр", "Петрович", "1"], ["ИВТ-22", "Борисов", "Борис", "", "2"]],
            sheets[0].Rows);
        Assert.Equal(
            [["Андреев Андрей Андреевич", "Физика", "ИВТ-22", "", ""], ["Яковлев Яков Яковлевич", "Физика", "ИВТ-21", "", ""]],
            sheets[1].Rows);
    }

    [Fact]
    public async Task Export_MissingPeriod_IsNotFound()
    {
        _repo.GetPeriodExportAsync(Period, null, _ct).Returns((PeriodExport?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            new ExportPeriodQueryHandler(_repo, _writer).Handle(new ExportPeriodQuery(2026, Term.Autumn, []), _ct));
    }

    [Fact]
    public async Task PeriodTeachers_AreLoadedForPeriod()
    {
        _repo.GetPeriodTeachersAsync(Period, _ct).Returns([new TeacherDto { Id = "t1" }]);

        var teachers = await new GetPeriodTeachersQueryHandler(_repo).Handle(new GetPeriodTeachersQuery(2026, Term.Autumn), _ct);

        Assert.Equal("t1", Assert.Single(teachers).Id);
    }

    [Fact]
    public void TemporaryPassword_IsDerivedFromLogin()
    {
        Assert.Equal("Vsoko210001", ImportFormat.TemporaryPassword("210001"));
    }
}
