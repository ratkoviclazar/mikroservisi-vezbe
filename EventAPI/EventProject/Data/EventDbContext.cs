using EventAPI.Domains;
using Microsoft.EntityFrameworkCore;

namespace EventAPI.Data
{
    public class EventDbContext : DbContext
    {
        public EventDbContext(DbContextOptions<EventDbContext> options) : base(options) { }

        protected EventDbContext() { }

        public DbSet<Event> Events => Set<Event>();
        public DbSet<EventLecture> EventLectures => Set<EventLecture>();
        public DbSet<EventType> EventTypes => Set<EventType>();
        public DbSet<Lecturer> Lecturers => Set<Lecturer>();
        public DbSet<Location> Locations => Set<Location>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<EventType>(entity =>
            {
                entity.ToTable("Types");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            });

            modelBuilder.Entity<Location>(entity =>
            {
                entity.ToTable("Locations");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Address).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Capacity).IsRequired();
            });

            modelBuilder.Entity<Lecturer>(entity =>
            {
                entity.ToTable("Lecturers");

                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Surname).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
                entity.Property(e => e.ExpertiseArea).IsRequired().HasMaxLength(200);
            });

            modelBuilder.Entity<Event>(entity =>
            {
                entity.ToTable("Events");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Agenda).IsRequired().HasMaxLength(200);
                entity.Property(e => e.DateTime).IsRequired();
                entity.Property(e => e.DurationInHours).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Price).HasColumnType("decimal(18,2)");

                entity.HasOne(e => e.EventType)
                    .WithMany(t => t.Events)
                    .HasForeignKey(e => e.TypeId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Location)
                    .WithMany(l => l.Events)
                    .HasForeignKey(e => e.LocationId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<EventLecture>(entity =>
            {
                entity.ToTable("EventLectures");
                entity.HasKey(el => el.Id);
                entity.HasIndex(el => new { el.DateTime, el.EventId, el.LecturerId }).IsUnique();
                entity.Property(el => el.DurationInHours).HasColumnType("decimal(18,2)");

                entity.HasOne(el => el.Event)
                    .WithMany(e => e.EventLectures)
                    .HasForeignKey(el => el.EventId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(el => el.Lecturer)
                    .WithMany(l => l.EventLectures)
                    .HasForeignKey(el => el.LecturerId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
            modelBuilder.Entity<EventType>().HasData(
                new EventType { Id = 1, Name = "Seminar" },
                new EventType { Id = 2, Name = "Workshop" }
            );

            modelBuilder.Entity<Location>().HasData(
                new Location { Id = 1, Name = "Hall A", Address = "Main Street 1", Capacity = 100 },
                new Location { Id = 2, Name = "Hall B", Address = "Main Street 2", Capacity = 200 }
            );

            modelBuilder.Entity<Lecturer>().HasData(
                new Lecturer { Id = 1, Name = "John", Surname = "Doe", Title = "Professor", ExpertiseArea = "IT" },
                new Lecturer { Id = 2, Name = "Jane", Surname = "Smith", Title = "Dr", ExpertiseArea = "AI" }
            );

            modelBuilder.Entity<Event>().HasData(
                new Event
                {
                    Id = 1,
                    Name = "Tech Conference",
                    Agenda = "Tech topics",
                    DateTime = new DateTime(2025, 5, 1),
                    DurationInHours = 5,
                    Price = 100,
                    TypeId = 1,
                    LocationId = 1
                }
            );

            modelBuilder.Entity<EventLecture>().HasData(
                new EventLecture
                {
                    Id = 1,
                    EventId = 1,
                    LecturerId = 1,
                    DateTime = new DateTime(2025, 5, 1, 10, 0, 0),
                    DurationInHours = 2
                }
            );
        }
    }
}
