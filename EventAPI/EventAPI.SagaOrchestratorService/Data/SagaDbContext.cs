using EventAPI.SagaOrchestratorService.Models;
using Microsoft.EntityFrameworkCore;

namespace EventAPI.SagaOrchestratorService.Data
{
    public class SagaDbContext : DbContext
    {
        public SagaDbContext(DbContextOptions<SagaDbContext> options)
            : base(options)
        {
        }

        public DbSet<SagaState> SagaStates => Set<SagaState>();
        public DbSet<SagaOutboxMessage> SagaOutboxMessages => Set<SagaOutboxMessage>();
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<SagaState>(entity =>
            {
                entity.ToTable("SagaStates");

                entity.HasKey(x => x.Id);

                entity.Property(x => x.SagaType)
                    .IsRequired()
                    .HasMaxLength(150);

                entity.Property(x => x.CurrentStep)
                    .IsRequired()
                    .HasMaxLength(150);

                entity.Property(x => x.Status)
                    .HasConversion<string>()
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(x => x.EventName)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(x => x.EventAgenda)
                    .IsRequired()
                    .HasMaxLength(1000);

                entity.Property(x => x.EventDurationInHours)
                    .HasColumnType("decimal(18,2)");

                entity.Property(x => x.EventPrice)
                    .HasColumnType("decimal(18,2)");

                entity.Property(x => x.LectureDurationInHours)
                    .HasColumnType("decimal(18,2)");

                entity.Property(x => x.Log)
                    .IsRequired();

                entity.Property(x => x.StartedAtUtc)
                    .IsRequired();

                entity.HasIndex(x => x.Status);

                entity.HasIndex(x => x.StartedAtUtc);
            });

            modelBuilder.Entity<SagaOutboxMessage>(entity =>
            {
                entity.ToTable("SagaOutboxMessages");

                entity.HasKey(x => x.Id);

                entity.Property(x => x.MessageId)
                    .IsRequired();

                entity.Property(x => x.SagaId)
                    .IsRequired();

                entity.Property(x => x.Exchange)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(x => x.RoutingKey)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(x => x.Payload)
                    .IsRequired();

                entity.Property(x => x.CreatedAtUtc)
                    .IsRequired();

                entity.HasIndex(x => x.MessageId)
                    .IsUnique();

                entity.HasIndex(x => new { x.IsPublished, x.CreatedAtUtc });
            });
        }
    }
}
