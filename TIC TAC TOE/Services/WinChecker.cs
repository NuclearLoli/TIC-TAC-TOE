using TIC_TAC_TOE.Models;

namespace TIC_TAC_TOE.Services;

public static class WinChecker
{
    private static readonly (int RowDirection, int ColumnDirection)[] Directions =
    {
        (0, 1),
        (1, 0),
        (1, 1),
        (1, -1)
    };

    public static bool CheckWin(Board board, int row, int column, CellState player)
    {
        foreach ((int rowDirection, int columnDirection) in Directions)
        {
            int totalCount =
                CountDirection(board, row, column, rowDirection, columnDirection, player) +
                CountDirection(board, row, column, -rowDirection, -columnDirection, player) -
                1;

            if (totalCount >= 5)
            {
                return true;
            }
        }

        return false;
    }

    private static int CountDirection(
        Board board,
        int row,
        int column,
        int rowDirection,
        int columnDirection,
        CellState player)
    {
        int count = 0;
        int currentRow = row;
        int currentColumn = column;

        while (currentRow >= 0 &&
               currentRow < Board.Size &&
               currentColumn >= 0 &&
               currentColumn < Board.Size &&
               board.GetCell(currentRow, currentColumn) == player)
        {
            count++;
            currentRow += rowDirection;
            currentColumn += columnDirection;
        }

        return count;
    }
}
