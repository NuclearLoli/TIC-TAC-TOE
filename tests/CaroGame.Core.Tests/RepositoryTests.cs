using CaroGame.Data;
using CaroGame.Data.Entities;
using CaroGame.Data.Repositories;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CaroGame.Core.Tests;

public class RepositoryTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<CaroDbContext> _options;

    public RepositoryTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _options = new DbContextOptionsBuilder<CaroDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var context = new CaroDbContext(_options);
        context.Database.EnsureCreated();
    }

    [Fact]
    public async Task SqliteGameRepository_ShouldSaveAndRetrieveGame_WithMoves()
    {
        using var context = new CaroDbContext(_options);

        var game = new GameEntity
        {
            PlayedAt = DateTime.UtcNow,
            GameMode = "PvC",
            AiDifficulty = "Hard",
            RuleType = "BlockedBothEnds",
            Winner = "X",
            DurationSeconds = 45,
            TotalMoves = 2
        };

        var moves = new List<MoveEntity>
        {
            new() { TurnNumber = 1, Player = "X", Row = 0, Col = 0 },
            new() { TurnNumber = 2, Player = "O", Row = 0, Col = 1 }
        };

        game.Moves = moves;
        context.Games.Add(game);
        await context.SaveChangesAsync();

        var retrieved = await context.Games
            .Include(g => g.Moves)
            .FirstOrDefaultAsync(g => g.Id == game.Id);

        retrieved.Should().NotBeNull();
        retrieved!.Winner.Should().Be("X");
        retrieved.Moves.Should().HaveCount(2);
        retrieved.Moves[0].Row.Should().Be(0);
        retrieved.Moves[0].Col.Should().Be(0);
    }

    public void Dispose()
    {
        _connection.Close();
        _connection.Dispose();
    }
}
