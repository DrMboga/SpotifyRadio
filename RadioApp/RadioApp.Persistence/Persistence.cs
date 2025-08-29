using Microsoft.EntityFrameworkCore;
using RadioApp.Common.Contracts;
using RadioApp.Persistence.Model;

namespace RadioApp.Persistence;

// dotnet tool install --global dotnet-ef
// dotnet ef migrations add InitialCreate
public class Persistence: DbContext
{
    private static string DbPath => "RadioSettings.db";

    [Obsolete("Delete this entity when restructure database")]
    public DbSet<RadioRegionEntity> RadioRegion { get; set; }
    public DbSet<RadioStationEntity> RadioStation { get; set; }
    public DbSet<SpotifySettings> SpotifySettings { get; set; }

    // The following configures EF to create a Sqlite database file in the
    // special "local" folder for your platform.
    protected override void OnConfiguring(DbContextOptionsBuilder options)
        => options.UseSqlite($"Data Source={Path.Combine(Directory.GetCurrentDirectory(), "Data", DbPath)}");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SpotifySettings>()
            .HasKey(s => s.ClientId);
        
        modelBuilder.Entity<RadioRegionEntity>()
            .Property(r => r.Id)
            .ValueGeneratedOnAdd();
        modelBuilder.Entity<RadioRegionEntity>()
            .HasKey(r => r.Id);
        modelBuilder.Entity<RadioRegionEntity>()
            .HasIndex(r => r.Region).IsUnique();

        modelBuilder.Entity<RadioStationEntity>()
            .Property(s => s.Id)
            .ValueGeneratedOnAdd();
        modelBuilder.Entity<RadioStationEntity>()
            .HasKey(s => s.Id);
        modelBuilder.Entity<RadioStationEntity>()
            .HasIndex(s => s.Region).IsUnique(false);
    }
}


