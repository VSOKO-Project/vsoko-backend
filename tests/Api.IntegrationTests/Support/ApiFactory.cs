using Domain.Entities;
using Domain.Enums;
using Infrastructure.DataManager.Contexts;
using Infrastructure.SecurityManager.AspNetCoreIdentity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;

namespace Api.IntegrationTests.Support;

/// <summary>
/// Приложение целиком поверх настоящих Postgres и Redis в контейнерах.
/// База мигрируется и засеивается DataSeeder: admin/Admin123!, student1..24/Student123!.
/// Вместо OpenAI подставлен <see cref="FakeChatCompletion"/>.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string AdminLogin = "admin";
    public const string AdminPassword = "Admin123!";
    public const string StudentPassword = "Student123!";
    public const string JwtSecret = "integration-tests-secret-key-0123456789-abcdef";

    private readonly PostgreSqlContainer _db = new PostgreSqlBuilder().WithImage("postgres:16-alpine").Build();
    private readonly RedisContainer _redis = new RedisBuilder().WithImage("redis:7-alpine").Build();

    public FakeChatCompletion Chat { get; } = new();

    public string DatabaseConnectionString => _db.GetConnectionString();

    public string RedisConnectionString => _redis.GetConnectionString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:Database", DatabaseConnectionString);
        builder.UseSetting("ConnectionStrings:Redis", RedisConnectionString);
        builder.UseSetting("Jwt:SecretKey", JwtSecret);
        builder.UseSetting("AI:ApiKey", "test-key");
        builder.UseSetting("Cors:AllowedOrigins", "http://localhost:3000");

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<Kernel>();
            services.AddSingleton(Chat.CreateKernel());
        });
    }

    public async Task InitializeAsync()
    {
        await Task.WhenAll(_db.StartAsync(), _redis.StartAsync());
        // Запуск хоста: миграции и сидинг.
        _ = Server;
    }

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
        await _db.DisposeAsync();
        await _redis.DisposeAsync();
    }

    public async Task<T> WithScopeAsync<T>(Func<IServiceProvider, Task<T>> action)
    {
        using var scope = Services.CreateScope();
        return await action(scope.ServiceProvider);
    }

    public Task WithDbAsync(Func<AppDbContext, Task> action) =>
        WithScopeAsync(async sp =>
        {
            await action(sp.GetRequiredService<AppDbContext>());
            return true;
        });

    public Task<T> WithDbAsync<T>(Func<AppDbContext, Task<T>> action) =>
        WithScopeAsync(sp => action(sp.GetRequiredService<AppDbContext>()));

    public static string Unique(string prefix) => $"{prefix}{Guid.NewGuid():N}"[..Math.Min(prefix.Length + 8, 20)];

    /// <summary>Создаёт студента в новой (или указанной) группе.</summary>
    public Task<ApplicationUser> CreateStudentAsync(
        string? groupId = null,
        bool mustChangePassword = false,
        UserType type = UserType.Student,
        bool? isBlocked = null
    ) =>
        WithScopeAsync(async sp =>
        {
            var db = sp.GetRequiredService<AppDbContext>();
            if (groupId is null)
            {
                var group = new StudentGroup { Name = Unique("T-"), Semester = 1 };
                db.StudentGroups.Add(group);
                await db.SaveChangesAsync();
                groupId = group.Id;
            }

            var user = await CreateUserAsync(sp, type, mustChangePassword, isBlocked);
            db.Students.Add(new Student { Id = user.Id, GroupId = groupId });
            await db.SaveChangesAsync();
            return user;
        });

    /// <summary>Создаёт сотрудника, по желанию с ролью Identity «Admin».</summary>
    public Task<ApplicationUser> CreateEmployeeAsync(bool isAdmin) =>
        WithScopeAsync(async sp =>
        {
            var db = sp.GetRequiredService<AppDbContext>();
            var user = await CreateUserAsync(sp, UserType.Employee, false, null);
            var role = await db.EmployeeRoles.FirstAsync();
            db.Employees.Add(new Employee { Id = user.Id, RoleId = role.Id });
            await db.SaveChangesAsync();
            if (isAdmin)
                await sp.GetRequiredService<UserManager<ApplicationUser>>().AddToRoleAsync(user, "Admin");
            return user;
        });

    private static async Task<ApplicationUser> CreateUserAsync(
        IServiceProvider sp,
        UserType type,
        bool mustChangePassword,
        bool? isBlocked
    )
    {
        var user = new ApplicationUser
        {
            UserName = Unique("u"),
            Name = "Тест",
            Surname = "Тестов",
            Type = type,
            MustChangePassword = mustChangePassword,
            IsBlocked = isBlocked,
        };
        var result = await sp.GetRequiredService<UserManager<ApplicationUser>>().CreateAsync(user, StudentPassword);
        Assert.True(result.Succeeded, string.Join("; ", result.Errors.Select(e => e.Description)));
        return user;
    }
}

[CollectionDefinition(Name)]
public class ApiCollection : ICollectionFixture<ApiFactory>
{
    public const string Name = "api";
}

/// <summary>Модель чата с управляемым ответом.</summary>
public sealed class FakeChatCompletion : IChatCompletionService
{
    public Func<ChatHistory, string?> Respond { get; set; } = _ => "Сводка от ИИ";

    public List<ChatHistory> Requests { get; } = [];

    public IReadOnlyDictionary<string, object?> Attributes { get; } = new Dictionary<string, object?>();

    public Kernel CreateKernel()
    {
        var builder = Kernel.CreateBuilder();
        builder.Services.AddSingleton<IChatCompletionService>(this);
        return builder.Build();
    }

    public Task<IReadOnlyList<ChatMessageContent>> GetChatMessageContentsAsync(
        ChatHistory chatHistory,
        PromptExecutionSettings? executionSettings = null,
        Kernel? kernel = null,
        CancellationToken cancellationToken = default)
    {
        Requests.Add(chatHistory);
        IReadOnlyList<ChatMessageContent> result = [new ChatMessageContent(AuthorRole.Assistant, Respond(chatHistory))];
        return Task.FromResult(result);
    }

    public IAsyncEnumerable<StreamingChatMessageContent> GetStreamingChatMessageContentsAsync(
        ChatHistory chatHistory,
        PromptExecutionSettings? executionSettings = null,
        Kernel? kernel = null,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
}
