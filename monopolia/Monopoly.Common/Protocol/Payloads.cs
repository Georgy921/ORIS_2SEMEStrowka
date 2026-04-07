namespace Monopoly.Common.Protocol;

[Serializable]
public class ConnectPayload
{
    public string Nickname { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

[Serializable]
public class ConnectResponsePayload
{
    public bool Success { get; set; }
    public string PlayerId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public int ColorIndex { get; set; }
}

[Serializable]
public class DiceResultPayload
{
    public string PlayerId { get; set; } = string.Empty;
    public int Dice1 { get; set; }
    public int Dice2 { get; set; }
    public int OldPosition { get; set; }
    public int NewPosition { get; set; }
    public bool IsDouble { get; set; }
    public bool TripleDouble { get; set; }
}

[Serializable]
public class PropertyActionPayload
{
    public int PropertyId { get; set; }
    public string PlayerId { get; set; } = string.Empty;
    public int Amount { get; set; }
    public string PropertyName { get; set; } = string.Empty;
}

[Serializable]
public class RentPayload
{
    public string PayerId { get; set; } = string.Empty;
    public string PayerName { get; set; } = string.Empty;
    public string ReceiverId { get; set; } = string.Empty;
    public string ReceiverName { get; set; } = string.Empty;
    public int Amount { get; set; }
    public string PropertyName { get; set; } = string.Empty;
}

[Serializable]
public class CardPayload
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int MoneyChange { get; set; }
    public int NewPosition { get; set; } = -1;
    public bool GoToJail { get; set; }
    public bool CollectFromPlayers { get; set; }
    public int CollectAmount { get; set; }
}

[Serializable]
public class VictoryPayload
{
    public string WinnerId { get; set; } = string.Empty;
    public string WinnerNickname { get; set; } = string.Empty;
    public int FinalMoney { get; set; }
    public int PropertiesCount { get; set; }
    public int TotalAssets { get; set; }
}

[Serializable]
public class ChatPayload
{
    public string SenderName { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

[Serializable]
public class ServerMessagePayload
{
    public string Message { get; set; } = string.Empty;
    public string Type { get; set; } = "info"; // info, warning, error
}

[Serializable]
public class ErrorPayload
{
    public string Message { get; set; } = string.Empty;
}