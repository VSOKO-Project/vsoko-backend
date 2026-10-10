using Application.Common.DTOs;
using Application.Common.Exceptions;
using Application.Common.Periods;
using Application.Features.ImportFeatures;
using Application.Interfaces.DataManager.Repositories;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.DataManager.Contexts;
using Infrastructure.SecurityManager.AspNetCoreIdentity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.DataManager.Repositories;

public class ImportRepository : IImportRepository
{
    private readonly AppDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IPasswordHasher<ApplicationUser> _passwordHasher;

    public ImportRepository(
        AppDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        IPasswordHasher<ApplicationUser> passwordHasher
    )
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _passwordHasher = passwordHasher;
    }

    public async Task<ImportReferenceData> LoadReferenceDataAsync(PeriodKey period, CancellationToken cancellationToken)
    {
        var periodId = await _dbContext.AcademicPeriods
            .Where(p => p.StartYear == period.StartYear && p.Term == period.Term)
            .Select(p => p.Id)
            .FirstOrDefaultAsync(cancellationToken);

        var refs = new ImportReferenceData { PeriodId = periodId };

        foreach (var group in await _dbContext.StudentGroups.Select(g => new { g.Id, g.Name }).ToListAsync(cancellationToken))
            refs.GroupIds.TryAdd(ImportPlanBuilder.Normalize(group.Name), group.Id);

        var accounts = await _dbContext.Users
            .Select(u => new
            {
                u.UserName,
                IsStudent = u.StudentRef != null,
                GroupName = u.StudentRef != null ? u.StudentRef.GroupRef!.Name : null,
            })
            .ToListAsync(cancellationToken);

        foreach (var account in accounts.Where(a => a.UserName is not null))
            refs.Accounts.TryAdd(account.UserName!, new ExistingAccount(account.IsStudent, account.GroupName));

        foreach (var teacher in await _dbContext.Teachers.Select(t => new { t.Id, t.Surname, t.Name, t.Patronymic }).ToListAsync(cancellationToken))
        {
            var key = ImportPlanBuilder.TeacherKey(
                ImportPlanBuilder.Normalize(teacher.Surname),
                ImportPlanBuilder.Normalize(teacher.Name),
                ImportPlanBuilder.Normalize(teacher.Patronymic)
            );
            refs.TeacherIds.TryAdd(key, teacher.Id);
        }

        foreach (var discipline in await _dbContext.Disciplines.Select(d => new { d.Id, d.Name }).ToListAsync(cancellationToken))
            refs.DisciplineIds.TryAdd(ImportPlanBuilder.Normalize(discipline.Name), discipline.Id);

        if (periodId is not null)
        {
            var workloads = await _dbContext.Workloads
                .Where(w => w.PeriodId == periodId)
                .Select(w => new { w.TeacherId, w.DisciplineId, w.GroupId })
                .ToListAsync(cancellationToken);

            foreach (var w in workloads)
                refs.Workloads.Add(new WorkloadKey(w.TeacherId, w.DisciplineId, w.GroupId));
        }

        return refs;
    }

    public async Task<ImportApplyResult> ApplyAsync(
        ImportPlan plan,
        ImportReferenceData refs,
        CancellationToken cancellationToken
    )
    {
        var periodId = refs.PeriodId;
        if (periodId is null)
        {
            var period = new AcademicPeriod { StartYear = plan.Period.StartYear, Term = plan.Period.Term };
            _dbContext.AcademicPeriods.Add(period);
            periodId = period.Id;
        }

        var groupIds = new Dictionary<string, string>(refs.GroupIds, StringComparer.OrdinalIgnoreCase);
        var createdGroupIds = new List<string>();
        foreach (var name in plan.GroupsToCreate)
        {
            // Семестр группы в файле не задаётся, поле остаётся для совместимости.
            var group = new StudentGroup { Name = name, Semester = 0 };
            _dbContext.StudentGroups.Add(group);
            groupIds[name] = group.Id;
            createdGroupIds.Add(group.Id);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        await CreateStudentsAsync(plan.StudentsToCreate, groupIds, cancellationToken);

        var teacherIds = new Dictionary<string, string>(refs.TeacherIds, StringComparer.OrdinalIgnoreCase);
        foreach (var planned in plan.TeachersToCreate)
        {
            var teacher = new Teacher { Surname = planned.Surname, Name = planned.Name, Patronymic = planned.Patronymic };
            _dbContext.Teachers.Add(teacher);
            teacherIds[planned.Key] = teacher.Id;
        }

        var disciplineIds = new Dictionary<string, string>(refs.DisciplineIds, StringComparer.OrdinalIgnoreCase);
        foreach (var name in plan.DisciplinesToCreate)
        {
            var discipline = new Discipline { Name = name };
            _dbContext.Disciplines.Add(discipline);
            disciplineIds[name] = discipline.Id;
        }

        foreach (var planned in plan.WorkloadsToCreate)
        {
            _dbContext.Workloads.Add(new Workload
            {
                TeacherId = teacherIds[planned.TeacherKey],
                DisciplineId = disciplineIds[planned.DisciplineName],
                GroupId = groupIds[planned.GroupName],
                PeriodId = periodId,
            });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        var studentGroupIds = plan.StudentGroupNames.Select(name => groupIds[name]).Distinct().ToList();
        return new ImportApplyResult(periodId, createdGroupIds, studentGroupIds);
    }

    private async Task CreateStudentsAsync(
        List<PlannedStudent> students,
        Dictionary<string, string> groupIds,
        CancellationToken cancellationToken
    )
    {
        var users = students
            .Select(s => new ApplicationUser
            {
                UserName = s.StudentNumber,
                Name = s.Name,
                Surname = s.Surname,
                Patronymic = s.Patronymic,
                Type = UserType.Student,
                MustChangePassword = true,
                CreatedAt = DateTime.UtcNow,
            })
            .ToList();

        // Хэширование пароля — самая дорогая часть, считаем параллельно.
        // Логин — номер зачётки, временный пароль выводится из него, как в ImportStudents.
        Parallel.ForEach(users, new ParallelOptions { CancellationToken = cancellationToken }, user =>
            user.PasswordHash = _passwordHasher.HashPassword(user, ImportFormat.TemporaryPassword(user.UserName!)));

        for (var i = 0; i < users.Count; i++)
        {
            var result = await _userManager.CreateAsync(users[i]);
            if (!result.Succeeded)
            {
                throw new ValidationException(
                    $"Не удалось создать студента {students[i].StudentNumber}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }

            _dbContext.Students.Add(new Student { Id = users[i].Id, GroupId = groupIds[students[i].GroupName] });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<StudentCredentialDto>> GetStudentCredentialsAsync(
        IReadOnlyCollection<string>? groupIds,
        CancellationToken cancellationToken
    )
    {
        var query = _dbContext.Users.Where(u => u.StudentRef != null);

        if (groupIds is not null)
            query = query.Where(u => groupIds.Contains(u.StudentRef!.GroupId));

        var users = await query
            .Select(u => new
            {
                u.StudentRef!.GroupId,
                GroupName = u.StudentRef.GroupRef!.Name,
                u.Surname,
                u.Name,
                u.Patronymic,
                u.UserName,
                u.MustChangePassword,
            })
            .ToListAsync(cancellationToken);

        return users
            .Select(u => new StudentCredentialDto
            {
                GroupId = u.GroupId,
                GroupName = u.GroupName,
                FullName = $"{u.Surname} {u.Name} {u.Patronymic}".Trim(),
                Login = u.UserName!,
                MustChangePassword = u.MustChangePassword,
            })
            .ToList();
    }

    public async Task<List<StudentGroupDto>> GetGroupsAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.StudentGroups
            .OrderBy(g => g.Name)
            .Select(g => new StudentGroupDto { Id = g.Id, Name = g.Name, Semester = g.Semester })
            .ToListAsync(cancellationToken);
    }
}
