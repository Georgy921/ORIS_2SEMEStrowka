using System.Drawing.Drawing2D;
using Monopoly.Client.Services;
using Monopoly.Common.Models;
using Monopoly.Common.Protocol;

namespace Monopoly.Client.Forms;

public class GameForm : Form
{
    private readonly NetworkService _network;
    private GameState? _state;

    // UI элементы
    private PictureBox picBoard = null!;
    private Panel pnlPlayersOverlay = null!;
    private Panel pnlInfo = null!;
    private ListBox lstPlayers = null!;
    private ListBox lstMyProperties = null!;
    private ListBox lstLog = null!;
    private Label lblCurrentTurn = null!;
    private Label lblMyMoney = null!;
    private Label lblMyPosition = null!;
    private Label lblDice1 = null!;
    private Label lblDice2 = null!;
    private Button btnRoll = null!;
    private Button btnBuy = null!;
    private Button btnEndTurn = null!;
    private Button btnBuild = null!;

    // Анимация
    private System.Windows.Forms.Timer _diceAnimTimer = null!;
    private System.Windows.Forms.Timer _moveAnimTimer = null!;
    private int _diceAnimFrame;
    private int _moveAnimCurrentPos;
    private int _moveAnimTargetPos;
    private string _moveAnimPlayerId = "";
    private readonly Random _random = new();

    // Цвета игроков
    private static readonly Color[] PlayerColors =
    {
        Color.Red, Color.Blue, Color.LimeGreen, Color.Gold
    };

    private static readonly string[] DiceChars = { "⚀", "⚁", "⚂", "⚃", "⚄", "⚅" };

    // Размер игрового поля (картинки)
    private const int BoardSize = 640;



    /// <summary>
    /// Размер фишки игрока (диаметр)
    /// </summary>
    private const int TokenSize = 20;

    /// <summary>
    /// Смещение между фишками игроков на одной клетке
    /// </summary>
    private static readonly Point[] PlayerOffsets = new Point[]
    {
        new Point(0, 0),      // Игрок 1 (красный) - базовая позиция
        new Point(22, 0),     // Игрок 2 (синий) - справа
        new Point(0, 22),     // Игрок 3 (зелёный) - снизу
        new Point(22, 22)     // Игрок 4 (жёлтый) - по диагонали
    };

    /// <summary>
    /// Базовые координаты фишки ПЕРВОГО игрока для каждой клетки.
    /// Остальные игроки позиционируются относительно этих координат.
    private static readonly Dictionary<int, Point> TokenBasePositions = CreateTokenPositions();

    private static Dictionary<int, Point> CreateTokenPositions()
    {
        var positions = new Dictionary<int, Point>();

        // ===== УГЛОВЫЕ КЛЕТКИ =====

        // 0 - СТАРТ (нижний правый угол)
        positions[0] = new Point(570, 565);

        // 10 - ТЮРЬМА (нижний левый угол)
        positions[10] = new Point(15, 565);

        // 20 - БЕСПЛАТНАЯ ПАРКОВКА (верхний левый угол)
        positions[20] = new Point(15, 10);

        // 30 - ИДИТЕ В ТЮРЬМУ (верхний правый угол)
        positions[30] = new Point(570, 10);

        // ===== НИЖНЯЯ СТОРОНА (справа налево: 1-9) =====

        positions[1] = new Point(510, 580);
        positions[2] = new Point(458, 580);
        positions[3] = new Point(406, 580);
        positions[4] = new Point(354, 580);
        positions[5] = new Point(302, 580);
        positions[6] = new Point(250, 580);
        positions[7] = new Point(198, 580);
        positions[8] = new Point(146, 580);
        positions[9] = new Point(94, 580);

        // ===== ЛЕВАЯ СТОРОНА (снизу вверх: 11-19) =====

        positions[11] = new Point(15, 505);
        positions[12] = new Point(15, 453);
        positions[13] = new Point(15, 401);
        positions[14] = new Point(15, 349);
        positions[15] = new Point(15, 297);
        positions[16] = new Point(15, 245);
        positions[17] = new Point(15, 193);
        positions[18] = new Point(15, 141);
        positions[19] = new Point(15, 89);

        // ===== ВЕРХНЯЯ СТОРОНА (слева направо: 21-29) =====

        positions[21] = new Point(94, 10);
        positions[22] = new Point(146, 10);
        positions[23] = new Point(198, 10);
        positions[24] = new Point(250, 10);
        positions[25] = new Point(302, 10);
        positions[26] = new Point(354, 10);
        positions[27] = new Point(406, 10);
        positions[28] = new Point(458, 10);
        positions[29] = new Point(510, 10);

        // ===== ПРАВАЯ СТОРОНА (сверху вниз: 31-39) =====

        positions[31] = new Point(585, 89);
        positions[32] = new Point(585, 141);
        positions[33] = new Point(585, 193);
        positions[34] = new Point(585, 245);
        positions[35] = new Point(585, 297);
        positions[36] = new Point(585, 349);
        positions[37] = new Point(585, 401);
        positions[38] = new Point(585, 453);
        positions[39] = new Point(585, 505);

        return positions;
    }
    /// <summary>
    /// Получает позицию фишки для конкретного игрока на конкретной клетке
    /// </summary>
    /// <param name="cellIndex">Номер клетки (0-39)</param>
    /// <param name="playerIndex">Индекс игрока (0-3)</param>
    /// <returns>Координаты центра фишки</returns>
    private Point GetTokenPosition(int cellIndex, int playerIndex)
    {
        // Получаем базовую позицию для первого игрока
        if (!TokenBasePositions.TryGetValue(cellIndex, out var basePos))
        {
            // Если позиция не задана - возвращаем центр поля
            return new Point(BoardSize / 2, BoardSize / 2);
        }

        // Добавляем смещение для конкретного игрока
        var offset = PlayerOffsets[playerIndex % PlayerOffsets.Length];

        return new Point(basePos.X + offset.X, basePos.Y + offset.Y);
    }

    // =====================================================
    // КОНЕЦ НАСТРОЙКИ ПОЗИЦИЙ
    // =====================================================

    public GameForm(NetworkService network)
    {
        _state = new GameState();
        _network = network;
        _network.MessageReceived += OnMessageReceived;
        _network.Disconnected += OnDisconnected;

        InitializeComponents();
        InitializeAnimations();
    }

    private void InitializeComponents()
    {
        Text = $"Монополия - {_network.Nickname}";
        Size = new Size(1300, 750);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(25, 28, 35);
        DoubleBuffered = true;

        // Игровое поле - картинка
        picBoard = new PictureBox
        {
            Location = new Point(10, 10),
            Size = new Size(BoardSize, BoardSize),
            SizeMode = PictureBoxSizeMode.StretchImage,
            Image = LoadBoardImage()
        };
        Controls.Add(picBoard);

        // Прозрачная панель поверх картинки для отрисовки фишек
        pnlPlayersOverlay = new TransparentPanel
        {
            Location = new Point(0, 0),
            Size = new Size(BoardSize, BoardSize),
            BackColor = Color.Transparent
        };
        pnlPlayersOverlay.Paint += PnlPlayersOverlay_Paint;

        // Добавляем обработчик клика для отладки позиций
        pnlPlayersOverlay.MouseClick += PnlPlayersOverlay_MouseClick;

        picBoard.Controls.Add(pnlPlayersOverlay);

        // Информационная панель
        pnlInfo = new Panel
        {
            Location = new Point(BoardSize + 20, 10),
            Size = new Size(560, 690),
            BackColor = Color.FromArgb(38, 42, 52)
        };
        Controls.Add(pnlInfo);

        InitializeInfoPanel();
    }

    /// <summary>
    /// Обработчик клика по полю - для отладки позиций
    /// Показывает координаты клика в консоли
    /// </summary>
    private void PnlPlayersOverlay_MouseClick(object? sender, MouseEventArgs e)
    {
        // Выводим координаты в Debug Output (View -> Output в Visual Studio)
        System.Diagnostics.Debug.WriteLine($"// Клик: new Point({e.X}, {e.Y})");

        // Также можно добавить в лог игры
        AddLog($"📍 Координаты: ({e.X}, {e.Y})");
    }

    private Image LoadBoardImage()
    {
        string imagePath = Path.Combine(Application.StartupPath, "Resources", "board.png");
        if (File.Exists(imagePath))
        {
            return Image.FromFile(imagePath);
        }

        // Заглушка
        var placeholder = new Bitmap(BoardSize, BoardSize);
        using (var g = Graphics.FromImage(placeholder))
        {
            g.Clear(Color.FromArgb(195, 225, 195));
            using var font = new Font("Segoe UI", 12);
            g.DrawString("Поместите board.png\nв папку Resources\n\nКликайте по полю чтобы\nузнать координаты",
                font, Brushes.Black, 180, BoardSize / 2 - 50);

            // Рисуем сетку для ориентира
            using var pen = new Pen(Color.LightGray, 1);
            for (int i = 0; i <= BoardSize; i += 50)
            {
                g.DrawLine(pen, i, 0, i, BoardSize);
                g.DrawLine(pen, 0, i, BoardSize, i);

                // Подписи координат
                using var smallFont = new Font("Arial", 7);
                g.DrawString(i.ToString(), smallFont, Brushes.Gray, i + 2, 2);
                g.DrawString(i.ToString(), smallFont, Brushes.Gray, 2, i + 2);
            }
        }
        return placeholder;
    }

    private void InitializeInfoPanel()
    {
        int y = 10;

        lblCurrentTurn = new Label
        {
            Text = "Ход: ---",
            Font = new Font("Segoe UI", 18, FontStyle.Bold),
            ForeColor = Color.White,
            AutoSize = true,
            Location = new Point(15, y)
        };
        pnlInfo.Controls.Add(lblCurrentTurn);

        lblDice1 = new Label
        {
            Text = "🎲",
            Font = new Font("Segoe UI Emoji", 38),
            Location = new Point(370, y - 5),
            Size = new Size(80, 70),
            TextAlign = ContentAlignment.MiddleCenter
        };
        pnlInfo.Controls.Add(lblDice1);

        lblDice2 = new Label
        {
            Text = "🎲",
            Font = new Font("Segoe UI Emoji", 38),
            Location = new Point(450, y - 5),
            Size = new Size(80, 70),
            TextAlign = ContentAlignment.MiddleCenter
        };
        pnlInfo.Controls.Add(lblDice2);

        y += 60;

        lblMyMoney = new Label
        {
            Text = "💰 $1500",
            Font = new Font("Segoe UI", 22, FontStyle.Bold),
            ForeColor = Color.Gold,
            AutoSize = true,
            Location = new Point(15, y)
        };
        pnlInfo.Controls.Add(lblMyMoney);

        lblMyPosition = new Label
        {
            Text = "📍 СТАРТ",
            Font = new Font("Segoe UI", 11),
            ForeColor = Color.LightGray,
            AutoSize = true,
            Location = new Point(200, y + 5)
        };
        pnlInfo.Controls.Add(lblMyPosition);

        y += 50;

        btnRoll = CreateButton("🎲 Бросить кубики", 15, y, 160, Color.FromArgb(0, 100, 200));
        btnRoll.Click += BtnRoll_Click;
        pnlInfo.Controls.Add(btnRoll);

        btnBuy = CreateButton("🏠 Купить", 185, y, 120, Color.FromArgb(0, 140, 0));
        btnBuy.Click += BtnBuy_Click;
        btnBuy.Enabled = false;
        pnlInfo.Controls.Add(btnBuy);

        btnBuild = CreateButton("🏗️ Строить", 315, y, 110, Color.FromArgb(139, 90, 43));
        btnBuild.Click += BtnBuild_Click;
        btnBuild.Enabled = false;
        pnlInfo.Controls.Add(btnBuild);

        btnEndTurn = CreateButton("➡️ Конец хода", 435, y, 115, Color.FromArgb(160, 100, 0));
        btnEndTurn.Click += BtnEndTurn_Click;
        btnEndTurn.Enabled = false;
        pnlInfo.Controls.Add(btnEndTurn);

        y += 55;

        pnlInfo.Controls.Add(new Label
        {
            Text = "👥 Игроки:",
            Font = new Font("Segoe UI", 11, FontStyle.Bold),
            ForeColor = Color.White,
            AutoSize = true,
            Location = new Point(15, y)
        });

        y += 25;

        lstPlayers = new ListBox
        {
            Font = new Font("Segoe UI", 11),
            Location = new Point(15, y),
            Size = new Size(260, 100),
            BackColor = Color.FromArgb(50, 55, 65),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.None
        };
        pnlInfo.Controls.Add(lstPlayers);

        pnlInfo.Controls.Add(new Label
        {
            Text = "🏠 Моя недвижимость:",
            Font = new Font("Segoe UI", 11, FontStyle.Bold),
            ForeColor = Color.White,
            AutoSize = true,
            Location = new Point(290, y - 25)
        });

        lstMyProperties = new ListBox
        {
            Font = new Font("Segoe UI", 10),
            Location = new Point(290, y),
            Size = new Size(255, 100),
            BackColor = Color.FromArgb(50, 55, 65),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.None
        };
        lstMyProperties.DoubleClick += LstMyProperties_DoubleClick;
        pnlInfo.Controls.Add(lstMyProperties);

        y += 115;

        pnlInfo.Controls.Add(new Label
        {
            Text = "📋 События:",
            Font = new Font("Segoe UI", 11, FontStyle.Bold),
            ForeColor = Color.White,
            AutoSize = true,
            Location = new Point(15, y)
        });

        y += 25;

        lstLog = new ListBox
        {
            Font = new Font("Consolas", 9),
            Location = new Point(15, y),
            Size = new Size(530, 280),
            BackColor = Color.FromArgb(25, 28, 32),
            ForeColor = Color.LightGreen,
            BorderStyle = BorderStyle.None
        };
        pnlInfo.Controls.Add(lstLog);
    }

    private Button CreateButton(string text, int x, int y, int width, Color color)
    {
        var btn = new Button
        {
            Text = text,
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            Location = new Point(x, y),
            Size = new Size(width, 42),
            BackColor = color,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btn.FlatAppearance.BorderSize = 0;
        return btn;
    }

    private void InitializeAnimations()
    {
        _diceAnimTimer = new System.Windows.Forms.Timer { Interval = 70 };
        _diceAnimTimer.Tick += DiceAnimTimer_Tick;

        _moveAnimTimer = new System.Windows.Forms.Timer { Interval = 120 };
        _moveAnimTimer.Tick += MoveAnimTimer_Tick;
    }

    #region Отрисовка фишек

    private void PnlPlayersOverlay_Paint(object? sender, PaintEventArgs e)
    {
        if (_state == null) return;

        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        // Рисуем домики
        DrawHousesOnBoard(g);

        // Рисуем индикаторы владельцев
        DrawOwnershipIndicators(g);

        // Рисуем фишки игроков
        DrawPlayers(g);

        // Рисуем отладочные точки (опционально)
        // DrawDebugPositions(g);
    }

    /// <summary>
    /// Рисует все базовые позиции для отладки
    /// Раскомментируй вызов в PnlPlayersOverlay_Paint чтобы увидеть
    /// </summary>
    private void DrawDebugPositions(Graphics g)
    {
        using var font = new Font("Arial", 7);

        foreach (var kvp in TokenBasePositions)
        {
            var pos = kvp.Value;

            // Рисуем крестик
            g.DrawLine(Pens.Red, pos.X - 5, pos.Y, pos.X + 5, pos.Y);
            g.DrawLine(Pens.Red, pos.X, pos.Y - 5, pos.X, pos.Y + 5);

            // Подпись номера клетки
            g.DrawString(kvp.Key.ToString(), font, Brushes.Red, pos.X + 3, pos.Y + 3);
        }
    }

    private void DrawHousesOnBoard(Graphics g)
    {
        if (_state == null) return;

        for (int i = 0; i < 40; i++)
        {
            var prop = _state.Properties[i];
            if (prop.Houses > 0 && TokenBasePositions.TryGetValue(i, out var basePos))
            {
                DrawHouses(g, prop.Houses, basePos, i);
            }
        }
    }

    private void DrawHouses(Graphics g, int houses, Point basePos, int cellIndex)
    {
        int size = 8;
        int x, y;

        // Смещение домиков относительно базовой позиции фишки
        // В зависимости от стороны доски
        if (cellIndex >= 1 && cellIndex <= 9) // Низ
        {
            x = basePos.X - 10;
            y = basePos.Y - 30;
        }
        else if (cellIndex >= 11 && cellIndex <= 19) // Лево
        {
            x = basePos.X + 40;
            y = basePos.Y - 10;
        }
        else if (cellIndex >= 21 && cellIndex <= 29) // Верх
        {
            x = basePos.X - 10;
            y = basePos.Y + 40;
        }
        else // Право (31-39)
        {
            x = basePos.X - 30;
            y = basePos.Y - 10;
        }

        if (houses == 5) // Отель
        {
            g.FillRectangle(Brushes.DarkRed, x, y, size * 2 + 2, size + 2);
            g.DrawRectangle(Pens.White, x, y, size * 2 + 2, size + 2);
        }
        else
        {
            for (int i = 0; i < houses; i++)
            {
                int hx, hy;

                // Горизонтальное или вертикальное расположение домиков
                if (cellIndex >= 1 && cellIndex <= 9 || cellIndex >= 21 && cellIndex <= 29)
                {
                    hx = x + i * (size + 2);
                    hy = y;
                }
                else
                {
                    hx = x;
                    hy = y + i * (size + 2);
                }

                g.FillRectangle(Brushes.LimeGreen, hx, hy, size, size);
                g.DrawRectangle(Pens.DarkGreen, hx, hy, size, size);
            }
        }
    }

    private void DrawOwnershipIndicators(Graphics g)
    {
        if (_state == null) return;

        for (int i = 0; i < 40; i++)
        {
            var prop = _state.Properties[i];
            if (prop.OwnerId != null && TokenBasePositions.TryGetValue(i, out var basePos))
            {
                var owner = _state.Players.FirstOrDefault(p => p.Id == prop.OwnerId);
                if (owner != null)
                {
                    using var brush = new SolidBrush(PlayerColors[owner.ColorIndex % 4]);

                    // Индикатор в углу клетки
                    int ox = basePos.X + 35;
                    int oy = basePos.Y - 15;

                    g.FillEllipse(brush, ox, oy, 10, 10);
                    g.DrawEllipse(Pens.Black, ox, oy, 10, 10);
                }
            }
        }
    }

    /// <summary>
    /// Отрисовка фишек игроков с использованием заданных координат
    /// </summary>
    private void DrawPlayers(Graphics g)
    {
        if (_state == null) return;

        foreach (var player in _state.Players.Where(p => !p.IsBankrupt))
        {
            int position = player.Position;

            // Если идёт анимация для этого игрока
            if (_moveAnimTimer.Enabled && player.Id == _moveAnimPlayerId)
            {
                position = _moveAnimCurrentPos;
            }

            // Получаем позицию фишки для данного игрока
            var tokenPos = GetTokenPosition(position, player.ColorIndex);

            int x = tokenPos.X;
            int y = tokenPos.Y;

            // Тень
            using (var shadowBrush = new SolidBrush(Color.FromArgb(80, 0, 0, 0)))
            {
                g.FillEllipse(shadowBrush, x + 2, y + 2, TokenSize, TokenSize);
            }

            // Основная фишка
            using (var brush = new SolidBrush(PlayerColors[player.ColorIndex % 4]))
            {
                g.FillEllipse(brush, x, y, TokenSize, TokenSize);
            }

            // Белая обводка
            using (var pen = new Pen(Color.White, 2))
            {
                g.DrawEllipse(pen, x, y, TokenSize, TokenSize);
            }

            // Чёрная обводка
            g.DrawEllipse(Pens.Black, x, y, TokenSize, TokenSize);

            // Подсветка текущего игрока (золотая рамка)
            if (player.Id == _state.CurrentPlayerId)
            {
                using var glowPen = new Pen(Color.Gold, 3);
                g.DrawEllipse(glowPen, x - 4, y - 4, TokenSize + 8, TokenSize + 8);
            }

            // Индикатор тюрьмы
            if (player.IsInJail)
            {
                using var jailFont = new Font("Segoe UI Emoji", 10);
                g.DrawString("🔒", jailFont, Brushes.Black, x + 1, y - 18);
            }
        }
    }

    #endregion

    #region Event Handlers

    private void BtnRoll_Click(object? sender, EventArgs e)
    {
        if (_state?.CurrentPlayerId != _network.PlayerId) return;

        var player = _state.Players.FirstOrDefault(p => p.Id == _network.PlayerId);
        if (player == null || player.HasRolledDice) return;

        btnRoll.Enabled = false;
        _network.SendMessage(new GameMessage(MessageType.RollDice));

        _diceAnimFrame = 0;
        _diceAnimTimer.Start();
    }

    private void BtnBuy_Click(object? sender, EventArgs e)
    {
        _network.SendMessage(new GameMessage(MessageType.BuyProperty));
        btnBuy.Enabled = false;
    }

    private void BtnBuild_Click(object? sender, EventArgs e)
    {
        ShowBuildDialog();
    }

    private void BtnEndTurn_Click(object? sender, EventArgs e)
    {
        _network.SendMessage(new GameMessage(MessageType.EndTurn));
        btnEndTurn.Enabled = false;
        btnBuy.Enabled = false;
        btnBuild.Enabled = false;
    }

    private void LstMyProperties_DoubleClick(object? sender, EventArgs e)
    {
        if (lstMyProperties.SelectedIndex >= 0 && _state != null)
        {
            var myProps = _state.Properties.Where(p => p.OwnerId == _network.PlayerId).ToList();
            if (lstMyProperties.SelectedIndex < myProps.Count)
            {
                var prop = myProps[lstMyProperties.SelectedIndex];
                ShowPropertyInfo(prop);
            }
        }
    }

    private void ShowPropertyInfo(Property prop)
    {
        string info = $"📋 {prop.Name}\n\n";
        info += $"💰 Цена: ${prop.Price}\n";
        info += $"🏠 Домов: {prop.Houses}/5\n";

        if (prop.Type == PropertyType.Street)
        {
            info += $"💵 Аренда: ${prop.BaseRent}\n";
            info += $"🏗️ Цена дома: ${prop.HousePrice}\n";
        }

        MessageBox.Show(info, "Информация о недвижимости", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void ShowBuildDialog()
    {
        if (_state == null) return;

        var myStreets = _state.Properties
            .Where(p => p.OwnerId == _network.PlayerId && p.Type == PropertyType.Street)
            .ToList();

        var buildable = new List<Property>();
        foreach (var prop in myStreets)
        {
            var sameGroup = _state.Properties.Where(p => p.Group == prop.Group).ToList();
            if (sameGroup.All(p => p.OwnerId == _network.PlayerId) && prop.Houses < 5)
            {
                int minHouses = sameGroup.Min(p => p.Houses);
                if (prop.Houses <= minHouses)
                {
                    buildable.Add(prop);
                }
            }
        }

        if (buildable.Count == 0)
        {
            MessageBox.Show("Нет доступных для застройки улиц.\n\nТребуется:\n• Владеть всеми улицами группы\n• Иметь достаточно денег",
                "Строительство", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var me = _state.Players.FirstOrDefault(p => p.Id == _network.PlayerId);
        buildable = buildable.Where(p => me != null && me.Money >= p.HousePrice).ToList();

        if (buildable.Count == 0)
        {
            MessageBox.Show("Недостаточно денег для строительства!", "Строительство",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var dialog = new Form
        {
            Text = "Выберите улицу для строительства",
            Size = new Size(350, 300),
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false
        };

        var listBox = new ListBox
        {
            Location = new Point(10, 10),
            Size = new Size(310, 200)
        };

        foreach (var prop in buildable)
        {
            string type = prop.Houses == 4 ? "отель" : $"дом #{prop.Houses + 1}";
            listBox.Items.Add($"{prop.Name} - {type} (${prop.HousePrice})");
        }

        var btnConfirm = new Button
        {
            Text = "Построить",
            Location = new Point(120, 220),
            Size = new Size(100, 35),
            DialogResult = DialogResult.OK
        };

        dialog.Controls.Add(listBox);
        dialog.Controls.Add(btnConfirm);

        if (dialog.ShowDialog() == DialogResult.OK && listBox.SelectedIndex >= 0)
        {
            var selectedProp = buildable[listBox.SelectedIndex];
            _network.SendMessage(new GameMessage(MessageType.BuildHouse, new PropertyActionPayload
            {
                PropertyId = selectedProp.Id
            }));
        }
    }

    #endregion

    #region Animations

    private void DiceAnimTimer_Tick(object? sender, EventArgs e)
    {
        _diceAnimFrame++;

        lblDice1.Text = DiceChars[_random.Next(6)];
        lblDice2.Text = DiceChars[_random.Next(6)];

        if (_diceAnimFrame >= 15)
        {
            _diceAnimTimer.Stop();

            if (_state != null)
            {
                lblDice1.Text = DiceChars[Math.Clamp(_state.Dice1 - 1, 0, 5)];
                lblDice2.Text = DiceChars[Math.Clamp(_state.Dice2 - 1, 0, 5)];
            }
        }
    }

    private void MoveAnimTimer_Tick(object? sender, EventArgs e)
    {
        if (_moveAnimCurrentPos == _moveAnimTargetPos)
        {
            _moveAnimTimer.Stop();
            _moveAnimPlayerId = "";
            pnlPlayersOverlay.Invalidate();
            return;
        }

        _moveAnimCurrentPos = (_moveAnimCurrentPos + 1) % 40;
        pnlPlayersOverlay.Invalidate();
    }

    private void StartMoveAnimation(string playerId, int from, int to)
    {
        _moveAnimPlayerId = playerId;
        _moveAnimCurrentPos = from;
        _moveAnimTargetPos = to;
        _moveAnimTimer.Start();
    }

    #endregion

    #region Network Messages

    private void OnMessageReceived(object? sender, GameMessage message)
    {
        if (InvokeRequired)
        {
            Invoke(() => OnMessageReceived(sender, message));
            return;
        }

        switch (message.Type)
        {
            case MessageType.GameState:
                _state = message.GetData<GameState>();
                UpdateUI();
                CheckBuyButton();
                CheckBuildButton();
                pnlPlayersOverlay.Invalidate();
                break;

            case MessageType.DiceResult:
                HandleDiceResult(message.GetData<DiceResultPayload>());
                break;

            case MessageType.BuyProperty:
                var buyData = message.GetData<PropertyActionPayload>();
                if (buyData != null)
                    AddLog($"🏠 {GetPlayerName(buyData.PlayerId)} купил {buyData.PropertyName} за ${buyData.Amount}");
                break;

            case MessageType.BuildHouse:
                var buildData = message.GetData<PropertyActionPayload>();
                if (buildData != null)
                    AddLog($"🏗️ {GetPlayerName(buildData.PlayerId)} построил дом на {buildData.PropertyName}");
                break;

            case MessageType.PayRent:
                var rentData = message.GetData<RentPayload>();
                if (rentData != null)
                    AddLog($"💸 {rentData.PayerName} заплатил ${rentData.Amount} за {rentData.PropertyName}");
                break;

            case MessageType.PayTax:
                var taxData = message.GetData<PropertyActionPayload>();
                if (taxData != null)
                    AddLog($"💰 {GetPlayerName(taxData.PlayerId)} заплатил налог ${taxData.Amount}");
                break;

            case MessageType.PassedStart:
                var passData = message.GetData<PropertyActionPayload>();
                if (passData != null)
                    AddLog($"🏁 {GetPlayerName(passData.PlayerId)} прошёл СТАРТ (+$200)");
                break;

            case MessageType.ChanceCard:
                var chance = message.GetData<CardPayload>();
                if (chance != null)
                {
                    AddLog($"🃏 Шанс: {chance.Title}");
                    ShowCardDialog("Шанс", chance);
                }
                break;

            case MessageType.CommunityChest:
                var community = message.GetData<CardPayload>();
                if (community != null)
                {
                    AddLog($"🃏 Казна: {community.Title}");
                    ShowCardDialog("Общественная казна", community);
                }
                break;

            case MessageType.GoToJail:
                var jailData = message.GetData<PropertyActionPayload>();
                if (jailData != null)
                {
                    AddLog($"🚔 {GetPlayerName(jailData.PlayerId)} отправляется в тюрьму!");
                    AnimateJail();
                }
                break;

            case MessageType.FreeParking:
                var parkingData = message.GetData<PropertyActionPayload>();
                if (parkingData != null)
                    AddLog($"🅿️ {GetPlayerName(parkingData.PlayerId)} забрал ${parkingData.Amount} с парковки!");
                break;

            case MessageType.Bankruptcy:
                var bankruptData = message.GetData<PropertyActionPayload>();
                if (bankruptData != null)
                {
                    AddLog($"💥 {GetPlayerName(bankruptData.PlayerId)} БАНКРОТ!");
                    AnimateBankruptcy();
                }
                break;

            case MessageType.Victory:
                HandleVictory(message.GetData<VictoryPayload>());
                break;

            case MessageType.ServerMessage:
                var serverMsg = message.GetData<ServerMessagePayload>();
                if (serverMsg != null)
                    AddLog($"📢 {serverMsg.Message}");
                break;

            case MessageType.Chat:
                var chatData = message.GetData<ChatPayload>();
                if (chatData != null)
                    AddLog($"💬 {chatData.SenderName}: {chatData.Message}");
                break;
        }
    }

    private void HandleDiceResult(DiceResultPayload? result)
    {
        if (result == null) return;

        string doubleText = result.IsDouble ? " (ДУБЛЬ!)" : "";
        AddLog($"🎲 {GetPlayerName(result.PlayerId)}: {result.Dice1} + {result.Dice2} = {result.Dice1 + result.Dice2}{doubleText}");

        _diceAnimTimer.Stop();
        lblDice1.Text = DiceChars[result.Dice1 - 1];
        lblDice2.Text = DiceChars[result.Dice2 - 1];

        if (result.OldPosition != result.NewPosition)
        {
            StartMoveAnimation(result.PlayerId, result.OldPosition, result.NewPosition);
        }

        if (result.PlayerId == _network.PlayerId)
        {
            btnEndTurn.Enabled = true;
            CheckBuyButton();
            CheckBuildButton();
        }
    }

    private void CheckBuyButton()
    {
        if (_state == null)
        {
            btnBuy.Enabled = false;
            return;
        }

        // Проверяем что это наш ход
        if (_state.CurrentPlayerId != _network.PlayerId)
        {
            btnBuy.Enabled = false;
            return;
        }

        var player = _state.Players.FirstOrDefault(p => p.Id == _network.PlayerId);
        if (player == null)
        {
            btnBuy.Enabled = false;
            return;
        }

        // Проверяем что игрок бросил кубики
        if (!player.HasRolledDice)
        {
            btnBuy.Enabled = false;
            return;
        }

        // Проверяем что игрок не в тюрьме и не банкрот
        if (player.IsInJail || player.IsBankrupt)
        {
            btnBuy.Enabled = false;
            return;
        }

        var property = _state.Properties[player.Position];

        btnBuy.Enabled = property.OwnerId == null &&
            (property.Type == PropertyType.Street ||
             property.Type == PropertyType.Railroad ||
             property.Type == PropertyType.Utility) &&
            player.Money >= property.Price;
    }

    private void CheckBuildButton()
    {
        if (_state == null)
        {
            btnBuild.Enabled = false;
            return;
        }

        if (_state.CurrentPlayerId != _network.PlayerId)
        {
            btnBuild.Enabled = false;
            return;
        }

        var myStreets = _state.Properties
            .Where(p => p.OwnerId == _network.PlayerId && p.Type == PropertyType.Street)
            .ToList();

        foreach (var prop in myStreets)
        {
            var sameGroup = _state.Properties.Where(p => p.Group == prop.Group).ToList();
            if (sameGroup.All(p => p.OwnerId == _network.PlayerId) && prop.Houses < 5)
            {
                btnBuild.Enabled = true;
                return;
            }
        }

        btnBuild.Enabled = false;
    }

    private void ShowCardDialog(string title, CardPayload card)
    {
        string msg = $"📜 {card.Description}";

        if (card.MoneyChange > 0)
            msg += $"\n\n✅ +${card.MoneyChange}";
        else if (card.MoneyChange < 0)
            msg += $"\n\n❌ -${Math.Abs(card.MoneyChange)}";

        MessageBox.Show(msg, title, MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private async void AnimateJail()
    {
        var original = picBoard.Location;

        for (int i = 0; i < 10; i++)
        {
            picBoard.Location = new Point(
                original.X + _random.Next(-10, 11),
                original.Y + _random.Next(-10, 11));
            await Task.Delay(40);
        }

        picBoard.Location = original;
    }

    private async void AnimateBankruptcy()
    {
        for (int i = 0; i < 3; i++)
        {
            picBoard.BackColor = Color.Red;
            await Task.Delay(150);
            picBoard.BackColor = Color.Transparent;
            await Task.Delay(150);
        }
    }

    private void HandleVictory(VictoryPayload? victory)
    {
        if (victory == null) return;

        btnRoll.Enabled = false;
        btnBuy.Enabled = false;
        btnEndTurn.Enabled = false;
        btnBuild.Enabled = false;

        string msg;
        if (victory.WinnerId == _network.PlayerId)
        {
            msg = $" ПОЗДРАВЛЯЕМ! ВЫ ПОБЕДИЛИ! \n\n" +
                  $" Итоговый баланс: ${victory.FinalMoney}\n" +
                  $" Недвижимость: {victory.PropertiesCount} объектов\n" +
                  $" Всего активов: ${victory.TotalAssets}";
        }
        else
        {
            msg = $" Игра окончена!\n\n" +
                  $"Победитель: {victory.WinnerNickname}\n\n" +
                  $" Баланс: ${victory.FinalMoney}\n" +
                  $" Недвижимость: {victory.PropertiesCount} объектов\n" +
                  $" Всего активов: ${victory.TotalAssets}";
        }

        AddLog($"🏆 ПОБЕДИТЕЛЬ: {victory.WinnerNickname}!");
        MessageBox.Show(msg, "Игра окончена!", MessageBoxButtons.OK, MessageBoxIcon.Information);
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

    #endregion

    #region UI Update

    private void UpdateUI()
    {
        if (_state == null) return;

        var currentPlayer = _state.Players.FirstOrDefault(p => p.Id == _state.CurrentPlayerId);
        if (currentPlayer != null)
        {
            lblCurrentTurn.Text = $"Ход: {currentPlayer.Nickname}";
            lblCurrentTurn.ForeColor = PlayerColors[currentPlayer.ColorIndex % 4];
        }

        var me = _state.Players.FirstOrDefault(p => p.Id == _network.PlayerId);
        if (me != null)
        {
            lblMyMoney.Text = $" ${me.Money}";
            lblMyMoney.ForeColor = me.Money < 0 ? Color.Red : Color.Gold;

            var myProp = _state.Properties[me.Position];
            string jailStatus = me.IsInJail ? " [В ТЮРЬМЕ]" : "";
            lblMyPosition.Text = $" {myProp.Name}{jailStatus}";
        }

        lstPlayers.Items.Clear();
        foreach (var player in _state.Players)
        {
            string icon = player.IsBankrupt ? "" : GetPlayerIcon(player.ColorIndex);
            string status = player.IsBankrupt ? "Банкрот" : $"${player.Money}";
            string jail = player.IsInJail ? " " : "";
            string you = player.Id == _network.PlayerId ? " (Вы)" : "";
            lstPlayers.Items.Add($"{icon} {player.Nickname}{you}: {status}{jail}");
        }

        lstMyProperties.Items.Clear();
        foreach (var prop in _state.Properties.Where(p => p.OwnerId == _network.PlayerId))
        {
            string houses = prop.Houses > 0 ? $" [{prop.Houses}]" : "";
            string mortgaged = prop.IsMortgaged ? " [ЗАЛОЖЕНО]" : "";
            lstMyProperties.Items.Add($"{prop.Name}{houses}{mortgaged}");
        }

        bool isMyTurn = _state.CurrentPlayerId == _network.PlayerId;
        bool hasRolled = me?.HasRolledDice ?? true;
        btnRoll.Enabled = isMyTurn && !hasRolled && !_moveAnimTimer.Enabled && !(me?.IsBankrupt ?? true);

        if (!isMyTurn)
        {
            btnBuy.Enabled = false;
            btnEndTurn.Enabled = false;
            btnBuild.Enabled = false;
        }
    }

    private void AddLog(string message)
    {
        lstLog.Items.Insert(0, $"[{DateTime.Now:HH:mm:ss}] {message}");
        while (lstLog.Items.Count > 200)
            lstLog.Items.RemoveAt(lstLog.Items.Count - 1);
    }

    private string GetPlayerName(string playerId)
    {
        return _state?.Players.FirstOrDefault(p => p.Id == playerId)?.Nickname ?? "Игрок";
    }

    private string GetPlayerIcon(int colorIndex)
    {
        return colorIndex switch
        {
            0 => "🔴",
            1 => "🔵",
            2 => "🟢",
            3 => "🟡",
            _ => "⚪"
        };
    }

    #endregion

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        _diceAnimTimer?.Stop();
        _diceAnimTimer?.Dispose();
        _moveAnimTimer?.Stop();
        _moveAnimTimer?.Dispose();
        _network.Disconnect();
        base.OnFormClosing(e);
    }
}

/// <summary>
/// Прозрачная панель для рисования поверх PictureBox
/// </summary>
public class TransparentPanel : Panel
{
    public TransparentPanel()
    {
        SetStyle(ControlStyles.SupportsTransparentBackColor |
                 ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.UserPaint |
                 ControlStyles.OptimizedDoubleBuffer, true);
        BackColor = Color.Transparent;
    }

    protected override CreateParams CreateParams
    {
        get
        {
            CreateParams cp = base.CreateParams;
            cp.ExStyle |= 0x20; // WS_EX_TRANSPARENT
            return cp;
        }
    }
}