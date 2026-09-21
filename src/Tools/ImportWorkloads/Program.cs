// One-off import tool for teacher workload assignments.
// Usage:
//   IMPORT_DB_CONNECTION="Host=...;Port=5432;Database=...;Username=...;Password=..." \
//   IMPORT_WORKLOADS_CSV="/path/to/workloads.csv" \
//   dotnet run
//
// CSV columns: TeacherFio,Discipline,GroupName
// (TeacherFio = "Фамилия Имя Отчество", any casing)
//
// Idempotent: existing teachers (by Surname+Name+Patronymic), disciplines (by Name),
// and workloads (by Teacher+Discipline+Group) are reused/skipped. Groups must already exist.

using Domain.Entities;
using Infrastructure.DataManager;
using Infrastructure.DataManager.Contexts;
using Infrastructure.SecurityManager.AspNetCoreIdentity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

var connectionString = Environment.GetEnvironmentVariable("IMPORT_DB_CONNECTION");
var csvPath = Environment.GetEnvironmentVariable("IMPORT_WORKLOADS_CSV");

if (string.IsNullOrWhiteSpace(connectionString))
{
    Console.Error.WriteLine("Set IMPORT_DB_CONNECTION env var with the Postgres connection string.");
    return 1;
}

if (string.IsNullOrWhiteSpace(csvPath) || !File.Exists(csvPath))
{
    Console.Error.WriteLine("Set IMPORT_WORKLOADS_CSV env var to an existing CSV file (TeacherFio,Discipline,GroupName).");
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

var teachersCreated = 0;
var disciplinesCreated = 0;
var workloadsCreated = 0;
var workloadsSkipped = 0;
var errors = new List<string>();

foreach (var row in rows)
{
    var group = await db.StudentGroups.FirstOrDefaultAsync(g => g.Name == row.GroupName);
    if (group is null)
    {
        errors.Add($"Group not found: {row.GroupName} ({row.TeacherFio} / {row.Discipline})");
        continue;
    }

    var fioParts = row.TeacherFio.Split(' ', StringSplitOptions.RemoveEmptyEntries);
    if (fioParts.Length < 3)
    {
        errors.Add($"Unexpected teacher FIO format: {row.TeacherFio}");
        continue;
    }

    var surname = TitleCase(fioParts[0]);
    var name = TitleCase(fioParts[1]);
    var patronymic = TitleCase(string.Join(' ', fioParts[2..]));

    var teacher = await db.Teachers.FirstOrDefaultAsync(t =>
        t.Surname == surname && t.Name == name && t.Patronymic == patronymic);

    if (teacher is null)
    {
        teacher = new Teacher { Surname = surname, Name = name, Patronymic = patronymic };
        db.Teachers.Add(teacher);
        await db.SaveChangesAsync();
        teachersCreated++;
    }

    var discipline = await db.Disciplines.FirstOrDefaultAsync(d => d.Name == row.Discipline);
    if (discipline is null)
    {
        discipline = new Discipline { Name = row.Discipline };
        db.Disciplines.Add(discipline);
        await db.SaveChangesAsync();
        disciplinesCreated++;
    }

    var existingWorkload = await db.Workloads.FirstOrDefaultAsync(w =>
        w.TeacherId == teacher.Id && w.DisciplineId == discipline.Id && w.GroupId == group.Id);

    if (existingWorkload is not null)
    {
        workloadsSkipped++;
        continue;
    }

    db.Workloads.Add(new Workload
    {
        TeacherId = teacher.Id,
        DisciplineId = discipline.Id,
        GroupId = group.Id,
    });
    await db.SaveChangesAsync();
    workloadsCreated++;
}

Console.WriteLine();
Console.WriteLine($"Teachers created: {teachersCreated}");
Console.WriteLine($"Disciplines created: {disciplinesCreated}");
Console.WriteLine($"Workloads created: {workloadsCreated}");
Console.WriteLine($"Workloads skipped (already existed): {workloadsSkipped}");

if (errors.Count > 0)
{
    Console.WriteLine($"Errors: {errors.Count}");
    foreach (var e in errors)
        Console.WriteLine($"  - {e}");
    return 1;
}

return 0;

static string TitleCase(string s) =>
    string.Join(' ', s.Split(' ', StringSplitOptions.RemoveEmptyEntries)
        .Select(w => char.ToUpperInvariant(w[0]) + w[1..].ToLowerInvariant()));

static (string TeacherFio, string Discipline, string GroupName) ParseCsvLine(string line)
{
    var parts = line.Split(',');
    return (
        TeacherFio: parts[0].Trim(),
        Discipline: parts[1].Trim(),
        GroupName: parts[2].Trim()
    );
}
