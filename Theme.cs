using System.Drawing;
using System.Drawing.Drawing2D;

namespace SnakeGame
{
    public class Theme
    {
        public string Name { get; }
        public Color BackgroundColor { get; }
        public Image BackgroundImage { get; }
        public bool HasWalls { get; }
        public Color SnakeHeadColor { get; }
        public Color SnakeBodyColor { get; }
        public Color FoodColor { get; }
        public Color WallColor { get; }
        public DashStyle WallDashStyle { get; }
        // Optionally, you can add a secondary wall color for glow/highlight if you want more customization

        public Theme(
            string name,
            Color bgColor,
            bool hasWalls,
            Color snakeHead,
            Color snakeBody,
            Color food,
            Image bgImage = null,
            Color? wallColor = null,
            DashStyle wallDashStyle = DashStyle.Solid)
        {
            Name = name;
            BackgroundColor = bgColor;
            HasWalls = hasWalls;
            SnakeHeadColor = snakeHead;
            SnakeBodyColor = snakeBody;
            FoodColor = food;
            BackgroundImage = bgImage;
            WallColor = wallColor ?? Color.SaddleBrown;
            WallDashStyle = wallDashStyle;
        }

        public static Theme[] GetThemes()
        {
            return new[]
            {
                new Theme(
                    "Classic Wall",
                    Color.Black, true,
                    Color.YellowGreen, Color.LimeGreen, Color.Red,
                    null,
                    Color.FromArgb(220, 120, 60), DashStyle.Solid // More vibrant brown
                ),
                new Theme(
                    "Neon Loop",
                    Color.FromArgb(20, 20, 40), false,
                    Color.Cyan, Color.DeepSkyBlue, Color.Magenta
                ),
                new Theme(
                    "Desert Wall",
                    Color.BurlyWood, true,
                    Color.SaddleBrown, Color.Peru, Color.Orange,
                    null,
                    Color.OrangeRed, DashStyle.Dot // Brighter wall
                ),
                new Theme(
                    "Forest Loop",
                    Color.DarkOliveGreen, false,
                    Color.ForestGreen, Color.Green, Color.Gold
                ),
                new Theme(
                    "Ice Wall",
                    Color.LightBlue, true,
                    Color.White, Color.LightCyan, Color.BlueViolet,
                    null,
                    Color.DeepSkyBlue, DashStyle.Dash // More visible blue
                ),
                // Add more as desired
            };
        }
    }

    // When calling Level.GetLevel, pass useBorderWalls = true for wall themes and for levels 2, 4, 5, 8, 9, 10.
    // For example:
    // Level.GetLevel(levelNumber, isWallThemeActive);
}
