namespace Monopoly.Common.Models;

[Serializable]
public class Property
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public PropertyType Type { get; set; }
    public int Price { get; set; }
    public int BaseRent { get; set; }
    public int[] RentWithHouses { get; set; } = new int[5];
    public int HousePrice { get; set; }
    public int Houses { get; set; } = 0;
    public string? OwnerId { get; set; }
    public PropertyGroup Group { get; set; }
    public bool IsMortgaged { get; set; } = false;
    public int MortgageValue => Price / 2;
}

public enum PropertyType
{
    Start,
    Street,
    Railroad,
    Utility,
    Tax,
    Chance,
    CommunityChest,
    Jail,
    GoToJail,
    FreeParking
}

public enum PropertyGroup
{
    None,
    Brown,
    LightBlue,
    Pink,
    Orange,
    Red,
    Yellow,
    Green,
    DarkBlue,
    Railroad,
    Utility
}