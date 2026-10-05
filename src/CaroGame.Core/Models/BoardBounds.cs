namespace CaroGame.Core.Models;

public record struct BoardBounds(int MinRow, int MaxRow, int MinCol, int MaxCol)
{
    public int RowCount => MaxRow - MinRow + 1;
    public int ColCount => MaxCol - MinCol + 1;

    public static BoardBounds CreateSymmetric(int halfSize)
    {
        return new BoardBounds(-halfSize, halfSize, -halfSize, halfSize);
    }

    public static BoardBounds FromZero(int size)
    {
        return new BoardBounds(0, size - 1, 0, size - 1);
    }

    public bool Contains(Coordinate coord)
    {
        return coord.Row >= MinRow && coord.Row <= MaxRow &&
               coord.Col >= MinCol && coord.Col <= MaxCol;
    }
}
