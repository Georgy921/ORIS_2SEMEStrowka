namespace Monopoly.Common.Models;

[Serializable]
public class GameState
{
    public List<Player> Players { get; set; } = new();
    public List<Property> Properties { get; set; } = new();
    public string CurrentPlayerId { get; set; } = string.Empty;
    public int CurrentPlayerIndex { get; set; } = 0;
    public bool IsGameStarted { get; set; } = false;
    public bool IsGameOver { get; set; } = false;
    public string? WinnerId { get; set; }
    public int Dice1 { get; set; }
    public int Dice2 { get; set; }
    public string LastAction { get; set; } = string.Empty;
    public int FreeParkingMoney { get; set; } = 0;
}