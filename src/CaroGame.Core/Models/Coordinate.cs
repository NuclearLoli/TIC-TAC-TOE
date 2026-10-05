namespace CaroGame.Core.Models;

public readonly record struct Coordinate(int Row, int Col)
{
    public override string ToString() => $"({Row}, {Col})";

    public static Coordinate Center => new(0, 0);

    public Coordinate Offset(int deltaRow, int deltaCol) => new(Row + deltaRow, Col + deltaCol);
}
