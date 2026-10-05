using CaroGame.Core.Enums;
using CaroGame.Core.Models;

namespace CaroGame.Core.Rules;

/// <summary>
/// Luật Caro Việt Nam truyền thống:
/// Đủ 5 quân liên tiếp (hoặc hơn) thắng, TRỪ KHI bị đối thủ chặn cả 2 đầu của chuỗi 5.
/// Nếu chỉ bị chặn 1 đầu (hoặc mở cả 2 đầu) thì thắng!
/// </summary>
public class VietnameseRuleEngine : IRuleEngine
{
    public RuleType RuleType => RuleType.BlockedBothEnds;

    private static readonly (int dr, int dc)[] Directions =
    {
        (0, 1),  // Ngang
        (1, 0),  // Dọc
        (1, 1),  // Chéo chính
        (1, -1)  // Chéo phụ
    };

    public WinResult CheckWin(DynamicBoard board, Coordinate lastMove)
    {
        CellState player = board.GetCell(lastMove);
        if (player == CellState.Empty) return WinResult.None;
        CellState opponent = player == CellState.X ? CellState.O : CellState.X;

        foreach ((int dr, int dc) in Directions)
        {
            List<Coordinate> forwardStones = new();
            int step = 1;
            while (true)
            {
                Coordinate next = lastMove.Offset(dr * step, dc * step);
                if (board.GetCell(next) == player)
                {
                    forwardStones.Add(next);
                    step++;
                }
                else break;
            }
            Coordinate forwardBoundary = lastMove.Offset(dr * step, dc * step);
            bool forwardBlocked = board.GetCell(forwardBoundary) == opponent;

            List<Coordinate> backwardStones = new();
            step = 1;
            while (true)
            {
                Coordinate prev = lastMove.Offset(-dr * step, -dc * step);
                if (board.GetCell(prev) == player)
                {
                    backwardStones.Add(prev);
                    step++;
                }
                else break;
            }
            Coordinate backwardBoundary = lastMove.Offset(-dr * step, -dc * step);
            bool backwardBlocked = board.GetCell(backwardBoundary) == opponent;

            int totalStones = 1 + forwardStones.Count + backwardStones.Count;

            if (totalStones >= 5)
            {
                // Nếu bị chặn cả 2 đầu thì KHÔNG thắng
                if (forwardBlocked && backwardBlocked)
                {
                    continue;
                }

                List<Coordinate> winningLine = new(backwardStones);
                winningLine.Reverse();
                winningLine.Add(lastMove);
                winningLine.AddRange(forwardStones);

                return new WinResult(true, player, winningLine);
            }
        }

        return WinResult.None;
    }
}
