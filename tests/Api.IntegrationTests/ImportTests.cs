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

    private static MultipartFormDataContent Form(byte[] file, int startYear, Term term, string fileName = "roster.xlsx")
    {
        var content = new ByteArrayContent(file);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        return new MultipartFormDataContent
        {
            { content, "file", fileName },
            { new StringContent(startYear.ToString()), "startYear" },
            { new StringContent(term.ToString()), "term" },
        };
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

        var applied = await (await admin.PostAsync("/api/Import/apply", Form(file, 2040, Term.Spring))).DataAsync<ImportReportDto>();
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
    public async Task Apply_WithErrors_Is400WithReport_AndWritesNothing()
    {
        var admin = await factory.AdminAsync();
        var number = ApiFactory.Unique("e");
        var file = File([["ИВТ-99", "", "Иван", "", number]], [["Петрова", "Физика", "НЕТ-ТАКОЙ"]]);

        var response = await admin.PostAsync("/api/Import/apply", Form(file, 2041, Term.Autumn));

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
    public async Task Template_IsValidImportFile()
    {
        var admin = await factory.AdminAsync();

        var response = await admin.GetAsync("/api/Import/template");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var template = await response.Content.ReadAsByteArrayAsync();
        var preview = await (await admin.PostAsync("/api/Import/preview", Form(template, 2026, Term.Autumn))).DataAsync<ImportReportDto>();
        Assert.Empty(preview.Errors);
    }

    [Fact]
    public async Task Student_IsForbidden()
    {
        var student = await factory.LoginClientAsync("student6", ApiFactory.StudentPassword);

        Assert.Equal(HttpStatusCode.Forbidden, (await student.GetAsync("/api/Import/template")).StatusCode);
    }
}
