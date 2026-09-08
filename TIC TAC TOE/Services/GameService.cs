using System.Collections.Generic;
using TIC_TAC_TOE.Models;

namespace TIC_TAC_TOE.Services;

public class GameService
{
    public Board Board { get; private set; }

    public CellState CurrentPlayer { get; private set; }

    public GameResult Result { get; private set; }

    public int MoveCount { get; private set; }

    public bool IsGameOver => Result != GameResult.InProgress;

    public List<GameMove> Moves { get; private set; }

    public DateTime StartedAt { get; private set; }

    public GameService()
    {
        Board = new Board();
        CurrentPlayer = CellState.PlayerX;
        Result = GameResult.InProgress;
        MoveCount = 0;
        Moves = new List<GameMove>();
        StartedAt = DateTime.Now;
    }

    public bool CanMakeMove(int row, int column)
    {
        if (IsGameOver || !Board.IsEmpty(row, column))
        {
            return false;
        }

        if (MoveCount == 0)
        {
            return true;
        }

        return Board.IsAdjacentToPlayedCell(row, column);
    }

    public bool MakeMove(int row, int column)
    {
        if (!CanMakeMove(row, column))
        {
            return false;
        }

        CellState player = CurrentPlayer;
        bool success = Board.PlaceMove(row, column, player);

        if (!success)
        {
            return false;
        }

        MoveCount++;

        Moves.Add(new GameMove
        {
            Row = row,
            Column = column,
            Player = player,
            MoveNumber = MoveCount
        });

        if (WinChecker.CheckWin(Board, row, column, player))
        {
            Result = player == CellState.PlayerX
                ? GameResult.PlayerXWin
                : GameResult.PlayerOWin;

            return true;
        }

        if (Board.IsFull())
        {
            Result = GameResult.Draw;
            return true;
        }

        SwitchPlayer();
        return true;
    }

    public void Restart()
    {
        Board.Reset();
        CurrentPlayer = CellState.PlayerX;
        Result = GameResult.InProgress;
        MoveCount = 0;
        Moves.Clear();
        StartedAt = DateTime.Now;
    }

    private void SwitchPlayer()
    {
        CurrentPlayer = CurrentPlayer == CellState.PlayerX
            ? CellState.PlayerO
            : CellState.PlayerX;
    }
}
