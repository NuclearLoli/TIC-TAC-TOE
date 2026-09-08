using System;

namespace TIC_TAC_TOE.Models;

public class Move
{
    public int Id { get; set; }

    public int GameId { get; set; }

    public Game Game { get; set; } = null!;

    public int PlayerId { get; set; }

    public Player Player { get; set; } = null!;

    public int Row { get; set; }

    public int Column { get; set; }

    public int MoveNumber { get; set; }

    public CellState CellState { get; set; }

    public DateTime PlayedAt { get; set; } = DateTime.Now;
}
