using Microsoft.EntityFrameworkCore;
using Racing.Infrastructure.Persistence.Entities;

namespace Racing.Infrastructure.Persistence;

public sealed class RacingDbContext : DbContext
{
    public DbSet<CompetitionEntity> Competitions => Set<CompetitionEntity>();
    public DbSet<ParticipantEntity> Participants => Set<ParticipantEntity>();
    public DbSet<AgentSessionEntity> AgentSessions => Set<AgentSessionEntity>();
    public DbSet<RaceResultEntity> RaceResults => Set<RaceResultEntity>();

    public RacingDbContext(DbContextOptions<RacingDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Competition
        modelBuilder.Entity<CompetitionEntity>(b =>
        {
            b.HasKey(c => c.Id);
            b.HasIndex(c => c.Code).IsUnique();
            b.Property(c => c.Code).HasMaxLength(8).IsRequired();
            b.Property(c => c.Name).HasMaxLength(100).IsRequired();
            b.Property(c => c.Status).HasMaxLength(20).IsRequired();
            b.HasMany(c => c.Participants)
                .WithOne(p => p.Competition)
                .HasForeignKey(p => p.CompetitionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Participant
        modelBuilder.Entity<ParticipantEntity>(b =>
        {
            b.HasKey(p => p.Id);
            b.Property(p => p.Name).HasMaxLength(50).IsRequired();
            b.HasIndex(p => new { p.CompetitionId, p.Name }).IsUnique();
        });

        // AgentSession
        modelBuilder.Entity<AgentSessionEntity>(b =>
        {
            b.HasKey(s => s.Id);
            b.HasIndex(s => s.TokenHash).IsUnique();
            b.Property(s => s.TokenHash).HasMaxLength(64).IsRequired();
        });

        // RaceResult
        modelBuilder.Entity<RaceResultEntity>(b =>
        {
            b.HasKey(r => r.Id);
            b.HasIndex(r => r.CompetitionId);
            b.HasIndex(r => r.RaceId);
        });
    }
}
