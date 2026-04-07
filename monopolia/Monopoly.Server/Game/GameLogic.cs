using Monopoly.Common.Models;
using Monopoly.Common.Protocol;

namespace Monopoly.Server.Game;

public class GameLogic
{
    public GameState State { get; private set; } = new();
    private readonly Random _random = new();
    private readonly List<CardPayload> _chanceCards = new();
    private readonly List<CardPayload> _communityCards = new();

    public GameLogic()
    {
        InitializeBoard();
        InitializeCards();
    }

    public void Reset()
    {
        State = new GameState();
        InitializeBoard();
    }

    private void InitializeBoard()
    {
        State.Properties = new List<Property>
        {
            // Ряд 0 - нижний (справа налево)
            new() { Id = 0, Name = "СТАРТ", Type = PropertyType.Start },
            new() { Id = 1, Name = "Житная ул.", Type = PropertyType.Street, Price = 60, BaseRent = 2, HousePrice = 50, Group = PropertyGroup.Brown, RentWithHouses = new[] { 10, 30, 90, 160, 250 } },
            new() { Id = 2, Name = "Казна", Type = PropertyType.CommunityChest },
            new() { Id = 3, Name = "Нагатинская ул.", Type = PropertyType.Street, Price = 60, BaseRent = 4, HousePrice = 50, Group = PropertyGroup.Brown, RentWithHouses = new[] { 20, 60, 180, 320, 450 } },
            new() { Id = 4, Name = "Подоходный налог", Type = PropertyType.Tax, Price = 200 },
            new() { Id = 5, Name = "Рижская ж/д", Type = PropertyType.Railroad, Price = 200, BaseRent = 25, Group = PropertyGroup.Railroad },
            new() { Id = 6, Name = "Варшавское ш.", Type = PropertyType.Street, Price = 100, BaseRent = 6, HousePrice = 50, Group = PropertyGroup.LightBlue, RentWithHouses = new[] { 30, 90, 270, 400, 550 } },
            new() { Id = 7, Name = "Шанс", Type = PropertyType.Chance },
            new() { Id = 8, Name = "Ул. Огарёва", Type = PropertyType.Street, Price = 100, BaseRent = 6, HousePrice = 50, Group = PropertyGroup.LightBlue, RentWithHouses = new[] { 30, 90, 270, 400, 550 } },
            new() { Id = 9, Name = "Первая Парковая", Type = PropertyType.Street, Price = 120, BaseRent = 8, HousePrice = 50, Group = PropertyGroup.LightBlue, RentWithHouses = new[] { 40, 100, 300, 450, 600 } },

            // Ряд 1 - левый (снизу вверх)
            new() { Id = 10, Name = "Тюрьма", Type = PropertyType.Jail },
            new() { Id = 11, Name = "Полянка", Type = PropertyType.Street, Price = 140, BaseRent = 10, HousePrice = 100, Group = PropertyGroup.Pink, RentWithHouses = new[] { 50, 150, 450, 625, 750 } },
            new() { Id = 12, Name = "Электростанция", Type = PropertyType.Utility, Price = 150, BaseRent = 0, Group = PropertyGroup.Utility },
            new() { Id = 13, Name = "Ул. Сретенка", Type = PropertyType.Street, Price = 140, BaseRent = 10, HousePrice = 100, Group = PropertyGroup.Pink, RentWithHouses = new[] { 50, 150, 450, 625, 750 } },
            new() { Id = 14, Name = "Ростовская наб.", Type = PropertyType.Street, Price = 160, BaseRent = 12, HousePrice = 100, Group = PropertyGroup.Pink, RentWithHouses = new[] { 60, 180, 500, 700, 900 } },
            new() { Id = 15, Name = "Курская ж/д", Type = PropertyType.Railroad, Price = 200, BaseRent = 25, Group = PropertyGroup.Railroad },
            new() { Id = 16, Name = "Рязанский пр.", Type = PropertyType.Street, Price = 180, BaseRent = 14, HousePrice = 100, Group = PropertyGroup.Orange, RentWithHouses = new[] { 70, 200, 550, 750, 950 } },
            new() { Id = 17, Name = "Казна", Type = PropertyType.CommunityChest },
            new() { Id = 18, Name = "Ул. Вавилова", Type = PropertyType.Street, Price = 180, BaseRent = 14, HousePrice = 100, Group = PropertyGroup.Orange, RentWithHouses = new[] { 70, 200, 550, 750, 950 } },
            new() { Id = 19, Name = "Рублёвское ш.", Type = PropertyType.Street, Price = 200, BaseRent = 16, HousePrice = 100, Group = PropertyGroup.Orange, RentWithHouses = new[] { 80, 220, 600, 800, 1000 } },

            // Ряд 2 - верхний (слева направо)
            new() { Id = 20, Name = "Бесплатная парковка", Type = PropertyType.FreeParking },
            new() { Id = 21, Name = "Ул. Тверская", Type = PropertyType.Street, Price = 220, BaseRent = 18, HousePrice = 150, Group = PropertyGroup.Red, RentWithHouses = new[] { 90, 250, 700, 875, 1050 } },
            new() { Id = 22, Name = "Шанс", Type = PropertyType.Chance },
            new() { Id = 23, Name = "Пушкинская ул.", Type = PropertyType.Street, Price = 220, BaseRent = 18, HousePrice = 150, Group = PropertyGroup.Red, RentWithHouses = new[] { 90, 250, 700, 875, 1050 } },
            new() { Id = 24, Name = "Пл. Маяковского", Type = PropertyType.Street, Price = 240, BaseRent = 20, HousePrice = 150, Group = PropertyGroup.Red, RentWithHouses = new[] { 100, 300, 750, 925, 1100 } },
            new() { Id = 25, Name = "Казанская ж/д", Type = PropertyType.Railroad, Price = 200, BaseRent = 25, Group = PropertyGroup.Railroad },
            new() { Id = 26, Name = "Ул. Грузинский Вал", Type = PropertyType.Street, Price = 260, BaseRent = 22, HousePrice = 150, Group = PropertyGroup.Yellow, RentWithHouses = new[] { 110, 330, 800, 975, 1150 } },
            new() { Id = 27, Name = "Чистопрудный б-р", Type = PropertyType.Street, Price = 260, BaseRent = 22, HousePrice = 150, Group = PropertyGroup.Yellow, RentWithHouses = new[] { 110, 330, 800, 975, 1150 } },
            new() { Id = 28, Name = "Водопровод", Type = PropertyType.Utility, Price = 150, BaseRent = 0, Group = PropertyGroup.Utility },
            new() { Id = 29, Name = "Ул. Малая Бронная", Type = PropertyType.Street, Price = 280, BaseRent = 24, HousePrice = 150, Group = PropertyGroup.Yellow, RentWithHouses = new[] { 120, 360, 850, 1025, 1200 } },

            // Ряд 3 - правый (сверху вниз)
            new() { Id = 30, Name = "Идите в тюрьму", Type = PropertyType.GoToJail },
            new() { Id = 31, Name = "Ул. Арбат", Type = PropertyType.Street, Price = 300, BaseRent = 26, HousePrice = 200, Group = PropertyGroup.Green, RentWithHouses = new[] { 130, 390, 900, 1100, 1275 } },
            new() { Id = 32, Name = "Столешников пер.", Type = PropertyType.Street, Price = 300, BaseRent = 26, HousePrice = 200, Group = PropertyGroup.Green, RentWithHouses = new[] { 130, 390, 900, 1100, 1275 } },
            new() { Id = 33, Name = "Казна", Type = PropertyType.CommunityChest },
            new() { Id = 34, Name = "Кузнецкий мост", Type = PropertyType.Street, Price = 320, BaseRent = 28, HousePrice = 200, Group = PropertyGroup.Green, RentWithHouses = new[] { 150, 450, 1000, 1200, 1400 } },
            new() { Id = 35, Name = "Ленинградская ж/д", Type = PropertyType.Railroad, Price = 200, BaseRent = 25, Group = PropertyGroup.Railroad },
            new() { Id = 36, Name = "Шанс", Type = PropertyType.Chance },
            new() { Id = 37, Name = "Ул. Петровка", Type = PropertyType.Street, Price = 350, BaseRent = 35, HousePrice = 200, Group = PropertyGroup.DarkBlue, RentWithHouses = new[] { 175, 500, 1100, 1300, 1500 } },
            new() { Id = 38, Name = "Налог на роскошь", Type = PropertyType.Tax, Price = 100 },
            new() { Id = 39, Name = "Ул. Ордынка", Type = PropertyType.Street, Price = 400, BaseRent = 50, HousePrice = 200, Group = PropertyGroup.DarkBlue, RentWithHouses = new[] { 200, 600, 1400, 1700, 2000 } }
        };
    }

    private void InitializeCards()
    {
        _chanceCards.Clear();
        _chanceCards.AddRange(new[]
        {
            new CardPayload { Title = "Банковские дивиденды", Description = "Получите $50", MoneyChange = 50 },
            new CardPayload { Title = "Штраф за превышение скорости", Description = "Заплатите $15", MoneyChange = -15 },
            new CardPayload { Title = "Выигрыш в лотерею", Description = "Получите $150", MoneyChange = 150 },
            new CardPayload { Title = "Ремонт автомобиля", Description = "Заплатите $75", MoneyChange = -75 },
            new CardPayload { Title = "Идите на СТАРТ", Description = "Переместитесь на СТАРТ и получите $200", NewPosition = 0, MoneyChange = 200 },
            new CardPayload { Title = "Отправляйтесь в тюрьму", Description = "Идите прямо в тюрьму, не проходите СТАРТ", GoToJail = true },
            new CardPayload { Title = "Возврат налогов", Description = "Получите $20", MoneyChange = 20 },
            new CardPayload { Title = "День рождения", Description = "Получите $100 в подарок", MoneyChange = 100 },
            new CardPayload { Title = "Оплата обучения", Description = "Заплатите $150", MoneyChange = -150 },
            new CardPayload { Title = "Премия от работы", Description = "Получите $200", MoneyChange = 200 }
        });

        _communityCards.Clear();
        _communityCards.AddRange(new[]
        {
            new CardPayload { Title = "Ошибка банка в вашу пользу", Description = "Получите $200", MoneyChange = 200 },
            new CardPayload { Title = "Оплата услуг доктора", Description = "Заплатите $50", MoneyChange = -50 },
            new CardPayload { Title = "Продажа акций", Description = "Получите $45", MoneyChange = 45 },
            new CardPayload { Title = "Оплата страховки", Description = "Заплатите $100", MoneyChange = -100 },
            new CardPayload { Title = "Наследство", Description = "Получите $100", MoneyChange = 100 },
            new CardPayload { Title = "Возврат подоходного налога", Description = "Получите $25", MoneyChange = 25 },
            new CardPayload { Title = "Второе место на конкурсе красоты", Description = "Получите $10", MoneyChange = 10 },
            new CardPayload { Title = "Оплата больницы", Description = "Заплатите $100", MoneyChange = -100 },
            new CardPayload { Title = "Выигрыш в конкурсе", Description = "Получите $50", MoneyChange = 50 },
            new CardPayload { Title = "Продажа имущества", Description = "Получите $75", MoneyChange = 75 }
        });
    }

    public void AddPlayer(Player player)
    {
        if (State.Players.Count < 4 && !State.IsGameStarted)
        {
            State.Players.Add(player);
        }
    }

    public void RemovePlayer(string playerId)
    {
        var player = GetPlayer(playerId);
        if (player != null)
        {
            // Освобождаем недвижимость
            foreach (var property in State.Properties.Where(p => p.OwnerId == playerId))
            {
                property.OwnerId = null;
                property.Houses = 0;
            }

            State.Players.Remove(player);

            // Если игра идёт и это был текущий игрок - передаём ход
            if (State.IsGameStarted && State.CurrentPlayerId == playerId)
            {
                NextTurn();
            }
        }
    }

    public Player? GetPlayer(string playerId)
    {
        return State.Players.FirstOrDefault(p => p.Id == playerId);
    }

    public Player? GetCurrentPlayer()
    {
        return State.Players.FirstOrDefault(p => p.Id == State.CurrentPlayerId);
    }

    public void StartGame()
    {
        State.IsGameStarted = true;
        State.IsGameOver = false;
        State.CurrentPlayerIndex = 0;
        State.CurrentPlayerId = State.Players[0].Id;

        foreach (var player in State.Players)
        {
            player.Money = 1500;
            player.Position = 0;
            player.HasRolledDice = false;
            player.IsBankrupt = false;
            player.IsInJail = false;
            player.JailTurns = 0;
            player.DoublesCount = 0;
        }

        State.LastAction = "Игра началась!";
    }

    public DiceResultPayload RollDice(string playerId)
    {
        var player = GetPlayer(playerId);
        if (player == null)
            return new DiceResultPayload();

        int dice1 = _random.Next(1, 7);
        int dice2 = _random.Next(1, 7);
        bool isDouble = dice1 == dice2;

        State.Dice1 = dice1;
        State.Dice2 = dice2;

        int oldPosition = player.Position;

        // Обработка дублей
        if (isDouble)
        {
            player.DoublesCount++;
            if (player.DoublesCount >= 3)
            {
                // 3 дубля - тюрьма
                return new DiceResultPayload
                {
                    PlayerId = playerId,
                    Dice1 = dice1,
                    Dice2 = dice2,
                    OldPosition = oldPosition,
                    NewPosition = 10,
                    IsDouble = true,
                    TripleDouble = true
                };
            }
        }
        else
        {
            player.DoublesCount = 0;
        }

        // Обработка тюрьмы
        if (player.IsInJail)
        {
            if (isDouble)
            {
                player.IsInJail = false;
                player.JailTurns = 0;
                State.LastAction = $"{player.Nickname} выбросил дубль и вышел из тюрьмы!";
            }
            else
            {
                player.JailTurns++;
                if (player.JailTurns >= 3)
                {
                    player.IsInJail = false;
                    player.JailTurns = 0;
                    player.Money -= 50;
                    State.FreeParkingMoney += 50;
                    State.LastAction = $"{player.Nickname} заплатил $50 и вышел из тюрьмы";
                }
                else
                {
                    player.HasRolledDice = true;
                    State.LastAction = $"{player.Nickname} остаётся в тюрьме ({player.JailTurns}/3)";
                    return new DiceResultPayload
                    {
                        PlayerId = playerId,
                        Dice1 = dice1,
                        Dice2 = dice2,
                        OldPosition = oldPosition,
                        NewPosition = player.Position,
                        IsDouble = isDouble
                    };
                }
            }
        }

        // Движение
        int total = dice1 + dice2;
        player.Position = (player.Position + total) % 40;

        // Прохождение через СТАРТ
        if (player.Position < oldPosition)
        {
            player.Money += 200;
            State.LastAction = $"{player.Nickname} прошёл через СТАРТ и получил $200";
        }

        player.HasRolledDice = true;

        return new DiceResultPayload
        {
            PlayerId = playerId,
            Dice1 = dice1,
            Dice2 = dice2,
            OldPosition = oldPosition,
            NewPosition = player.Position,
            IsDouble = isDouble
        };
    }

    public int CalculateRent(Property property)
    {
        if (property.OwnerId == null || property.IsMortgaged)
            return 0;

        switch (property.Type)
        {
            case PropertyType.Street:
                if (property.Houses > 0)
                    return property.RentWithHouses[Math.Min(property.Houses - 1, 4)];

                // Проверка монополии
                var sameGroup = State.Properties.Where(p => p.Group == property.Group).ToList();
                if (sameGroup.All(p => p.OwnerId == property.OwnerId))
                    return property.BaseRent * 2;

                return property.BaseRent;

            case PropertyType.Railroad:
                int railroads = State.Properties.Count(p =>
                    p.Type == PropertyType.Railroad && p.OwnerId == property.OwnerId);
                return 25 * (int)Math.Pow(2, railroads - 1);

            case PropertyType.Utility:
                int utilities = State.Properties.Count(p =>
                    p.Type == PropertyType.Utility && p.OwnerId == property.OwnerId);
                int multiplier = utilities == 2 ? 10 : 4;
                return (State.Dice1 + State.Dice2) * multiplier;

            default:
                return 0;
        }
    }

    public bool BuyProperty(string playerId, int propertyId)
    {
        var player = GetPlayer(playerId);
        var property = State.Properties.FirstOrDefault(p => p.Id == propertyId);

        if (player == null || property == null) return false;
        if (property.OwnerId != null) return false;
        if (player.Money < property.Price) return false;
        if (property.Type != PropertyType.Street &&
            property.Type != PropertyType.Railroad &&
            property.Type != PropertyType.Utility)
            return false;

        player.Money -= property.Price;
        property.OwnerId = playerId;

        State.LastAction = $"{player.Nickname} купил {property.Name} за ${property.Price}";
        return true;
    }

    public void PayRent(string payerId, string receiverId, int amount)
    {
        var payer = GetPlayer(payerId);
        var receiver = GetPlayer(receiverId);

        if (payer == null || receiver == null) return;

        payer.Money -= amount;
        receiver.Money += amount;

        State.LastAction = $"{payer.Nickname} заплатил ${amount} игроку {receiver.Nickname}";
    }

    public void PayTax(string playerId, int amount)
    {
        var player = GetPlayer(playerId);
        if (player == null) return;

        player.Money -= amount;
        State.FreeParkingMoney += amount;
        State.LastAction = $"{player.Nickname} заплатил налог ${amount}";
    }

    public void SendToJail(string playerId)
    {
        var player = GetPlayer(playerId);
        if (player == null) return;

        player.Position = 10;
        player.IsInJail = true;
        player.JailTurns = 0;
        player.DoublesCount = 0;
        player.HasRolledDice = true;

        State.LastAction = $"{player.Nickname} отправляется в тюрьму!";
    }

    public int CollectFreeParking(string playerId)
    {
        var player = GetPlayer(playerId);
        if (player == null) return 0;

        int amount = State.FreeParkingMoney;
        player.Money += amount;
        State.FreeParkingMoney = 0;

        return amount;
    }

    public bool BuildHouse(string playerId, int propertyId)
    {
        var player = GetPlayer(playerId);
        var property = State.Properties.FirstOrDefault(p => p.Id == propertyId);

        if (player == null || property == null) return false;
        if (property.OwnerId != playerId) return false;
        if (property.Type != PropertyType.Street) return false;
        if (property.Houses >= 5) return false;
        if (player.Money < property.HousePrice) return false;

        // Проверка монополии
        var sameGroup = State.Properties.Where(p => p.Group == property.Group).ToList();
        if (!sameGroup.All(p => p.OwnerId == playerId))
            return false;

        // Проверка равномерности застройки
        int minHouses = sameGroup.Min(p => p.Houses);
        if (property.Houses > minHouses)
            return false;

        player.Money -= property.HousePrice;
        property.Houses++;

        string type = property.Houses == 5 ? "отель" : $"дом #{property.Houses}";
        State.LastAction = $"{player.Nickname} построил {type} на {property.Name}";

        return true;
    }

    public CardPayload DrawChanceCard(string playerId)
    {
        var player = GetPlayer(playerId);
        if (player == null) return new CardPayload();

        var card = _chanceCards[_random.Next(_chanceCards.Count)];
        ApplyCard(player, card);
        return card;
    }

    public CardPayload DrawCommunityChestCard(string playerId)
    {
        var player = GetPlayer(playerId);
        if (player == null) return new CardPayload();

        var card = _communityCards[_random.Next(_communityCards.Count)];
        ApplyCard(player, card);
        return card;
    }

    private void ApplyCard(Player player, CardPayload card)
    {
        player.Money += card.MoneyChange;

        if (card.MoneyChange < 0)
        {
            State.FreeParkingMoney += Math.Abs(card.MoneyChange);
        }

        if (card.GoToJail)
        {
            SendToJail(player.Id);
        }
        else if (card.NewPosition >= 0)
        {
            int oldPos = player.Position;
            player.Position = card.NewPosition;
        }

        State.LastAction = $"{player.Nickname}: {card.Title}";
    }

    public void DeclareBankruptcy(string playerId)
    {
        var player = GetPlayer(playerId);
        if (player == null) return;

        player.IsBankrupt = true;
        player.Money = 0;

        // Освобождаем недвижимость
        foreach (var property in State.Properties.Where(p => p.OwnerId == playerId))
        {
            property.OwnerId = null;
            property.Houses = 0;
        }

        State.LastAction = $"{player.Nickname} обанкротился!";
    }

    public Player? CheckWinner()
    {
        var activePlayers = State.Players.Where(p => !p.IsBankrupt).ToList();

        if (activePlayers.Count == 1)
        {
            State.IsGameOver = true;
            State.WinnerId = activePlayers[0].Id;
            return activePlayers[0];
        }

        if (activePlayers.Count == 0)
        {
            State.IsGameOver = true;
            return null;
        }

        return null;
    }

    public void NextTurn()
    {
        var currentPlayer = GetCurrentPlayer();
        if (currentPlayer != null)
        {
            currentPlayer.HasRolledDice = false;
            currentPlayer.DoublesCount = 0;
        }

        // Ищем следующего активного игрока
        int attempts = 0;
        do
        {
            State.CurrentPlayerIndex = (State.CurrentPlayerIndex + 1) % State.Players.Count;
            attempts++;
        }
        while (State.Players[State.CurrentPlayerIndex].IsBankrupt && attempts < State.Players.Count);

        if (attempts >= State.Players.Count)
        {
            // Все игроки банкроты - проверяем победителя
            CheckWinner();
            return;
        }

        State.CurrentPlayerId = State.Players[State.CurrentPlayerIndex].Id;

        var newPlayer = GetCurrentPlayer();
        State.LastAction = $"Ход переходит к {newPlayer?.Nickname}";
    }
}