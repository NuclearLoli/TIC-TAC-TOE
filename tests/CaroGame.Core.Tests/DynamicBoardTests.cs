using CaroGame.Core.Enums;
using CaroGame.Core.Models;
using FluentAssertions;
using Xunit;

namespace CaroGame.Core.Tests;

public class DynamicBoardTests
{
    [Fact]
    public void DynamicBoard_ShouldInitialize_WithSpecifiedHalfSize()
    {
        DynamicBoard board = new(initialHalfSize: 7);
        board.Bounds.MinRow.Should().Be(-7);
        board.Bounds.MaxRow.Should().Be(7);
        board.Bounds.MinCol.Should().Be(-7);
        board.Bounds.MaxCol.Should().Be(7);
        board.Bounds.RowCount.Should().Be(15);
        board.Bounds.ColCount.Should().Be(15);
    }

    [Fact]
    public void DynamicBoard_ShouldAutoExpand_WhenMoveIsNearMargin()
    {
        DynamicBoard board = new(initialHalfSize: 7, expandMargin: 2, expandStep: 4);
        bool expandedTriggered = false;
        board.BoundsExpanded += _ => expandedTriggered = true;

        // Đánh vào ô Row = 6, Col = 0 (cách MaxRow = 7 là 1 ô <= expandMargin 2)
        board.SetCell(new Coordinate(6, 0), CellState.X);

        expandedTriggered.Should().BeTrue();
        board.Bounds.MaxRow.Should().Be(11); // 7 + 4 = 11
        board.Bounds.MinRow.Should().Be(-7); // không đổi
    }

    [Fact]
    public void DynamicBoard_ShouldAutoExpandCorner_WhenMoveIsAtCorner()
    {
        DynamicBoard board = new(initialHalfSize: 7, expandMargin: 2, expandStep: 4);

        // Đánh vào góc (-6, -6) (gần cả MinRow và MinCol)
        board.SetCell(new Coordinate(-6, -6), CellState.O);

        board.Bounds.MinRow.Should().Be(-11);
        board.Bounds.MinCol.Should().Be(-11);
        board.GetCell(new Coordinate(-6, -6)).Should().Be(CellState.O);
    }

    [Fact]
    public void DynamicBoard_ShouldNotExpand_WhenMoveIsNearCenter()
    {
        DynamicBoard board = new(initialHalfSize: 7, expandMargin: 2, expandStep: 4);

        board.SetCell(new Coordinate(0, 0), CellState.X);

        board.Bounds.MinRow.Should().Be(-7);
        board.Bounds.MaxRow.Should().Be(7);
        board.Bounds.MinCol.Should().Be(-7);
        board.Bounds.MaxCol.Should().Be(7);
    }
}
