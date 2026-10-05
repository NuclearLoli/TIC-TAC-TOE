using CaroGame.Core.Enums;
using CaroGame.Core.History;
using CaroGame.Core.Models;
using FluentAssertions;
using Xunit;

namespace CaroGame.Core.Tests;

public class HistoryTests
{
    [Fact]
    public void MoveHistoryManager_ShouldUndoAndRedoCorrectly()
    {
        DynamicBoard board = new();
        MoveHistoryManager manager = new();

        Coordinate m1 = new(0, 0);
        Coordinate m2 = new(0, 1);

        board.SetCell(m1, CellState.X);
        manager.RecordMove(m1, CellState.X);

        board.SetCell(m2, CellState.O);
        manager.RecordMove(m2, CellState.O);

        manager.CanUndo.Should().BeTrue();
        manager.MoveCount.Should().Be(2);

        // Undo m2
        MoveRecord? undone = manager.Undo(board);
        undone.Should().NotBeNull();
        undone!.Coord.Should().Be(m2);
        board.GetCell(m2).Should().Be(CellState.Empty);
        manager.CanRedo.Should().BeTrue();
        manager.MoveCount.Should().Be(1);

        // Redo m2
        MoveRecord? redone = manager.Redo(board);
        redone.Should().NotBeNull();
        redone!.Coord.Should().Be(m2);
        board.GetCell(m2).Should().Be(CellState.O);
        manager.CanRedo.Should().BeFalse();
        manager.MoveCount.Should().Be(2);
    }

    [Fact]
    public void MoveHistoryManager_DeepStressTest_MaintainsBoardIntegrity()
    {
        DynamicBoard board = new();
        MoveHistoryManager manager = new();

        for (int i = 0; i < 50; i++)
        {
            var coord = new Coordinate(i, i % 5);
            var player = (i % 2 == 0) ? CellState.X : CellState.O;
            board.SetCell(coord, player);
            manager.RecordMove(coord, player);
        }

        manager.MoveCount.Should().Be(50);
        manager.CanUndo.Should().BeTrue();
        manager.CanRedo.Should().BeFalse();

        // Undo 25 moves
        for (int i = 0; i < 25; i++)
        {
            var undone = manager.Undo(board);
            undone.Should().NotBeNull();
            board.GetCell(undone!.Coord).Should().Be(CellState.Empty);
        }

        manager.MoveCount.Should().Be(25);
        manager.CanUndo.Should().BeTrue();
        manager.CanRedo.Should().BeTrue();

        // Redo 15 moves
        for (int i = 0; i < 15; i++)
        {
            var redone = manager.Redo(board);
            redone.Should().NotBeNull();
            board.GetCell(redone!.Coord).Should().NotBe(CellState.Empty);
        }

        manager.MoveCount.Should().Be(40);
    }
}
