using System.Collections.Generic;
using System.Drawing;

namespace SnakeGame
{
    public class Level
    {
        public int LevelNumber { get; }
        public List<Rectangle> Obstacles { get; }
        public List<Point> Walls { get; } // Add wall cells
        public Image Background { get; }

        public Level(int number, List<Rectangle> obstacles, List<Point> walls = null, Image background = null)
        {
            LevelNumber = number;
            Obstacles = obstacles;
            Walls = walls ?? new List<Point>();
            Background = background;
        }

        public static Level GetLevel(int number)
        {
            // Example: Add more levels with different obstacles/backgrounds/walls
            switch (number)
            {
                case 2:
                    return new Level(
                        2,
                        new List<Rectangle>
                        {
                            new Rectangle(10, 5, 10, 1),
                            new Rectangle(5, 10, 1, 5),
                            new Rectangle(20, 15, 5, 1)
                        },
                        new List<Point>() // No border walls, just obstacles
                    );
                case 3:
                    return new Level(
                        3,
                        new List<Rectangle>
                        {
                            new Rectangle(0, 8, 36, 1),
                            new Rectangle(18, 0, 1, 24)
                        },
                        new List<Point>() // No border walls, just obstacles
                    );
                default:
                    // For wall themes, add border walls
                    var borderWalls = new List<Point>();
                    int w = 36, h = 24;
                    for (int x = 0; x < w; x++)
                    {
                        borderWalls.Add(new Point(x, 0));
                        borderWalls.Add(new Point(x, h - 1));
                    }
                    for (int y = 1; y < h - 1; y++)
                    {
                        borderWalls.Add(new Point(0, y));
                        borderWalls.Add(new Point(w - 1, y));
                    }
                    return new Level(1, new List<Rectangle>(), borderWalls);
            }
        }
    }
}
