using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Racing.Infrastructure.Persistence;

public sealed class RacingDbContextFactory : IDesignTimeDbContextFactory<RacingDbContext>
{
    public RacingDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<RacingDbContext>();
        optionsBuilder.UseNpgsql(
            "Host=localhost;Port=5432;Database=racing_db;Username=racing_user;Password=development");

        return new RacingDbContext(optionsBuilder.Options);
    }
}
