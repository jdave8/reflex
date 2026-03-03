using UnityEngine;
using TMPro;
using Reflex.Core;

namespace Reflex.UI
{
    /// <summary>
    /// Displays score, streak multiplier, and stadium tier during gameplay.
    /// Polls GameManager state each frame for simplicity.
    /// </summary>
    public class HUDController : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private TMP_Text streakText;
        [SerializeField] private TMP_Text tierText;

        private void Update()
        {
            if (GameManager.Instance == null) return;

            bool show = GameManager.Instance.CurrentState == GameState.Playing;

            if (panel.activeSelf != show)
                panel.SetActive(show);

            if (show)
            {
                var gm = GameManager.Instance;

                scoreText.text = gm.Score.ToString("N0");

                int hits = gm.ConsecutiveHits;
                streakText.text = hits > 1 ? $"×{hits}" : "";

                tierText.text = gm.GetStadiumTierName();
            }
        }
    }
}
