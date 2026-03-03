using UnityEngine;

namespace Reflex.Data
{
    public static class PlayerData
    {
        private const string KEY_HIGH_SCORE = "HighScore";
        private const string KEY_BEST_STREAK = "BestStreak";

        public static int HighScore
        {
            get => PlayerPrefs.GetInt(KEY_HIGH_SCORE, 0);
            set
            {
                PlayerPrefs.SetInt(KEY_HIGH_SCORE, value);
                PlayerPrefs.Save();
            }
        }

        public static int BestStreak
        {
            get => PlayerPrefs.GetInt(KEY_BEST_STREAK, 0);
            set
            {
                PlayerPrefs.SetInt(KEY_BEST_STREAK, value);
                PlayerPrefs.Save();
            }
        }

        /// <summary>
        /// Update records if new score/streak are higher. Returns true if new high score.
        /// </summary>
        public static bool TryUpdateRecords(int score, int streak)
        {
            bool newHigh = false;
            if (score > HighScore)
            {
                HighScore = score;
                newHigh = true;
            }
            if (streak > BestStreak)
            {
                BestStreak = streak;
            }
            return newHigh;
        }
    }
}
