using Application.Common.Periods;
using Application.Features.ImportFeatures;
using Application.Interfaces.FileManager;
using Domain.Enums;

namespace Application.UnitTests;

public class ImportWorkflowTests
{
    private static readonly PeriodKey Period = new(2026, Term.Spring);
    private static ImportReferenceData Refs() => new()
    {
        PeriodId = "p1",
        GroupIds = { ["Г-1"] = "g1" },
        Accounts = { ["001"] = new(true, "Г-1", "Иванов", "Иван", "Иванович") },
        TeacherIds = { ["Петров Пётр Петрович"] = "t1", ["Сидоров Иван Петрович"] = "t2" },
        DisciplineIds = { ["Физика"] = "d1", ["Химия"] = "d2" },
        Workloads = { [new("t1", "d1", "g1")] = new("w1", 0) },
    };
    private static RosterSheet Students(params string[][] rows) => Sheet(ImportFormat.StudentsSheet, ImportFormat.StudentColumns, rows);
    private static RosterSheet Workloads(params string[][] rows) => Sheet(ImportFormat.WorkloadsSheet, ImportFormat.ExportWorkloadColumns, rows);
    private static RosterSheet Scope(params string[][] rows) => Sheet(ImportFormat.ScopeSheet, ImportFormat.ScopeColumns, rows);
    private static RosterSheet Sheet(string name, string[] columns, string[][] rows) =>
        new(name, columns, rows.Select((r, i) => new RosterRow(i + 2, r)).ToList());

    [Theory]
    [InlineData(ImportMode.AddOnly, 0)]
    [InlineData(ImportMode.UpdateExisting, 1)]
    public void StudentChanges_AreExplicitAndDoNotCreateDuplicateAccount(ImportMode mode, int updates)
    {
        var plan = ImportPlanBuilder.Build([Students(["Г-2", "Новиков", "Иван", "", "001"])], Period, Refs(), mode);
        Assert.Empty(plan.Errors);
        Assert.Empty(plan.StudentsToCreate);
        Assert.Equal(updates, plan.StudentsToUpdate.Count);
        Assert.Equal(updates, plan.ToReport().Summary.Students.Update);
        Assert.Equal(1 - updates, plan.ToReport().Summary.Students.Existing);
        if (updates == 0)
        {
            Assert.Single(plan.Warnings);
            Assert.Empty(plan.Changes);
            Assert.Empty(plan.GroupsToCreate);
        }
        else
        {
            Assert.Empty(plan.Warnings);
            Assert.Equal(["Г-2"], plan.GroupsToCreate);
            Assert.Contains(plan.Changes, c => c.Field == "Группа" && c.Before == "Г-1" && c.After == "Г-2");
            Assert.Contains(plan.Changes, c => c.Field == "ФИО" && c.Before == "Иванов Иван Иванович" && c.After == "Новиков Иван");
        }
    }

    [Fact]
    public void UnchangedStudent_AndMissingStudent_ArePreserved()
    {
        var refs = Refs();
        refs.Accounts["002"] = new(true, "Г-1", "Сидоров", "Иван");
        var plan = ImportPlanBuilder.Build([Students(["Г-1", "Иванов", "Иван", "Иванович", "001"])], Period, refs, ImportMode.UpdateExisting);
        Assert.Empty(plan.StudentsToUpdate);
        Assert.Empty(plan.Changes);
        Assert.Empty(plan.Errors);
        Assert.Equal(1, plan.ToReport().Summary.Students.Existing);
    }

    [Theory]
    [InlineData(ImportMode.AddOnly, 0)]
    [InlineData(ImportMode.UpdateExisting, 1)]
    [InlineData(ImportMode.SynchronizeWorkloads, 1)]
    public void RenameById_KeepsExistingWorkloadAndReferences(ImportMode mode, int updates)
    {
        var plan = ImportPlanBuilder.Build([
            Workloads(["Новиков Пётр Петрович", "Новая физика", "Г-1", "t1", "d1"]),
            Scope([ImportFormat.ScopePeriod, "2026:Spring"]),
        ], Period, Refs(), mode);
        Assert.Empty(plan.Errors);
        Assert.Empty(plan.TeachersToCreate);
        Assert.Empty(plan.DisciplinesToCreate);
        Assert.Empty(plan.WorkloadsToCreate);
        Assert.Empty(plan.WorkloadsToDelete);
        Assert.Equal(updates, plan.TeachersToUpdate.Count);
        Assert.Equal(updates, plan.DisciplinesToUpdate.Count);
        Assert.Equal(1, plan.ExistingWorkloads);
        Assert.Equal(updates * 2, plan.Changes.Count);
        Assert.Equal(mode == ImportMode.AddOnly ? 2 : 0, plan.Warnings.Count);
    }

    [Fact]
    public void RenamesWithoutIds_AreListedAsNewRecords()
    {
        var plan = ImportPlanBuilder.Build([Workloads(["Новиков Пётр Петрович", "Новая физика", "Г-1"])], Period, Refs(), ImportMode.UpdateExisting);
        Assert.Equal(["Новиков Пётр Петрович"], plan.ToReport().NewTeachers);
        Assert.Equal(["Новая физика"], plan.ToReport().NewDisciplines);
        Assert.Empty(plan.TeachersToUpdate);
        Assert.Empty(plan.DisciplinesToUpdate);
        Assert.Empty(plan.WorkloadsToDelete);
    }

    [Theory]
    [InlineData("missing", "d1")]
    [InlineData("t1", "missing")]
    public void UnknownIds_BlockImport(string teacher, string discipline)
    {
        var plan = ImportPlanBuilder.Build([Workloads(["Петров Пётр Петрович", "Физика", "Г-1", teacher, discipline])], Period, Refs(), ImportMode.UpdateExisting);
        Assert.NotEmpty(plan.Errors);
        Assert.Empty(plan.WorkloadsToCreate);
    }

    [Theory]
    [InlineData("Сидоров Иван Петрович", "Физика")]
    [InlineData("Петров Пётр Петрович", "Химия")]
    public void RenameToExistingRecord_RequiresMerge(string teacher, string discipline)
    {
        var plan = ImportPlanBuilder.Build([Workloads([teacher, discipline, "Г-1", "t1", "d1"])], Period, Refs(), ImportMode.UpdateExisting);
        Assert.Contains(plan.Errors, e => e.Message.Contains("Объедините"));
    }

    [Fact]
    public void SameIdWithDifferentNames_IsErrorEvenForDuplicateWorkload()
    {
        var plan = ImportPlanBuilder.Build([Workloads(
            ["Новиков Пётр Петрович", "Физика", "Г-1", "t1", "d1"],
            ["Петров Пётр Петрович", "Физика", "Г-1", "t1", "d1"])], Period, Refs(), ImportMode.UpdateExisting);
        Assert.Contains(plan.Errors, e => e.Row == 3 && e.Message.Contains("разные названия"));
    }

    [Theory]
    [InlineData(ImportMode.AddOnly, 0)]
    [InlineData(ImportMode.UpdateExisting, 0)]
    [InlineData(ImportMode.SynchronizeWorkloads, 1)]
    public void DeletionRequiresExplicitSynchronization_AndAllowsEmptyWorkload(ImportMode mode, int deletions)
    {
        var plan = ImportPlanBuilder.Build([
            Students(["Г-1", "Иванов", "Иван", "Иванович", "001"]), Workloads(),
            Scope([ImportFormat.ScopePeriod, "2026:Spring"]),
        ], Period, Refs(), mode);
        Assert.Empty(plan.Errors);
        Assert.Equal(deletions, plan.WorkloadsToDelete.Count);
        Assert.True(plan.CanSynchronize);
    }

    [Theory]
    [InlineData("2025:Spring", "0")]
    [InlineData("broken", "0")]
    [InlineData("2026:Spring", "1")]
    public void WrongOrDamagedScope_CannotDelete(string sourcePeriod, string teacherCount)
    {
        var plan = ImportPlanBuilder.Build([
            Workloads(["Петров Пётр Петрович", "Физика", "Г-1"]),
            Scope([ImportFormat.ScopePeriod, sourcePeriod], [ImportFormat.ScopeTeacherCount, teacherCount]),
        ], Period, Refs(), ImportMode.SynchronizeWorkloads);
        Assert.False(plan.CanSynchronize);
        Assert.NotEmpty(plan.Errors);
        Assert.Empty(plan.WorkloadsToDelete);
    }

    [Fact]
    public void NoScope_CannotSynchronize_AndReportsWhy()
    {
        var plan = ImportPlanBuilder.Build([Workloads(["Петров Пётр Петрович", "Физика", "Г-1"])], Period, Refs(), ImportMode.SynchronizeWorkloads);
        Assert.NotEmpty(plan.Errors);
        Assert.Contains("нет корректных", plan.ScopeDescription);
    }

    [Fact]
    public void ConfirmationToken_ChangesForDifferentIdentityEvenWhenCountsMatch()
    {
        var file = new[] { Workloads(["Петров Пётр Петрович", "Физика", "Г-1"]), Scope([ImportFormat.ScopePeriod, "2026:Spring"]) };
        var originalRefs = Refs();
        originalRefs.Workloads[new("t2", "d2", "g1")] = new("old-to-delete", 0);
        var before = ImportPlanBuilder.Build(file, Period, originalRefs, ImportMode.SynchronizeWorkloads).ToReport();
        Assert.Equal(before.PreviewToken, ImportPlanBuilder.Build(file, Period, originalRefs, ImportMode.SynchronizeWorkloads).ToReport().PreviewToken);
        var changedRefs = Refs();
        changedRefs.Workloads[new("t2", "d2", "g1")] = new("new-to-delete", 0);
        var after = ImportPlanBuilder.Build(file, Period, changedRefs, ImportMode.SynchronizeWorkloads).ToReport();
        Assert.Equal(before.Summary.Workloads.Delete, after.Summary.Workloads.Delete);
        Assert.NotEqual(before.PreviewToken, after.PreviewToken);
        var anotherMode = ImportPlanBuilder.Build(file, Period, Refs(), ImportMode.UpdateExisting).ToReport();
        Assert.NotEqual(before.PreviewToken, anotherMode.PreviewToken);
    }

    [Fact]
    public void StudentNumberCase_DoesNotChangeAccountIdentityOnUpdate()
    {
        var refs = Refs();
        refs.Accounts["ABC001"] = new(true, "Г-1", "Иванов", "Иван", Login: "ABC001");
        var plan = ImportPlanBuilder.Build([Students(["Г-1", "Новиков", "Иван", "", "abc001"])], Period, refs, ImportMode.UpdateExisting);
        Assert.Equal("ABC001", Assert.Single(plan.StudentsToUpdate).Student.StudentNumber);
        Assert.Empty(plan.StudentsToCreate);
    }

    [Fact]
    public void DuplicateNames_RequireId_AndNewWorkloadKeepsThatExactId()
    {
        var refs = Refs();
        refs.TeacherNamesById["t1"] = "Петров Пётр Петрович";
        refs.TeacherNamesById["t9"] = "Петров Пётр Петрович";
        var ambiguous = ImportPlanBuilder.Build([Workloads(["Петров Пётр Петрович", "Химия", "Г-1"])], Period, refs);
        Assert.Contains(ambiguous.Errors, e => e.Message.Contains("несколько преподавателей"));
        var exact = ImportPlanBuilder.Build([Workloads(["Петров Пётр Петрович", "Химия", "Г-1", "t9", "d2"])], Period, refs);
        Assert.Empty(exact.Errors);
        Assert.Equal("t9", Assert.Single(exact.WorkloadsToCreate).TeacherId);
    }

    [Fact]
    public void EntireScopeCanBeCleared_WithoutAnyStudentRows()
    {
        var plan = ImportPlanBuilder.Build([Workloads(), Scope([ImportFormat.ScopePeriod, "2026:Spring"])], Period, Refs(), ImportMode.SynchronizeWorkloads);
        Assert.Empty(plan.Errors);
        Assert.Single(plan.WorkloadsToDelete);
        Assert.Empty(plan.StudentsToCreate);
    }
}
