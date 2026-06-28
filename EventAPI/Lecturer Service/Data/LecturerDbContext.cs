using EventProject.LecturerService.Models;
using Lecturer_Service.Models;
using Microsoft.EntityFrameworkCore;

namespace EventProject.LecturerService.Data
{
    public class LecturerDbContext : DbContext
    {
        public LecturerDbContext(DbContextOptions<LecturerDbContext> options)
            : base(options) { }

        public DbSet<Lecturer> Lecturers => Set<Lecturer>();
        public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Lecturer>(entity =>
            {
                entity.ToTable("Lecturers");

                entity.HasKey(x => x.Id);

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

                entity.Property(x => x.CreatedAt)
                    .IsRequired()
                    .HasDefaultValueSql("GETUTCDATE()");

                entity.Property(x => x.UpdatedAt)
                    .IsRequired()
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