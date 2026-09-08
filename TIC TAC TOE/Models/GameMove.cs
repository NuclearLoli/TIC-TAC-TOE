namespace TIC_TAC_TOE.Models;

public class GameMove
{
    public int Row { get; set; }

    public int Column { get; set; }

    public CellState Player { get; set; }

    public int MoveNumber { get; set; }
}
