using System.Net;
using System.Net.Http.Headers;
using Api.IntegrationTests.Support;
using Application.Common.DTOs;
using Application.Features.ImportFeatures;
using Application.Interfaces.FileManager;
using Domain.Enums;
using Infrastructure.FileManager;
using Microsoft.EntityFrameworkCore;

namespace Api.IntegrationTests;

[Collection(ApiCollection.Name)]
public class ImportTests(ApiFactory factory)
{
    private static readonly RosterFile Roster = new();

    private static MultipartFormDataContent Form(byte[] file, int startYear, Term term, string fileName = "roster.xlsx", ImportMode mode = ImportMode.AddOnly, string? token = null)
    {
        var content = new ByteArrayContent(file);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        return new MultipartFormDataContent
        {
            { content, "file", fileName },
            { new StringContent(startYear.ToString()), "startYear" },
            { new StringContent(term.ToString()), "term" },
            { new StringContent(mode.ToString()), "mode" },
            { new StringContent(token ?? ""), "previewToken" },
        };
    }

    private static async Task<HttpResponseMessage> Apply(HttpClient admin, byte[] file, int year, Term term,
        string fileName = "roster.xlsx", ImportMode mode = ImportMode.AddOnly)
    {
        var preview = await (await admin.PostAsync("/api/Import/preview", Form(file, year, term, fileName, mode))).DataAsync<ImportReportDto>();
        return await admin.PostAsync("/api/Import/apply", Form(file, year, term, fileName, mode, preview.PreviewToken));
    }

    private static byte[] File(string[][] students, string[][] workloads) =>
        Roster.Write([
            new SpreadsheetSheet(ImportFormat.StudentsSheet, ImportFormat.StudentColumns, students),
            new SpreadsheetSheet(ImportFormat.WorkloadsSheet, ImportFormat.WorkloadColumns, workloads),
        ]);

    [Fact]
    public async Task PreviewThenApply_CreatesAccountsThatMustChangePassword()
    {
        var admin = await factory.AdminAsync();
        var group = ApiFactory.Unique("И-");
        var number = ApiFactory.Unique("n");
        var teacher = $"Импортова Ирина {ApiFactory.Unique("Б")}";
        var discipline = ApiFactory.Unique("Импорт ");
        var file = File(
            [[group, "Импортов", "Иван", "", number]],
            [[teacher, discipline, group], ["Сидоров Дмитрий Сергеевич", "Физика", group]]);

        var preview = await (await admin.PostAsync("/api/Import/preview", Form(file, 2040, Term.Spring))).DataAsync<ImportReportDto>();
        Assert.True(preview.Period.WillBeCreated);
        Assert.Empty(preview.Errors);
        Assert.Equal((1, 1, 2), (preview.Summary.Groups.Create, preview.Summary.Students.Create, preview.Summary.Workloads.Create));
        Assert.Equal(1, preview.Summary.Disciplines.Existing);

        var applied = await (await Apply(admin, file, 2040, Term.Spring)).DataAsync<ImportReportDto>();
        Assert.NotNull(applied.Period.Id);
        var groupId = Assert.Single(applied.CreatedGroupIds!);
        Assert.Equal([groupId], applied.StudentGroupIds);

        // Повторный импорт того же файла ничего не создаёт.
        var repeat = await (await admin.PostAsync("/api/Import/preview", Form(file, 2040, Term.Spring))).DataAsync<ImportReportDto>();
        Assert.False(repeat.Period.WillBeCreated);
        Assert.Equal((0, 1, 0, 2), (repeat.Summary.Students.Create, repeat.Summary.Students.Existing,
            repeat.Summary.Workloads.Create, repeat.Summary.Workloads.Existing));

        var login = await factory.LoginAsync(number, ImportFormat.TemporaryPassword(number));
        Assert.True(login.MustChangePassword);

        var groups = await (await admin.GetAsync("/api/Import/groups")).DataAsync<List<StudentGroupDto>>();
        Assert.Contains(groups, g => g.Id == groupId && g.Name == group);

        var credentials = await admin.GetAsync($"/api/Import/credentials?groupIds={groupId}");
        Assert.Equal(HttpStatusCode.OK, credentials.StatusCode);
        var sheet = Assert.Single(Roster.Read(await credentials.Content.ReadAsStreamAsync()));
        Assert.Equal(group, sheet.Name);
        var row = Assert.Single(sheet.Rows);
        Assert.Equal(["Импортов Иван", number, ImportFormat.TemporaryPassword(number), ""], row.Values.Skip(1));

        var all = await admin.GetAsync("/api/Import/credentials");
        Assert.True(Roster.Read(await all.Content.ReadAsStreamAsync()).Count > 1);
    }

    [Fact]
    public async Task Export_RoundTripsAndFiltersByTeacher()
    {
        var admin = await factory.AdminAsync();
        var group = ApiFactory.Unique("Э-");
        var number = ApiFactory.Unique("x");
        var teacher = $"Выгрузова Елена {ApiFactory.Unique("В")}";
        var file = File(
            [[group, "Выгрузов", "Пётр", "", number]],
            [[teacher, "Физика", group], ["Сидоров Дмитрий Сергеевич", "Физика", group]]);
        await Apply(admin, file, 2042, Term.Autumn);

        var teachers = await (await admin.GetAsync("/api/Import/teachers?startYear=2042&term=Autumn")).DataAsync<List<TeacherDto>>();
        Assert.Equal(2, teachers.Count);

        // Выгрузка повторно загружается без ошибок и ничего не создаёт.
        var export = await admin.GetAsync("/api/Import/export?startYear=2042&term=Autumn");
        Assert.Equal(HttpStatusCode.OK, export.StatusCode);
        var exported = await export.Content.ReadAsByteArrayAsync();
        var repeat = await (await admin.PostAsync("/api/Import/preview", Form(exported, 2042, Term.Autumn))).DataAsync<ImportReportDto>();
        Assert.Empty(repeat.Errors);
        Assert.Equal((0, 1, 0, 2), (repeat.Summary.Students.Create, repeat.Summary.Students.Existing,
            repeat.Summary.Workloads.Create, repeat.Summary.Workloads.Existing));

        var teacherId = teachers.Single(t => t.FullName == teacher).Id;
        var filtered = await admin.GetAsync($"/api/Import/export?startYear=2042&term=Autumn&teacherIds={teacherId}");
        var sheets = Roster.Read(await filtered.Content.ReadAsStreamAsync());
        var workload = Assert.Single(sheets.Single(s => s.Name == ImportFormat.WorkloadsSheet).Rows);
        Assert.Equal([teacher, "Физика", group], workload.Values.Take(3));
        Assert.Equal(teacherId, workload.Values[3]);
        Assert.NotEmpty(workload.Values[4]);
        Assert.Single(sheets.Single(s => s.Name == ImportFormat.StudentsSheet).Rows);

        var missing = await admin.GetAsync("/api/Import/export?startYear=2043&term=Autumn");
        await missing.ProblemAsync(HttpStatusCode.NotFound);
        Assert.Empty(await (await admin.GetAsync("/api/Import/teachers?startYear=2043&term=Autumn")).DataAsync<List<TeacherDto>>());
    }

    [Fact]
    public async Task ExportedFile_WithRemovedRows_DeletesWorkloadWithoutFeedback()
    {
        var admin = await factory.AdminAsync();
        var group = ApiFactory.Unique("У-");
        var number = ApiFactory.Unique("u");
        var kept = $"Оставлена Ольга {ApiFactory.Unique("О")}";
        var removed = $"Удалёнов Устин {ApiFactory.Unique("У")}";
        var reviewed = $"Отзывова Ольга {ApiFactory.Unique("О")}";
        await Apply(admin,
            File([[group, "Удалёнов", "Пётр", "", number]],
                [[kept, "Физика", group], [removed, "Физика", group], [reviewed, "Физика", group]]),
            2044, Term.Autumn);

        await factory.WithDbAsync(async db =>
        {
            var studentId = await db.Users.Where(u => u.UserName == number).Select(u => u.Id).SingleAsync();
            var workloadId = await db.Workloads
                .Where(w => w.TeacherRef!.Surname == "Отзывова" && w.GroupRef!.Name == group)
                .Select(w => w.Id)
                .SingleAsync();
            db.Feedbacks.Add(new Domain.Entities.Feedback { StudentId = studentId, WorkloadId = workloadId, Comment = "Хорошо" });
            await db.SaveChangesAsync();
        });

        // В выгрузке оставляем одну строку нагрузки, служебный лист переносим как есть.
        var exported = Roster.Read(await (await admin.GetAsync("/api/Import/export?startYear=2044&term=Autumn")).Content.ReadAsStreamAsync());
        var scope = exported.Single(s => s.Name == ImportFormat.ScopeSheet);
        var edited = Roster.Write([
            new SpreadsheetSheet(ImportFormat.WorkloadsSheet, ImportFormat.WorkloadColumns, [[kept, "Физика", group]]),
            new SpreadsheetSheet(scope.Name, scope.Headers, scope.Rows.Select(r => r.Values).ToList(), Hidden: true),
        ]);

        var preview = await (await admin.PostAsync("/api/Import/preview", Form(edited, 2044, Term.Autumn, mode: ImportMode.SynchronizeWorkloads))).DataAsync<ImportReportDto>();
        Assert.Empty(preview.Errors);
        Assert.Equal(1, preview.Summary.Workloads.Delete);
        Assert.Equal(new ImportWorkloadDto(removed, "Физика", group), Assert.Single(preview.WorkloadsToDelete));
        Assert.Contains(preview.Warnings, w => w.Message.Contains(reviewed));

        // В другой семестр тот же файл только добавляет.
        var otherPeriod = await (await admin.PostAsync("/api/Import/preview", Form(edited, 2045, Term.Spring))).DataAsync<ImportReportDto>();
        Assert.Equal(0, otherPeriod.Summary.Workloads.Delete);

        var applied = await (await Apply(admin, edited, 2044, Term.Autumn, mode: ImportMode.SynchronizeWorkloads)).DataAsync<ImportReportDto>();
        Assert.Equal(1, applied.Summary.Workloads.Delete);

        var surnames = await factory.WithDbAsync(db => db.Workloads
            .Where(w => w.GroupRef!.Name == group)
            .Select(w => w.TeacherRef!.Surname)
            .OrderBy(s => s)
            .ToListAsync());
        Assert.Equal(["Оставлена", "Отзывова"], surnames);
        Assert.True(await factory.WithDbAsync(db => db.Feedbacks.AnyAsync(f => f.WorkloadRef!.GroupRef!.Name == group)));
    }

    [Fact]
    public async Task Apply_WithErrors_Is400WithReport_AndWritesNothing()
    {
        var admin = await factory.AdminAsync();
        var number = ApiFactory.Unique("e");
        var file = File([["ИВТ-99", "", "Иван", "", number]], [["Петрова", "Физика", "НЕТ-ТАКОЙ"]]);

        var response = await Apply(admin, file, 2041, Term.Autumn);

        var problem = await response.ProblemAsync(HttpStatusCode.BadRequest);
        Assert.True(problem.GetProperty("report").GetProperty("errors").GetArrayLength() >= 3);
        Assert.False(await factory.WithDbAsync(db => db.Users.AnyAsync(u => u.UserName == number)));
        Assert.False(await factory.WithDbAsync(db => db.AcademicPeriods.AnyAsync(p => p.StartYear == 2041)));
    }

    [Fact]
    public async Task InvalidUploads_Are400()
    {
        var admin = await factory.AdminAsync();

        await (await admin.PostAsync("/api/Import/preview", Form("not excel"u8.ToArray(), 2026, Term.Autumn)))
            .ProblemAsync(HttpStatusCode.BadRequest);
        await (await admin.PostAsync("/api/Import/preview", Form([1, 2], 2026, Term.Autumn, "roster.csv")))
            .ProblemAsync(HttpStatusCode.BadRequest);
        await (await admin.PostAsync("/api/Import/preview", new MultipartFormDataContent { { new StringContent("2026"), "startYear" } }))
            .ProblemAsync(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Template_IsEmptyAndDoesNotImportExamples()
    {
        var admin = await factory.AdminAsync();

        var response = await admin.GetAsync("/api/Import/template");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var template = await response.Content.ReadAsByteArrayAsync();
        var preview = await (await admin.PostAsync("/api/Import/preview", Form(template, 2026, Term.Autumn))).DataAsync<ImportReportDto>();
        Assert.NotEmpty(preview.Errors);
        Assert.Equal(0, preview.Summary.Students.Create);
        Assert.Equal(0, preview.Summary.Workloads.Create);
    }

    [Fact]
    public async Task UpdateExisting_MovesStudentAndRenamesById_PreservingPasswordWorkloadAndFeedback()
    {
        var admin = await factory.AdminAsync();
        var group = ApiFactory.Unique("Old-");
        var targetGroup = ApiFactory.Unique("New-");
        var number = ApiFactory.Unique("edit-");
        var teacher = $"Правкин Пётр {ApiFactory.Unique("П")}";
        var discipline = ApiFactory.Unique("Правка ");
        var source = File([[group, "Иванов", "Иван", "Иванович", number]], [[teacher, discipline, group]]);
        (await Apply(admin, source, 2060, Term.Autumn)).EnsureSuccessStatusCode();
        var original = await factory.WithDbAsync(async db =>
        {
            var user = await db.Users.SingleAsync(u => u.UserName == number);
            user.MustChangePassword = false;
            var workload = await db.Workloads.SingleAsync(w => w.GroupRef!.Name == group);
            db.Feedbacks.Add(new Domain.Entities.Feedback { StudentId = user.Id, WorkloadId = workload.Id, Comment = "Сохранить отзыв" });
            await db.SaveChangesAsync();
            return new { user.Id, user.PasswordHash, user.MustChangePassword, WorkloadId = workload.Id, workload.TeacherId, workload.DisciplineId };
        });
        var activeStudentSession = await factory.LoginClientAsync(number, ImportFormat.TemporaryPassword(number));
        var sheets = Roster.Read(await (await admin.GetAsync("/api/Import/export?startYear=2060&term=Autumn")).Content.ReadAsStreamAsync());
        var workloadSheet = sheets.Single(s => s.Name == ImportFormat.WorkloadsSheet);
        var workloadRow = Assert.Single(workloadSheet.Rows).Values.ToArray();
        var newTeacher = $"Исправкин Пётр {ApiFactory.Unique("П")}";
        var newDiscipline = ApiFactory.Unique("Исправлено ");
        workloadRow[0] = newTeacher;
        workloadRow[1] = newDiscipline;
        // Keep the workload group: moving a student must not rewrite old workload/feedback.
        var edited = Roster.Write([
            new SpreadsheetSheet(ImportFormat.StudentsSheet, ImportFormat.StudentColumns,
                [[targetGroup, "Новиков", "Иван", "", number]]),
            new SpreadsheetSheet(workloadSheet.Name, workloadSheet.Headers,
                [workloadRow, [newTeacher, newDiscipline, targetGroup, workloadRow[3], workloadRow[4]]]),
        ]);
        var addOnly = await (await admin.PostAsync("/api/Import/preview", Form(edited, 2060, Term.Autumn))).DataAsync<ImportReportDto>();
        Assert.Equal(3, addOnly.Warnings.Count);
        Assert.Empty(addOnly.Changes);
        var preview = await (await admin.PostAsync("/api/Import/preview", Form(edited, 2060, Term.Autumn, mode: ImportMode.UpdateExisting))).DataAsync<ImportReportDto>();
        Assert.Empty(preview.Errors);
        Assert.Equal((1, 1, 1), (preview.Summary.Students.Update, preview.Summary.Teachers.Update, preview.Summary.Disciplines.Update));
        Assert.Equal(4, preview.Changes.Count);
        Assert.Equal(1, preview.Summary.Workloads.Create);
        var applied = await (await admin.PostAsync("/api/Import/apply", Form(edited, 2060, Term.Autumn,
            mode: ImportMode.UpdateExisting, token: preview.PreviewToken))).DataAsync<ImportReportDto>();
        Assert.Equal(1, applied.Summary.Students.Update);
        var visibleWorkloads = await (await activeStudentSession.GetAsync($"/api/Workload?periodId={applied.Period.Id}"))
            .DataAsync<Application.Common.Results.PagedResultDto<WorkloadDto>>();
        Assert.Equal(targetGroup, Assert.Single(visibleWorkloads.Items!).Group!.Name);
        await factory.WithDbAsync(async db =>
        {
            var user = await db.Users.Include(u => u.StudentRef!).ThenInclude(s => s.GroupRef).SingleAsync(u => u.UserName == number);
            Assert.Equal((original.Id, original.PasswordHash, original.MustChangePassword), (user.Id, user.PasswordHash, user.MustChangePassword));
            Assert.Equal(("Новиков", "Иван", (string?)null, targetGroup), (user.Surname, user.Name, user.Patronymic, user.StudentRef!.GroupRef!.Name));
            var workload = await db.Workloads.Include(w => w.TeacherRef).Include(w => w.DisciplineRef).SingleAsync(w => w.Id == original.WorkloadId);
            Assert.Equal((original.TeacherId, original.DisciplineId), (workload.TeacherId, workload.DisciplineId));
            Assert.Equal(newTeacher, $"{workload.TeacherRef!.Surname} {workload.TeacherRef.Name} {workload.TeacherRef.Patronymic}");
            Assert.Equal(newDiscipline, workload.DisciplineRef!.Name);
            Assert.Single(await db.Feedbacks.Where(f => f.WorkloadId == original.WorkloadId).ToListAsync());
        });
        var repeat = await (await admin.PostAsync("/api/Import/preview", Form(edited, 2060, Term.Autumn, mode: ImportMode.UpdateExisting))).DataAsync<ImportReportDto>();
        Assert.Empty(repeat.Changes);
        Assert.Empty(repeat.Errors);
    }

    [Fact]
    public async Task ChangedDeletionPlan_Returns409_AndRequiresConfirmationAgain()
    {
        var admin = await factory.AdminAsync();
        var group = ApiFactory.Unique("Drift-");
        var teacher = $"Планов Пётр {ApiFactory.Unique("П")}";
        var source = File([[group, "Иванов", "Иван", "", ApiFactory.Unique("drift-")]],
            [[teacher, "Физика", group], [teacher, "Химия", group]]);
        (await Apply(admin, source, 2061, Term.Spring)).EnsureSuccessStatusCode();
        var sheets = Roster.Read(await (await admin.GetAsync("/api/Import/export?startYear=2061&term=Spring")).Content.ReadAsStreamAsync());
        var scope = sheets.Single(s => s.Name == ImportFormat.ScopeSheet);
        var edited = Roster.Write([
            new SpreadsheetSheet(ImportFormat.WorkloadsSheet, ImportFormat.WorkloadColumns, [[teacher, "Физика", group]]),
            new SpreadsheetSheet(scope.Name, scope.Headers, scope.Rows.Select(r => r.Values).ToList(), Hidden: true),
        ]);
        var preview = await (await admin.PostAsync("/api/Import/preview", Form(edited, 2061, Term.Spring, mode: ImportMode.SynchronizeWorkloads))).DataAsync<ImportReportDto>();
        Assert.Equal(1, preview.Summary.Workloads.Delete);

        // A different administrator adds another workload after preview.
        (await Apply(admin, File([], [[teacher, "Информатика", group]]), 2061, Term.Spring)).EnsureSuccessStatusCode();
        var response = await admin.PostAsync("/api/Import/apply", Form(edited, 2061, Term.Spring,
            mode: ImportMode.SynchronizeWorkloads, token: preview.PreviewToken));
        var problem = await response.ProblemAsync(HttpStatusCode.Conflict);
        var newReport = problem.GetProperty("report");
        Assert.Equal(2, newReport.GetProperty("summary").GetProperty("workloads").GetProperty("delete").GetInt32());
        Assert.Equal(3, await factory.WithDbAsync(db => db.Workloads.CountAsync(w => w.GroupRef!.Name == group)));
        (await admin.PostAsync("/api/Import/apply", Form(edited, 2061, Term.Spring,
            mode: ImportMode.SynchronizeWorkloads, token: newReport.GetProperty("previewToken").GetString()))).EnsureSuccessStatusCode();
        Assert.Equal(1, await factory.WithDbAsync(db => db.Workloads.CountAsync(w => w.GroupRef!.Name == group)));
    }

    [Fact]
    public async Task MissingPreviewToken_WritesNothing()
    {
        var admin = await factory.AdminAsync();
        var number = ApiFactory.Unique("token-");
        var response = await admin.PostAsync("/api/Import/apply", Form(
            File([[ApiFactory.Unique("NoToken-"), "Иванов", "Иван", "", number]], []), 2062, Term.Autumn));
        await response.ProblemAsync(HttpStatusCode.Conflict);
        Assert.False(await factory.WithDbAsync(db => db.Users.AnyAsync(u => u.UserName == number)));
        Assert.False(await factory.WithDbAsync(db => db.AcademicPeriods.AnyAsync(p => p.StartYear == 2062)));
    }

    [Fact]
    public async Task Student_IsForbidden()
    {
        var student = await factory.LoginClientAsync("student6", ApiFactory.StudentPassword);

        Assert.Equal(HttpStatusCode.Forbidden, (await student.GetAsync("/api/Import/template")).StatusCode);
    }
}
