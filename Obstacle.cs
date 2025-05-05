using System.Drawing;

namespace SnakeGame
{
    public class Obstacle
    {
        public Rectangle Area { get; }
        public bool IsMoving { get; set; }
        public int Direction { get; set; } // 0=Right,1=Down,2=Left,3=Up

        public Obstacle(Rectangle area, bool isMoving = false, int direction = 0)
        {
            Area = area;
            IsMoving = isMoving;
            Direction = direction;
        }

        public void Move(int gridWidth, int gridHeight)
        {
            if (!IsMoving) return;
            // Example: Move horizontally
            var newArea = Area;
            if (Direction == 0) newArea.X = (Area.X + 1) % gridWidth;
            else if (Direction == 2) newArea.X = (Area.X - 1 + gridWidth) % gridWidth;
            // Add more movement logic as needed
        }
    }
}
