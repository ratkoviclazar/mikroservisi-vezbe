using EventProject.LecturerService.Models;
using Microsoft.EntityFrameworkCore;

namespace EventProject.LecturerService.Data
{
    public class LecturerDbContext : DbContext
    {
        public LecturerDbContext(DbContextOptions<LecturerDbContext> options)
            : base(options) { }

        public DbSet<Lecturer> Lecturers => Set<Lecturer>();

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
        }
    }
}