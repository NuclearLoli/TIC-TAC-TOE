using System;
using System.Collections.Generic;

namespace TIC_TAC_TOE.Models;

public class Board
{
    public const int Size = 30;
    public const int CenterIndex = Size / 2;

    private readonly CellState[,] _cells;

    public Board()
    {
        _cells = new CellState[Size, Size];
        Reset();
    }

    public CellState GetCell(int row, int column)
    {
        if (!IsValidPosition(row, column))
        {
            throw new ArgumentOutOfRangeException();
        }

        return _cells[row, column];
    }

    public bool IsEmpty(int row, int column)
    {
        if (!IsValidPosition(row, column))
        {
            return false;
        }

        return _cells[row, column] == CellState.Empty;
    }

    public bool HasAnyMoves()
    {
        for (int row = 0; row < Size; row++)
        {
            for (int column = 0; column < Size; column++)
            {
                if (_cells[row, column] != CellState.Empty)
                {
                    return true;
                }
            }
        }

        return false;
    }

    public bool IsAdjacentToPlayedCell(int row, int column)
    {
        if (!IsValidPosition(row, column))
        {
            return false;
        }

        for (int rowOffset = -1; rowOffset <= 1; rowOffset++)
        {
            for (int columnOffset = -1; columnOffset <= 1; columnOffset++)
            {
                if (rowOffset == 0 && columnOffset == 0)
                {
                    continue;
                }

                int targetRow = row + rowOffset;
                int targetColumn = column + columnOffset;

                if (!IsValidPosition(targetRow, targetColumn))
                {
                    continue;
                }

                if (_cells[targetRow, targetColumn] != CellState.Empty)
                {
                    return true;
                }
            }
        }

        return false;
    }

    public bool PlaceMove(int row, int column, CellState player)
    {
        if (!IsValidPosition(row, column))
        {
            return false;
        }

        if (_cells[row, column] != CellState.Empty)
        {
            return false;
        }

        if (player == CellState.Empty)
        {
            return false;
        }

        _cells[row, column] = player;
        return true;
    }

    public void ClearCell(int row, int column)
    {
        if (!IsValidPosition(row, column))
        {
            throw new ArgumentOutOfRangeException();
        }

        _cells[row, column] = CellState.Empty;
    }

    public Board Clone()
    {
        Board clonedBoard = new();

        for (int row = 0; row < Size; row++)
        {
            for (int column = 0; column < Size; column++)
            {
                clonedBoard._cells[row, column] = _cells[row, column];
            }
        }

        return clonedBoard;
    }

    public void Reset()
    {
        Array.Clear(_cells, 0, _cells.Length);
    }

    public bool IsFull()
    {
        for (int row = 0; row < Size; row++)
        {
            for (int column = 0; column < Size; column++)
            {
                if (_cells[row, column] == CellState.Empty)
                {
                    return false;
                }
            }
        }

        return true;
    }

    public IEnumerable<(int Row, int Column)> GetEmptyCells()
    {
        for (int row = 0; row < Size; row++)
        {
            for (int column = 0; column < Size; column++)
            {
                if (_cells[row, column] == CellState.Empty)
                {
                    yield return (row, column);
                }
            }
        }
    }

    private static bool IsValidPosition(int row, int column)
    {
        return row >= 0 &&
               row < Size &&
               column >= 0 &&
               column < Size;
    }
}
