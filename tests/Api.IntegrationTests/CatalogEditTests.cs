using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Api.IntegrationTests.Support;
using Application.Common.DTOs;
using Application.Common.Results;
using Application.Features.ImportFeatures;
using Application.Interfaces.FileManager;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.FileManager;
using Microsoft.EntityFrameworkCore;

namespace Api.IntegrationTests;

/// <summary>Переименование и объединение преподавателей и дисциплин, фильтр справочников по периоду.</summary>
[Collection(ApiCollection.Name)]
public class CatalogEditTests(ApiFactory factory)
{
    private static readonly RosterFile Roster = new();

    /// <summary>Импортирует группу из двух студентов и нагрузку в период, возвращает логины студентов.</summary>
    private async Task<(string Group, string First, string Second)> ImportAsync(HttpClient admin, int startYear, params string[][] workloads)
    {
        var group = ApiFactory.Unique("К-");
        var (first, second) = (ApiFactory.Unique("k"), ApiFactory.Unique("k"));
        var file = Roster.Write([
            new SpreadsheetSheet(ImportFormat.StudentsSheet, ImportFormat.StudentColumns,
                [[group, "Первый", "Пётр", "", first], [group, "Второй", "Павел", "", second]]),
            new SpreadsheetSheet(ImportFormat.WorkloadsSheet, ImportFormat.WorkloadColumns,
                workloads.Select(w => (IReadOnlyList<string>)[w[0], w[1], group]).ToList()),
        ]);
        var content = new ByteArrayContent(file);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        var response = await admin.PostAsync("/api/Import/apply", new MultipartFormDataContent
        {
            { content, "file", "roster.xlsx" },
            { new StringContent(startYear.ToString()), "startYear" },
            { new StringContent(nameof(Term.Autumn)), "term" },
        });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (group, first, second);
    }

    private Task AddFeedbackAsync(string login, string group, string teacherSurname, string discipline) =>
        factory.WithDbAsync(async db =>
        {
            var studentId = await db.Users.Where(u => u.UserName == login).Select(u => u.Id).SingleAsync();
            var workloadId = await db.Workloads
                .Where(w => w.GroupRef!.Name == group && w.TeacherRef!.Surname == teacherSurname && w.DisciplineRef!.Name == discipline)
                .Select(w => w.Id)
                .SingleAsync();
            db.Feedbacks.Add(new Feedback { StudentId = studentId, WorkloadId = workloadId, Comment = $"{login} {teacherSurname}" });
            await db.SaveChangesAsync();
        });

    /// <summary>Нагрузка группы строками «Фамилия / Дисциплина / отзывы через запятую».</summary>
    private Task<List<string>> WorkloadsAsync(string group) =>
        factory.WithDbAsync(async db => (await db.Workloads
                .Where(w => w.GroupRef!.Name == group)
                .Select(w => new
                {
                    Teacher = w.TeacherRef!.Surname,
                    Discipline = w.DisciplineRef!.Name,
                    Comments = w.FeedbackRefs!.Select(f => f.Comment).OrderBy(c => c).ToList(),
                })
                .OrderBy(w => w.Teacher).ThenBy(w => w.Discipline)
                .ToListAsync())
            .Select(w => $"{w.Teacher} / {w.Discipline} / {string.Join(", ", w.Comments)}")
            .ToList());

    private static async Task<T> FindAsync<T>(HttpClient admin, string url) =>
        Assert.Single((await (await admin.GetAsync(url)).DataAsync<PagedResultDto<T>>()).Items!);

    [Fact]
    public async Task MergeTeachers_MovesWorkloadAndFeedback_KeepsTargetFeedbackOnConflict()
    {
        var admin = await factory.AdminAsync();
        var dupe = $"Дублёва Анна {ApiFactory.Unique("Д")}";
        var main = $"Основнова Анна {ApiFactory.Unique("О")}";
        var (group, first, second) = await ImportAsync(admin, 2046,
            [dupe, "Физика"], [dupe, "Химия"], [main, "Физика"]);

        // Первый оценил обоих по физике, второй — только дубль.
        await AddFeedbackAsync(first, group, "Дублёва", "Физика");
        await AddFeedbackAsync(first, group, "Основнова", "Физика");
        await AddFeedbackAsync(second, group, "Дублёва", "Физика");

        var source = await FindAsync<TeacherDto>(admin, $"/api/Teachers?query={dupe.Split(' ')[2]}");
        var target = await FindAsync<TeacherDto>(admin, $"/api/Teachers?query={main.Split(' ')[2]}");

        var merged = await (await admin.PostAsJsonAsync($"/api/Teachers/{source.Id}/merge", new { targetId = target.Id })).DataAsync<TeacherDto>();
        Assert.Equal(target.Id, merged.Id);

        Assert.Equal(
            [$"Основнова / Физика / {first} Основнова, {second} Дублёва", "Основнова / Химия / "],
            await WorkloadsAsync(group));
        Assert.Empty((await (await admin.GetAsync($"/api/Teachers?query={dupe.Split(' ')[2]}")).DataAsync<PagedResultDto<TeacherDto>>()).Items!);

        var self = await admin.PostAsJsonAsync($"/api/Teachers/{target.Id}/merge", new { targetId = target.Id });
        await self.ProblemAsync(HttpStatusCode.BadRequest);
        var missing = await admin.PostAsJsonAsync($"/api/Teachers/{target.Id}/merge", new { targetId = "нет-такого" });
        await missing.ProblemAsync(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task RenameTeacher_RejectsExistingName_AndListFiltersByPeriod()
    {
        var admin = await factory.AdminAsync();
        var typo = ApiFactory.Unique("Опечаткин");
        var other = $"Занятова Зоя {ApiFactory.Unique("З")}";
        await ImportAsync(admin, 2047, [$"{typo} Иван Иванович", "Физика"], [other, "Физика"]);

        var teacher = await FindAsync<TeacherDto>(admin, $"/api/Teachers?query={typo}");
        var parts = other.Split(' ');

        var taken = await admin.PutAsJsonAsync($"/api/Teachers/{teacher.Id}",
            new { surname = parts[0], name = parts[1].ToUpper(), patronymic = parts[2] });
        await taken.ProblemAsync(HttpStatusCode.BadRequest);

        var fixedName = ApiFactory.Unique("Исправлен");
        var renamed = await (await admin.PutAsJsonAsync($"/api/Teachers/{teacher.Id}",
            new { surname = $" {fixedName} ", name = "Иван", patronymic = "Иванович" })).DataAsync<TeacherDto>();
        Assert.Equal($"{fixedName} Иван Иванович", renamed.FullName);

        var inPeriod = await (await admin.GetAsync($"/api/Teachers?startYear=2047&query={fixedName}")).DataAsync<PagedResultDto<TeacherDto>>();
        Assert.Single(inPeriod.Items!);
        var otherPeriod = await (await admin.GetAsync($"/api/Teachers?startYear=2091&query={fixedName}")).DataAsync<PagedResultDto<TeacherDto>>();
        Assert.Empty(otherPeriod.Items!);

        var notFound = await admin.PutAsJsonAsync("/api/Teachers/нет-такого", new { surname = "А", name = "Б", patronymic = "В" });
        await notFound.ProblemAsync(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Disciplines_RenameAndMerge()
    {
        var admin = await factory.AdminAsync();
        var teacher = $"Дисциплинов Денис {ApiFactory.Unique("Д")}";
        var (typo, main) = (ApiFactory.Unique("Матанализ "), ApiFactory.Unique("Математика "));
        var (group, first, _) = await ImportAsync(admin, 2048, [teacher, typo], [teacher, main]);
        await AddFeedbackAsync(first, group, "Дисциплинов", typo);

        var source = await FindAsync<DisciplineDto>(admin, $"/api/Disciplines?query={typo}");
        var target = await FindAsync<DisciplineDto>(admin, $"/api/Disciplines?query={main}&startYear=2048");

        var taken = await admin.PutAsJsonAsync($"/api/Disciplines/{source.Id}", new { name = main.ToLower() });
        await taken.ProblemAsync(HttpStatusCode.BadRequest);

        var renamed = ApiFactory.Unique("Анализ ");
        Assert.Equal(renamed, (await (await admin.PutAsJsonAsync($"/api/Disciplines/{source.Id}", new { name = renamed })).DataAsync<DisciplineDto>()).Name);

        await admin.PostAsJsonAsync($"/api/Disciplines/{source.Id}/merge", new { targetId = target.Id });

        Assert.Equal([$"Дисциплинов / {main} / {first} Дисциплинов"], await WorkloadsAsync(group));
        Assert.Empty((await (await admin.GetAsync($"/api/Disciplines?query={renamed}")).DataAsync<PagedResultDto<DisciplineDto>>()).Items!);
    }
}
