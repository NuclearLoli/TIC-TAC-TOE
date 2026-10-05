using CaroGame.Core.AI;
using CaroGame.Core.Enums;
using CaroGame.Core.Models;
using FluentAssertions;
using Xunit;

namespace CaroGame.Core.Tests;

public class AiEngineTests
{
    [Fact]
    public async Task MinimaxAi_ShouldChooseWinningMove_WhenAvailable()
    {
        DynamicBoard board = new();
        MinimaxAiEngine ai = new();

        // O có 4 ô liên tiếp (0, 0) đến (0, 3)
        for (int c = 0; c < 4; c++)
        {
            board.SetCell(new Coordinate(0, c), CellState.O);
        }

        // Máy là O, máy phải đánh (0, 4) hoặc (0, -1) để thắng ngay
        Coordinate bestMove = await ai.CalculateBestMoveAsync(board, CellState.O, AiDifficulty.Medium, RuleType.FreeRule);

        bestMove.Should().Match<Coordinate>(c => (c.Row == 0 && c.Col == 4) || (c.Row == 0 && c.Col == -1));
    }

    [Fact]
    public async Task MinimaxAi_ShouldBlockOpponentWinningMove()
    {
        DynamicBoard board = new();
        MinimaxAiEngine ai = new();

        // Người chơi X có 4 ô liên tiếp (0, 0) đến (0, 3)
        for (int c = 0; c < 4; c++)
        {
            board.SetCell(new Coordinate(0, c), CellState.X);
        }

        // Máy là O, máy phải chặn ở (0, 4) hoặc (0, -1)
        Coordinate bestMove = await ai.CalculateBestMoveAsync(board, CellState.O, AiDifficulty.Hard, RuleType.FreeRule);

        bestMove.Should().Match<Coordinate>(c => (c.Row == 0 && c.Col == 4) || (c.Row == 0 && c.Col == -1));
    }

    [Fact]
    public async Task MinimaxAi_EmptyBoard_ShouldPlayCenter()
    {
        DynamicBoard board = new();
        MinimaxAiEngine ai = new();

        Coordinate move = await ai.CalculateBestMoveAsync(board, CellState.O, AiDifficulty.Medium, RuleType.BlockedBothEnds);
        move.Should().Be(new Coordinate(0, 0));
    }
}
