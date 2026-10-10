using Application.Common.Periods;
using Application.Features.ImportFeatures;
using Application.Interfaces.FileManager;
using Domain.Enums;

namespace Application.UnitTests;

public class ImportPlanBuilderTests
{
    private static readonly PeriodKey Period = new(2026, Term.Spring);

    private static RosterSheet Students(params string[][] rows) =>
        Sheet(ImportFormat.StudentsSheet, ImportFormat.StudentColumns, rows);

    private static RosterSheet Workloads(params string[][] rows) =>
        Sheet(ImportFormat.WorkloadsSheet, ImportFormat.WorkloadColumns, rows);

    private static RosterSheet Sheet(string name, string[] headers, string[][] rows) =>
        new(name, headers, rows.Select((r, i) => new RosterRow(i + 2, r)).ToList());

    [Fact]
    public void DuplicateStudentRow_IsWarningAndSkipped()
    {
        var plan = ImportPlanBuilder.Build(
            [Students(
                ["ИВТ-21", "Иванов", "Пётр", "Сергеевич", "210001"],
                [" ИВТ-21 ", "Иванов", "Пётр", "Сергеевич", "210001"])],
            Period,
            new ImportReferenceData());

        Assert.Empty(plan.Errors);
        Assert.Single(plan.StudentsToCreate);
        var warning = Assert.Single(plan.Warnings);
        Assert.Equal(3, warning.Row);
    }

    [Fact]
    public void SameStudentNumberWithDifferentData_IsError()
    {
        var plan = ImportPlanBuilder.Build(
            [Students(
                ["ИВТ-21", "Иванов", "Пётр", "", "210001"],
                ["ИВТ-21", "Петров", "Иван", "", "210001"])],
            Period,
            new ImportReferenceData());

        var error = Assert.Single(plan.Errors);
        Assert.Equal(3, error.Row);
    }

    [Fact]
    public void DuplicateWorkloadRow_IsWarningAndSkipped()
    {
        var refs = new ImportReferenceData { GroupIds = { ["ИВТ-21"] = "g1" } };

        var plan = ImportPlanBuilder.Build(
            [Workloads(
                ["Петрова Анна Ивановна", "Математический анализ", "ИВТ-21"],
                ["ПЕТРОВА  анна ивановна", "математический анализ", "ивт-21"])],
            Period,
            refs);

        Assert.Empty(plan.Errors);
        Assert.Single(plan.WorkloadsToCreate);
        Assert.Single(plan.Warnings);
    }

    [Fact]
    public void GroupOnlyOnStudentsSheet_IsCreatedAndUsableInWorkloads()
    {
        var plan = ImportPlanBuilder.Build(
            [
                Students(["ИВТ-12", "Иванов", "Пётр", "", "120001"]),
                Workloads(["Петрова Анна Ивановна", "Физика", "ИВТ-12"]),
            ],
            Period,
            new ImportReferenceData());

        Assert.Empty(plan.Errors);
        Assert.Equal(["ИВТ-12"], plan.GroupsToCreate);
        Assert.Equal(["ИВТ-12"], plan.StudentGroupNames);
        Assert.Single(plan.WorkloadsToCreate);
        Assert.Single(plan.TeachersToCreate);
        Assert.Equal(["Физика"], plan.DisciplinesToCreate);
    }

    [Fact]
    public void UnknownGroupInWorkloads_IsError()
    {
        var plan = ImportPlanBuilder.Build(
            [Workloads(["Петрова Анна Ивановна", "Физика", "ИВТ-12"])],
            Period,
            new ImportReferenceData());

        var error = Assert.Single(plan.Errors);
        Assert.Equal(2, error.Row);
        Assert.Contains("ИВТ-12", error.Message);
        Assert.Empty(plan.GroupsToCreate);
    }

    [Fact]
    public void ExistingWorkload_IsSkipped()
    {
        var refs = new ImportReferenceData
        {
            PeriodId = "p1",
            GroupIds = { ["ИВТ-21"] = "g1" },
            TeacherIds = { ["Петрова Анна Ивановна"] = "t1" },
            DisciplineIds = { ["Физика"] = "d1" },
            Workloads = { new WorkloadKey("t1", "d1", "g1") },
        };

        var plan = ImportPlanBuilder.Build(
            [Workloads(["петрова анна ивановна", "ФИЗИКА", "ИВТ-21"])],
            Period,
            refs);

        Assert.Empty(plan.Errors);
        Assert.Empty(plan.WorkloadsToCreate);
        Assert.Empty(plan.TeachersToCreate);
        Assert.Empty(plan.DisciplinesToCreate);
        Assert.Equal(1, plan.ExistingWorkloads);
        Assert.Equal(1, plan.ExistingTeachers);
        Assert.Equal(1, plan.ExistingDisciplines);
        Assert.Equal(1, plan.ExistingGroups);
    }

    [Fact]
    public void ExistingWorkloadInAnotherPeriod_IsCreated()
    {
        // Справочник нагрузки загружается только для выбранного периода.
        var refs = new ImportReferenceData
        {
            GroupIds = { ["ИВТ-21"] = "g1" },
            TeacherIds = { ["Петрова Анна Ивановна"] = "t1" },
            DisciplineIds = { ["Физика"] = "d1" },
        };

        var plan = ImportPlanBuilder.Build(
            [Workloads(["Петрова Анна Ивановна", "Физика", "ИВТ-21"])],
            Period,
            refs);

        Assert.Single(plan.WorkloadsToCreate);
        Assert.True(plan.ToReport().Period.WillBeCreated);
    }

    [Theory]
    [InlineData("Петрова Анна")]
    [InlineData("Петрова")]
    public void TeacherFioWithLessThanThreeWords_IsError(string fio)
    {
        var refs = new ImportReferenceData { GroupIds = { ["ИВТ-21"] = "g1" } };

        var plan = ImportPlanBuilder.Build([Workloads([fio, "Физика", "ИВТ-21"])], Period, refs);

        var error = Assert.Single(plan.Errors);
        Assert.Contains(fio, error.Message);
    }

    [Fact]
    public void TeacherFio_IsTitleCased()
    {
        var teacher = ImportPlanBuilder.ParseTeacher("  петрова-водкина   АННА ивановна ");

        Assert.Equal(new PlannedTeacher("Петрова-Водкина", "Анна", "Ивановна"), teacher);
    }

    [Fact]
    public void ExistingStudentInAnotherGroup_IsWarningAndNotMoved()
    {
        var refs = new ImportReferenceData
        {
            GroupIds = { ["ИВТ-21"] = "g1", ["ИВТ-22"] = "g2" },
            Accounts = { ["210001"] = new ExistingAccount(true, "ИВТ-22") },
        };

        var plan = ImportPlanBuilder.Build(
            [Students(["ИВТ-21", "Иванов", "Пётр", "", "210001"])],
            Period,
            refs);

        Assert.Empty(plan.Errors);
        Assert.Empty(plan.StudentsToCreate);
        Assert.Equal(1, plan.ExistingStudents);
        Assert.Contains("ИВТ-22", Assert.Single(plan.Warnings).Message);
    }

    [Fact]
    public void MissingHeaderAndEmptyRequiredCell_AreErrors()
    {
        var noNumberColumn = Sheet(ImportFormat.StudentsSheet, ["группа", "ФАМИЛИЯ", "Имя"], [["ИВТ-21", "Иванов", "Пётр"]]);
        var plan = ImportPlanBuilder.Build([noNumberColumn], Period, new ImportReferenceData());

        var headerError = Assert.Single(plan.Errors);
        Assert.Equal(1, headerError.Row);

        plan = ImportPlanBuilder.Build(
            [Students(["ИВТ-21", "", "Пётр", "", "210001"])],
            Period,
            new ImportReferenceData());

        Assert.Equal(2, Assert.Single(plan.Errors).Row);
    }

    [Fact]
    public void HeadersMatchIgnoringCaseSpacesAndOrder()
    {
        var sheet = Sheet(
            "  студенты ",
            ["Номер зачетки", "  ИМЯ", "фамилия", "Г р у п п а"],
            [["210001", "Пётр", "Иванов", "ИВТ-21"]]);

        var plan = ImportPlanBuilder.Build([sheet], Period, new ImportReferenceData());

        Assert.Empty(plan.Errors);
        Assert.Equal(new PlannedStudent("ИВТ-21", "Иванов", "Пётр", null, "210001"), Assert.Single(plan.StudentsToCreate));
    }

    [Fact]
    public void NoKnownSheets_IsError()
    {
        var plan = ImportPlanBuilder.Build([Sheet("Лист1", ["a"], [["1"]])], Period, new ImportReferenceData());

        Assert.Equal(0, Assert.Single(plan.Errors).Row);
    }

    [Fact]
    public void SheetsWithoutRows_IsError()
    {
        var plan = ImportPlanBuilder.Build([Students(), Workloads()], Period, new ImportReferenceData());

        Assert.Contains("нет строк", Assert.Single(plan.Errors).Message);
    }

    [Fact]
    public void EmptyRows_AreIgnored_ShortRowsArePadded()
    {
        var plan = ImportPlanBuilder.Build(
            [Students(["", " ", "", "", ""], ["ИВТ-21", "Иванов", "Пётр"])],
            Period,
            new ImportReferenceData());

        var error = Assert.Single(plan.Errors);
        Assert.Equal(3, error.Row);
        Assert.Contains(ImportFormat.StudentNumber, error.Message);
    }

    [Fact]
    public void TooManyRows_IsError()
    {
        var rows = Enumerable.Range(0, ImportFormat.MaxRowsPerSheet + 1).Select(_ => new[] { "x" }).ToArray();

        var plan = ImportPlanBuilder.Build([Workloads(rows)], Period, new ImportReferenceData());

        Assert.Contains(ImportFormat.MaxRowsPerSheet.ToString(), Assert.Single(plan.Errors).Message);
    }

    [Fact]
    public void TooLongValuesAndBadLogin_AreErrors()
    {
        var plan = ImportPlanBuilder.Build(
            [Students([new string('Г', 21), new string('Ф', 71), new string('И', 61), new string('О', 71), "№ 1"])],
            Period,
            new ImportReferenceData());

        Assert.Equal(5, plan.Errors.Count);
        Assert.All(plan.Errors, e => Assert.Equal(2, e.Row));
        Assert.Contains(plan.Errors, e => e.Message.Contains("латинские"));
    }

    [Fact]
    public void LoginOfEmployee_IsError()
    {
        var refs = new ImportReferenceData { Accounts = { ["admin"] = new ExistingAccount(false, null) } };

        var plan = ImportPlanBuilder.Build([Students(["ИВТ-21", "Иванов", "Пётр", "", "admin"])], Period, refs);

        Assert.Contains("занят", Assert.Single(plan.Errors).Message);
    }

    [Fact]
    public void ExistingStudentInSameGroup_IsCountedSilently()
    {
        var refs = new ImportReferenceData
        {
            GroupIds = { ["ИВТ-21"] = "g1" },
            Accounts = { ["210001"] = new ExistingAccount(true, "ивт-21") },
        };

        var plan = ImportPlanBuilder.Build(
            [Students(["ивт-21", "Иванов", "Пётр", "", "210001"], ["ИВТ-21", "Петров", "Иван", "", "210002"])],
            Period,
            refs);

        Assert.Empty(plan.Errors);
        Assert.Empty(plan.Warnings);
        Assert.Equal(1, plan.ExistingStudents);
        Assert.Equal(1, plan.ExistingGroups);
        Assert.Empty(plan.GroupsToCreate);
        Assert.Equal("ИВТ-21", Assert.Single(plan.StudentsToCreate).GroupName);
        Assert.Equal(["ИВТ-21"], plan.StudentGroupNames);
    }

    [Fact]
    public void WorkloadRowErrors_AreReported()
    {
        var refs = new ImportReferenceData { GroupIds = { ["ИВТ-21"] = "g1" } };
        var longPart = new string('а', 256);

        var plan = ImportPlanBuilder.Build(
            [Workloads(
                ["", "", ""],
                ["", "Физика", "ИВТ-21"],
                [$"{longPart} Анна Ивановна", "Физика", "ИВТ-21"],
                ["Петрова Анна Ивановна", new string('Д', 101), "ИВТ-21"])],
            Period,
            refs);

        Assert.Equal([3, 4, 5], plan.Errors.Select(e => e.Row));
        Assert.Empty(plan.WorkloadsToCreate);
    }

    [Fact]
    public void MissingWorkloadColumn_IsError()
    {
        var plan = ImportPlanBuilder.Build(
            [Sheet(ImportFormat.WorkloadsSheet, [ImportFormat.TeacherFio, ImportFormat.Group], [["Петрова Анна Ивановна", "ИВТ-21"]])],
            Period,
            new ImportReferenceData());

        Assert.Contains(ImportFormat.Discipline, Assert.Single(plan.Errors).Message);
    }

    [Fact]
    public void ExistingTeacherAndDiscipline_UseDatabaseSpelling()
    {
        var refs = new ImportReferenceData
        {
            GroupIds = { ["ИВТ-21"] = "g1", ["ИВТ-22"] = "g2" },
            TeacherIds = { ["Петрова Анна Ивановна"] = "t1" },
            DisciplineIds = { ["Физика"] = "d1" },
        };

        var plan = ImportPlanBuilder.Build(
            [Workloads(
                ["петрова анна ивановна", "физика", "ивт-21"],
                ["Петрова Анна Ивановна", "ФИЗИКА", "ИВТ-22"],
                ["Иванов Пётр Сергеевич", "Химия", "ИВТ-21"],
                ["Иванов Пётр Сергеевич", "химия", "ИВТ-22"])],
            Period,
            refs);

        Assert.Empty(plan.Errors);
        Assert.Equal(
            [
                new PlannedWorkload("Петрова Анна Ивановна", "Физика", "ИВТ-21"),
                new PlannedWorkload("Петрова Анна Ивановна", "Физика", "ИВТ-22"),
                new PlannedWorkload("Иванов Пётр Сергеевич", "Химия", "ИВТ-21"),
                new PlannedWorkload("Иванов Пётр Сергеевич", "Химия", "ИВТ-22"),
            ],
            plan.WorkloadsToCreate);
        Assert.Equal("Иванов Пётр Сергеевич", Assert.Single(plan.TeachersToCreate).Key);
        Assert.Equal(["Химия"], plan.DisciplinesToCreate);
        Assert.Equal(1, plan.ExistingTeachers);
        Assert.Equal(1, plan.ExistingDisciplines);
        Assert.Equal(2, plan.ExistingGroups);
    }

    [Fact]
    public void ToReport_SummarizesPlan()
    {
        var refs = new ImportReferenceData { PeriodId = "p1" };

        var report = ImportPlanBuilder.Build(
            [
                Students(["ИВТ-21", "Иванов", "Пётр", "Сергеевич", "210001"], ["ИВТ-21", "Иванов", "Пётр", "Сергеевич", "210001"]),
                Workloads(["Петрова Анна Ивановна", "Физика", "ИВТ-21"]),
            ],
            Period,
            refs).ToReport();

        Assert.Equal(("p1", false, 2026, Term.Spring), (report.Period.Id, report.Period.WillBeCreated, report.Period.StartYear, report.Period.Term));
        Assert.Equal((1, 1, 1, 1, 1),
            (report.Summary.Groups.Create, report.Summary.Students.Create, report.Summary.Teachers.Create,
             report.Summary.Disciplines.Create, report.Summary.Workloads.Create));
        Assert.Empty(report.Errors);
        Assert.Single(report.Warnings);
    }

    [Theory]
    [InlineData(null, "")]
    [InlineData("  a   b ", "a b")]
    public void Normalize_TrimsAndCollapsesSpaces(string? value, string expected)
    {
        Assert.Equal(expected, ImportPlanBuilder.Normalize(value));
    }
}
