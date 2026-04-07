using Monopoly.Client.Services;
using Monopoly.Common.Models;
using Monopoly.Common.Protocol;

namespace Monopoly.Client.Forms;

public class LobbyForm : Form
{
    private readonly NetworkService _network;
    private ListBox lstPlayers = null!;
    private Button btnReady = null!;
    private Label lblStatus = null!;
    private Label lblInfo = null!;
    private bool _isReady;

    private static readonly string[] PlayerIcons = { "🔴", "🔵", "🟢", "🟡" };
    private static readonly Color[] PlayerColors = { Color.Red, Color.Blue, Color.LimeGreen, Color.Gold };

    public LobbyForm(NetworkService network)
    {
        _network = network;
        _network.MessageReceived += OnMessageReceived;
        _network.Disconnected += OnDisconnected;

        InitializeComponents();
    }

    private void InitializeComponents()
    {
        Text = $"Монополия - Лобби ({_network.Nickname})";
        Size = new Size(550, 500);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        BackColor = Color.FromArgb(30, 35, 45);

        // Заголовок
        var lblTitle = new Label
        {
            Text = "🎯 ИГРОВОЕ ЛОББИ",
            Font = new Font("Segoe UI", 22, FontStyle.Bold),
            ForeColor = Color.White,
            AutoSize = true,
            Location = new Point(140, 20)
        };
        Controls.Add(lblTitle);

        // Информация
        lblInfo = new Label
        {
            Text = "Ожидание игроков... (минимум 2, максимум 4)",
            Font = new Font("Segoe UI", 11),
            ForeColor = Color.Gray,
            AutoSize = true,
            Location = new Point(115, 60)
        };
        Controls.Add(lblInfo);

        // Заголовок списка
        Controls.Add(new Label
        {
            Text = "👥 Подключённые игроки:",
            Font = new Font("Segoe UI", 13, FontStyle.Bold),
            ForeColor = Color.White,
            AutoSize = true,
            Location = new Point(30, 100)
        });

        // Список игроков
        lstPlayers = new ListBox
        {
            Font = new Font("Segoe UI", 14),
            Location = new Point(30, 135),
            Size = new Size(480, 180),
            BackColor = Color.FromArgb(45, 50, 60),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.None,
            DrawMode = DrawMode.OwnerDrawFixed,
            ItemHeight = 40
        };
        lstPlayers.DrawItem += LstPlayers_DrawItem;
        Controls.Add(lstPlayers);

        // Кнопка готовности
        btnReady = new Button
        {
            Text = "✓ Я ГОТОВ",
            Font = new Font("Segoe UI", 16, FontStyle.Bold),
            Size = new Size(250, 60),
            Location = new Point(145, 340),
            BackColor = Color.FromArgb(0, 160, 0),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btnReady.FlatAppearance.BorderSize = 0;
        btnReady.Click += BtnReady_Click;
        Controls.Add(btnReady);

        // Статус
        lblStatus = new Label
        {
            Text = "Нажмите 'Готов' когда будете готовы начать",
            Font = new Font("Segoe UI", 11),
            ForeColor = Color.Yellow,
            AutoSize = true,
            Location = new Point(110, 420)
        };
        Controls.Add(lblStatus);
    }

    private void LstPlayers_DrawItem(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0 || e.Index >= lstPlayers.Items.Count) return;

        e.DrawBackground();

        var item = lstPlayers.Items[e.Index] as PlayerListItem;
        if (item == null) return;

        // Иконка
        using var iconFont = new Font("Segoe UI Emoji", 16);
        e.Graphics.DrawString(item.Icon, iconFont, Brushes.White, e.Bounds.X + 10, e.Bounds.Y + 5);

        // Имя
        using var nameFont = new Font("Segoe UI", 12, FontStyle.Bold);
        using var nameBrush = new SolidBrush(item.Color);
        e.Graphics.DrawString(item.Name, nameFont, nameBrush, e.Bounds.X + 50, e.Bounds.Y + 8);

        // Статус
        string status = item.IsReady ? "✓ Готов" : "⏳ Ожидание";
        Color statusColor = item.IsReady ? Color.LightGreen : Color.Orange;
        using var statusBrush = new SolidBrush(statusColor);
        using var statusFont = new Font("Segoe UI", 10);
        e.Graphics.DrawString(status, statusFont, statusBrush, e.Bounds.Right - 100, e.Bounds.Y + 10);
    }

    private void BtnReady_Click(object? sender, EventArgs e)
    {
        _isReady = !_isReady;

        if (_isReady)
        {
            btnReady.Text = "✗ НЕ ГОТОВ";
            btnReady.BackColor = Color.FromArgb(180, 0, 0);
        }
        else
        {
            btnReady.Text = "✓ Я ГОТОВ";
            btnReady.BackColor = Color.FromArgb(0, 160, 0);
        }

        _network.SendMessage(new GameMessage(MessageType.PlayerReady));
    }

    private void OnMessageReceived(object? sender, GameMessage message)
    {
        if (InvokeRequired)
        {
            Invoke(() => OnMessageReceived(sender, message));
            return;
        }

        switch (message.Type)
        {
            case MessageType.PlayerList:
                var players = message.GetData<List<Player>>();
                UpdatePlayerList(players);
                break;

            case MessageType.GameStart:
                StartGame();
                break;

            case MessageType.ServerMessage:
                var serverMsg = message.GetData<ServerMessagePayload>();
                if (serverMsg != null)
                {
                    lblStatus.Text = serverMsg.Message;
                    lblStatus.ForeColor = serverMsg.Type == "warning" ? Color.Orange : Color.LightGreen;
                }
                break;
        }
    }

    private void UpdatePlayerList(List<Player>? players)
    {
        if (players == null) return;

        lstPlayers.Items.Clear();

        foreach (var player in players)
        {
            string icon = PlayerIcons[player.ColorIndex % PlayerIcons.Length];
            Color color = PlayerColors[player.ColorIndex % PlayerColors.Length];
            string name = player.Nickname;
            if (player.Id == _network.PlayerId) name += " (Вы)";

            lstPlayers.Items.Add(new PlayerListItem
            {
                Icon = icon,
                Name = name,
                Color = color,
                IsReady = player.IsReady
            });
        }

        int readyCount = players.Count(p => p.IsReady);
        lblInfo.Text = $"Игроков: {players.Count}/4  |  Готово: {readyCount}/{players.Count}";

        if (players.Count >= 2 && readyCount == players.Count)
        {
            lblStatus.Text = "🚀 Все готовы! Игра скоро начнётся...";
            lblStatus.ForeColor = Color.LightGreen;
        }
    }

    private void StartGame()
    {
        var gameForm = new GameForm(_network);
        gameForm.FormClosed += (s, e) => Close();
        gameForm.Show();
        Hide();
    }

    private void OnDisconnected(object? sender, EventArgs e)
    {
        if (InvokeRequired)
        {
            Invoke(() => OnDisconnected(sender, e));
            return;
        }

        MessageBox.Show("Соединение с сервером потеряно!", "Ошибка",
            MessageBoxButtons.OK, MessageBoxIcon.Error);
        Close();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        _network.Disconnect();
        base.OnFormClosing(e);
    }

    private class PlayerListItem
    {
        public string Icon { get; set; } = "";
        public string Name { get; set; } = "";
        public Color Color { get; set; }
        public bool IsReady { get; set; }
    }
}