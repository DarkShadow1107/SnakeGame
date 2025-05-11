using System;
using System.Collections.Generic;
using System.Drawing;

namespace SnakeGame
{
    // Serializable class to hold the game state for saving/loading
    [Serializable]
    public class GameState
    {
        public int Level { get; set; }
        public string ThemeName { get; set; }
        public List<Point> Snake { get; set; }
        public Point Food { get; set; }
        public int Score { get; set; }
        public string Direction { get; set; }
        public List<Rectangle> Obstacles { get; set; } // Ensure this is List<Rectangle>
        public int Speed { get; set; }
        // Add other fields as needed to fully restore the game

        // You can add more properties as needed for your game
    }
}
