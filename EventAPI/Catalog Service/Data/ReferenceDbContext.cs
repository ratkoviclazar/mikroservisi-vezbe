using Catalog_Service.Models;
using EventProject.CatalogService.Models;
using Microsoft.EntityFrameworkCore;

namespace EventProject.CatalogService.Data
{
    public class ReferenceDbContext : DbContext
    {
        public ReferenceDbContext(DbContextOptions<ReferenceDbContext> options)
            : base(options) { }

        public DbSet<Location> Locations => Set<Location>();
        public DbSet<EventType> EventTypes => Set<EventType>();

        public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
        public DbSet<LocationReservation> LocationReservations => Set<LocationReservation>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<EventType>(entity =>
            {
                entity.ToTable("EventTypes");

                entity.HasKey(e => e.Id);

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.HasIndex(e => e.Name).IsUnique();

                entity.Property(x => x.CreatedAt)
                    .HasDefaultValueSql("GETUTCDATE()");

                entity.Property(x => x.UpdatedAt)
                    .HasDefaultValueSql("GETUTCDATE()");
            });

            modelBuilder.Entity<Location>(entity =>
            {
                entity.ToTable("Locations");

                entity.HasKey(e => e.Id);

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(e => e.Address)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(e => e.Capacity)
                    .IsRequired();

                entity.HasIndex(e => e.Name).IsUnique();

                entity.Property(x => x.CreatedAt)
                    .HasDefaultValueSql("GETUTCDATE()");

                entity.Property(x => x.UpdatedAt)
                    .HasDefaultValueSql("GETUTCDATE()");
            });

            modelBuilder.Entity<LocationReservation>(entity =>
            {
                entity.ToTable("LocationReservations");

                entity.HasKey(x => x.Id);

                entity.Property(x => x.SagaId)
                    .IsRequired();

                entity.Property(x => x.CorrelationId)
                    .IsRequired();

                entity.Property(x => x.EventId)
                    .IsRequired();

                entity.Property(x => x.LocationId)
                    .IsRequired();

                entity.Property(x => x.EventDateTime)
                    .IsRequired();

                entity.Property(x => x.IsCancelled)
                    .HasDefaultValue(false);

                entity.Property(x => x.CancelReason)
                    .HasMaxLength(500);

                entity.Property(x => x.CreatedAt)
                    .HasDefaultValueSql("GETUTCDATE()");

                entity.Property(x => x.CancelledAt)
                    .IsRequired(false);

                entity.HasOne(x => x.Location)
                    .WithMany()
                    .HasForeignKey(x => x.LocationId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(x => x.SagaId);

                entity.HasIndex(x => x.CorrelationId);

                entity.HasIndex(x => new { x.EventId, x.LocationId, x.EventDateTime });
            });

            modelBuilder.Entity<OutboxMessage>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.Property(x => x.Type)
                      .IsRequired()
                      .HasMaxLength(200);

                entity.Property(x => x.Payload)
                      .IsRequired();

                entity.Property(x => x.CreatedAt)
                      .IsRequired();

                entity.Property(x => x.IsProcessed)
                      .HasDefaultValue(false);

                entity.HasIndex(x => x.IsProcessed);
                entity.HasIndex(x => x.MessageId)
                      .IsUnique();
            });
        }
    }
}