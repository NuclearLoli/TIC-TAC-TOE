using Microsoft.EntityFrameworkCore;
using TIC_TAC_TOE.Data;
using TIC_TAC_TOE.Models;

namespace TIC_TAC_TOE.Services;

public class GameHistoryService
{
    public async Task SaveCompletedGameAsync(
        GameMode gameMode,
        AiDifficulty? aiDifficulty,
        GameResult result,
        DateTime startedAt,
        IReadOnlyList<GameMove> moves)
    {
        if (result == GameResult.InProgress || moves.Count == 0)
        {
            return;
        }

        await using AppDbContext dbContext = new();

        Player playerX = await GetOrCreatePlayerAsync(dbContext, GetPlayerXName(gameMode));
        Player playerO = await GetOrCreatePlayerAsync(dbContext, GetPlayerOName(gameMode, aiDifficulty));

        int? winnerId = result switch
        {
            GameResult.PlayerXWin => playerX.Id,
            GameResult.PlayerOWin => playerO.Id,
            _ => null
        };

        Game game = new()
        {
            Mode = gameMode,
            Result = result,
            StartedAt = startedAt,
            EndedAt = DateTime.Now,
            PlayerXId = playerX.Id,
            PlayerOId = playerO.Id,
            WinnerId = winnerId
        };

        dbContext.Games.Add(game);
        await dbContext.SaveChangesAsync();

        List<Move> moveEntities = moves
            .Select(move => new Move
            {
                GameId = game.Id,
                PlayerId = move.Player == CellState.PlayerX ? playerX.Id : playerO.Id,
                Row = move.Row,
                Column = move.Column,
                MoveNumber = move.MoveNumber,
                CellState = move.Player,
                PlayedAt = startedAt.AddSeconds(move.MoveNumber)
            })
            .ToList();

        dbContext.Moves.AddRange(moveEntities);
        await dbContext.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<GameHistorySummary>> GetRecentGamesAsync(int take = 5)
    {
        await using AppDbContext dbContext = new();

        List<Game> games = await dbContext.Games
            .AsNoTracking()
            .Include(game => game.PlayerX)
            .Include(game => game.PlayerO)
            .Include(game => game.Winner)
            .Include(game => game.Moves)
            .OrderByDescending(game => game.EndedAt)
            .Take(take)
            .ToListAsync();

        return games
            .Select(game => new GameHistorySummary
            {
                GameId = game.Id,
                PlayerXName = game.PlayerX.Username,
                PlayerOName = game.PlayerO.Username,
                ModeText = game.Mode == GameMode.PvP ? "PvP" : "PvC",
                ResultText = game.Result switch
                {
                    GameResult.PlayerXWin => "X thắng",
                    GameResult.PlayerOWin => "O thắng",
                    GameResult.Draw => "Hòa",
                    _ => "Đang chơi"
                },
                WinnerName = game.Winner != null ? game.Winner.Username : "Không có",
                TotalMoves = game.Moves.Count,
                StartedAt = game.StartedAt,
                EndedAt = game.EndedAt
            })
            .ToList();
    }

    private static async Task<Player> GetOrCreatePlayerAsync(AppDbContext dbContext, string username)
    {
        Player? existingPlayer = await dbContext.Players
            .FirstOrDefaultAsync(player => player.Username == username);

        if (existingPlayer != null)
        {
            return existingPlayer;
        }

        Player playerEntity = new()
        {
            Username = username,
            CreatedAt = DateTime.Now
        };

        dbContext.Players.Add(playerEntity);
        await dbContext.SaveChangesAsync();
        return playerEntity;
    }

    private static string GetPlayerXName(GameMode gameMode)
    {
        return gameMode == GameMode.PvP ? "Player X" : "Human";
    }

    private static string GetPlayerOName(GameMode gameMode, AiDifficulty? aiDifficulty)
    {
        if (gameMode == GameMode.PvP)
        {
            return "Player O";
        }

        return aiDifficulty switch
        {
            AiDifficulty.De => "Computer - Dễ",
            AiDifficulty.SieuKho => "Computer - Siêu khó",
            _ => "Computer - Vừa"
        };
    }
}
