// One-off import tool for a student roster.
// Usage:
//   IMPORT_DB_CONNECTION="Host=...;Port=5432;Database=...;Username=...;Password=..." \
//   IMPORT_ROSTER_CSV="/path/to/roster.csv" \
//   dotnet run
//
// CSV columns: GroupName,Semester,Surname,Name,Patronymic,StudentNumber
// (Patronymic may be empty)
//
// Idempotent: existing groups (by Name) and existing users (by UserName == StudentNumber) are skipped.
// Temp password for new accounts: "Vsoko{StudentNumber}" + MustChangePassword=true (forced reset on first login).

using Domain.Entities;
using Domain.Enums;
using Infrastructure.DataManager;
using Infrastructure.DataManager.Contexts;
using Infrastructure.SecurityManager.AspNetCoreIdentity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

var connectionString = Environment.GetEnvironmentVariable("IMPORT_DB_CONNECTION");
var csvPath = Environment.GetEnvironmentVariable("IMPORT_ROSTER_CSV");

if (string.IsNullOrWhiteSpace(connectionString))
{
    Console.Error.WriteLine("Set IMPORT_DB_CONNECTION env var with the Postgres connection string.");
    return 1;
}

if (string.IsNullOrWhiteSpace(csvPath) || !File.Exists(csvPath))
{
    Console.Error.WriteLine("Set IMPORT_ROSTER_CSV env var to an existing CSV file (GroupName,Semester,Surname,Name,Patronymic,StudentNumber).");
    return 1;
}

var rows = File.ReadAllLines(csvPath)
    .Skip(1) // header
    .Where(l => !string.IsNullOrWhiteSpace(l))
    .Select(ParseCsvLine)
    .ToList();

var configuration = new ConfigurationBuilder()
    .AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["ConnectionStrings:Database"] = connectionString,
    })
    .Build();

var services = new ServiceCollection();
services.AddLogging();
services.ApplyDataManager(configuration);
services.ApplySecurityManager();

await using var provider = services.BuildServiceProvider();
using var scope = provider.CreateScope();

var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

var groupsCreated = 0;
var studentsCreated = 0;
var studentsSkipped = 0;
var errors = new List<string>();

foreach (var groupRows in rows.GroupBy(r => (r.GroupName, r.Semester)))
{
    var (groupName, semester) = groupRows.Key;

    var group = await db.StudentGroups.FirstOrDefaultAsync(g => g.Name == groupName);

    if (group is null)
    {
        group = new StudentGroup { Name = groupName, Semester = semester };
        db.StudentGroups.Add(group);
        await db.SaveChangesAsync();
        groupsCreated++;
        Console.WriteLine($"[group] created {groupName} (semester {semester})");
    }

    foreach (var row in groupRows)
    {
        var existing = await userManager.FindByNameAsync(row.StudentNumber);
        if (existing is not null)
        {
            studentsSkipped++;
            continue;
        }

        var user = new ApplicationUser
        {
            UserName = row.StudentNumber,
            Name = row.Name,
            Surname = row.Surname,
            Patronymic = string.IsNullOrWhiteSpace(row.Patronymic) ? null : row.Patronymic,
            Type = UserType.Student,
            MustChangePassword = true,
            CreatedAt = DateTime.UtcNow,
        };

        var tempPassword = $"Vsoko{row.StudentNumber}";
        var result = await userManager.CreateAsync(user, tempPassword);

        if (!result.Succeeded)
        {
            errors.Add($"{row.Surname} {row.Name} ({row.StudentNumber}): {string.Join(", ", result.Errors.Select(e => e.Description))}");
            continue;
        }

        db.Students.Add(new Student { Id = user.Id, GroupId = group.Id });
        studentsCreated++;
    }

    await db.SaveChangesAsync();
}

Console.WriteLine();
Console.WriteLine($"Groups created: {groupsCreated}");
Console.WriteLine($"Students created: {studentsCreated}");
Console.WriteLine($"Students skipped (already existed): {studentsSkipped}");

if (errors.Count > 0)
{
    Console.WriteLine($"Errors: {errors.Count}");
    foreach (var e in errors)
        Console.WriteLine($"  - {e}");
    return 1;
}

return 0;

static (string GroupName, int Semester, string Surname, string Name, string Patronymic, string StudentNumber) ParseCsvLine(string line)
{
    var parts = line.Split(',');
    return (
        GroupName: parts[0],
        Semester: int.Parse(parts[1]),
        Surname: parts[2],
        Name: parts[3],
        Patronymic: parts[4],
        StudentNumber: parts[5]
    );
}
