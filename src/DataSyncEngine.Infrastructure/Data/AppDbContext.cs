using DataSyncEngine.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace DataSyncEngine.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<SyncLog> SyncLogs => Set<SyncLog>();

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("Products");

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).UseIdentityColumn();

            entity.HasIndex(e => e.ExternalId)
                  .IsUnique()
                  .HasFilter(null);

            entity.Property(e => e.ExternalId)
                  .IsRequired()
                  .HasMaxLength(100);

            entity.Property(e => e.Name)
                  .IsRequired()
                  .HasMaxLength(500);

            entity.Property(e => e.CategoryCode)
                  .IsRequired()
                  .HasMaxLength(50);

            entity.Property(e => e.Price)
                  .HasColumnType("decimal(18,4)");

            entity.Property(e => e.StockQuantity)
                  .HasDefaultValue(0);

            entity.Property(e => e.IsActive)
                  .HasDefaultValue(true);

            entity.Property(e => e.IsDeleted)
                  .HasDefaultValue(false);

            entity.Property(e => e.LastSyncedAtUtc)
                  .HasColumnType("datetime2");

            entity.Property(e => e.CreatedAtUtc)
                  .HasColumnType("datetime2")
                  .HasDefaultValueSql("SYSUTCDATETIME()");

            entity.Property(e => e.UpdatedAtUtc)
                  .HasColumnType("datetime2")
                  .HasDefaultValueSql("SYSUTCDATETIME()");
        });

        modelBuilder.Entity<SyncLog>(entity =>
        {
            entity.ToTable("SyncLogs");

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).UseIdentityColumn();

            entity.Property(e => e.JobName)
                  .IsRequired()
                  .HasMaxLength(200);

            entity.Property(e => e.StartedAtUtc)
                  .HasColumnType("datetime2");

            entity.Property(e => e.CompletedAtUtc)
                  .HasColumnType("datetime2");

            entity.Property(e => e.Status)
                  .IsRequired()
                  .HasMaxLength(50);

            entity.Property(e => e.ErrorMessage)
                  .HasMaxLength(4000);
        });
    }
}
