using System;

namespace TIC_TAC_TOE.Models;

public class GameHistorySummary
{
    public int GameId { get; set; }

    public string PlayerXName { get; set; } = string.Empty;

    public string PlayerOName { get; set; } = string.Empty;

    public string ModeText { get; set; } = string.Empty;

    public string ResultText { get; set; } = string.Empty;

    public string WinnerName { get; set; } = string.Empty;

    public int TotalMoves { get; set; }

    public DateTime StartedAt { get; set; }

    public DateTime? EndedAt { get; set; }

    public string DisplayText =>
        $"#{GameId} | {ModeText} | {PlayerXName} vs {PlayerOName} | {ResultText} | " +
        $"Người thắng: {WinnerName} | Số nước: {TotalMoves} | " +
        $"Kết thúc: {(EndedAt.HasValue ? EndedAt.Value.ToString("dd/MM/yyyy HH:mm:ss") : "N/A")}";
}
