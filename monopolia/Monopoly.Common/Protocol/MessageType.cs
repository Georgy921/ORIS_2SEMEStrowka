namespace Monopoly.Common.Protocol;

public enum MessageType : byte
{
    // Подключение (0x01 - 0x0F)
    Connect = 0x01,
    ConnectResponse = 0x02,
    Disconnect = 0x03,
    PlayerList = 0x04,
    PlayerReady = 0x05,

    // Игровой процесс (0x10 - 0x1F)
    GameStart = 0x10,
    GameState = 0x11,
    RollDice = 0x12,
    DiceResult = 0x13,
    EndTurn = 0x14,
    PlayerMoved = 0x15,

    // Действия с недвижимостью (0x20 - 0x2F)
    BuyProperty = 0x20,
    DeclineBuy = 0x21,
    BuildHouse = 0x22,
    SellHouse = 0x23,
    MortgageProperty = 0x24,
    UnmortgageProperty = 0x25,

    // События (0x30 - 0x3F)
    PayRent = 0x30,
    PayTax = 0x31,
    ChanceCard = 0x32,
    CommunityChest = 0x33,
    GoToJail = 0x34,
    GetOutOfJail = 0x35,
    PassedStart = 0x36,
    FreeParking = 0x37,

    // Конец игры (0x40 - 0x4F)
    Bankruptcy = 0x40,
    Victory = 0x41,

    // Чат и ошибки (0xF0 - 0xFF)
    Chat = 0xF0,
    ServerMessage = 0xF1,
    Error = 0xFF
}