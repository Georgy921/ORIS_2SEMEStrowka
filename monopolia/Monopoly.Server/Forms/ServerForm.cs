using Monopoly.Common.Models;
using Monopoly.Common.Protocol;
using Monopoly.Server.Game;
using Monopoly.Server.Network;

namespace Monopoly.Server.Forms;

public class ServerForm : Form
{
    private GameServer? _server;
    private readonly GameLogic _gameLogic;

    // UI Controls
    private TextBox txtPort = null!;
    private Button btnStart = null!;
    private Button btnStop = null!;
    private ListBox lstPlayers = null!;
    private ListBox lstLog = null!;
    private Label lblStatus = null!;
    private Label lblPlayersCount = null!;
    private Button btnKickPlayer = null!;
    private Button btnStartGame = null!;
    private Panel pnlGameInfo = null!;
    private Label lblCurrentPlayer = null!;
    private System.ComponentModel.BackgroundWorker backgroundWorker1;
    private System.ComponentModel.BackgroundWorker backgroundWorker2;
    private Label lblGameStatus = null!;

    public ServerForm()
    {
        _gameLogic = new GameLogic();
        InitializeComponents();
    }

    private void InitializeComponents()
    {
        Text = "Монополия - Сервер";
        Size = new Size(800, 700);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(30, 30, 40);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;

        // Заголовок
        var lblTitle = new Label
        {
            Text = "MONOPOLY SERVER",
            Font = new Font("Segoe UI", 22, FontStyle.Bold),
            ForeColor = Color.Gold,
            AutoSize = true,
            Location = new Point(250, 15)
        };
        Controls.Add(lblTitle);

        // Панель настроек
        var pnlSettings = new Panel
        {
            Location = new Point(20, 60),
            Size = new Size(350, 120),
            BackColor = Color.FromArgb(45, 45, 55),
            BorderStyle = BorderStyle.FixedSingle
        };
        Controls.Add(pnlSettings);

        pnlSettings.Controls.Add(new Label
        {
            Text = "⚙️ Настройки сервера",
            Font = new Font("Segoe UI", 12, FontStyle.Bold),
            ForeColor = Color.White,
            Location = new Point(10, 10),
            AutoSize = true
        });

        pnlSettings.Controls.Add(new Label
        {
            Text = "Порт:",
            Font = new Font("Segoe UI", 11),
            ForeColor = Color.LightGray,
            Location = new Point(10, 45),
            AutoSize = true
        });

        txtPort = new TextBox
        {
            Text = "5000",
            Font = new Font("Segoe UI", 12),
            Location = new Point(60, 42),
            Width = 80,
            BackColor = Color.FromArgb(60, 60, 70),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle
        };
        pnlSettings.Controls.Add(txtPort);

        btnStart = new Button
        {
            Text = "▶ Запустить",
            Font = new Font("Segoe UI", 11, FontStyle.Bold),
            Location = new Point(160, 40),
            Size = new Size(120, 35),
            BackColor = Color.FromArgb(0, 150, 0),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btnStart.Click += BtnStart_Click;
        pnlSettings.Controls.Add(btnStart);

        btnStop = new Button
        {
            Text = "⬛ Остановить",
            Font = new Font("Segoe UI", 11, FontStyle.Bold),
            Location = new Point(160, 40),
            Size = new Size(120, 35),
            BackColor = Color.FromArgb(180, 0, 0),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            Visible = false
        };
        btnStop.Click += BtnStop_Click;
        pnlSettings.Controls.Add(btnStop);

        lblStatus = new Label
        {
            Text = "● Остановлен",
            Font = new Font("Segoe UI", 11),
            ForeColor = Color.Red,
            Location = new Point(10, 85),
            AutoSize = true
        };
        pnlSettings.Controls.Add(lblStatus);

        // Панель игроков
        var pnlPlayers = new Panel
        {
            Location = new Point(390, 60),
            Size = new Size(380, 200),
            BackColor = Color.FromArgb(45, 45, 55),
            BorderStyle = BorderStyle.FixedSingle
        };
        Controls.Add(pnlPlayers);

        lblPlayersCount = new Label
        {
            Text = "👥 Игроки (0/4)",
            Font = new Font("Segoe UI", 12, FontStyle.Bold),
            ForeColor = Color.White,
            Location = new Point(10, 10),
            AutoSize = true
        };
        pnlPlayers.Controls.Add(lblPlayersCount);

        lstPlayers = new ListBox
        {
            Font = new Font("Segoe UI", 11),
            Location = new Point(10, 40),
            Size = new Size(250, 110),
            BackColor = Color.FromArgb(35, 35, 45),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.None
        };
        pnlPlayers.Controls.Add(lstPlayers);

        btnKickPlayer = new Button
        {
            Text = "❌ Кикнуть",
            Font = new Font("Segoe UI", 10),
            Location = new Point(270, 40),
            Size = new Size(100, 35),
            BackColor = Color.FromArgb(150, 50, 50),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Enabled = false
        };
        btnKickPlayer.Click += BtnKickPlayer_Click;
        pnlPlayers.Controls.Add(btnKickPlayer);

        btnStartGame = new Button
        {
            Text = "🎮 Начать игру",
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            Location = new Point(270, 85),
            Size = new Size(100, 40),
            BackColor = Color.FromArgb(0, 100, 180),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Enabled = false
        };
        btnStartGame.Click += BtnStartGame_Click;
        pnlPlayers.Controls.Add(btnStartGame);

        lstPlayers.SelectedIndexChanged += (s, e) =>
        {
            btnKickPlayer.Enabled = lstPlayers.SelectedIndex >= 0 && !_gameLogic.State.IsGameStarted;
        };

        // Панель информации об игре
        pnlGameInfo = new Panel
        {
            Location = new Point(20, 190),
            Size = new Size(350, 70),
            BackColor = Color.FromArgb(45, 45, 55),
            BorderStyle = BorderStyle.FixedSingle,
            Visible = false
        };
        Controls.Add(pnlGameInfo);

        lblGameStatus = new Label
        {
            Text = "🎲 Игра идёт",
            Font = new Font("Segoe UI", 11, FontStyle.Bold),
            ForeColor = Color.LightGreen,
            Location = new Point(10, 10),
            AutoSize = true
        };
        pnlGameInfo.Controls.Add(lblGameStatus);

        lblCurrentPlayer = new Label
        {
            Text = "Ход: ---",
            Font = new Font("Segoe UI", 11),
            ForeColor = Color.White,
            Location = new Point(10, 38),
            AutoSize = true
        };
        pnlGameInfo.Controls.Add(lblCurrentPlayer);

        // Лог сервера
        Controls.Add(new Label
        {
            Text = "📋 Лог сервера:",
            Font = new Font("Segoe UI", 12, FontStyle.Bold),
            ForeColor = Color.White,
            Location = new Point(20, 275),
            AutoSize = true
        });

        lstLog = new ListBox
        {
            Font = new Font("Consolas", 10),
            Location = new Point(20, 305),
            Size = new Size(750, 340),
            BackColor = Color.FromArgb(20, 20, 25),
            ForeColor = Color.LightGreen,
            BorderStyle = BorderStyle.FixedSingle
        };
        Controls.Add(lstLog);
    }

    private async void BtnStart_Click(object? sender, EventArgs e)
    {
        if (!int.TryParse(txtPort.Text, out int port) || port < 1 || port > 65535)
        {
            MessageBox.Show("Введите корректный порт (1-65535)", "Ошибка",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        try
        {
            _server = new GameServer(port, _gameLogic);
            _server.OnLog += Server_OnLog;
            _server.OnPlayerListChanged += Server_OnPlayerListChanged;
            _server.OnGameStateChanged += Server_OnGameStateChanged;

            await Task.Run(() => _server.Start());

            btnStart.Visible = false;
            btnStop.Visible = true;
            txtPort.Enabled = false;
            lblStatus.Text = $"● Запущен (порт {port})";
            lblStatus.ForeColor = Color.LightGreen;

            Log($"Сервер запущен на порту {port}");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка запуска: {ex.Message}", "Ошибка",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void BtnStop_Click(object? sender, EventArgs e)
    {
        _server?.Stop();
        _server = null;

        btnStart.Visible = true;
        btnStop.Visible = false;
        txtPort.Enabled = true;
        lblStatus.Text = "● Остановлен";
        lblStatus.ForeColor = Color.Red;
        pnlGameInfo.Visible = false;

        _gameLogic.Reset();
        UpdatePlayersList();

        Log("Сервер остановлен");
    }

    private void BtnKickPlayer_Click(object? sender, EventArgs e)
    {
        if (lstPlayers.SelectedIndex < 0) return;

        var players = _gameLogic.State.Players;
        if (lstPlayers.SelectedIndex < players.Count)
        {
            var player = players[lstPlayers.SelectedIndex];
            _server?.KickPlayer(player.Id);
            Log($"Игрок {player.Nickname} кикнут");
        }
    }

    private void BtnStartGame_Click(object? sender, EventArgs e)
    {
        if (_gameLogic.State.Players.Count < 2)
        {
            MessageBox.Show("Нужно минимум 2 игрока!", "Ошибка",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _server?.ForceStartGame();
        Log("Игра принудительно запущена администратором");
    }

    private void Server_OnLog(string message)
    {
        if (InvokeRequired)
        {
            Invoke(() => Server_OnLog(message));
            return;
        }
        Log(message);
    }

    private void Server_OnPlayerListChanged()
    {
        if (InvokeRequired)
        {
            Invoke(() => Server_OnPlayerListChanged());
            return;
        }
        UpdatePlayersList();
    }

    private void Server_OnGameStateChanged()
    {
        if (InvokeRequired)
        {
            Invoke(() => Server_OnGameStateChanged());
            return;
        }
        UpdateGameInfo();
    }

    private void UpdatePlayersList()
    {
        lstPlayers.Items.Clear();

        var players = _gameLogic.State.Players;
        lblPlayersCount.Text = $"👥 Игроки ({players.Count}/4)";

        foreach (var player in players)
        {
            string status = player.IsReady ? "✓" : "○";
            string bankrupt = player.IsBankrupt ? " [БАНКРОТ]" : "";
            lstPlayers.Items.Add($"{status} {player.Nickname} ({player.Email}) - ${player.Money}{bankrupt}");
        }

        btnStartGame.Enabled = players.Count >= 2 && !_gameLogic.State.IsGameStarted;
        btnKickPlayer.Enabled = lstPlayers.SelectedIndex >= 0 && !_gameLogic.State.IsGameStarted;
    }

    private void UpdateGameInfo()
    {
        if (_gameLogic.State.IsGameStarted)
        {
            pnlGameInfo.Visible = true;

            var currentPlayer = _gameLogic.GetCurrentPlayer();
            lblCurrentPlayer.Text = $"Ход: {currentPlayer?.Nickname ?? "---"}";

            if (_gameLogic.State.IsGameOver)
            {
                var winner = _gameLogic.State.Players.FirstOrDefault(p => p.Id == _gameLogic.State.WinnerId);
                lblGameStatus.Text = $"🏆 Победитель: {winner?.Nickname ?? "---"}";
                lblGameStatus.ForeColor = Color.Gold;
            }
            else
            {
                var activePlayers = _gameLogic.State.Players.Count(p => !p.IsBankrupt);
                lblGameStatus.Text = $"🎲 Игра идёт ({activePlayers} активных игроков)";
                lblGameStatus.ForeColor = Color.LightGreen;
            }
        }
        else
        {
            pnlGameInfo.Visible = false;
        }

        UpdatePlayersList();
    }

    private void Log(string message)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        lstLog.Items.Insert(0, $"[{timestamp}] {message}");

        while (lstLog.Items.Count > 500)
            lstLog.Items.RemoveAt(lstLog.Items.Count - 1);
    }

    private void InitializeComponent()
    {
        backgroundWorker1 = new System.ComponentModel.BackgroundWorker();
        backgroundWorker2 = new System.ComponentModel.BackgroundWorker();
        SuspendLayout();
        // 
        // ServerForm
        // 
        ClientSize = new Size(811, 517);
        Name = "ServerForm";
        ResumeLayout(false);

    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        _server?.Stop();
        base.OnFormClosing(e);
    }
}