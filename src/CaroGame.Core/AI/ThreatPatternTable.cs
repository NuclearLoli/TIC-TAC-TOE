using CaroGame.Core.Enums;
using CaroGame.Core.Models;
using CaroGame.Core.Rules;

namespace CaroGame.Core.AI;

public static class ThreatPatternTable
{
    // Điểm số thế cờ tấn công & phòng thủ
    public const int FiveInRow = 1000000;
    public const int OpenFour = 100000;
    public const int BlockedFour = 10000;
    public const int OpenThree = 5000;
    public const int BlockedThree = 500;
    public const int OpenTwo = 200;
    public const int BlockedTwo = 20;

    private static readonly (int dr, int dc)[] Directions =
    {
        (0, 1),
        (1, 0),
        (1, 1),
        (1, -1)
    };

    public static int EvaluatePosition(DynamicBoard board, Coordinate coord, CellState player, RuleType ruleType)
    {
        CellState opponent = player == CellState.X ? CellState.O : CellState.X;
        int totalScore = 0;

        foreach ((int dr, int dc) in Directions)
        {
            totalScore += EvaluateDirection(board, coord, dr, dc, player, opponent, ruleType);
        }

        return totalScore;
    }

    private static int EvaluateDirection(DynamicBoard board, Coordinate coord, int dr, int dc, CellState player, CellState opponent, RuleType ruleType)
    {
        int count = 1;
        int forwardOpen = 0;
        int backwardOpen = 0;

        // Tiến
        int step = 1;
        while (step <= 5)
        {
            Coordinate c = coord.Offset(dr * step, dc * step);
            CellState state = board.GetCell(c);
            if (state == player) count++;
            else
            {
                if (state == CellState.Empty) forwardOpen++;
                break;
            }
            step++;
        }

        // Lùi
        step = 1;
        while (step <= 5)
        {
            Coordinate c = coord.Offset(-dr * step, -dc * step);
            CellState state = board.GetCell(c);
            if (state == player) count++;
            else
            {
                if (state == CellState.Empty) backwardOpen++;
                break;
            }
            step++;
        }

        int openEnds = forwardOpen + backwardOpen;

        if (count >= 5)
        {
            if (ruleType == RuleType.BlockedBothEnds && openEnds == 0)
                return BlockedFour; // Bị chặn 2 đầu thì chưa thắng
            return FiveInRow;
        }

        if (count == 4)
        {
            if (openEnds == 2) return OpenFour;
            if (openEnds == 1) return BlockedFour;
        }
        else if (count == 3)
        {
            if (openEnds == 2) return OpenThree;
            if (openEnds == 1) return BlockedThree;
        }
        else if (count == 2)
        {
            if (openEnds == 2) return OpenTwo;
            if (openEnds == 1) return BlockedTwo;
        }

        return 0;
    }
}
