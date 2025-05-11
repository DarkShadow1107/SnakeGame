using System.Drawing;
using SnakeGame;

namespace SnakeGame
{
    public class Obstacle
    {
        public Rectangle Area { get; set; }

        public Obstacle(Rectangle area)
        {
            Area = area;
        }

        public void Move(int gridWidth, int gridHeight)
        {
            // Stub: No movement by default
        }
    }
}
