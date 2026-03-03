using UnityEngine;
using TMPro;
using Reflex.Core;
using Reflex.Data;

namespace Reflex.UI
{
    /// <summary>
    /// Shows "REFLEX / TAP TO START / BEST: xxx" during ReadyToPlay state.
    /// Polls GameManager state each frame — no event subscription needed.
    /// </summary>
    public class TapToStartUI : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private TMP_Text bestScoreText;

        private void Update()
        {
            if (GameManager.Instance == null) return;

            bool show = GameManager.Instance.CurrentState == GameState.ReadyToPlay;

            if (panel.activeSelf != show)
            {
                panel.SetActive(show);

                if (show && bestScoreText != null)
                {
                    int best = PlayerData.HighScore;
                    bestScoreText.text = best > 0 ? $"BEST: {best:N0}" : "";
                }
            }
        }
    }
}
