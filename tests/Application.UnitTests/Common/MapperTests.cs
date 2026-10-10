using Application.Common.Mappings;
using Application.Common.Periods;
using Domain.Entities;
using Domain.Enums;

namespace Application.UnitTests.Common;

public class MapperTests
{
    private static readonly AcademicPeriod Autumn = new() { Id = "p1", StartYear = 2026, Term = Term.Autumn, IsFeedbackOpen = true, WorkloadRefs = [] };
    private static readonly AcademicPeriod Spring = new() { Id = "p0", StartYear = 2025, Term = Term.Spring, WorkloadRefs = [] };

    private static readonly Criteria TeacherCriteria = new() { Id = "ct", Name = "Объясняет", Object = CriteriaObject.Teacher, CriteriaFeedbackRefs = [] };

    /// <summary>
    /// Преподаватель с одной дисциплиной в двух периодах: осенью оценки 4 и 2, весной 5 и 1.
    /// </summary>
    private sealed class Fixture
    {
        public Teacher Teacher { get; }
        public Discipline Discipline { get; }
        public StudentGroup Group { get; }
        public Student Student { get; }
        public Workload AutumnWorkload { get; }
        public Feedback AutumnFeedback { get; }
        public List<Criteria> Criteria { get; }

        public Fixture()
        {
            var teacherCriteria = new Criteria { Id = "ct", Name = "Объясняет", Object = CriteriaObject.Teacher, CriteriaFeedbackRefs = [] };
            var disciplineCriteria = new Criteria { Id = "cd", Name = "Полезность", Object = CriteriaObject.Discipline, CriteriaFeedbackRefs = [] };
            Criteria = [teacherCriteria, disciplineCriteria];

            Teacher = new Teacher { Id = "t1", Surname = "Петрова", Name = "Анна", Patronymic = "Ивановна", WorkloadsRefs = [] };
            Discipline = new Discipline { Id = "d1", Name = "Физика", WorkloadRefs = [] };
            Group = new StudentGroup { Id = "g1", Name = "ИВТ-21", Semester = 3 };
            Student = new Student { Id = "s1", GroupId = "g1", GroupRef = Group };

            AutumnWorkload = AddWorkload(Autumn, teacherScore: 4, disciplineScore: 2, out var autumnFeedback);
            AutumnFeedback = autumnFeedback;
            AddWorkload(Spring, teacherScore: 5, disciplineScore: 1, out _);
        }

        private Workload AddWorkload(AcademicPeriod period, int teacherScore, int disciplineScore, out Feedback feedback)
        {
            var workload = new Workload
            {
                Id = "w-" + period.Id,
                TeacherRef = Teacher,
                DisciplineRef = Discipline,
                GroupRef = Group,
                PeriodId = period.Id,
                PeriodRef = period,
                FeedbackRefs = [],
            };
            feedback = new Feedback
            {
                Id = "f-" + period.Id,
                Comment = "ok",
                StudentRef = Student,
                WorkloadRef = workload,
                CriteriaFeedbackRefs = [],
            };
            foreach (var (criteria, score) in new[] { (Criteria[0], teacherScore), (Criteria[1], disciplineScore) })
            {
                var cf = new CriteriaFeedback { CriteriaRef = criteria, CriteriaScore = score, FeedbackRef = feedback };
                feedback.CriteriaFeedbackRefs!.Add(cf);
                criteria.CriteriaFeedbackRefs!.Add(cf);
            }
            workload.FeedbackRefs!.Add(feedback);
            Teacher.WorkloadsRefs!.Add(workload);
            Discipline.WorkloadRefs!.Add(workload);
            return workload;
        }
    }

    [Fact]
    public void Teacher_MapAndProject()
    {
        var f = new Fixture();
        var mapper = new TeacherMapper();

        var dto = mapper.MapSingle(f.Teacher);
        Assert.Equal(("t1", "Петрова Анна Ивановна"), (dto.Id, dto.FullName));

        var projected = Assert.Single(mapper.ProjectToDto(new[] { f.Teacher }.AsQueryable()));
        Assert.Equal("Анна", projected.Name);

        var rating = mapper.MapToRating(f.Teacher);
        Assert.Equal(("t1", "Петрова", 0f), (rating.Id, rating.Name, rating.Grade));
    }

    [Fact]
    public void Teacher_ProjectToRating_AveragesTeacherCriteriaInPeriod()
    {
        var f = new Fixture();
        var mapper = new TeacherMapper();
        var empty = new Teacher { Id = "t2", Surname = "Иванов", Name = "Пётр", Patronymic = "", WorkloadsRefs = [] };
        var teachers = new[] { f.Teacher, empty }.AsQueryable();

        var all = mapper.ProjectToRating(teachers, PeriodFilter.All).ToList();
        Assert.Equal(4.5f, all[0].Grade);
        Assert.Equal("Петрова Анна Ивановна", all[0].Name);
        Assert.Equal(("Иванов Пётр", 0f), (all[1].Name, all[1].Grade));

        Assert.Equal(4f, mapper.ProjectToRating(teachers, new PeriodFilter("p1")).First().Grade);
    }

    [Fact]
    public void Discipline_MapProjectAndRating()
    {
        var f = new Fixture();
        var mapper = new DisciplineMapper();
        var disciplines = new[] { f.Discipline }.AsQueryable();

        Assert.Equal("Физика", mapper.MapSingle(f.Discipline).Name);
        Assert.Equal("d1", Assert.Single(mapper.ProjectToDto(disciplines)).Id);
        Assert.Equal(1.5f, Assert.Single(mapper.ProjectToRating(disciplines, PeriodFilter.All)).Grade);
        Assert.Equal(1f, Assert.Single(mapper.ProjectToRating(disciplines, new PeriodFilter(StartYear: 2025))).Grade);
    }

    [Fact]
    public void Criteria_MapProjectAndRating()
    {
        var f = new Fixture();
        var mapper = new CriteriaMapper();
        var criteria = f.Criteria.AsQueryable();

        var dto = mapper.MapSingle(f.Criteria[0]);
        Assert.Equal(("ct", "Объясняет", CriteriaObject.Teacher), (dto.Id, dto.Name, dto.Object));
        Assert.Equal(2, mapper.ProjectToDto(criteria).Count());

        var rating = mapper.ProjectToRating(criteria, new PeriodFilter("p1")).ToList();
        Assert.Equal([4f, 2f], rating.Select(r => r.Grade));
        Assert.Equal([4.5f, 1.5f], mapper.ProjectToRating(criteria, PeriodFilter.All).Select(r => r.Grade));
    }

    [Fact]
    public void Period_MapProjectAndListItem()
    {
        var f = new Fixture();
        var period = new AcademicPeriod
        {
            Id = "p1", StartYear = 2026, Term = Term.Autumn, IsFeedbackOpen = true,
            WorkloadRefs = [f.AutumnWorkload, new Workload { FeedbackRefs = [] }],
        };
        var mapper = new AcademicPeriodMapper();

        var dto = mapper.MapSingle(period);
        Assert.Equal(("p1", 2026, Term.Autumn, true), (dto.Id, dto.StartYear, dto.Term, dto.IsFeedbackOpen));
        Assert.Single(mapper.ProjectToDto(new[] { period }.AsQueryable()));

        var item = Assert.Single(mapper.ProjectToListItem(new[] { period }.AsQueryable()));
        Assert.Equal((2, 1), (item.WorkloadCount, item.FeedbackCount));
        Assert.True(item.IsFeedbackOpen);
    }

    [Fact]
    public void Workload_MapAndProject()
    {
        var f = new Fixture();
        var mapper = new WorkloadMapper();

        var dto = mapper.MapSingle(f.AutumnWorkload);
        Assert.Equal("w-p1", dto.Id);
        Assert.Equal("Петрова Анна Ивановна", dto.Teacher!.FullName);
        Assert.Equal("Физика", dto.Discipline!.Name);
        Assert.Equal(("ИВТ-21", 3), (dto.Group!.Name, dto.Group.Semester));
        Assert.Equal("p1", dto.Period!.Id);

        var empty = mapper.MapSingle(new Workload { Id = "w0" });
        Assert.Null(empty.Teacher);
        Assert.Null(empty.Period);

        Assert.Equal("w-p1", Assert.Single(mapper.WorkloadToDto(new[] { f.AutumnWorkload }.AsQueryable())).Id);
    }

    [Fact]
    public void Feedback_MapAndProject()
    {
        var f = new Fixture();
        var mapper = new FeedbackMapper();

        var dto = mapper.MapSingle(f.AutumnFeedback);
        Assert.Equal(("f-p1", "ok"), (dto.Id, dto.Comment));
        Assert.Equal("w-p1", dto.Workload!.Id);
        Assert.Equal("g1", dto.Student!.Group!.Id);
        Assert.Equal([4, 2], dto.CriteriaFeedback!.Select(c => c.CriteriaScore));
        Assert.Equal("ct", dto.CriteriaFeedback![0].Criteria!.Id);

        var bare = mapper.MapSingle(new Feedback { Id = "f0", Comment = "" });
        Assert.Null(bare.Workload);
        Assert.Null(bare.Student);
        Assert.Null(bare.CriteriaFeedback);

        var item = mapper.MapCriteriaItem(new CriteriaFeedback { CriteriaRef = TeacherCriteria, CriteriaScore = 3 });
        Assert.Equal(("ct", 3), (item.Criteria!.Id, item.CriteriaScore));

        Assert.Equal("f-p1", Assert.Single(mapper.ProjectToDto(new[] { f.AutumnFeedback }.AsQueryable())).Id);
    }

    [Fact]
    public void StudentAndGroup_MapAndProject()
    {
        var f = new Fixture();

        var student = new StudentMapper().MapSingle(f.Student);
        Assert.Equal(("s1", "ИВТ-21"), (student.Id, student.Group!.Name));
        Assert.Null(new StudentMapper().MapSingle(new Student { Id = "s0" }).Group);
        Assert.Single(new StudentMapper().ProjectToDto(new[] { f.Student }.AsQueryable()));

        Assert.Equal(3, new StudentGroupMapper().MapSingle(f.Group).Semester);
        Assert.Single(new StudentGroupMapper().ProjectToDto(new[] { f.Group }.AsQueryable()));
    }

    [Fact]
    public void EmployeeAndRole_MapAndProject()
    {
        var role = new EmployeeRole { Id = "r1", Name = "admin" };
        var employee = new Employee { Id = "e1", RoleId = "r1", RoleRef = role };

        var dto = new EmployeeMapper().MapSingle(employee);
        Assert.Equal(("e1", "admin"), (dto.Id, dto.Role!.Name));
        Assert.Null(new EmployeeMapper().MapSingle(new Employee { RoleId = "r1" }).Role);
        Assert.Single(new EmployeeMapper().ProjectToDto(new[] { employee }.AsQueryable()));

        Assert.Equal("r1", new EmployeeRoleMapper().MapSingle(role).Id);
        Assert.Single(new EmployeeRoleMapper().ProjectToDto(new[] { role }.AsQueryable()));
    }
}
