using Application.Common.Exceptions;
using Application.Common.Extension;
using Application.Common.Periods;
using Application.Common.Specification.FeedbackSpecification;
using Application.Common.Specification.WorkloadSpecification;
using Application.UnitTests.TestDoubles;
using Domain.Entities;
using Domain.Enums;

namespace Application.UnitTests.Common;

public class QueryFilterTests
{
    private static readonly AcademicPeriod Period = new() { Id = "p1", StartYear = 2026, Term = Term.Autumn };

    private static Teacher NewTeacher(string surname, string name, string patronymic, List<Workload>? workloads = null) =>
        new() { Surname = surname, Name = name, Patronymic = patronymic, WorkloadsRefs = workloads ?? [] };

    private static (Teacher Petrova, Teacher Ivanov, Discipline Math, Discipline Physics, List<Workload> Workloads) Graph()
    {
        var petrova = NewTeacher("Петрова", "Анна", "Ивановна", []);
        var ivanov = NewTeacher("Иванов", "Пётр", "Сергеевич", []);
        var math = new Discipline { Name = "Математика", WorkloadRefs = [] };
        var physics = new Discipline { Name = "Физика", WorkloadRefs = [] };

        var w = new Workload { TeacherRef = petrova, DisciplineRef = math, PeriodId = Period.Id, PeriodRef = Period };
        petrova.WorkloadsRefs!.Add(w);
        math.WorkloadRefs!.Add(w);

        return (petrova, ivanov, math, physics, [w]);
    }

    [Theory]
    [InlineData(null, 2)]
    [InlineData("  ", 2)]
    [InlineData("петр", 1)] // «ё» не равна «е»: «Пётр» не находится, «Петрова» находится
    [InlineData("АННА", 1)]
    [InlineData("ивановна", 1)]
    [InlineData("анна ивановна", 1)]
    [InlineData("петрова анна", 1)]
    [InlineData("петрова анна ивановна", 1)]
    [InlineData("сидоров", 0)]
    public void Teacher_WhereNameContains(string? term, int expected)
    {
        var g = Graph();
        var teachers = new[] { g.Petrova, g.Ivanov }.AsQueryable();

        Assert.Equal(expected, teachers.WhereNameContains(term).Count());
    }

    [Fact]
    public void Teacher_WhereHasWorkloadIn()
    {
        var g = Graph();
        var teachers = new[] { g.Petrova, g.Ivanov }.AsQueryable();

        Assert.Equal(2, teachers.WhereHasWorkloadIn(PeriodFilter.All).Count());
        Assert.Same(g.Petrova, Assert.Single(teachers.WhereHasWorkloadIn(new PeriodFilter("p1"))));
        Assert.Empty(teachers.WhereHasWorkloadIn(new PeriodFilter("other")));
    }

    [Theory]
    [InlineData(null, 2)]
    [InlineData("физ", 1)]
    [InlineData("анна", 1)]
    [InlineData("Петрова", 1)]
    [InlineData("ивановна", 1)]
    [InlineData("петрова анна", 1)]
    [InlineData("петрова анна ивановна", 1)]
    [InlineData("химия", 0)]
    public void Discipline_WhereNameOrTeacherContains(string? term, int expected)
    {
        var g = Graph();
        var disciplines = new[] { g.Math, g.Physics }.AsQueryable();

        Assert.Equal(expected, disciplines.WhereNameOrTeacherContains(term).Count());
    }

    [Fact]
    public void Discipline_WhereHasWorkloadIn()
    {
        var g = Graph();
        var disciplines = new[] { g.Math, g.Physics }.AsQueryable();

        Assert.Equal(2, disciplines.WhereHasWorkloadIn(PeriodFilter.All).Count());
        Assert.Same(g.Math, Assert.Single(disciplines.WhereHasWorkloadIn(new PeriodFilter(StartYear: 2026))));
    }

    [Theory]
    [InlineData(null, 1)]
    [InlineData("матем", 1)]
    [InlineData("анна", 1)]
    [InlineData("петрова", 1)]
    [InlineData("ивановна", 1)]
    [InlineData("петрова анна", 1)]
    [InlineData("петрова анна ивановна", 1)]
    [InlineData("физика", 0)]
    public void Workload_WhereNameOrTeacherContains(string? term, int expected)
    {
        var g = Graph();

        Assert.Equal(expected, g.Workloads.AsQueryable().WhereNameOrTeacherContains(term).Count());
    }

    [Fact]
    public void WorkloadAccess_Student_SeesOwnGroupWithoutOwnFeedback()
    {
        var spec = new WorkloadAccessService(FakeUserContext.Student("s1", "g1")).GetSpecification();
        var workloads = new[]
        {
            new Workload { Id = "own", GroupId = "g1", FeedbackRefs = null },
            new Workload { Id = "others", GroupId = "g1", FeedbackRefs = [new Feedback { StudentId = "s2" }] },
            new Workload { Id = "done", GroupId = "g1", FeedbackRefs = [new Feedback { StudentId = "s1" }] },
            new Workload { Id = "foreign", GroupId = "g2", FeedbackRefs = [] },
        }.AsQueryable();

        Assert.IsType<StudentWorkloadSpecification>(spec);
        Assert.Equal(["own", "others"], spec.Apply(workloads).Select(w => w.Id));
    }

    [Fact]
    public void WorkloadAccess_StudentWithoutClaims_UsesEmptyIds()
    {
        var spec = new WorkloadAccessService(new FakeUserContext { Role = "student" }).GetSpecification();
        var workloads = new[] { new Workload { GroupId = "", FeedbackRefs = [] }, new Workload { GroupId = "g1" } }.AsQueryable();

        Assert.Single(spec.Apply(workloads));
    }

    [Fact]
    public void WorkloadAccess_AdminSeesAll_OtherRolesRejected()
    {
        var spec = new WorkloadAccessService(FakeUserContext.Admin()).GetSpecification();
        Assert.IsType<EmployeeWorkloadSpecification>(spec);
        Assert.Equal(2, spec.Apply(new[] { new Workload(), new Workload() }.AsQueryable()).Count());

        Assert.Throws<UnauthorizationException>(() =>
            new WorkloadAccessService(new FakeUserContext { Role = "teacher" }).GetSpecification());
    }

    [Fact]
    public void FeedbackAccess_StudentSeesOwn_AdminSeesAll()
    {
        var feedbacks = new[] { new Feedback { StudentId = "s1" }, new Feedback { StudentId = "s2" } }.AsQueryable();

        var student = new FeedbackAccessService(FakeUserContext.Student("s1")).GetSpecification();
        Assert.IsType<StudentFeedbackSpecification>(student);
        Assert.Equal("s1", Assert.Single(student.Apply(feedbacks)).StudentId);

        Assert.Empty(new FeedbackAccessService(new FakeUserContext { Role = "student" }).GetSpecification().Apply(feedbacks));

        var admin = new FeedbackAccessService(FakeUserContext.Admin()).GetSpecification();
        Assert.IsType<EmployeeFeedbackSpecification>(admin);
        Assert.Equal(2, admin.Apply(feedbacks).Count());

        Assert.Throws<UnauthorizationException>(() =>
            new FeedbackAccessService(new FakeUserContext()).GetSpecification());
    }
}
