using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

using Infrastructure.DataManager.Contexts;

namespace Infrastructure.DataManager;

// EF tooling and the migration bundle do not need Redis, JWT or OpenRouter.
public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Database");
        var options = new DbContextOptionsBuilder<AppDbContext>();
        // A connection is only required when applying migrations, not when building the bundle.
        options.UseNpgsql(connectionString ?? "Host=localhost;Database=vsoko;Username=vsoko_app");
        return new AppDbContext(options.Options);
    }
}
