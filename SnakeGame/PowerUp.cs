using System;
using System.Drawing;

namespace SnakeGame
{
    public enum PowerUpType
    {
        Invincibility,
        Ghost,
        ScoreMultiplier,
        Slow,
        Speed,
        Shrink
    }

    public class PowerUp
    {
        public Point Position { get; set; }
        public PowerUpType Type { get; }
        public int Duration { get; } // in ticks
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
