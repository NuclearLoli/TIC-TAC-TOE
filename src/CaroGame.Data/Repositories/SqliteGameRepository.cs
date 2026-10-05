using CaroGame.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace CaroGame.Data.Repositories;

public interface IGameRepository
{
    Task EnsureDatabaseCreatedAsync();
    Task<int> SaveGameAsync(GameEntity game, IEnumerable<MoveEntity> moves);
    Task<List<GameEntity>> GetRecentGamesAsync(int limit = 50);
    Task<GameEntity?> GetGameWithMovesAsync(int gameId);
    Task<bool> DeleteGameAsync(int gameId);
}

public class SqliteGameRepository : IGameRepository
{
    private readonly IDbContextFactory<CaroDbContext>? _contextFactory;

    public SqliteGameRepository(IDbContextFactory<CaroDbContext>? contextFactory = null)
    {
        _contextFactory = contextFactory;
    }

    private CaroDbContext CreateContext()
    {
        return _contextFactory != null ? _contextFactory.CreateDbContext() : new CaroDbContext();
    }

    public async Task EnsureDatabaseCreatedAsync()
    {
        await using CaroDbContext context = CreateContext();
        await context.Database.EnsureCreatedAsync();
    }

    public async Task<int> SaveGameAsync(GameEntity game, IEnumerable<MoveEntity> moves)
    {
        await using CaroDbContext context = CreateContext();
        await context.Database.EnsureCreatedAsync();

        game.Moves = moves.ToList();
        game.TotalMoves = game.Moves.Count;

        context.Games.Add(game);
        await context.SaveChangesAsync();

        return game.Id;
    }

    public async Task<List<GameEntity>> GetRecentGamesAsync(int limit = 50)
    {
        await using CaroDbContext context = CreateContext();
        await context.Database.EnsureCreatedAsync();

        return await context.Games
            .OrderByDescending(g => g.PlayedAt)
            .Take(limit)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<GameEntity?> GetGameWithMovesAsync(int gameId)
    {
        await using CaroDbContext context = CreateContext();
        await context.Database.EnsureCreatedAsync();

        return await context.Games
            .Include(g => g.Moves.OrderBy(m => m.TurnNumber))
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == gameId);
    }

    public async Task<bool> DeleteGameAsync(int gameId)
    {
        await using CaroDbContext context = CreateContext();
        await context.Database.EnsureCreatedAsync();

        GameEntity? entity = await context.Games.FindAsync(gameId);
        if (entity == null) return false;

        context.Games.Remove(entity);
        await context.SaveChangesAsync();
        return true;
    }
}
