using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using Monopoly.Common.Models;
using Monopoly.Common.Protocol;
using Monopoly.Server.Game;

namespace Monopoly.Server.Network;

public class GameServer
{
    private readonly int _port;
    private readonly GameLogic _gameLogic;
    private TcpListener? _listener;
    private bool _isRunning;
    private readonly ConcurrentDictionary<string, ClientHandler> _clients = new();

    public event Action<string>? OnLog;
    public event Action? OnPlayerListChanged;
    public event Action? OnGameStateChanged;

    public GameServer(int port, GameLogic gameLogic)
    {
        _port = port;
        _gameLogic = gameLogic;
    }

    public void Start()
    {
        _listener = new TcpListener(IPAddress.Any, _port);
        _listener.Start();
        _isRunning = true;

        Log($"Сервер слушает порт {_port}");

        Task.Run(AcceptClientsAsync);
    }

    public void Stop()
    {
        _isRunning = false;

        foreach (var client in _clients.Values)
        {
            client.Disconnect();
        }
        _clients.Clear();

        _listener?.Stop();
        Log("Сервер остановлен");
    }

    private async Task AcceptClientsAsync()
    {
        while (_isRunning)
        {
            try
            {
                var tcpClient = await _listener!.AcceptTcpClientAsync();
                _ = Task.Run(() => HandleClientAsync(tcpClient));
            }
            catch when (!_isRunning)
            {
                break;
            }
            catch (Exception ex)
            {
                Log($"Ошибка принятия подключения: {ex.Message}");
            }
        }
    }

    private async Task HandleClientAsync(TcpClient tcpClient)
    {
        var endpoint = tcpClient.Client.RemoteEndPoint?.ToString() ?? "unknown";
        Log($"Новое подключение: {endpoint}");

        var handler = new ClientHandler(tcpClient);
        string? playerId = null;

        try
        {
            while (_isRunning && tcpClient.Connected)
            {
                var message = await handler.ReceiveMessageAsync();
                if (message == null) break;

                playerId = await ProcessMessageAsync(message, handler, playerId);
            }
        }
        catch (Exception ex)
        {
            Log($"Ошибка клиента {endpoint}: {ex.Message}");
        }
        finally
        {
            if (playerId != null)
            {
                RemovePlayer(playerId);
            }
            handler.Disconnect();
            Log($"Клиент отключился: {endpoint}");
        }
    }

    private void RemovePlayer(string playerId)
    {
        if (_clients.TryRemove(playerId, out _))
        {
            var player = _gameLogic.GetPlayer(playerId);
            var name = player?.Nickname ?? playerId;

            _gameLogic.RemovePlayer(playerId);

            BroadcastPlayerList();
            OnPlayerListChanged?.Invoke();

            if (_gameLogic.State.IsGameStarted)
            {
                BroadcastGameState();
                OnGameStateChanged?.Invoke();

                Broadcast(new GameMessage(MessageType.ServerMessage, new ServerMessagePayload
                {
                    Message = $"Игрок {name} покинул игру",
                    Type = "warning"
                }));

                var winner = _gameLogic.CheckWinner();
                if (winner != null)
                {
                    HandleVictory(winner);
                }
            }

            Log($"Игрок {name} удалён");
        }
    }

    public void KickPlayer(string playerId)
    {
        if (_clients.TryGetValue(playerId, out var handler))
        {
            handler.Disconnect();
        }
        RemovePlayer(playerId);
    }

    public void ForceStartGame()
    {
        if (_gameLogic.State.Players.Count >= 2 && !_gameLogic.State.IsGameStarted)
        {
            foreach (var player in _gameLogic.State.Players)
            {
                player.IsReady = true;
            }

            StartGame();
        }
    }

    private async Task<string?> ProcessMessageAsync(GameMessage message, ClientHandler handler, string? playerId)
    {
        switch (message.Type)
        {
            case MessageType.Connect:
                return HandleConnect(message, handler);

            case MessageType.PlayerReady:
                HandlePlayerReady(playerId);
                break;

            case MessageType.RollDice:
                await HandleRollDiceAsync(playerId);
                break;

            case MessageType.BuyProperty:
                HandleBuyProperty(playerId);
                break;

            case MessageType.DeclineBuy:
                HandleDeclineBuy(playerId);
                break;

            case MessageType.BuildHouse:
                var buildPayload = message.GetData<PropertyActionPayload>();
                if (buildPayload != null)
                    HandleBuildHouse(playerId, buildPayload.PropertyId);
                break;

            case MessageType.EndTurn:
                HandleEndTurn(playerId);
                break;

            case MessageType.Chat:
                HandleChat(message, playerId);
                break;
        }

        return playerId;
    }

    private string? HandleConnect(GameMessage message, ClientHandler handler)
    {
        var payload = message.GetData<ConnectPayload>();
        if (payload == null)
        {
            SendError(handler, "Неверные данные подключения");
            return null;
        }

        if (_clients.Count >= 4)
        {
            handler.SendMessage(new GameMessage(MessageType.ConnectResponse,
                new ConnectResponsePayload
                {
                    Success = false,
                    Message = "Сервер полон (максимум 4 игрока)"
                }));
            return null;
        }

        if (_gameLogic.State.IsGameStarted)
        {
            handler.SendMessage(new GameMessage(MessageType.ConnectResponse,
                new ConnectResponsePayload
                {
                    Success = false,
                    Message = "Игра уже началась"
                }));
            return null;
        }

        var player = new Player(payload.Nickname, payload.Email)
        {
            ColorIndex = _clients.Count
        };

        _clients.TryAdd(player.Id, handler);
        _gameLogic.AddPlayer(player);

        handler.SendMessage(new GameMessage(MessageType.ConnectResponse,
            new ConnectResponsePayload
            {
                Success = true,
                PlayerId = player.Id,
                ColorIndex = player.ColorIndex,
                Message = $"Добро пожаловать, {player.Nickname}!"
            }));

        Log($"Игрок подключился: {player.Nickname} ({player.Email})");

        BroadcastPlayerList();
        OnPlayerListChanged?.Invoke();

        Broadcast(new GameMessage(MessageType.ServerMessage, new ServerMessagePayload
        {
            Message = $"Игрок {player.Nickname} присоединился к игре",
            Type = "info"
        }), player.Id);

        return player.Id;
    }

    private void HandlePlayerReady(string? playerId)
    {
        if (playerId == null) return;

        var player = _gameLogic.GetPlayer(playerId);
        if (player == null) return;

        player.IsReady = !player.IsReady;
        Log($"{player.Nickname}: {(player.IsReady ? "готов" : "не готов")}");

        BroadcastPlayerList();
        OnPlayerListChanged?.Invoke();

        // Проверяем условия старта (все готовы и минимум 2 игрока)
        var players = _gameLogic.State.Players;
        if (players.Count >= 2 && players.All(p => p.IsReady))
        {
            StartGame();
        }
    }

    private void StartGame()
    {
        Log("Все игроки готовы. Игра начинается!");

        _gameLogic.StartGame();

        Broadcast(new GameMessage(MessageType.GameStart));
        BroadcastGameState();
        OnGameStateChanged?.Invoke();

        var firstPlayer = _gameLogic.GetCurrentPlayer();
        Log($"Первый ход: {firstPlayer?.Nickname}");
    }

    private async Task HandleRollDiceAsync(string? playerId)
    {
        if (playerId == null) return;
        if (!_gameLogic.State.IsGameStarted) return;
        if (_gameLogic.State.CurrentPlayerId != playerId) return;

        var player = _gameLogic.GetPlayer(playerId);
        if (player == null || player.HasRolledDice) return;

        var result = _gameLogic.RollDice(playerId);

        Log($"{player.Nickname} бросил {result.Dice1}+{result.Dice2}={result.Dice1 + result.Dice2}" +
            (result.IsDouble ? " (дубль!)" : ""));

        Broadcast(new GameMessage(MessageType.DiceResult, result));

        if (result.TripleDouble)
        {
            Log($"{player.Nickname} выбросил 3 дубля подряд - тюрьма!");
            _gameLogic.SendToJail(playerId);
            Broadcast(new GameMessage(MessageType.GoToJail, new PropertyActionPayload { PlayerId = playerId }));
            BroadcastGameState();
            OnGameStateChanged?.Invoke();
            return;
        }

        await Task.Delay(800); // Пауза для анимации

        await ProcessLandingAsync(playerId);
    }

    private async Task ProcessLandingAsync(string playerId)
    {
        var player = _gameLogic.GetPlayer(playerId);
        if (player == null) return;

        var property = _gameLogic.State.Properties[player.Position];
        Log($"{player.Nickname} попал на: {property.Name}");

        switch (property.Type)
        {
            case PropertyType.Start:
                // Уже получил $200 при прохождении
                break;

            case PropertyType.Street:
            case PropertyType.Railroad:
            case PropertyType.Utility:
                if (property.OwnerId != null && property.OwnerId != playerId && !property.IsMortgaged)
                {
                    int rent = _gameLogic.CalculateRent(property);
                    var owner = _gameLogic.GetPlayer(property.OwnerId);

                    _gameLogic.PayRent(playerId, property.OwnerId, rent);

                    Broadcast(new GameMessage(MessageType.PayRent, new RentPayload
                    {
                        PayerId = playerId,
                        PayerName = player.Nickname,
                        ReceiverId = property.OwnerId,
                        ReceiverName = owner?.Nickname ?? "",
                        Amount = rent,
                        PropertyName = property.Name
                    }));

                    Log($"{player.Nickname} заплатил ${rent} за {property.Name}");

                    await CheckBankruptcy(playerId);
                }
                break;

            case PropertyType.Tax:
                int tax = property.Price;
                _gameLogic.PayTax(playerId, tax);

                Broadcast(new GameMessage(MessageType.PayTax, new PropertyActionPayload
                {
                    PlayerId = playerId,
                    Amount = tax,
                    PropertyName = property.Name
                }));

                Log($"{player.Nickname} заплатил налог ${tax}");
                await CheckBankruptcy(playerId);
                break;

            case PropertyType.GoToJail:
                _gameLogic.SendToJail(playerId);
                Broadcast(new GameMessage(MessageType.GoToJail, new PropertyActionPayload
                {
                    PlayerId = playerId
                }));
                Log($"{player.Nickname} отправляется в тюрьму!");
                break;

            case PropertyType.Chance:
                var chanceCard = _gameLogic.DrawChanceCard(playerId);
                Broadcast(new GameMessage(MessageType.ChanceCard, chanceCard));
                Log($"{player.Nickname} - Шанс: {chanceCard.Title}");

                if (chanceCard.GoToJail)
                {
                    Broadcast(new GameMessage(MessageType.GoToJail, new PropertyActionPayload
                    {
                        PlayerId = playerId
                    }));
                }

                await CheckBankruptcy(playerId);
                break;

            case PropertyType.CommunityChest:
                var communityCard = _gameLogic.DrawCommunityChestCard(playerId);
                Broadcast(new GameMessage(MessageType.CommunityChest, communityCard));
                Log($"{player.Nickname} - Казна: {communityCard.Title}");

                await CheckBankruptcy(playerId);
                break;

            case PropertyType.FreeParking:
                int parking = _gameLogic.CollectFreeParking(playerId);
                if (parking > 0)
                {
                    Broadcast(new GameMessage(MessageType.FreeParking, new PropertyActionPayload
                    {
                        PlayerId = playerId,
                        Amount = parking
                    }));
                    Log($"{player.Nickname} забрал ${parking} с бесплатной парковки");
                }
                break;

            case PropertyType.Jail:
                // Просто посещение - ничего не происходит
                break;
        }

        BroadcastGameState();
        OnGameStateChanged?.Invoke();
    }

    private async Task CheckBankruptcy(string playerId)
    {
        var player = _gameLogic.GetPlayer(playerId);
        if (player == null) return;

        if (player.Money < 0)
        {
            _gameLogic.DeclareBankruptcy(playerId);

            Broadcast(new GameMessage(MessageType.Bankruptcy, new PropertyActionPayload
            {
                PlayerId = playerId
            }));

            Log($"{player.Nickname} БАНКРОТ!");

            var winner = _gameLogic.CheckWinner();
            if (winner != null)
            {
                HandleVictory(winner);
            }
        }
    }

    private void HandleVictory(Player winner)
    {
        int totalAssets = winner.Money;
        int propertiesCount = 0;

        foreach (var prop in _gameLogic.State.Properties.Where(p => p.OwnerId == winner.Id))
        {
            propertiesCount++;
            totalAssets += prop.Price;
            totalAssets += prop.Houses * prop.HousePrice;
        }

        Broadcast(new GameMessage(MessageType.Victory, new VictoryPayload
        {
            WinnerId = winner.Id,
            WinnerNickname = winner.Nickname,
            FinalMoney = winner.Money,
            PropertiesCount = propertiesCount,
            TotalAssets = totalAssets
        }));

        Log($"ПОБЕДИТЕЛЬ: {winner.Nickname}! Активы: ${totalAssets}");
        OnGameStateChanged?.Invoke();
    }

    private void HandleBuyProperty(string? playerId)
    {
        if (playerId == null) return;

        var player = _gameLogic.GetPlayer(playerId);
        if (player == null) return;

        var property = _gameLogic.State.Properties[player.Position];

        if (_gameLogic.BuyProperty(playerId, property.Id))
        {
            Broadcast(new GameMessage(MessageType.BuyProperty, new PropertyActionPayload
            {
                PropertyId = property.Id,
                PlayerId = playerId,
                Amount = property.Price,
                PropertyName = property.Name
            }));

            Log($"{player.Nickname} купил {property.Name} за ${property.Price}");
            BroadcastGameState();
            OnGameStateChanged?.Invoke();
        }
    }

    private void HandleDeclineBuy(string? playerId)
    {
        if (playerId == null) return;

        var player = _gameLogic.GetPlayer(playerId);
        if (player == null) return;

        var property = _gameLogic.State.Properties[player.Position];
        Log($"{player.Nickname} отказался покупать {property.Name}");
    }

    private void HandleBuildHouse(string? playerId, int propertyId)
    {
        if (playerId == null) return;

        if (_gameLogic.BuildHouse(playerId, propertyId))
        {
            var property = _gameLogic.State.Properties.FirstOrDefault(p => p.Id == propertyId);
            var player = _gameLogic.GetPlayer(playerId);

            Broadcast(new GameMessage(MessageType.BuildHouse, new PropertyActionPayload
            {
                PropertyId = propertyId,
                PlayerId = playerId,
                PropertyName = property?.Name ?? ""
            }));

            string type = property?.Houses == 5 ? "отель" : $"дом #{property?.Houses}";
            Log($"{player?.Nickname} построил {type} на {property?.Name}");

            BroadcastGameState();
            OnGameStateChanged?.Invoke();
        }
    }

    private void HandleEndTurn(string? playerId)
    {
        if (playerId == null) return;
        if (_gameLogic.State.CurrentPlayerId != playerId) return;

        var player = _gameLogic.GetPlayer(playerId);
        if (player == null) return;

        // Если выпал дубль и не в тюрьме - можно бросить ещё раз
        if (_gameLogic.State.Dice1 == _gameLogic.State.Dice2 &&
            !player.IsInJail &&
            player.DoublesCount < 3)
        {
            player.HasRolledDice = false;
            BroadcastGameState();
            Log($"{player.Nickname} бросает ещё раз (дубль)");
            return;
        }

        _gameLogic.NextTurn();

        var newPlayer = _gameLogic.GetCurrentPlayer();
        Log($"Ход переходит к {newPlayer?.Nickname}");

        BroadcastGameState();
        OnGameStateChanged?.Invoke();
    }

    private void HandleChat(GameMessage message, string? playerId)
    {
        if (playerId == null) return;

        var player = _gameLogic.GetPlayer(playerId);
        if (player == null) return;

        var chatPayload = message.GetData<ChatPayload>();
        if (chatPayload == null) return;

        chatPayload.SenderName = player.Nickname;
        Broadcast(new GameMessage(MessageType.Chat, chatPayload));
        Log($"[ЧАТ] {player.Nickname}: {chatPayload.Message}");
    }

    private void BroadcastPlayerList()
    {
        Broadcast(new GameMessage(MessageType.PlayerList, _gameLogic.State.Players));
    }

    private void BroadcastGameState()
    {
        Broadcast(new GameMessage(MessageType.GameState, _gameLogic.State));
    }

    private void Broadcast(GameMessage message, string? excludePlayerId = null)
    {
        foreach (var kvp in _clients)
        {
            if (kvp.Key != excludePlayerId)
            {
                try
                {
                    kvp.Value.SendMessage(message);
                }
                catch { }
            }
        }
    }

    private void SendError(ClientHandler handler, string errorMessage)
    {
        handler.SendMessage(new GameMessage(MessageType.Error, new ErrorPayload
        {
            Message = errorMessage
        }));
    }

    private void Log(string message)
    {
        OnLog?.Invoke(message);
    }
}