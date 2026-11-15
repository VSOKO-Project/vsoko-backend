using Infrastructure.DataManager.Contexts;
using Microsoft.EntityFrameworkCore;

namespace Api.IntegrationTests;

public class ReportHistoryModelTests
{
    [Fact]
    public void ArchiveMigrationMatchesTheModelAndStoresPdfBytes()
    {
        using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Database=model_only;Username=test").Options);
        Assert.Contains("20261115174500_AddReportHistory", db.Database.GetMigrations());
        Assert.False(db.Database.HasPendingModelChanges());
        var sql = db.Database.GenerateCreateScript();
        Assert.Contains("CREATE TABLE \"ReportArchives\"", sql);
        Assert.Contains("\"Content\" bytea NOT NULL", sql);
    }
}
