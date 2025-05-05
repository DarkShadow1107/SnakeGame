using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Media;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SnakeGame
{
    public partial class Form1 : Form
    {
        // --- Game constants ---
        private const int CellSize = 28; // was 32
        private const int GridWidth = 36; // was 40
        private const int GridHeight = 24; // was 28
        private const int InitialSnakeLength = 6;
        private const int GameSpeed = 60; // ms per tick

        // --- Game state ---
        private List<Point> snake;
        private Point food;
        private Point? specialFood = null;
        private int direction = 0; // 0=Right, 1=Down, 2=Left, 3=Up
        private int nextDirection = 0;
        private int score = 0;
        private bool gameOver = false;
        private Timer timer;
        private Random rand = new Random();
        private int specialFoodTimer = 0;

        private int currentLevel = 1;
        private int maxLevel = 10;
        private Theme currentTheme;
        private Theme[] allThemes = Theme.GetThemes();
        private Random themeRand = new Random();
        private int foodsToNextLevel;
        private int foodsEatenThisLevel;

        private Level levelData;
        private List<Obstacle> obstacles = new List<Obstacle>();
        private List<PowerUp> powerUps = new List<PowerUp>();
        private List<Particle> particles = new List<Particle>();
        private int powerUpTimer = 0;
        private PowerUpType? activePowerUp = null;
        private int powerUpDuration = 0;
        private bool paused = false;
        private List<Point> wallCells = new List<Point>(); // Store wall cells for current level

        // --- Graphics ---
        private BufferedGraphicsContext context;
        private BufferedGraphics buffer;

        public Form1()
        {
            InitializeComponent();
            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            this.ClientSize = new Size(GridWidth * CellSize, GridHeight * CellSize + 40);
            this.Text = "Snake Game - Complex Graphics Edition";
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.KeyDown += Form1_KeyDown;
            this.BackColor = Color.Black;

            // Set the app icon at runtime (ensure snake.ico is in output directory)
            try
            {
                var iconPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "snake.ico");
                if (File.Exists(iconPath))
                    this.Icon = new Icon(iconPath);
            }
            catch { /* ignore icon errors */ }

            context = BufferedGraphicsManager.Current;
            buffer = context.Allocate(this.CreateGraphics(), this.DisplayRectangle);

            using (var menu = new MenuForm())
            {
                if (menu.ShowDialog() == DialogResult.OK)
                {
                    currentLevel = menu.SelectedLevel;
                    currentTheme = menu.SelectedTheme;
                }
            }

            HighScoreManager.Load();
            StartGame();
        }

        private int GetGameSpeedForLevel(int level)
        {
            int minSpeed = 40;
            int maxSpeed = 180;
            int clampedLevel = Math.Max(1, Math.Min(level, maxLevel));
            return maxSpeed - ((maxSpeed - minSpeed) * (clampedLevel - 1) / (maxLevel - 1));
        }

        private void StartGame()
        {
            snake = new List<Point>();
            for (int i = InitialSnakeLength - 1; i >= 0; i--)
                snake.Add(new Point(i + 5, GridHeight / 2));
            direction = 0;
            nextDirection = 0;
            score = 0;
            gameOver = false;
            specialFood = null;
            specialFoodTimer = 0;
            powerUps.Clear();
            particles.Clear();
            activePowerUp = null;
            powerUpDuration = 0;
            paused = false;

            foodsToNextLevel = 8 + currentLevel * 2;
            foodsEatenThisLevel = 0;

            levelData = Level.GetLevel(currentLevel);
            obstacles = levelData.Obstacles.Select(r => new Obstacle(r)).ToList();
            wallCells = (currentTheme.HasWalls && levelData.Walls != null) ? new List<Point>(levelData.Walls) : new List<Point>();

            SpawnFood();
            SpawnPowerUp();

            if (timer != null)
                timer.Stop();
            timer = new Timer();
            timer.Interval = GetGameSpeedForLevel(currentLevel);
            timer.Tick += GameTick;
            timer.Start();
            SoundManager.PlayBackgroundMusic();
        }

        private void NextLevel()
        {
            if (currentLevel < maxLevel)
            {
                currentLevel++;
                Theme newTheme;
                do
                {
                    newTheme = allThemes[themeRand.Next(allThemes.Length)];
                } while (newTheme == currentTheme && allThemes.Length > 1);
                currentTheme = newTheme;
                StartGame();
            }
            else
            {
                MessageBox.Show("Congratulations! You finished all levels!", "Snake", MessageBoxButtons.OK, MessageBoxIcon.Information);
                timer?.Stop();
                using (var menu = new MenuForm())
                {
                    if (menu.ShowDialog() == DialogResult.OK)
                    {
                        currentLevel = menu.SelectedLevel;
                        currentTheme = menu.SelectedTheme;
                        StartGame();
                    }
                }
            }
        }

        private void GameTick(object sender, EventArgs e)
        {
            if (gameOver || paused) return;

            foreach (var obs in obstacles)
                obs.Move(GridWidth, GridHeight);

            direction = nextDirection;
            Point head = snake[0];
            Point newHead = head;
            switch (direction)
            {
                case 0: newHead.X += 1; break;
                case 1: newHead.Y += 1; break;
                case 2: newHead.X -= 1; break;
                case 3: newHead.Y -= 1; break;
            }

            if (!currentTheme.HasWalls)
            {
                if (newHead.X < 0) newHead.X = GridWidth - 1;
                if (newHead.X >= GridWidth) newHead.X = 0;
                if (newHead.Y < 0) newHead.Y = GridHeight - 1;
                if (newHead.Y >= GridHeight) newHead.Y = 0;
            }
            else
            {
                if (wallCells.Contains(newHead))
                {
                    gameOver = true;
                    timer.Stop();
                    HighScoreManager.AddScore(score);
                    SoundManager.PlayGameOver();
                    Invalidate();
                    return;
                }
            }

            if (snake.Contains(newHead) || obstacles.Any(o => o.Area.Contains(newHead)))
            {
                if (activePowerUp != PowerUpType.Invincibility)
                {
                    gameOver = true;
                    timer.Stop();
                    HighScoreManager.AddScore(score);
                    SoundManager.PlayGameOver();
                    Invalidate();
                    return;
                }
            }

            snake.Insert(0, newHead);

            if (newHead == food)
            {
                score += 10 * (activePowerUp == PowerUpType.ScoreMultiplier ? 2 : 1);
                foodsEatenThisLevel++;
                SpawnFood();
                SoundManager.PlayEat();
                EmitParticles(newHead, currentTheme.FoodColor);
                if (score % 50 == 0 && specialFood == null)
                {
                    specialFood = RandomEmptyCell();
                    specialFoodTimer = 50;
                }
                if (foodsEatenThisLevel >= foodsToNextLevel)
                {
                    NextLevel();
                    return;
                }
            }
            else if (specialFood.HasValue && newHead == specialFood.Value)
            {
                score += 50;
                specialFood = null;
                specialFoodTimer = 0;
                EmitParticles(newHead, Color.Magenta);
            }
            else
            {
                if (activePowerUp == PowerUpType.Shrink && snake.Count > InitialSnakeLength)
                    snake.RemoveAt(snake.Count - 1);
                snake.RemoveAt(snake.Count - 1);
            }

            var hitPowerUp = powerUps.FirstOrDefault(p => p.Position == newHead && !p.Active);
            if (hitPowerUp != null)
            {
                ActivatePowerUp(hitPowerUp.Type);
                hitPowerUp.Active = true;
                SoundManager.PlayPowerUp();
                EmitParticles(newHead, Color.Cyan);
            }

            if (activePowerUp.HasValue)
            {
                powerUpDuration--;
                if (powerUpDuration <= 0)
                    activePowerUp = null;
            }

            if (specialFood.HasValue)
            {
                specialFoodTimer--;
                if (specialFoodTimer <= 0)
                    specialFood = null;
            }

            for (int i = particles.Count - 1; i >= 0; i--)
            {
                particles[i].Update();
                if (particles[i].Life <= 0)
                    particles.RemoveAt(i);
            }

            Invalidate();
        }

        private void SpawnFood()
        {
            food = RandomEmptyCell();
        }

        private void SpawnPowerUp()
        {
            if (rand.NextDouble() < 0.2)
            {
                var pos = RandomEmptyCell();
                var type = (PowerUpType)rand.Next(Enum.GetValues(typeof(PowerUpType)).Length);
                powerUps.Add(new PowerUp(pos, type, 100));
            }
        }

        private void ActivatePowerUp(PowerUpType type)
        {
            activePowerUp = type;
            powerUpDuration = 100;
            if (type == PowerUpType.Slow)
                timer.Interval = GetGameSpeedForLevel(currentLevel) + 40;
            else if (type == PowerUpType.Speed)
                timer.Interval = Math.Max(20, GetGameSpeedForLevel(currentLevel) - 30);
            else
                timer.Interval = GetGameSpeedForLevel(currentLevel);
        }

        private Point RandomEmptyCell()
        {
            Point p;
            do
            {
                p = new Point(rand.Next(GridWidth), rand.Next(GridHeight));
            } while (
                snake.Contains(p) ||
                (specialFood.HasValue && p == specialFood.Value) ||
                obstacles.Any(o => o.Area.Contains(p)) ||
                powerUps.Any(u => u.Position == p) ||
                (currentTheme.HasWalls && wallCells.Contains(p))
            );
            return p;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            DrawGame(buffer.Graphics);
            buffer.Render(e.Graphics);
        }

        private void DrawGame(Graphics g)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(currentTheme.BackgroundColor);

            // Draw background image if any
            if (currentTheme.BackgroundImage != null)
                g.DrawImage(currentTheme.BackgroundImage, 0, 0, GridWidth * CellSize, GridHeight * CellSize);

            // --- Draw wall cells if theme has walls ---
            if (currentTheme.HasWalls && wallCells.Count > 0)
            {
                foreach (var wall in wallCells)
                {
                    Rectangle wallRect = new Rectangle(wall.X * CellSize, wall.Y * CellSize, CellSize, CellSize);
                    using (var wallBrush = new SolidBrush(currentTheme.WallColor))
                        g.FillRectangle(wallBrush, wallRect);
                    using (var wallPen = new Pen(Color.White, 2))
                        g.DrawRectangle(wallPen, wallRect);
                }
            }

            // --- Draw highly visible wall if theme has walls ---
            if (currentTheme.HasWalls)
            {
                // Draw a shadow/glow for the wall
                using (var glowPen = new Pen(Color.FromArgb(120, currentTheme.WallColor), 18))
                {
                    glowPen.LineJoin = System.Drawing.Drawing2D.LineJoin.Round;
                    glowPen.DashStyle = currentTheme.WallDashStyle;
                    g.DrawRectangle(
                        glowPen,
                        0 + 9, 0 + 9,
                        GridWidth * CellSize - 18,
                        GridHeight * CellSize - 18
                    );
                }
                // Draw the main wall border (thicker and vibrant)
                using (var wallPen = new Pen(currentTheme.WallColor, 8))
                {
                    wallPen.LineJoin = System.Drawing.Drawing2D.LineJoin.Round;
                    wallPen.DashStyle = currentTheme.WallDashStyle;
                    g.DrawRectangle(
                        wallPen,
                        0 + 4, 0 + 4,
                        GridWidth * CellSize - 8,
                        GridHeight * CellSize - 8
                    );
                }
                // Optional: add a highlight
                using (var highlightPen = new Pen(Color.White, 2))
                {
                    highlightPen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dot;
                    g.DrawRectangle(
                        highlightPen,
                        0 + 8, 0 + 8,
                        GridWidth * CellSize - 16,
                        GridHeight * CellSize - 16
                    );
                }
            }

            // Draw grid (subtle)
            using (var gridPen = new Pen(Color.FromArgb(30, 255, 255, 255)))
            {
                for (int x = 0; x <= GridWidth; x++)
                    g.DrawLine(gridPen, x * CellSize, 0, x * CellSize, GridHeight * CellSize);
                for (int y = 0; y <= GridHeight; y++)
                    g.DrawLine(gridPen, 0, y * CellSize, GridWidth * CellSize, y * CellSize);
            }

            foreach (var obs in obstacles)
            {
                using (var brush = new LinearGradientBrush(
                    new Rectangle(obs.Area.X * CellSize, obs.Area.Y * CellSize, obs.Area.Width * CellSize, obs.Area.Height * CellSize),
                    Color.Gray, Color.DarkSlateGray, LinearGradientMode.ForwardDiagonal))
                {
                    g.FillRectangle(brush, obs.Area.X * CellSize, obs.Area.Y * CellSize, obs.Area.Width * CellSize, obs.Area.Height * CellSize);
                }
                g.DrawRectangle(Pens.Black, obs.Area.X * CellSize, obs.Area.Y * CellSize, obs.Area.Width * CellSize, obs.Area.Height * CellSize);
            }

            DrawFood(g, food, false);
            if (specialFood.HasValue)
                DrawFood(g, specialFood.Value, true);

            foreach (var pu in powerUps.Where(p => !p.Active))
                DrawPowerUp(g, pu);

            for (int i = snake.Count - 1; i >= 0; i--)
            {
                var pt = snake[i];
                Rectangle rect = new Rectangle(pt.X * CellSize, pt.Y * CellSize, CellSize, CellSize);

                using (var shadowBrush = new SolidBrush(Color.FromArgb(60, 0, 0, 0)))
                    g.FillEllipse(shadowBrush, rect.X + 3, rect.Y + 3, CellSize, CellSize);

                Color c1 = currentTheme.SnakeBodyColor;
                Color c2 = currentTheme.SnakeBodyColor;
                if (i == 0)
                {
                    c1 = currentTheme.SnakeHeadColor;
                    c2 = currentTheme.SnakeHeadColor;
                }
                using (var brush = new LinearGradientBrush(rect, c1, c2, LinearGradientMode.ForwardDiagonal))
                    g.FillEllipse(brush, rect);

                if (i == 0)
                {
                    int eyeSize = CellSize / 5;
                    int offsetX = direction == 0 ? eyeSize : direction == 2 ? -eyeSize : 0;
                    int offsetY = direction == 1 ? eyeSize : direction == 3 ? -eyeSize : 0;
                    g.FillEllipse(Brushes.White, rect.X + CellSize / 2 + offsetX - eyeSize / 2, rect.Y + CellSize / 2 + offsetY - eyeSize / 2, eyeSize, eyeSize);
                    g.FillEllipse(Brushes.Black, rect.X + CellSize / 2 + offsetX - eyeSize / 4, rect.Y + CellSize / 2 + offsetY - eyeSize / 4, eyeSize / 2, eyeSize / 2);
                }
            }

            foreach (var p in particles)
            {
                using (var b = new SolidBrush(Color.FromArgb((int)(255 * (p.Life / 20f)), p.Color)))
                    g.FillEllipse(b, p.Position.X * CellSize, p.Position.Y * CellSize, 6, 6);
            }

            using (var font = new Font("Segoe UI", 16, FontStyle.Bold))
            using (var brush = new LinearGradientBrush(new Rectangle(0, GridHeight * CellSize, ClientSize.Width, 40), Color.Gold, Color.OrangeRed, LinearGradientMode.Horizontal))
            {
                g.FillRectangle(brush, 0, GridHeight * CellSize, ClientSize.Width, 40);
                int highScore = HighScoreManager.GetHighScores().FirstOrDefault();
                string info = $"Score: {score}    Level: {currentLevel}    High Score: {highScore}    Theme: {currentTheme.Name}";
                g.DrawString(info, font, Brushes.White, 10, GridHeight * CellSize + 5);

                int barWidth = 250;
                int barHeight = 16;
                int barX = ClientSize.Width - barWidth - 20;
                int barY = GridHeight * CellSize + 12;
                float progress = foodsToNextLevel > 0 ? (float)foodsEatenThisLevel / foodsToNextLevel : 1f;
                g.FillRectangle(Brushes.DarkGray, barX, barY, barWidth, barHeight);
                g.FillRectangle(Brushes.LimeGreen, barX, barY, (int)(barWidth * progress), barHeight);
                g.DrawRectangle(Pens.Black, barX, barY, barWidth, barHeight);
                using (var pf = new Font("Segoe UI", 10, FontStyle.Bold))
                    g.DrawString("Level Progress", pf, Brushes.Black, barX + 4, barY - 18);
            }

            if (gameOver)
            {
                string msg = "Game Over! Press Enter to Restart";
                using (var font = new Font("Segoe UI", 24, FontStyle.Bold))
                using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                {
                    var rect = new Rectangle(0, 0, ClientSize.Width, ClientSize.Height);
                    g.DrawString(msg, font, Brushes.Red, rect, sf);
                }
            }

            if (paused)
            {
                using (var font = new Font("Segoe UI", 32, FontStyle.Bold))
                using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                {
                    var rect = new Rectangle(0, 0, ClientSize.Width, ClientSize.Height);
                    g.DrawString("Paused", font, Brushes.Yellow, rect, sf);
                }
            }
        }

        private void DrawPowerUp(Graphics g, PowerUp pu)
        {
            Rectangle rect = new Rectangle(pu.Position.X * CellSize, pu.Position.Y * CellSize, CellSize, CellSize);
            Color c = Color.Cyan;
            switch (pu.Type)
            {
                case PowerUpType.Invincibility: c = Color.Gold; break;
                case PowerUpType.Ghost: c = Color.LightGray; break;
                case PowerUpType.ScoreMultiplier: c = Color.DeepSkyBlue; break;
                case PowerUpType.Slow: c = Color.LightGreen; break;
                case PowerUpType.Speed: c = Color.OrangeRed; break;
                case PowerUpType.Shrink: c = Color.Purple; break;
            }
            using (var brush = new LinearGradientBrush(rect, c, Color.White, LinearGradientMode.ForwardDiagonal))
                g.FillEllipse(brush, rect);
            g.DrawEllipse(Pens.Black, rect);
        }

        private void DrawFood(Graphics g, Point pt, bool special)
        {
            Rectangle rect = new Rectangle(pt.X * CellSize, pt.Y * CellSize, CellSize, CellSize);
            if (special)
            {
                using (var path = new System.Drawing.Drawing2D.GraphicsPath())
                {
                    path.AddEllipse(rect);
                    using (var brush = new System.Drawing.Drawing2D.PathGradientBrush(path)
                    {
                        CenterColor = Color.Magenta,
                        SurroundColors = new[] { Color.DeepPink }
                    })
                    {
                        g.FillEllipse(brush, rect);
                    }
                }
                for (int i = 0; i < 6; i++)
                {
                    double angle = i * Math.PI / 3;
                    int x = rect.X + CellSize / 2 + (int)(Math.Cos(angle) * CellSize / 2.5);
                    int y = rect.Y + CellSize / 2 + (int)(Math.Sin(angle) * CellSize / 2.5);
                    g.FillEllipse(Brushes.White, x - 2, y - 2, 4, 4);
                }
            }
            else
            {
                using (var brush = new System.Drawing.Drawing2D.LinearGradientBrush(rect, Color.Red, Color.Orange, System.Drawing.Drawing2D.LinearGradientMode.ForwardDiagonal))
                    g.FillEllipse(brush, rect);
                g.DrawEllipse(Pens.DarkRed, rect);
            }
        }

        private void EmitParticles(Point pt, Color color)
        {
            for (int i = 0; i < 10; i++)
            {
                float angle = (float)(rand.NextDouble() * 2 * Math.PI);
                float speed = (float)(rand.NextDouble() * 0.5 + 0.5);
                particles.Add(new Particle(
                    new PointF(pt.X + 0.5f, pt.Y + 0.5f),
                    new PointF((float)Math.Cos(angle) * speed, (float)Math.Sin(angle) * speed),
                    20, color));
            }
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (gameOver && e.KeyCode == Keys.Enter)
            {
                StartGame();
                return;
            }
            if (e.KeyCode == Keys.P)
            {
                paused = !paused;
                Invalidate();
                return;
            }
            if (e.KeyCode == Keys.Escape)
            {
                timer?.Stop();
                using (var menu = new MenuForm())
                {
                    if (menu.ShowDialog() == DialogResult.OK)
                    {
                        currentLevel = menu.SelectedLevel;
                        currentTheme = menu.SelectedTheme;
                        StartGame();
                    }
                }
                return;
            }
            if (paused) return;

            // Arrow keys and WASD support
            if ((e.KeyCode == Keys.Right || e.KeyCode == Keys.D) && direction != 2) nextDirection = 0;
            else if ((e.KeyCode == Keys.Down || e.KeyCode == Keys.S) && direction != 3) nextDirection = 1;
            else if ((e.KeyCode == Keys.Left || e.KeyCode == Keys.A) && direction != 0) nextDirection = 2;
            else if ((e.KeyCode == Keys.Up || e.KeyCode == Keys.W) && direction != 1) nextDirection = 3;
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (context != null)
            {
                if (buffer != null) buffer.Dispose();
                buffer = context.Allocate(this.CreateGraphics(), this.DisplayRectangle);
            }
            Invalidate();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }
    }
}
