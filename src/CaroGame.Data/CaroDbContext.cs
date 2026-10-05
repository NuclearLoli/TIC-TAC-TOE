using CaroGame.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace CaroGame.Data;

public class CaroDbContext : DbContext
{
    public DbSet<GameEntity> Games => Set<GameEntity>();
    public DbSet<MoveEntity> Moves => Set<MoveEntity>();

    private readonly string _dbPath;

    public CaroDbContext()
    {
        string folder = AppDomain.CurrentDomain.BaseDirectory;
        _dbPath = System.IO.Path.Combine(folder, "caro_game.db");
    }

    public CaroDbContext(DbContextOptions<CaroDbContext> options) : base(options)
    {
        string folder = AppDomain.CurrentDomain.BaseDirectory;
        _dbPath = System.IO.Path.Combine(folder, "caro_game.db");
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlite($"Data Source={_dbPath}");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<GameEntity>()
            .HasMany(g => g.Moves)
            .WithOne(m => m.Game)
            .HasForeignKey(m => m.GameId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
