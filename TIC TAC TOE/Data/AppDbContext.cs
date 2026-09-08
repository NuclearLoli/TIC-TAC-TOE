using Microsoft.EntityFrameworkCore;
using TIC_TAC_TOE.Models;

namespace TIC_TAC_TOE.Data;

public class AppDbContext : DbContext
{
    public DbSet<Player> Players => Set<Player>();

    public DbSet<Game> Games => Set<Game>();

    public DbSet<Move> Moves => Set<Move>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        string connectionString =
            Environment.GetEnvironmentVariable("CARO_SQLSERVER_CONNECTION")
            ?? "Server=(localdb)\\MSSQLLocalDB;Database=CaroGameDb;Trusted_Connection=True;TrustServerCertificate=True;";

        optionsBuilder.UseSqlServer(connectionString);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Game>()
            .HasOne(g => g.PlayerX)
            .WithMany(p => p.GamesAsPlayerX)
            .HasForeignKey(g => g.PlayerXId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Game>()
            .HasOne(g => g.PlayerO)
            .WithMany(p => p.GamesAsPlayerO)
            .HasForeignKey(g => g.PlayerOId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Game>()
            .HasOne(g => g.Winner)
            .WithMany()
            .HasForeignKey(g => g.WinnerId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Move>()
            .HasOne(m => m.Game)
            .WithMany(g => g.Moves)
            .HasForeignKey(m => m.GameId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Move>()
            .HasOne(m => m.Player)
            .WithMany(p => p.Moves)
            .HasForeignKey(m => m.PlayerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
