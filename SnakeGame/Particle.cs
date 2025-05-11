using System;
using System.Drawing;
using SnakeGame;

namespace SnakeGame
{
    public class Particle
    {
        public PointF Position { get; set; }
        public PointF Velocity { get; set; }
        public int Life { get; set; }
        public Color Color { get; set; }

        public Particle(PointF pos, PointF vel, int life, Color color)
        {
            Position = pos;
            Velocity = vel;
            Life = life;
            Color = color;
        }

        public void Update()
        {
            Position = new PointF(Position.X + Velocity.X, Position.Y + Velocity.Y);
            Life--;
        }
    }
}
