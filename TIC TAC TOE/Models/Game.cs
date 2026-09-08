using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TIC_TAC_TOE.Models;

public class Game
{
    public int Id { get; set; }

    public GameMode Mode { get; set; }

    public GameResult Result { get; set; } = GameResult.InProgress;

    public DateTime StartedAt { get; set; } = DateTime.Now;

    public DateTime? EndedAt { get; set; }

    public int PlayerXId { get; set; }

    public Player PlayerX { get; set; } = null!;

    public int PlayerOId { get; set; }

    public Player PlayerO { get; set; } = null!;

    public int? WinnerId { get; set; }

    public Player? Winner { get; set; }

    public ICollection<Move> Moves { get; set; } = new List<Move>();
}