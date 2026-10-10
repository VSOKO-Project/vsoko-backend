using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Api.IntegrationTests.Support;
using Application.Common.DTOs;
using Application.Common.ResultsDto;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace Api.IntegrationTests;

[Collection(ApiCollection.Name)]
public class AuthTests(ApiFactory factory)
{
    private HttpClient Anonymous => factory.CreateClient();

    private static async Task<HttpStatusCode> GetCriteriaStatus(HttpClient client) =>
        (await client.GetAsync("/api/Criteria")).StatusCode;

    [Fact]
    public async Task Admin_Login_IsAdmin()
    {
        var result = await factory.LoginAsync(ApiFactory.AdminLogin, ApiFactory.AdminPassword);

        Assert.True(result.IsAdmin);
        Assert.False(result.MustChangePassword);
        Assert.False(string.IsNullOrEmpty(result.RefreshToken));
        Assert.True(result.Expires > DateTime.UtcNow);
    }

    [Fact]
    public async Task Student_Login_GetsStudentToken()
    {
        var result = await factory.LoginAsync("student1", ApiFactory.StudentPassword);

        Assert.False(result.IsAdmin);
        var token = new JwtSecurityTokenHandler().ReadJwtToken(result.AccessToken);
        Assert.Contains(token.Claims, c => c.Type == ClaimTypes.Role && c.Value == "student");
        Assert.Contains(token.Claims, c => c.Type == "group");
        Assert.Equal(HttpStatusCode.OK, await GetCriteriaStatus(factory.ClientWithToken(result.AccessToken)));
    }

    [Theory]
    [InlineData("student2", "wrong-password")]
    [InlineData("no-such-user", "whatever")]
    public async Task Login_BadCredentials_Is401(string login, string password)
    {
        var response = await Anonymous.PostAsJsonAsync("/api/Security/Login", new { login, password });

        await response.ProblemAsync(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_EmptyBody_Is400()
    {
        var response = await Anonymous.PostAsJsonAsync("/api/Security/Login", new { login = "", password = "" });

        var problem = await response.ProblemAsync(HttpStatusCode.BadRequest);
        Assert.Equal("Validation Exception", problem.GetProperty("title").GetString());
    }

    [Fact]
    public async Task Login_BlockedUser_Is401()
    {
        var user = await factory.CreateStudentAsync(isBlocked: true);

        var response = await Anonymous.PostAsJsonAsync("/api/Security/Login", new { login = user.UserName, password = ApiFactory.StudentPassword });

        await response.ProblemAsync(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_UserWithUnknownType_IsDomainError()
    {
        var user = await factory.CreateStudentAsync(type: (UserType)42);

        var response = await Anonymous.PostAsJsonAsync("/api/Security/Login", new { login = user.UserName, password = ApiFactory.StudentPassword });

        var problem = await response.ProblemAsync(HttpStatusCode.BadRequest);
        Assert.Equal("Domain Exception", problem.GetProperty("title").GetString());
    }

    [Fact]
    public async Task Refresh_RotatesToken_OldOneIsRejected()
    {
        var user = await factory.CreateStudentAsync();
        var login = await factory.LoginAsync(user.UserName!, ApiFactory.StudentPassword);

        var refreshed = await (await Anonymous.PostAsJsonAsync("/api/Security/Refresh", new { refresh = login.RefreshToken }))
            .DataAsync<LoginResultDto>();

        Assert.NotEqual(login.RefreshToken, refreshed.RefreshToken);
        Assert.Equal(HttpStatusCode.OK, await GetCriteriaStatus(factory.ClientWithToken(refreshed.AccessToken)));
        // Сессия та же, поэтому старый access-токен тоже действует.
        Assert.Equal(HttpStatusCode.OK, await GetCriteriaStatus(factory.ClientWithToken(login.AccessToken)));

        var reuse = await Anonymous.PostAsJsonAsync("/api/Security/Refresh", new { refresh = login.RefreshToken });
        await reuse.ProblemAsync(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_LegacySessionWithoutStamp_IsRejected()
    {
        var user = await factory.CreateStudentAsync();
        var token = Guid.NewGuid().ToString();
        await factory.WithDbAsync(async db =>
        {
            db.Refreshes.Add(new Refresh { UserId = user.Id, Token = token, ExpiresAt = DateTime.UtcNow.AddDays(1) });
            await db.SaveChangesAsync();
        });

        var response = await Anonymous.PostAsJsonAsync("/api/Security/Refresh", new { refresh = token });

        Assert.Equal("Session revoked", (await response.ProblemAsync(HttpStatusCode.Unauthorized)).GetProperty("detail").GetString());
    }

    [Fact]
    public async Task Refresh_Unknown_Is401_Empty_Is400()
    {
        await (await Anonymous.PostAsJsonAsync("/api/Security/Refresh", new { refresh = "nope" })).ProblemAsync(HttpStatusCode.Unauthorized);
        await (await Anonymous.PostAsJsonAsync("/api/Security/Refresh", new { refresh = "" })).ProblemAsync(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task LogOut_RevokesSessionAndAccessToken()
    {
        var user = await factory.CreateStudentAsync();
        var login = await factory.LoginAsync(user.UserName!, ApiFactory.StudentPassword);

        var response = await Anonymous.PostAsJsonAsync("/api/Security/LogOut", new { refresh = login.RefreshToken });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        Assert.Equal(HttpStatusCode.Unauthorized, await GetCriteriaStatus(factory.ClientWithToken(login.AccessToken)));
        await (await Anonymous.PostAsJsonAsync("/api/Security/Refresh", new { refresh = login.RefreshToken })).ProblemAsync(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PendingPasswordChange_BlocksResourcesUntilChanged()
    {
        var user = await factory.CreateStudentAsync(mustChangePassword: true);
        var login = await factory.LoginAsync(user.UserName!, ApiFactory.StudentPassword);
        Assert.True(login.MustChangePassword);
        var client = factory.ClientWithToken(login.AccessToken);

        var blocked = await client.GetAsync("/api/Criteria");
        Assert.Equal("Password Change Required", (await blocked.ProblemAsync(HttpStatusCode.Forbidden)).GetProperty("title").GetString());

        var wrong = await client.PostAsJsonAsync("/api/Security/ChangePassword", new { currentPassword = "bad", newPassword = "NewPass1" });
        await wrong.ProblemAsync(HttpStatusCode.BadRequest);

        var tooShort = await client.PostAsJsonAsync("/api/Security/ChangePassword", new { currentPassword = ApiFactory.StudentPassword, newPassword = "1" });
        await tooShort.ProblemAsync(HttpStatusCode.BadRequest);

        var changed = await (await client.PostAsJsonAsync("/api/Security/ChangePassword",
            new { currentPassword = ApiFactory.StudentPassword, newPassword = "NewPass1" })).DataAsync<LoginResultDto>();

        Assert.False(changed.MustChangePassword);
        Assert.Equal(HttpStatusCode.OK, await GetCriteriaStatus(factory.ClientWithToken(changed.AccessToken)));
        // Смена пароля отзывает прежние сессии.
        Assert.Equal(HttpStatusCode.Unauthorized, await GetCriteriaStatus(client));
        await (await Anonymous.PostAsJsonAsync("/api/Security/Refresh", new { refresh = login.RefreshToken })).ProblemAsync(HttpStatusCode.Unauthorized);
        Assert.True((await factory.LoginAsync(user.UserName!, "NewPass1")).AccessToken is { Length: > 0 });
    }

    [Fact]
    public async Task Anonymous_IsRejected()
    {
        Assert.Equal(HttpStatusCode.Unauthorized, await GetCriteriaStatus(Anonymous));
        Assert.Equal(HttpStatusCode.Unauthorized, (await Anonymous.PostAsJsonAsync("/api/Security/ChangePassword", new { })).StatusCode);
    }

    [Fact]
    public async Task TokenWithoutSession_IsRejected()
    {
        var user = await factory.CreateStudentAsync();
        var jwt = new JwtSecurityToken(
            issuer: "VSOKOproject.Client",
            audience: "VSOKOproject.API",
            claims: [new Claim(ClaimTypes.NameIdentifier, user.Id)],
            expires: DateTime.UtcNow.AddMinutes(5),
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(ApiFactory.JwtSecret)), SecurityAlgorithms.HmacSha256));

        var client = factory.ClientWithToken(new JwtSecurityTokenHandler().WriteToken(jwt));

        Assert.Equal(HttpStatusCode.Unauthorized, await GetCriteriaStatus(client));
    }

    [Fact]
    public async Task StudentOfDeletedGroup_LosesAccess()
    {
        var user = await factory.CreateStudentAsync();
        var client = await factory.LoginClientAsync(user.UserName!, ApiFactory.StudentPassword);
        Assert.Equal(HttpStatusCode.OK, await GetCriteriaStatus(client));

        await factory.WithDbAsync(async db =>
        {
            var student = await db.Students.Include(s => s.GroupRef).SingleAsync(s => s.Id == user.Id);
            student.GroupRef!.IsDeleted = true;
            await db.SaveChangesAsync();
        });

        Assert.Equal(HttpStatusCode.Unauthorized, await GetCriteriaStatus(client));
    }

    [Fact]
    public async Task EmployeeWithoutAdminRole_HasNoStudentOrAdminAccess()
    {
        var user = await factory.CreateEmployeeAsync(isAdmin: false);
        var login = await factory.LoginAsync(user.UserName!, ApiFactory.StudentPassword);
        var client = factory.ClientWithToken(login.AccessToken);

        Assert.False(login.IsAdmin);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/Teachers")).StatusCode);
        // Роль «employee» не даёт ни студенческой, ни админской выборки.
        await (await client.GetAsync("/api/Workload")).ProblemAsync(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task EmployeeWithAdminRole_IsAdmin()
    {
        var user = await factory.CreateEmployeeAsync(isAdmin: true);
        var login = await factory.LoginAsync(user.UserName!, ApiFactory.StudentPassword);

        Assert.True(login.IsAdmin);
        var teachers = await (await factory.ClientWithToken(login.AccessToken).GetAsync("/api/Teachers"))
            .DataAsync<Application.Common.Results.PagedResultDto<TeacherDto>>();
        Assert.NotEmpty(teachers.Items!);
    }
}
