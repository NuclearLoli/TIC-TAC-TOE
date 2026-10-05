using Microsoft.EntityFrameworkCore;

namespace CaroGame.Server.Data;

public class ServerDbContext : DbContext
{
    public ServerDbContext(DbContextOptions<ServerDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<OnlineMatch> Matches => Set<OnlineMatch>();
    public DbSet<EmailVerification> EmailVerifications => Set<EmailVerification>();
    public DbSet<Friendship> Friendships => Set<Friendship>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.Property(u => u.Id).UseCollation("NOCASE");
            entity.HasIndex(u => u.Username).IsUnique();
            entity.HasIndex(u => u.Email);
            entity.Property(u => u.Username).HasMaxLength(50).IsRequired();
            entity.Property(u => u.Email).HasMaxLength(100);
            entity.Property(u => u.DisplayName).HasMaxLength(50).IsRequired();
            entity.Property(u => u.PasswordHash).IsRequired();
            entity.Property(u => u.Avatar).HasMaxLength(30).HasDefaultValue("king");
            entity.Property(u => u.Country).HasMaxLength(10).HasDefaultValue("VN");
            entity.Property(u => u.Bio).HasMaxLength(250).HasDefaultValue("Đam mê cờ Caro!");
            entity.Property(u => u.Role).HasMaxLength(20).HasDefaultValue("Player");
            entity.Property(u => u.Status).HasMaxLength(20).HasDefaultValue("Active");
            entity.Property(u => u.Title).HasMaxLength(50).HasDefaultValue("Tân Thủ");
            entity.Property(u => u.AvatarFrame).HasMaxLength(30).HasDefaultValue("classic");
            entity.Property(u => u.LowestElo).HasDefaultValue(1000);
            entity.Property(u => u.Level).HasDefaultValue(1);
            entity.Property(u => u.ExperiencePoints).HasDefaultValue(0);
            entity.Property(u => u.TotalPlayTimeSeconds).HasDefaultValue(0);
        });

        modelBuilder.Entity<OnlineMatch>(entity =>
        {
            entity.HasKey(m => m.Id);
        });

        modelBuilder.Entity<EmailVerification>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).UseCollation("NOCASE");
            entity.HasIndex(e => new { e.Email, e.Code, e.Purpose });
            entity.Property(e => e.Email).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Code).HasMaxLength(10).IsRequired();
        });

        modelBuilder.Entity<Friendship>(entity =>
        {
            entity.HasKey(f => f.Id);
            entity.Property(f => f.Id).UseCollation("NOCASE");
            entity.HasIndex(f => new { f.RequesterId, f.AddresseeId });
            entity.Property(f => f.Status).HasMaxLength(20).IsRequired();
        });
    }
}
