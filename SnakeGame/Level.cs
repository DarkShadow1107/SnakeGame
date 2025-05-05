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

        // ...existing code...
        public static Level GetLevel(int number, bool useBorderWalls = false)
        {
            // Example: Add more levels with different obstacles/backgrounds/walls
            List<Point> borderWalls = null;
            if (useBorderWalls)
            {
                borderWalls = new List<Point>();
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
            }

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
                        borderWalls ?? new List<Point>() // Use border walls if requested
                    );
                case 3:
                    // For level 3, always return no border walls, just obstacles (regardless of theme)
                    return new Level(
                        3,
                        new List<Rectangle>
                        {
                                    new Rectangle(0, 8, 36, 1),
                                    new Rectangle(18, 0, 1, 24)
                        },
                        new List<Point>() // No border walls, just obstacles
                    );
                case 4:
                    // Unique: zig-zag obstacles, border walls if requested
                    return new Level(
                        4,
                        new List<Rectangle>
                        {
                                    new Rectangle(5, 4, 2, 12),
                                    new Rectangle(10, 8, 2, 10),
                                    new Rectangle(15, 2, 2, 14),
                                    new Rectangle(20, 10, 2, 10),
                                    new Rectangle(25, 6, 2, 12),
                                    new Rectangle(30, 3, 2, 14)
                        },
                        borderWalls ?? new List<Point>()
                    );
                case 5:
                    // Unique: central box and four corners, border walls if requested
                    return new Level(
                        5,
                        new List<Rectangle>
                        {
                                    new Rectangle(14, 7, 8, 10),
                                    new Rectangle(0, 0, 2, 2),
                                    new Rectangle(34, 0, 2, 2),
                                    new Rectangle(0, 22, 2, 2),
                                    new Rectangle(34, 22, 2, 2)
                        },
                        borderWalls
                    );
                case 6:
                    // ...unchanged...
                    return new Level(
                        6,
                        new List<Rectangle>
                        {
                                    new Rectangle(17, 0, 2, 24),
                                    new Rectangle(0, 11, 36, 2),
                                    new Rectangle(5, 5, 3, 3),
                                    new Rectangle(28, 16, 3, 3)
                        },
                        null
                    );
                case 7:
                    // ...unchanged...
                    return new Level(
                        7,
                        new List<Rectangle>
                        {
                                    new Rectangle(0, 0, 2, 2),
                                    new Rectangle(2, 2, 2, 2),
                                    new Rectangle(4, 4, 2, 2),
                                    new Rectangle(6, 6, 2, 2),
                                    new Rectangle(8, 8, 2, 2),
                                    new Rectangle(10, 10, 2, 2),
                                    new Rectangle(12, 12, 2, 2),
                                    new Rectangle(14, 14, 2, 2),
                                    new Rectangle(16, 16, 2, 2),
                                    new Rectangle(18, 18, 2, 2),
                                    new Rectangle(20, 20, 2, 2),
                                    new Rectangle(22, 22, 2, 2),
                                    new Rectangle(24, 20, 2, 2),
                                    new Rectangle(26, 18, 2, 2),
                                    new Rectangle(28, 16, 2, 2),
                                    new Rectangle(30, 14, 2, 2),
                                    new Rectangle(32, 12, 2, 2),
                                    new Rectangle(34, 10, 2, 2)
                        },
                        null
                    );
                case 8:
                    // Four vertical bars, border walls if requested
                    return new Level(
                        8,
                        new List<Rectangle>
                        {
                                    new Rectangle(6, 2, 2, 20),
                                    new Rectangle(14, 2, 2, 20),
                                    new Rectangle(22, 2, 2, 20),
                                    new Rectangle(30, 2, 2, 20)
                        },
                        borderWalls
                    );
                case 9:
                    // Unique: spiral pattern, border walls if requested
                    // Add an exit (gap) to the largest rectangle (outer spiral)
                    return new Level(
                        9,
                        new List<Rectangle>
                        {
                            // Top wall (full)
                            new Rectangle(2, 2, 32, 1),
                            // Right wall (full)
                            new Rectangle(33, 2, 1, 20),
                            // Bottom wall (with exit: split into two segments, leaving a gap at x=17..19)
                            new Rectangle(3, 21, 14, 1),   // left segment
                            // gap at (17,21),(18,21),(19,21)
                            new Rectangle(20, 21, 14, 1),  // right segment
                            // Left wall (full)
                            new Rectangle(2, 3, 1, 18),
                            // Inner spiral as before
                            new Rectangle(4, 4, 28, 1),
                            new Rectangle(31, 5, 1, 14),
                            new Rectangle(5, 18, 25, 1),
                            new Rectangle(4, 5, 1, 13)
                        },
                        borderWalls
                    );
                case 10:
                    // Checkerboard pattern, border walls if requested
                    var obstacles10 = new List<Rectangle>();
                    for (int y = 2; y < 22; y += 4)
                        for (int x = 2 + (y % 8 == 2 ? 0 : 2); x < 34; x += 4)
                            obstacles10.Add(new Rectangle(x, y, 2, 2));
                    return new Level(
                        10,
                        obstacles10,
                        borderWalls
                    );
                default:
                    // For wall themes, add border walls
                    var defaultBorderWalls = new List<Point>();
                    int w = 36, h = 24;
                    for (int x = 0; x < w; x++)
                    {
                        defaultBorderWalls.Add(new Point(x, 0));
                        defaultBorderWalls.Add(new Point(x, h - 1));
                    }
                    for (int y = 1; y < h - 1; y++)
                    {
                        defaultBorderWalls.Add(new Point(0, y));
                        defaultBorderWalls.Add(new Point(w - 1, y));
                    }
                    return new Level(1, new List<Rectangle>(), defaultBorderWalls);
            }
        }
    }
}
