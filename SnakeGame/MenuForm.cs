using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.IO;
using System.Reflection;
using SnakeGame;

namespace SnakeGame
{
    public partial class MenuForm : Form
    {
        public int SelectedLevel { get; private set; } = 1;
        public Theme SelectedTheme { get; private set; }
        public bool CanResume { get; set; } // Set this from Form1 before showing the menu

        private Button resumeBtn;
        private Button startGameButton;

        public MenuForm()
        {
            this.Text = "Snake Game - Ultra Complex Edition";
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Width = 420;
            this.Height = 380;
            this.BackColor = Color.FromArgb(30, 30, 40);

            this.KeyPreview = true; // Ensure the form receives key events
            this.KeyDown += MenuForm_KeyDown;

            // Set the app icon at runtime (ensure snake.ico is in output directory)
            try
            {
                var iconPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "snake.ico");
                if (File.Exists(iconPath))
                    this.Icon = new Icon(iconPath);
            }
            catch { /* ignore icon errors */ }

            var title = new Label
            {
                Text = "🐍 SNAKE GAME 🐍",
                Font = new Font("Segoe UI", 22, FontStyle.Bold),
                ForeColor = Color.LimeGreen,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Top,
                Height = 60,
                BackColor = Color.Transparent
            };

            var highScoreLabel = new Label
            {
                Text = $"High Score: {HighScoreManager.GetHighScores().FirstOrDefault()}",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Color.Gold,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Top,
                Height = 30,
                BackColor = Color.Transparent
            };

            var levelLabel = new Label
            {
                Text = "Select Level:",
                Font = new Font("Segoe UI", 12, FontStyle.Regular),
                ForeColor = Color.White,
                Left = 110,
                Top = 100,
                Width = 100,
                Height = 30,
                BackColor = Color.Transparent
            };

            var levelBox = new ComboBox
            {
                Left = 210,
                Top = 100,
                Width = 100,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 12, FontStyle.Regular)
            };
            for (int i = 1; i <= 10; i++)
                levelBox.Items.Add($"Level {i}");
            levelBox.SelectedIndex = 0;

            var themeLabel = new Label
            {
                Text = "Select Theme:",
                Font = new Font("Segoe UI", 12, FontStyle.Regular),
                ForeColor = Color.White,
                Left = 110,
                Top = 150,
                Width = 100,
                Height = 30,
                BackColor = Color.Transparent
            };

            var themeBox = new ComboBox
            {
                Left = 210,
                Top = 150,
                Width = 150,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 12, FontStyle.Regular)
            };
            var themes = Theme.GetThemes();
            foreach (var t in themes)
                themeBox.Items.Add(t.Name);
            themeBox.SelectedIndex = 0;

            startGameButton = new Button
            {
                Text = "Start Game",
                Left = 110,
                Top = 220,
                Width = 200,
                Height = 40,
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                BackColor = Color.LimeGreen,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            startGameButton.FlatAppearance.BorderSize = 0;

            // --- Resume Button ---
            resumeBtn = new Button
            {
                Text = "Resume",
                Left = 110,
                Top = 270,
                Width = 200,
                Height = 40,
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                BackColor = Color.DodgerBlue,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Enabled = CanResume // Ensure it is only enabled if the game is paused
            };
            resumeBtn.FlatAppearance.BorderSize = 0;

            resumeBtn.Click += (s, e) =>
            {
                // Set a property or flag to indicate resume was chosen
                this.SelectedLevel = -1; // Example: -1 means resume (adjust as needed)
                this.SelectedTheme = null;
                this.DialogResult = DialogResult.Retry; // Use Retry to distinguish from OK
                this.Close();
            };

            startGameButton.Click += (s, e) =>
            {
                SelectedLevel = levelBox.SelectedIndex + 1;
                SelectedTheme = themes[themeBox.SelectedIndex];
                this.DialogResult = DialogResult.OK;
                this.Close();
            };

            this.Controls.Add(title);
            this.Controls.Add(highScoreLabel);
            this.Controls.Add(levelLabel);
            this.Controls.Add(levelBox);
            this.Controls.Add(themeLabel);
            this.Controls.Add(themeBox);
            this.Controls.Add(startGameButton);
            this.Controls.Add(resumeBtn); // <-- Add resume button

            title.BringToFront();
            highScoreLabel.BringToFront();
            levelLabel.BringToFront();
            levelBox.BringToFront();
            themeLabel.BringToFront();
            themeBox.BringToFront();
            startGameButton.BringToFront();
            resumeBtn.BringToFront(); // <-- Bring resume button to front

            this.Load += MenuForm_Load; // Wire up the Load event
        }

        private void MenuForm_Load(object sender, EventArgs e)
        {
            // Enable/disable the Resume button based on CanResume property
            resumeBtn.Enabled = CanResume;
            // Optionally, hide if not allowed:
            // resumeBtn.Visible = CanResume;
        }

        private void MenuForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                // Simulate Start Game button click
                startGameButton.PerformClick();
                e.Handled = true;
            }
        }
    }
}
