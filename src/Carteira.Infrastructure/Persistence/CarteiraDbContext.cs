using Carteira.Domain.Orders;
using Microsoft.EntityFrameworkCore;

namespace Carteira.Infrastructure.Persistence;

public sealed class CarteiraDbContext : DbContext
{
    public CarteiraDbContext(DbContextOptions<CarteiraDbContext> options)
        : base(options)
    {
    }

    public DbSet<Order> Orders => Set<Order>();
    public DbSet<IdempotencyRecord> IdempotencyRecords => Set<IdempotencyRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<IdempotencyRecord>(record =>
        {
            record.ToTable("idempotency_records");

            record.HasKey(x => x.Key);

            record.Property(x => x.Fingerprint)
                .HasMaxLength(200)
                .IsRequired();

            record.HasOne<Order>()
                .WithMany()
                .HasForeignKey(x => x.OrderId)
                .OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<Order>(order =>
        {
            order.ToTable("orders");

            order.HasKey(x => x.Id);

            order.Property(x => x.Sequence)
                .UseIdentityByDefaultColumn();

            order.HasIndex(x => x.Sequence).IsUnique();

            order.Property(x => x.Symbol)
                .HasMaxLength(12)
                .IsRequired();

            order.Property(x => x.Side)
                .HasConversion<string>()
                .HasMaxLength(4)
                .IsRequired();

            order.Property(x => x.Quantity)
                .HasPrecision(18, 4);

            order.Property(x => x.Price)
                .HasPrecision(18, 2);

            order.Property(x => x.CreatedAt)
                .IsRequired();

            order.HasIndex(x => x.CreatedAt);
        });
    }
}


