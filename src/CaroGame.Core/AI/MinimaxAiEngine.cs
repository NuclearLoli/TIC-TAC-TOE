using CaroGame.Core.Enums;
using CaroGame.Core.Models;
using CaroGame.Core.Rules;

namespace CaroGame.Core.AI;

public interface IAiEngine
{
    Task<Coordinate> CalculateBestMoveAsync(
        DynamicBoard board,
        CellState aiPlayer,
        AiDifficulty difficulty,
        RuleType ruleType,
        CancellationToken cancellationToken = default);
}

public class MinimaxAiEngine : IAiEngine
{
    private readonly Random _random = new();

    public Task<Coordinate> CalculateBestMoveAsync(
        DynamicBoard board,
        CellState aiPlayer,
        AiDifficulty difficulty,
        RuleType ruleType,
        CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            List<Coordinate> candidates = board.GetCandidateMoves(distance: 2);
            if (candidates.Count == 0) return Coordinate.Center;
            if (candidates.Count == 1) return candidates[0];

            CellState opponent = aiPlayer == CellState.X ? CellState.O : CellState.X;

            // Độ khó Easy: chọn ngẫu nhiên trong top các nước hợp lý
            if (difficulty == AiDifficulty.Easy)
            {
                // Ưu tiên nếu có nước thắng ngay thì đánh luôn
                foreach (Coordinate c in candidates)
                {
                    if (ThreatPatternTable.EvaluatePosition(board, c, aiPlayer, ruleType) >= ThreatPatternTable.FiveInRow)
                        return c;
                }
                return candidates[_random.Next(candidates.Count)];
            }

            // Chấm điểm từng nước ứng viên
            var scoredMoves = new List<(Coordinate Coord, int AttackScore, int DefendScore, int TotalScore)>();

            foreach (Coordinate c in candidates)
            {
                if (cancellationToken.IsCancellationRequested) break;

                int attack = ThreatPatternTable.EvaluatePosition(board, c, aiPlayer, ruleType);
                int defend = ThreatPatternTable.EvaluatePosition(board, c, opponent, ruleType);

                // Ưu tiên tuyệt đối:
                // 1. Thắng ngay lập tức
                if (attack >= ThreatPatternTable.FiveInRow)
                    return c;

                // 2. Chặn đối thủ thắng ngay lập tức
                if (defend >= ThreatPatternTable.FiveInRow)
                    return c;

                // 3. Tạo thế Open 4
                if (attack >= ThreatPatternTable.OpenFour)
                    return c;

                // 4. Chặn đối thủ tạo Open 4
                if (defend >= ThreatPatternTable.OpenFour)
                    return c;

                // Tổng điểm: Phòng thủ nhân hệ số 1.1 để chắc chắn
                int total = attack + (int)(defend * 1.15);
                scoredMoves.Add((c, attack, defend, total));
            }

            scoredMoves.Sort((a, b) => b.TotalScore.CompareTo(a.TotalScore));

            if (difficulty == AiDifficulty.Medium)
            {
                return scoredMoves[0].Coord;
            }

            // Hard: Minimax 2-ply search trên top 8 nước tốt nhất
            int bestScore = int.MinValue;
            Coordinate bestMove = scoredMoves[0].Coord;
            int branchLimit = Math.Min(8, scoredMoves.Count);

            for (int i = 0; i < branchLimit; i++)
            {
                if (cancellationToken.IsCancellationRequested) break;

                Coordinate move = scoredMoves[i].Coord;
                board.SetCell(move, aiPlayer);

                // Đối thủ phản hồi tối ưu
                int worstOpponentReply = int.MinValue;
                List<Coordinate> opponentCandidates = board.GetCandidateMoves(distance: 1);
                int opponentBranchLimit = Math.Min(6, opponentCandidates.Count);

                foreach (Coordinate oppMove in opponentCandidates.Take(opponentBranchLimit))
                {
                    int oppScore = ThreatPatternTable.EvaluatePosition(board, oppMove, opponent, ruleType);
                    if (oppScore > worstOpponentReply) worstOpponentReply = oppScore;
                }

                board.RemoveCell(move);

                int netScore = scoredMoves[i].TotalScore - worstOpponentReply;
                if (netScore > bestScore)
                {
                    bestScore = netScore;
                    bestMove = move;
                }
            }

            return bestMove;
        }, cancellationToken);
    }
}
