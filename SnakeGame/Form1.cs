using System;
// ...other using statements...
using SnakeGame; // Ensure this is present
// ...rest of the file remains unchanged...
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Media;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
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
        private const int InitialSnakeLength = 3; // was 6, now shorter for all levels/themes
        private const int GameSpeed = 60; // ms per tick

        // --- Game state ---
        private List<Point> snake;
        private Point food;
        private Point? specialFood = null;
        private int direction = 0; // 0=Right, 1=Down, 2=Left, 3=Up
        private int nextDirection = 0;
        private int score = 0;
        private int startingLevel = 1; // Track the level the player started from
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

        private int scoreAtLevelStart = 0; // Store the score at the beginning of each level

        // --- Graphics ---
        private BufferedGraphicsContext context;
        private BufferedGraphics buffer;

        // --- Countdown state ---
        private int resumeCountdown = 0;
        private Timer resumeCountdownTimer;
        private string resumeCountdownText = "";
        private bool isLevelCountdown = false; // Add this flag to distinguish between level and resume countdown

        // Add a static flag to track if a game has been started since launch
        private static bool hasStartedGame = false;

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
                    startingLevel = currentLevel; // Save the starting level
                    currentTheme = menu.SelectedTheme;
                }
            }

            HighScoreManager.Load();
            StartGame();
            hasStartedGame = true; // Set flag after first game start
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
            // --- Level 6: vertical snake, head at bottom, tail at top, going up, X away from wall ---
            if (currentLevel == 6)
            {
                int startX = 3; // Start a few cells away from the left wall
                int startY = 3;
                
                // Tail at top, head at bottom
                for (int i = 0; i < InitialSnakeLength; i++)
                    snake.Add(new Point(startX, startY + i));
                direction = 3; // Up
                nextDirection = 3;
            }
            // --- Level 7: vertical snake, head at bottom, tail at top, going down, X away from wall, with full loop ---
            else if (currentLevel == 7)
            {
                int startX = 11;
                int startY = 6;
                for (int i = 0; i < InitialSnakeLength; i++)
                    snake.Add(new Point(startX, startY + i));
                direction = 3; // Down
                nextDirection = 3;
            }
            // --- Custom snake start for level 8: vertical, from top to bottom ---
            else if (currentLevel == 8)
            {
                for (int i = InitialSnakeLength - 1; i >= 0; i--)
                    snake.Add(new Point(GridWidth / 2, i + 2)); // Centered horizontally, starts at y=2
                direction = 1; // Down
                nextDirection = 1;
            }
            // --- Custom snake start for level 2 and 5: lower (closer to bottom) ---
            else if (currentLevel == 2 || currentLevel == 5)
            {
                int y = GridHeight - 4;
                for (int i = InitialSnakeLength - 1; i >= 0; i--)
                    snake.Add(new Point(i + 5, y));
                direction = 0; // Right
                nextDirection = 0;
            }
            // --- Custom snake start for level 4, 7: lower (closer to bottom) ---
            else if (currentLevel == 4)
            {
                int y = GridHeight - 4;
                for (int i = InitialSnakeLength - 1; i >= 0; i--)
                    snake.Add(new Point(i + 5, y));
                direction = 0; // Right
                nextDirection = 0;
            }
            else if (currentLevel == 7)
            {
                int y = GridHeight - 5;
                for (int i = InitialSnakeLength - 1; i >= 0; i--)
                    snake.Add(new Point(i + 5, y));
                direction = 0; // Right
                nextDirection = 0;
            }
            else
            {
                for (int i = InitialSnakeLength - 1; i >= 0; i--)
                    snake.Add(new Point(i + 5, GridHeight / 2));
                direction = 0;
                nextDirection = 0;
            }

            // --- Safeguard: Ensure snake is not initialized on a wall cell for any level ---
            if (currentTheme != null && currentTheme.HasWalls && wallCells != null && wallCells.Count > 0)
            {
                bool overlaps = snake.Any(pt => wallCells.Contains(pt));
                if (overlaps)
                {
                    // Try to find a safe Y row for horizontal snakes, or X for vertical
                    if (direction == 0 || direction == 2) // Horizontal
                    {
                        for (int tryY = 1; tryY < GridHeight - 1; tryY++)
                        {
                            bool collision = false;
                            for (int i = 0; i < InitialSnakeLength; i++)
                            {
                                Point pt = new Point(i + 5, tryY);
                                if (wallCells.Contains(pt))
                                {
                                    collision = true;
                                    break;
                                }
                            }
                            if (!collision)
                            {
                                snake.Clear();
                                for (int i = InitialSnakeLength - 1; i >= 0; i--)
                                    snake.Add(new Point(i + 5, tryY));
                                break;
                            }
                        }
                    }
                    else // Vertical
                    {
                        for (int tryX = 1; tryX < GridWidth - 1; tryX++)
                        {
                            bool collision = false;
                            for (int i = 0; i < InitialSnakeLength; i++)
                            {
                                Point pt = new Point(tryX, i + 2);
                                if (wallCells.Contains(pt))
                                {
                                    collision = true;
                                    break;
                                }
                            }
                            if (!collision)
                            {
                                snake.Clear();
                                for (int i = InitialSnakeLength - 1; i >= 0; i--)
                                    snake.Add(new Point(tryX, i + 2));
                                break;
                            }
                        }
                    }
                }
            }

            // Only reset score if starting a new run (i.e., not advancing to next level)
            if (currentLevel == startingLevel)
                score = 0;

            // Store the score at the start of the level for resume/restart
            scoreAtLevelStart = score;

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

            // --- Determine if border walls should be used for this level and theme ---
            bool useBorderWalls = false;
            if (currentTheme.HasWalls)
            {
                // Levels that should use border walls with wall themes
                if (currentLevel == 2 || currentLevel == 4 || currentLevel == 5 ||
                    currentLevel == 6 || currentLevel == 8 || currentLevel == 9 || currentLevel == 10)
                {
                    useBorderWalls = true;
                }
                // Level 1 and default already have border walls by default
            }

            levelData = Level.GetLevel(currentLevel, useBorderWalls);

            obstacles = levelData.Obstacles.Select(r => new Obstacle(r)).ToList();

            // For level 3, always use no border walls (loop logic), even for wall themes
            if (currentLevel == 3)
                wallCells = new List<Point>();
            else
                wallCells = (currentTheme.HasWalls && levelData.Walls != null) ? new List<Point>(levelData.Walls) : new List<Point>();

            SpawnFood();
            SpawnPowerUp();

            if (timer != null)
                timer.Stop();
            timer = new Timer();
            timer.Interval = GetGameSpeedForLevel(currentLevel);
            timer.Tick += GameTick;
            // Do not start timer here!
            // timer.Start();
            SoundManager.PlayBackgroundMusic();
            hasStartedGame = true;

            // Start the level countdown
            StartLevelCountdown();
        }

        private void StartLevelCountdown()
        {
            isLevelCountdown = true;
            resumeCountdown = 3;
            resumeCountdownText = "3";
            paused = true;
            timer?.Stop();

            if (resumeCountdownTimer != null)
            {
                resumeCountdownTimer.Stop();
                resumeCountdownTimer.Dispose();
            }
            resumeCountdownTimer = new Timer();
            resumeCountdownTimer.Interval = 1000;
            resumeCountdownTimer.Tick += (s, e) =>
            {
                resumeCountdown--;
                if (resumeCountdown == 2) resumeCountdownText = "2";
                else if (resumeCountdown == 1) resumeCountdownText = "1";
                else if (resumeCountdown == 0) resumeCountdownText = "Start";
                else if (resumeCountdown < 0)
                {
                    resumeCountdownTimer.Stop();
                    resumeCountdownText = "";
                    paused = false;
                    isLevelCountdown = false;
                    timer?.Start();
                }
                Invalidate();
            };
            resumeCountdownTimer.Start();
            Invalidate();
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
                // Do NOT reset score here; it should accumulate
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
                        startingLevel = currentLevel; // Reset starting level
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

            // --- Always use looping logic for level 3, regardless of theme ---
            if (currentLevel == 3)
            {
                if (newHead.X < 0) newHead.X = GridWidth - 1;
                if (newHead.X >= GridWidth) newHead.X = 0;
                if (newHead.Y < 0) newHead.Y = GridHeight - 1;
                if (newHead.Y >= GridHeight) newHead.Y = 0;
            }
            // --- Level 6: loop on all edges (left, right, top, bottom), all themes ---
            else if (currentLevel == 6)
            {
                if (newHead.X < 0) newHead.X = GridWidth - 1;
                if (newHead.X >= GridWidth) newHead.X = 0;
                if (newHead.Y < 0) newHead.Y = GridHeight - 1;
                if (newHead.Y >= GridHeight) newHead.Y = 0;
            }
            // --- Level 7: loop on all edges (left, right, top, bottom), all themes ---
            else if (currentLevel == 7)
            {
                if (newHead.X < 0) newHead.X = GridWidth - 1;
                if (newHead.X >= GridWidth) newHead.X = 0;
                if (newHead.Y < 0) newHead.Y = GridHeight - 1;
                if (newHead.Y >= GridHeight) newHead.Y = 0;
            }
            else if (!currentTheme.HasWalls)
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
                // Score for food: (currentLevel * 10), doubled if ScoreMultiplier is active
                int foodScore = currentLevel * 10 * (activePowerUp == PowerUpType.ScoreMultiplier ? 2 : 1);
                score += foodScore;
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

            // Draw countdown overlay if active (for both level and resume countdowns)
            if (resumeCountdown > 0 || resumeCountdownText == "Start")
            {
                using (var font = new Font("Segoe UI", resumeCountdownText == "Start" ? 36 : 48, FontStyle.Bold))
                using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                {
                    var rect = new Rectangle(0, 0, ClientSize.Width, ClientSize.Height);
                    e.Graphics.DrawString(resumeCountdownText, font, Brushes.Yellow, rect, sf);
                }
            }
            else if (paused)
            {
                using (var font = new Font("Segoe UI", 32, FontStyle.Bold))
                using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                {
                    var rect = new Rectangle(0, 0, ClientSize.Width, ClientSize.Height);
                    e.Graphics.DrawString("Paused", font, Brushes.Yellow, rect, sf);
                }
            }
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
                score = scoreAtLevelStart; // Reset score to the value at the start of the level
                StartGame();
                return;
            }
            if (e.KeyCode == Keys.P)
            {
                paused = !paused;
                if (paused) timer?.Stop(); else timer?.Start();
                Invalidate();
                return;
            }
            if (e.KeyCode == Keys.Escape)
            {
                if (!paused)
                {
                    paused = true;
                    timer?.Stop();
                    Invalidate();
                    ShowMenu();
                }
                return;
            }
            if (paused || resumeCountdown > 0 || isLevelCountdown) return; // Prevent input during pause/countdown

            // Arrow keys and WASD support
            if ((e.KeyCode == Keys.Right || e.KeyCode == Keys.D) && direction != 2) nextDirection = 0;
            else if ((e.KeyCode == Keys.Down || e.KeyCode == Keys.S) && direction != 3) nextDirection = 1;
            else if ((e.KeyCode == Keys.Left || e.KeyCode == Keys.A) && direction != 0) nextDirection = 2;
            else if ((e.KeyCode == Keys.Up || e.KeyCode == Keys.W) && direction != 1) nextDirection = 3;
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape)
            {
                if (!paused)
                {
                    paused = true;
                    timer?.Stop();
                    Invalidate();
                    ShowMenu();
                }
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void ShowMenu()
        {
            using (var menu = new MenuForm())
            {
                // Only allow resume if a game has been started, the game is paused, and not over
                menu.CanResume = hasStartedGame && paused && !gameOver;

                var result = menu.ShowDialog();
                if (result == DialogResult.Retry)
                {
                    // Resume: do NOT call LoadGame, do NOT change theme, just start countdown
                    StartResumeCountdown();
                }
                else if (result == DialogResult.OK)
                {
                    // Start new game: this is the only place where theme/level changes
                    StartNewGame(menu.SelectedLevel, menu.SelectedTheme);
                }
            }
        }

        private void StartResumeCountdown()
        {
            if (isLevelCountdown) return; // Don't start resume countdown if level countdown is running

            resumeCountdown = 3;
            resumeCountdownText = "3";
            paused = true;
            timer?.Stop();

            if (resumeCountdownTimer != null)
            {
                resumeCountdownTimer.Stop();
                resumeCountdownTimer.Dispose();
            }
            resumeCountdownTimer = new Timer();
            resumeCountdownTimer.Interval = 1000;
            resumeCountdownTimer.Tick += (s, e) =>
            {
                resumeCountdown--;
                if (resumeCountdown == 2) resumeCountdownText = "2";
                else if (resumeCountdown == 1) resumeCountdownText = "1";
                else if (resumeCountdown == 0) resumeCountdownText = "Start";
                else if (resumeCountdown < 0)
                {
                    resumeCountdownTimer.Stop();
                    resumeCountdownText = "";
                    paused = false;
                    timer?.Start();
                }
                Invalidate();
            };
            resumeCountdownTimer.Start();
            Invalidate();
        }

        private void SaveGame()
        {
            var state = new GameState
            {
                Level = this.currentLevel,
                ThemeName = this.currentTheme?.Name,
                Snake = this.snake.ToList(),
                Food = this.food,
                Score = this.score,
                Direction = this.direction.ToString(),
                Obstacles = this.obstacles?.Select(o => o.Area).ToList() ?? new List<Rectangle>(), // <-- convert to List<Rectangle>
                Speed = this.timer.Interval
                // Add other fields as needed
            };
            try
            {
                using (var fs = new FileStream("savegame.dat", FileMode.Create))
                {
                    var bf = new BinaryFormatter();
                    bf.Serialize(fs, state);
                }
            }
            catch { /* handle or log error if needed */ }
        }

        private bool LoadGame()
        {
            if (!File.Exists("savegame.dat"))
                return false;
            try
            {
                using (var fs = new FileStream("savegame.dat", FileMode.Open))
                {
                    var bf = new BinaryFormatter();
                    var state = (GameState)bf.Deserialize(fs);

                    // Restore state
                    this.currentLevel = state.Level;
                    this.currentTheme = Theme.GetThemes().FirstOrDefault(t => t.Name == state.ThemeName);
                    this.snake = new List<Point>(state.Snake);
                    this.food = state.Food;
                    this.score = state.Score;
                    this.direction = int.Parse(state.Direction);
                    this.obstacles = state.Obstacles?.Select(r => new Obstacle(r)).ToList() ?? new List<Obstacle>(); // <-- convert to List<Obstacle>
                    this.timer.Interval = state.Speed;
                    // Restore other fields as needed

                    // Redraw or refresh game as needed
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        private void StartNewGame(int level, Theme theme)
        {
            currentLevel = level;
            startingLevel = level;
            currentTheme = theme;
            StartGame();
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

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            base.OnFormClosed(e);
            hasStartedGame = false;
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }
    }
}
