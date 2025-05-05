using System;
using System.Drawing;

namespace SnakeGame
{
    public class Particle
    {
        public PointF Position;
        public PointF Velocity;
        public float Life;
        public Color Color;

        public Particle(PointF pos, PointF vel, float life, Color color)
        {
            Position = pos;
            Velocity = vel;
            Life = life;
            Color = color;
        }

        public void Update()
        {
            Position.X += Velocity.X;
            Position.Y += Velocity.Y;
            Life -= 1f;
        }
    }
}
