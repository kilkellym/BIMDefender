using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BIMDefender.Game
{
    /// <summary>
    /// Represents a single high score entry
    /// </summary>
    public class HighScoreEntry
    {
        public string Initials { get; set; }
        public int Score { get; set; }
        public int Wave { get; set; }
        public DateTime Date { get; set; }
    }

    /// <summary>
    /// Manages high score loading, saving, and ranking
    /// </summary>
    public class ScoreManager
    {
        private const int MaxEntries = 5;
        private readonly string _filePath;
        private List<HighScoreEntry> _highScores;

        public IReadOnlyList<HighScoreEntry> HighScores => _highScores.AsReadOnly();

        public ScoreManager(string addInFolder)
        {
            _filePath = Path.Combine(addInFolder, "highscores.json");
            LoadScores();
        }

        /// <summary>
        /// Load high scores from JSON file
        /// </summary>
        private void LoadScores()
        {
            try
            {
                if (File.Exists(_filePath))
                {
                    string json = File.ReadAllText(_filePath);
                    _highScores = JsonConvert.DeserializeObject<List<HighScoreEntry>>(json) ?? new List<HighScoreEntry>();
                }
                else
                {
                    _highScores = new List<HighScoreEntry>();
                }
            }
            catch
            {
                // If there's any error reading the file, start fresh
                _highScores = new List<HighScoreEntry>();
            }
        }

        /// <summary>
        /// Save high scores to JSON file
        /// </summary>
        private void SaveScores()
        {
            try
            {
                string directory = Path.GetDirectoryName(_filePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                string json = JsonConvert.SerializeObject(_highScores, Formatting.Indented);
                File.WriteAllText(_filePath, json);
            }
            catch
            {
                // Silently fail - high scores are not critical
            }
        }

        /// <summary>
        /// Check if a score qualifies for the high score board
        /// </summary>
        public bool IsHighScore(int score)
        {
            if (_highScores.Count < MaxEntries) return true;
            return score > _highScores.Min(s => s.Score);
        }

        /// <summary>
        /// Add a new high score entry
        /// </summary>
        public void AddScore(string initials, int score, int wave)
        {
            var entry = new HighScoreEntry
            {
                Initials = initials.ToUpper().PadRight(3).Substring(0, 3),
                Score = score,
                Wave = wave,
                Date = DateTime.Now
            };

            _highScores.Add(entry);
            _highScores = _highScores
                .OrderByDescending(s => s.Score)
                .Take(MaxEntries)
                .ToList();

            SaveScores();
        }

        /// <summary>
        /// Get the rank position for a given score (1-based)
        /// </summary>
        public int GetRank(int score)
        {
            int rank = 1;
            foreach (var entry in _highScores.OrderByDescending(s => s.Score))
            {
                if (score > entry.Score) break;
                rank++;
            }
            return Math.Min(rank, MaxEntries + 1);
        }

        /// <summary>
        /// Get the current high score (top score)
        /// </summary>
        public int GetTopScore()
        {
            return _highScores.Count > 0 ? _highScores.Max(s => s.Score) : 0;
        }
    }
}
