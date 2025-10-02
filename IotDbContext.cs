using IotTgBot;
using Microsoft.EntityFrameworkCore;

public class IotDbContext : DbContext
{
    public DbSet<SensorReading> Readings { get; set; } = null!;

    public IotDbContext(DbContextOptions<IotDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SensorReading>()
            .HasKey(x => x.Id);

        modelBuilder.Entity<SensorReading>()
            .HasIndex(x => x.TimestampUtc);
    }
}
