using System.Net;
using System.Net.Http.Json;
using Api.IntegrationTests.Support;
using Application.Common.DTOs;
using Domain.Enums;

namespace Api.IntegrationTests;

[Collection(ApiCollection.Name)]
public class CriteriaTests(ApiFactory factory)
{
    [Fact]
    public async Task Admin_Crud()
    {
        var admin = await factory.AdminAsync();
        var name = ApiFactory.Unique("Критерий ");

        var created = await (await admin.PostAsJsonAsync("/api/Criteria", new { name, criteriaObject = "Discipline" }, ApiClient.Json))
            .DataAsync<CriteriaDto>();
        Assert.Equal((name, CriteriaObject.Discipline), (created.Name, created.Object));

        var all = await (await admin.GetAsync("/api/Criteria")).DataAsync<List<CriteriaDto>>();
        Assert.Contains(all, c => c.Id == created.Id);

        var byId = await (await admin.GetAsync($"/api/Criteria/{created.Id}")).DataAsync<CriteriaDto>();
        Assert.Equal(name, byId.Name);

        var updated = await (await admin.PutAsJsonAsync($"/api/Criteria/{created.Id}",
            new { id = created.Id, name = name + "!", criteriaObject = "Teacher" }, ApiClient.Json)).DataAsync<CriteriaDto>();
        Assert.Equal((name + "!", CriteriaObject.Teacher), (updated.Name, updated.Object));
        Assert.Equal(name + "!", (await (await admin.GetAsync($"/api/Criteria/{created.Id}")).DataAsync<CriteriaDto>()).Name);

        Assert.Equal(HttpStatusCode.OK, (await admin.DeleteAsync($"/api/Criteria/{created.Id}")).StatusCode);
        Assert.DoesNotContain(await (await admin.GetAsync("/api/Criteria")).DataAsync<List<CriteriaDto>>(), c => c.Id == created.Id);
        await (await admin.GetAsync($"/api/Criteria/{created.Id}")).ProblemAsync(HttpStatusCode.NotFound);

        // Повторное удаление ничего не делает.
        Assert.Equal(HttpStatusCode.OK, (await admin.DeleteAsync($"/api/Criteria/{created.Id}")).StatusCode);
    }

    [Fact]
    public async Task Put_Errors()
    {
        var admin = await factory.AdminAsync();

        var unknown = await admin.PutAsJsonAsync("/api/Criteria/missing", new { id = "missing", name = "x", criteriaObject = "Teacher" }, ApiClient.Json);
        await unknown.ProblemAsync(HttpStatusCode.NotFound);

        var mismatch = await admin.PutAsJsonAsync("/api/Criteria/a", new { id = "b", name = "x" });
        Assert.Equal("Validation Exception", (await mismatch.ProblemAsync(HttpStatusCode.BadRequest)).GetProperty("title").GetString());

        var invalid = await admin.PostAsJsonAsync("/api/Criteria", new { name = "" });
        await invalid.ProblemAsync(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Student_CanReadButNotWrite()
    {
        var student = await factory.LoginClientAsync("student3", ApiFactory.StudentPassword);

        Assert.NotEmpty(await (await student.GetAsync("/api/Criteria")).DataAsync<List<CriteriaDto>>());
        Assert.Equal(HttpStatusCode.Forbidden, (await student.PostAsJsonAsync("/api/Criteria", new { name = "x" })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await student.DeleteAsync("/api/Criteria/x")).StatusCode);
    }
}
