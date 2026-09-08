using TIC_TAC_TOE.Models;
using TIC_TAC_TOE.Services;

namespace TIC_TAC_TOE.AI;

public class MoveFinder
{
    private readonly BoardEvaluator _boardEvaluator;
    private readonly Random _random;
    private const int SuperHardDepth = 2;
    private const int SuperHardBranchLimit = 6;

    public MoveFinder(BoardEvaluator boardEvaluator)
    {
        _boardEvaluator = boardEvaluator;
        _random = new Random();
    }

    public (int Row, int Column) FindBestMove(Board board, CellState aiPlayer, AiDifficulty difficulty)
    {
        List<ScoredMove> scoredMoves = GetScoredMoves(board, aiPlayer);

        if (scoredMoves.Count == 0)
        {
            return (Board.Size / 2, Board.Size / 2);
        }

        return difficulty switch
        {
            AiDifficulty.De => FindEasyMove(scoredMoves),
            AiDifficulty.SieuKho => FindSuperHardMove(board, aiPlayer, scoredMoves),
            _ => (scoredMoves[0].Row, scoredMoves[0].Column)
        };
    }

    public List<(int Row, int Column)> GetCandidateMoves(Board board)
    {
        HashSet<(int Row, int Column)> candidates = new();
        List<(int Row, int Column)> occupiedCells = GetOccupiedCells(board);

        if (occupiedCells.Count == 0)
        {
            return new List<(int Row, int Column)>
            {
                (Board.Size / 2, Board.Size / 2)
            };
        }

        foreach ((int row, int column) in occupiedCells)
        {
            for (int rowOffset = -1; rowOffset <= 1; rowOffset++)
            {
                for (int columnOffset = -1; columnOffset <= 1; columnOffset++)
                {
                    int candidateRow = row + rowOffset;
                    int candidateColumn = column + columnOffset;

                    if (!IsInsideBoard(candidateRow, candidateColumn))
                    {
                        continue;
                    }

                    if (!board.IsEmpty(candidateRow, candidateColumn))
                    {
                        continue;
                    }

                    candidates.Add((candidateRow, candidateColumn));
                }
            }
        }

        if (candidates.Count == 0)
        {
            foreach ((int row, int column) in board.GetEmptyCells())
            {
                candidates.Add((row, column));
            }
        }

        return candidates
            .OrderBy(position => Math.Abs(position.Row - (Board.Size / 2)) + Math.Abs(position.Column - (Board.Size / 2)))
            .ToList();
    }

    private (int Row, int Column) FindEasyMove(List<ScoredMove> scoredMoves)
    {
        if (scoredMoves[0].Score >= 900_000)
        {
            return (scoredMoves[0].Row, scoredMoves[0].Column);
        }

        int selectionPool = Math.Min(5, scoredMoves.Count);
        int randomIndex = _random.Next(selectionPool);
        ScoredMove selectedMove = scoredMoves[randomIndex];
        return (selectedMove.Row, selectedMove.Column);
    }

    private (int Row, int Column) FindSuperHardMove(
        Board board,
        CellState aiPlayer,
        List<ScoredMove> scoredMoves)
    {
        CellState opponent = GetOpponent(aiPlayer);
        int bestScore = int.MinValue;
        (int Row, int Column) bestMove = (scoredMoves[0].Row, scoredMoves[0].Column);

        foreach (ScoredMove move in scoredMoves.Take(Math.Min(8, scoredMoves.Count)))
        {
            Board clonedBoard = board.Clone();
            clonedBoard.PlaceMove(move.Row, move.Column, aiPlayer);

            int score = WinChecker.CheckWin(clonedBoard, move.Row, move.Column, aiPlayer)
                ? 5_000_000
                : move.Score + Minimax(
                    clonedBoard,
                    SuperHardDepth,
                    false,
                    aiPlayer,
                    opponent,
                    int.MinValue,
                    int.MaxValue);

            if (score > bestScore)
            {
                bestScore = score;
                bestMove = (move.Row, move.Column);
            }
        }

        return bestMove;
    }

    private int Minimax(
        Board board,
        int depth,
        bool maximizingPlayer,
        CellState aiPlayer,
        CellState currentPlayer,
        int alpha,
        int beta)
    {
        if (depth == 0 || board.IsFull())
        {
            return EvaluateBoardState(board, aiPlayer);
        }

        List<ScoredMove> scoredMoves = GetScoredMoves(board, currentPlayer)
            .Take(SuperHardBranchLimit)
            .ToList();

        if (scoredMoves.Count == 0)
        {
            return EvaluateBoardState(board, aiPlayer);
        }

        if (maximizingPlayer)
        {
            int bestValue = int.MinValue;

            foreach (ScoredMove move in scoredMoves)
            {
                board.PlaceMove(move.Row, move.Column, currentPlayer);

                int value = WinChecker.CheckWin(board, move.Row, move.Column, currentPlayer)
                    ? 4_000_000 + depth
                    : Minimax(
                        board,
                        depth - 1,
                        false,
                        aiPlayer,
                        GetOpponent(currentPlayer),
                        alpha,
                        beta);

                board.ClearCell(move.Row, move.Column);
                bestValue = Math.Max(bestValue, value);
                alpha = Math.Max(alpha, bestValue);

                if (beta <= alpha)
                {
                    break;
                }
            }

            return bestValue;
        }

        int worstValue = int.MaxValue;

        foreach (ScoredMove move in scoredMoves)
        {
            board.PlaceMove(move.Row, move.Column, currentPlayer);

            int value = WinChecker.CheckWin(board, move.Row, move.Column, currentPlayer)
                ? -4_000_000 - depth
                : Minimax(
                    board,
                    depth - 1,
                    true,
                    aiPlayer,
                    GetOpponent(currentPlayer),
                    alpha,
                    beta);

            board.ClearCell(move.Row, move.Column);
            worstValue = Math.Min(worstValue, value);
            beta = Math.Min(beta, worstValue);

            if (beta <= alpha)
            {
                break;
            }
        }

        return worstValue;
    }

    private List<ScoredMove> GetScoredMoves(Board board, CellState player)
    {
        return GetCandidateMoves(board)
            .Select(position => new ScoredMove(
                position.Row,
                position.Column,
                _boardEvaluator.EvaluateMove(board, position.Row, position.Column, player)))
            .OrderByDescending(move => move.Score)
            .ThenBy(move => Math.Abs(move.Row - (Board.Size / 2)) + Math.Abs(move.Column - (Board.Size / 2)))
            .ToList();
    }

    private int EvaluateBoardState(Board board, CellState aiPlayer)
    {
        List<(int Row, int Column)> candidates = GetCandidateMoves(board);

        if (candidates.Count == 0)
        {
            return 0;
        }

        CellState opponent = GetOpponent(aiPlayer);
        int aiScore = 0;
        int opponentScore = 0;

        foreach ((int row, int column) in candidates.Take(8))
        {
            aiScore = Math.Max(aiScore, _boardEvaluator.EvaluateMove(board, row, column, aiPlayer));
            opponentScore = Math.Max(opponentScore, _boardEvaluator.EvaluateMove(board, row, column, opponent));
        }

        return aiScore - opponentScore;
    }

    private static List<(int Row, int Column)> GetOccupiedCells(Board board)
    {
        List<(int Row, int Column)> occupiedCells = new();

        for (int row = 0; row < Board.Size; row++)
        {
            for (int column = 0; column < Board.Size; column++)
            {
                if (board.GetCell(row, column) != CellState.Empty)
                {
                    occupiedCells.Add((row, column));
                }
            }
        }

        return occupiedCells;
    }

    private static bool IsInsideBoard(int row, int column)
    {
        return row >= 0 &&
               row < Board.Size &&
               column >= 0 &&
               column < Board.Size;
    }

    private static CellState GetOpponent(CellState player)
    {
        return player == CellState.PlayerX
            ? CellState.PlayerO
            : CellState.PlayerX;
    }

    private sealed record ScoredMove(int Row, int Column, int Score);
}
