using UnityEngine;
using TMPro;
using Reflex.Core;
using Reflex.Data;

namespace Reflex.UI
{
    /// <summary>
    /// Displays final score, stats, and high-score badge on the death/score-card screen.
    /// Activates when GameManager enters ScoreCard state.
    /// </summary>
    public class ScoreCardUI : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private TMP_Text finalScoreText;
        [SerializeField] private TMP_Text statsText;
        [SerializeField] private TMP_Text newHighText;

        private bool _wasScoreCard;

        private void Update()
        {
            if (GameManager.Instance == null) return;

            bool show = GameManager.Instance.CurrentState == GameState.ScoreCard;

            // Transition INTO score card — populate data once
            if (show && !_wasScoreCard)
            {
                var gm = GameManager.Instance;
                bool isNewHigh = PlayerData.TryUpdateRecords(gm.Score, gm.ConsecutiveHits);

                finalScoreText.text = gm.Score.ToString("N0");

                statsText.text = $"Hits: {gm.ConsecutiveHits}  •  Time: {gm.TimeSurvived:F1}s\n" +
                                 gm.GetStadiumTierName();

                if (newHighText != null)
                    newHighText.gameObject.SetActive(isNewHigh);

                panel.SetActive(true);
            }
            // Transition OUT of score card
            else if (!show && _wasScoreCard)
            {
                panel.SetActive(false);
            }

            _wasScoreCard = show;
        }
    }
}
