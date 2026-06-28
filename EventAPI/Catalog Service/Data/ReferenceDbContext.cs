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