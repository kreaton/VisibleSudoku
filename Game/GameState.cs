using System.Text.Json.Serialization;

namespace VisibleSudoku.Game;

public class GameState
{
    public Difficulty Difficulty { get; set; }
    public int[] Givens { get; set; } = new int[81];
    public int[] Solution { get; set; } = new int[81];
    public int[] Values { get; set; } = new int[81];
    public int[] Notes { get; set; } = new int[81]; // bitmask, bit n = note for digit n
    public int Seconds { get; set; }
    public int Mistakes { get; set; }
    public int Hints { get; set; }
    public bool Completed { get; set; }
}

[JsonSerializable(typeof(GameState))]
public partial class AppJson : JsonSerializerContext { }
