using System;
using System.Drawing;
using SnakeGame;

namespace SnakeGame
{
    public class PowerUp
    {
        public Point Position { get; set; }
        public PowerUpType Type { get; set; }
        public int Duration { get; set; }
        public bool Active { get; set; }

        public PowerUp(Point pos, PowerUpType type, int duration)
        {
            Position = pos;
            Type = type;
            Duration = duration;
            Active = false;
        }
    }
}
