using System.Security.Cryptography.X509Certificates;

namespace Monopoly.Common.Models;

[Serializable]
public class Player
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Nickname { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int Money { get; set; } = 1500;
    public int Position { get; set; } = 0;
    public bool IsInJail { get; set; } = false;
    public int JailTurns { get; set; } = 0;
    public bool IsBankrupt { get; set; } = false;
    public int ColorIndex { get; set; }
    public bool IsReady { get; set; } = false;
    public bool HasRolledDice { get; set; } = false;

    public int DoublesCount { get; set; } = 0;

    public Player() { }

    public Player(string nickname, string email)
    {
        Nickname = nickname;
        Email = email;
    }
}