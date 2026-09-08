using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TIC_TAC_TOE.Models;
public class Player
{
    public int Id { get; set; }

    public string Username { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.Now;
 
    public ICollection<Game> GamesAsPlayerX { get; set; } = new List<Game>();

    public ICollection<Game> GamesAsPlayerO { get; set; } = new List<Game>();

    public ICollection<Move> Moves { get; set; } = new List<Move>();
}