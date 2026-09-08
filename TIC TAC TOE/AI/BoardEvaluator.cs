using TIC_TAC_TOE.Models;

namespace TIC_TAC_TOE.AI;

public class BoardEvaluator
{
    private static readonly (int RowDirection, int ColumnDirection)[] Directions =
    {
        (0, 1),
        (1, 0),
        (1, 1),
        (1, -1)
    };

    public int EvaluateMove(Board board, int row, int column, CellState aiPlayer)
    {
        if (!board.IsEmpty(row, column))
        {
            return int.MinValue;
        }

        CellState opponent = GetOpponent(aiPlayer);

        if (IsWinningMove(board, row, column, aiPlayer))
        {
            return 1_000_000;
        }

        if (IsWinningMove(board, row, column, opponent))
        {
            return 900_000;
        }

        int attackScore = 0;
        int defenseScore = 0;

        foreach ((int rowDirection, int columnDirection) in Directions)
        {
            attackScore += EvaluateDirection(board, row, column, aiPlayer, rowDirection, columnDirection);
            defenseScore += EvaluateDirection(board, row, column, opponent, rowDirection, columnDirection);
        }

        int centerScore = GetCenterBonus(row, column);
        return attackScore + (defenseScore * 11 / 10) + centerScore;
    }

    private bool IsWinningMove(Board board, int row, int column, CellState player)
    {
        foreach ((int rowDirection, int columnDirection) in Directions)
        {
            int totalCount =
                CountContinuous(board, row, column, rowDirection, columnDirection, player) +
                CountContinuous(board, row, column, -rowDirection, -columnDirection, player) +
                1;

            if (totalCount >= 5)
            {
                return true;
            }
        }

        return false;
    }

    private int EvaluateDirection(
        Board board,
        int row,
        int column,
        CellState player,
        int rowDirection,
        int columnDirection)
    {
        int forwardCount = CountContinuous(board, row, column, rowDirection, columnDirection, player);
        int backwardCount = CountContinuous(board, row, column, -rowDirection, -columnDirection, player);

        int totalCount = forwardCount + backwardCount + 1;
        int openEnds = CountOpenEnds(
            board,
            row,
            column,
            rowDirection,
            columnDirection,
            forwardCount,
            backwardCount);

        return ScorePattern(totalCount, openEnds);
    }

    private int CountContinuous(
        Board board,
        int row,
        int column,
        int rowDirection,
        int columnDirection,
        CellState player)
    {
        int count = 0;
        int currentRow = row + rowDirection;
        int currentColumn = column + columnDirection;

        while (IsInsideBoard(currentRow, currentColumn) &&
               board.GetCell(currentRow, currentColumn) == player)
        {
            count++;
            currentRow += rowDirection;
            currentColumn += columnDirection;
        }

        return count;
    }

    private int CountOpenEnds(
        Board board,
        int row,
        int column,
        int rowDirection,
        int columnDirection,
        int forwardCount,
        int backwardCount)
    {
        int openEnds = 0;

        int forwardRow = row + ((forwardCount + 1) * rowDirection);
        int forwardColumn = column + ((forwardCount + 1) * columnDirection);

        if (IsInsideBoard(forwardRow, forwardColumn) &&
            board.GetCell(forwardRow, forwardColumn) == CellState.Empty)
        {
            openEnds++;
        }

        int backwardRow = row - ((backwardCount + 1) * rowDirection);
        int backwardColumn = column - ((backwardCount + 1) * columnDirection);

        if (IsInsideBoard(backwardRow, backwardColumn) &&
            board.GetCell(backwardRow, backwardColumn) == CellState.Empty)
        {
            openEnds++;
        }

        return openEnds;
    }

    private static int ScorePattern(int totalCount, int openEnds)
    {
        if (totalCount >= 5)
        {
            return 100_000;
        }

        if (totalCount == 4 && openEnds == 2)
        {
            return 50_000;
        }

        if (totalCount == 4 && openEnds == 1)
        {
            return 12_000;
        }

        if (totalCount == 3 && openEnds == 2)
        {
            return 4_000;
        }

        if (totalCount == 3 && openEnds == 1)
        {
            return 800;
        }

        if (totalCount == 2 && openEnds == 2)
        {
            return 300;
        }

        if (totalCount == 2 && openEnds == 1)
        {
            return 60;
        }

        if (totalCount == 1 && openEnds == 2)
        {
            return 20;
        }

        return 0;
    }

    private static int GetCenterBonus(int row, int column)
    {
        int center = Board.Size / 2;
        int distance = Math.Abs(row - center) + Math.Abs(column - center);
        return Math.Max(0, 20 - distance);
    }

    private static CellState GetOpponent(CellState player)
    {
        return player == CellState.PlayerX
            ? CellState.PlayerO
            : CellState.PlayerX;
    }

    private static bool IsInsideBoard(int row, int column)
    {
        return row >= 0 &&
               row < Board.Size &&
               column >= 0 &&
               column < Board.Size;
    }
}
