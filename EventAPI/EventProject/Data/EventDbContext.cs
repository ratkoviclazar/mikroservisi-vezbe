using EventAPI.Domains;
using EventAPI.EventSourcing.Persistence;
using EventAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace EventAPI.Data
{
    public class EventsDbContext : DbContext
    {
        public EventsDbContext(DbContextOptions<EventsDbContext> options)
            : base(options)
        {
        }

        public DbSet<Event> Events => Set<Event>();
        public DbSet<EventLecture> EventLectures => Set<EventLecture>();

        public DbSet<EventTypeSnapshot> EventTypeSnapshots => Set<EventTypeSnapshot>();
        public DbSet<LocationSnapshot> LocationSnapshots => Set<LocationSnapshot>();
        public DbSet<LecturerSnapshot> LecturerSnapshots => Set<LecturerSnapshot>();

        public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
        public DbSet<ProcessedMessage> ProcessedMessages => Set<ProcessedMessage>();

        public DbSet<EventStoreEntry> EventStoreEntries => Set<EventStoreEntry>();
        public DbSet<EventSnapshot> EventSnapshots => Set<EventSnapshot>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<EventStoreEntry>(entity =>
            {
                entity.ToTable("EventStoreEntries");

                entity.HasKey(x => x.Id);

                entity.Property(x => x.AggregateType)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(x => x.EventType)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(x => x.EventData)
                    .IsRequired();

                entity.Property(x => x.OccurredAt)
                    .IsRequired();

                entity.HasIndex(x => new { x.AggregateId, x.Version })
                    .IsUnique();

                entity.HasIndex(x => x.AggregateId);
            });

            modelBuilder.Entity<EventSnapshot>(entity =>
            {
                entity.ToTable("EventSnapshots");

                entity.HasKey(x => x.Id);

                entity.Property(x => x.State)
                    .IsRequired();

                entity.Property(x => x.CreatedAt)
                    .IsRequired();

                entity.HasIndex(x => new { x.AggregateId, x.Version })
                    .IsUnique();

                entity.HasIndex(x => x.AggregateId);
            });

            modelBuilder.Entity<Event>(entity =>
            {
                entity.ToTable("Events");

                entity.HasKey(x => x.Id);

                entity.Property(x => x.Name)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(x => x.Agenda)
                    .HasMaxLength(4000);

                entity.Property(x => x.DurationInHours)
                    .HasPrecision(5, 2);

                entity.Property(x => x.Price)
                    .HasColumnType("decimal(18,2)");
            });

            modelBuilder.Entity<EventLecture>(entity =>
            {
                entity.ToTable("EventLectures");

                entity.HasKey(x => x.Id);

                entity.Property(x => x.DurationInHours)
                    .HasPrecision(5, 2);

                entity.HasOne(x => x.Event)
                    .WithMany(x => x.EventLectures)
                    .HasForeignKey(x => x.EventId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<EventTypeSnapshot>(entity =>
            {
                entity.ToTable("EventTypeSnapshots");

                entity.HasKey(x => x.Id);

                entity.HasIndex(x => x.ExternalId)
                    .IsUnique();

                entity.Property(x => x.Name)
                    .IsRequired()
                    .HasMaxLength(200);
            });

            modelBuilder.Entity<LocationSnapshot>(entity =>
            {
                entity.ToTable("LocationSnapshots");

                entity.HasKey(x => x.Id);

                entity.HasIndex(x => x.ExternalId)
                    .IsUnique();

                entity.Property(x => x.Name)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(x => x.Address)
                    .HasMaxLength(300);
            });

            modelBuilder.Entity<LecturerSnapshot>(entity =>
            {
                entity.ToTable("LecturerSnapshots");

                entity.HasKey(x => x.Id);

                entity.HasIndex(x => x.ExternalId)
                    .IsUnique();

                entity.Property(x => x.Name)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(x => x.Surname)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(x => x.Title)
                    .HasMaxLength(100);

                entity.Property(x => x.ExpertiseArea)
                    .HasMaxLength(200);
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

            modelBuilder.Entity<ProcessedMessage>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.Property(x => x.EventId)
                      .IsRequired()
                      .HasMaxLength(100);

                entity.Property(x => x.EventType)
                      .IsRequired()
                      .HasMaxLength(200);

                entity.Property(x => x.ProcessedAtUtc)
                      .IsRequired();

                entity.HasIndex(x => x.EventId)
                      .IsUnique();
            });

        }
    }
}