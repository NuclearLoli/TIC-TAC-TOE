using CaroGame.Core.Enums;
using CaroGame.Core.Models;

namespace CaroGame.Core.Rules;

public class FreeRuleEngine : IRuleEngine
{
    public RuleType RuleType => RuleType.FreeRule;

    private static readonly (int dr, int dc)[] Directions =
    {
        (0, 1),  // Ngang
        (1, 0),  // Dọc
        (1, 1),  // Chéo chính (\)
        (1, -1)  // Chéo phụ (/)
    };

    public WinResult CheckWin(DynamicBoard board, Coordinate lastMove)
    {
        CellState player = board.GetCell(lastMove);
        if (player == CellState.Empty) return WinResult.None;

        foreach ((int dr, int dc) in Directions)
        {
            List<Coordinate> line = new() { lastMove };

            // Hướng tới
            int step = 1;
            while (true)
            {
                Coordinate next = lastMove.Offset(dr * step, dc * step);
                if (board.GetCell(next) == player)
                {
                    line.Add(next);
                    step++;
                }
                else break;
            }

            // Hướng lùi
            step = 1;
            while (true)
            {
                Coordinate prev = lastMove.Offset(-dr * step, -dc * step);
                if (board.GetCell(prev) == player)
                {
                    line.Insert(0, prev);
                    step++;
                }
                else break;
            }

            if (line.Count >= 5)
            {
                return new WinResult(true, player, line);
            }
        }

        return WinResult.None;
    }
}
