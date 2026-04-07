using Monopoly.Client.Services;
using Monopoly.Common.Protocol;

namespace Monopoly.Client.Forms;

public class LoginForm : Form
{
    private TextBox txtNickname = null!;
    private TextBox txtEmail = null!;
    private TextBox txtHost = null!;
    private TextBox txtPort = null!;
    private Button btnConnect = null!;
    private Label lblStatus = null!;
    private readonly NetworkService _network;

    public LoginForm()
    {
        _network = new NetworkService();
        _network.MessageReceived += OnMessageReceived;
        _network.Disconnected += OnDisconnected;

        InitializeComponents();
    }

    private void InitializeComponents()
    {
        Text = "Монополия - Вход";
        Size = new Size(450, 420);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        BackColor = Color.FromArgb(35, 35, 50);

        // Заголовок
        var lblTitle = new Label
        {
            Text = "🎲 МОНОПОЛИЯ",
            Font = new Font("Segoe UI", 28, FontStyle.Bold),
            ForeColor = Color.Gold,
            AutoSize = true,
            Location = new Point(95, 25)
        };
        Controls.Add(lblTitle);

        int y = 90;
        int labelX = 40;
        int inputX = 40;
        int inputWidth = 350;

        // Никнейм
        Controls.Add(CreateLabel("👤 Никнейм:", labelX, y));
        y += 28;
        txtNickname = CreateTextBox(inputX, y, inputWidth);
        Controls.Add(txtNickname);

        y += 50;
        // Email
        Controls.Add(CreateLabel("📧 Email:", labelX, y));
        y += 28;
        txtEmail = CreateTextBox(inputX, y, inputWidth);
        Controls.Add(txtEmail);

        y += 50;
        // Сервер
        Controls.Add(CreateLabel("🌐 Сервер:", labelX, y));
        y += 28;
        txtHost = CreateTextBox(inputX, y, 230);
        txtHost.Text = "127.0.0.1";
        Controls.Add(txtHost);

        // Порт
        Controls.Add(CreateLabel("Порт:", 290, y - 28));
        txtPort = CreateTextBox(290, y, 100);
        txtPort.Text = "5000";
        Controls.Add(txtPort);

        y += 60;
        // Кнопка
        btnConnect = new Button
        {
            Text = "🚀 Подключиться",
            Font = new Font("Segoe UI", 14, FontStyle.Bold),
            Size = new Size(220, 50),
            Location = new Point(115, y),
            BackColor = Color.FromArgb(0, 120, 215),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btnConnect.FlatAppearance.BorderSize = 0;
        btnConnect.Click += BtnConnect_Click;
        Controls.Add(btnConnect);

        y += 65;
        // Статус
        lblStatus = new Label
        {
            Text = "",
            Font = new Font("Segoe UI", 10),
            ForeColor = Color.Orange,
            AutoSize = true,
            Location = new Point(40, y)
        };
        Controls.Add(lblStatus);
    }

    private Label CreateLabel(string text, int x, int y)
    {
        return new Label
        {
            Text = text,
            Font = new Font("Segoe UI", 11),
            ForeColor = Color.White,
            AutoSize = true,
            Location = new Point(x, y)
        };
    }

    private TextBox CreateTextBox(int x, int y, int width)
    {
        return new TextBox
        {
            Font = new Font("Segoe UI", 13),
            Location = new Point(x, y),
            Width = width,
            BackColor = Color.FromArgb(55, 55, 70),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle
        };
    }

    private async void BtnConnect_Click(object? sender, EventArgs e)
    {
        // Валидация
        if (string.IsNullOrWhiteSpace(txtNickname.Text))
        {
            ShowStatus("Введите никнейм!", Color.Red);
            return;
        }

        if (txtNickname.Text.Length < 2 || txtNickname.Text.Length > 20)
        {
            ShowStatus("Никнейм: 2-20 символов", Color.Red);
            return;
        }

        if (string.IsNullOrWhiteSpace(txtEmail.Text) || !txtEmail.Text.Contains('@'))
        {
            ShowStatus("Введите корректный email!", Color.Red);
            return;
        }

        if (!int.TryParse(txtPort.Text, out int port) || port < 1 || port > 65535)
        {
            ShowStatus("Неверный порт (1-65535)", Color.Red);
            return;
        }

        btnConnect.Enabled = false;
        ShowStatus("Подключение...", Color.Yellow);

        bool connected = await _network.ConnectAsync(txtHost.Text.Trim(), port);

        if (connected)
        {
            _network.Nickname = txtNickname.Text.Trim();

            var msg = new GameMessage(MessageType.Connect, new ConnectPayload
            {
                Nickname = txtNickname.Text.Trim(),
                Email = txtEmail.Text.Trim()
            });
            _network.SendMessage(msg);

            ShowStatus("Ожидание ответа сервера...", Color.Yellow);
        }
        else
        {
            ShowStatus("Не удалось подключиться к серверу!", Color.Red);
            btnConnect.Enabled = true;
        }
    }

    private void ShowStatus(string text, Color color)
    {
        lblStatus.Text = text;
        lblStatus.ForeColor = color;
    }

    private void OnMessageReceived(object? sender, GameMessage message)
    {
        if (InvokeRequired)
        {
            Invoke(() => OnMessageReceived(sender, message));
            return;
        }

        if (message.Type == MessageType.ConnectResponse)
        {
            var response = message.GetData<ConnectResponsePayload>();
            if (response == null) return;

            if (response.Success)
            {
                _network.PlayerId = response.PlayerId;
                _network.ColorIndex = response.ColorIndex;

                var lobbyForm = new LobbyForm(_network);
                lobbyForm.FormClosed += (s, e) => Close();
                lobbyForm.Show();
                Hide();
            }
            else
            {
                ShowStatus(response.Message, Color.Red);
                btnConnect.Enabled = true;
                _network.Disconnect();
            }
        }
    }

    private void OnDisconnected(object? sender, EventArgs e)
    {
        if (InvokeRequired)
        {
            Invoke(() => OnDisconnected(sender, e));
            return;
        }

        ShowStatus("Соединение разорвано", Color.Red);
        btnConnect.Enabled = true;
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        _network.Dispose();
        base.OnFormClosing(e);
    }
}