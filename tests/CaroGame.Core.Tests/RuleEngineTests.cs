using CaroGame.Core.Enums;
using CaroGame.Core.Models;
using CaroGame.Core.Rules;
using FluentAssertions;
using Xunit;

namespace CaroGame.Core.Tests;

public class RuleEngineTests
{
    [Fact]
    public void FreeRule_ShouldWin_WhenFiveInARowHorizontal()
    {
        DynamicBoard board = new();
        FreeRuleEngine engine = new();

        for (int c = 0; c < 4; c++)
        {
            board.SetCell(new Coordinate(0, c), CellState.X);
        }

        // Nước thứ 5
        Coordinate lastMove = new(0, 4);
        board.SetCell(lastMove, CellState.X);

        WinResult result = engine.CheckWin(board, lastMove);
        result.HasWon.Should().BeTrue();
        result.Winner.Should().Be(CellState.X);
        result.WinningLine.Should().HaveCount(5);
    }

    [Fact]
    public void FreeRule_ShouldNotWin_WhenOnlyFourInARow()
    {
        DynamicBoard board = new();
        FreeRuleEngine engine = new();

        for (int c = 0; c < 3; c++)
        {
            board.SetCell(new Coordinate(0, c), CellState.X);
        }

        Coordinate lastMove = new(0, 3);
        board.SetCell(lastMove, CellState.X);

        WinResult result = engine.CheckWin(board, lastMove);
        result.HasWon.Should().BeFalse();
    }

    [Fact]
    public void VietnameseRule_ShouldNotWin_WhenFiveInRowBlockedAtBothEnds()
    {
        DynamicBoard board = new();
        VietnameseRuleEngine engine = new();

        // Đối thủ O chặn đầu trái (-1, 0) và đầu phải (5, 0)
        board.SetCell(new Coordinate(0, -1), CellState.O);
        board.SetCell(new Coordinate(0, 5), CellState.O);

        // X đánh từ 0 đến 4
        for (int c = 0; c < 4; c++)
        {
            board.SetCell(new Coordinate(0, c), CellState.X);
        }

        Coordinate lastMove = new(0, 4);
        board.SetCell(lastMove, CellState.X);

        WinResult result = engine.CheckWin(board, lastMove);
        result.HasWon.Should().BeFalse("bị chặn cả 2 đầu theo luật Việt Nam");
    }

    [Fact]
    public void VietnameseRule_ShouldWin_WhenFiveInRowBlockedAtOnlyOneEnd()
    {
        DynamicBoard board = new();
        VietnameseRuleEngine engine = new();

        // Đối thủ O chỉ chặn đầu trái (-1, 0), đầu phải (5, 0) để trống
        board.SetCell(new Coordinate(0, -1), CellState.O);

        // X đánh từ 0 đến 4
        for (int c = 0; c < 4; c++)
        {
            board.SetCell(new Coordinate(0, c), CellState.X);
        }

        Coordinate lastMove = new(0, 4);
        board.SetCell(lastMove, CellState.X);

        WinResult result = engine.CheckWin(board, lastMove);
        result.HasWon.Should().BeTrue("chỉ bị chặn 1 đầu nên vẫn thắng");
        result.Winner.Should().Be(CellState.X);
    }
}
