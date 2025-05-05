using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SnakeGame
{
    public static class HighScoreManager
    {
        private static readonly string FilePath = "highscores.txt";
        private static List<int> scores = new List<int>();

        public static void Load()
        {
            if (File.Exists(FilePath))
                scores = File.ReadAllLines(FilePath).Select(s => int.TryParse(s, out var v) ? v : 0).ToList();
        }

        public static void Save()
        {
            File.WriteAllLines(FilePath, scores.Select(s => s.ToString()));
        }

        public static void AddScore(int score)
        {
            scores.Add(score);
            scores = scores.OrderByDescending(s => s).Take(10).ToList();
            Save();
        }

        public static IEnumerable<int> GetHighScores() => scores;
    }
}
