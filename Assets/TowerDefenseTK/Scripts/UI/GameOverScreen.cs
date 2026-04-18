using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

namespace TowerDefenseTK
{
    /// <summary>
    /// Attach this to the Game Over panel GameObject (the one wired into
    /// PlayerHealthUI.defeatedPanel).
    ///
    /// Setup in the Inspector:
    ///   restartButton  — the Restart / Try Again button
    ///   titleText      — (optional) big "GAME OVER" header label
    ///   subtitleText   — (optional) small flavour line e.g. "The enemies broke through!"
    ///
    /// The panel shows itself via PlayerHealthUI.defeatedPanel when lives reach 0.
    /// Restart reloads the active scene and resets all game state.
    /// </summary>
    public class GameOverScreen : MonoBehaviour
    {
        [Header("Buttons")]
        [SerializeField] private Button restartButton;

        [Header("Text (optional)")]
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text subtitleText;

        [Header("Appearance")]
        [Tooltip("Panel background CanvasGroup — used to fade the panel in.")]
        [SerializeField] private CanvasGroup canvasGroup;
        [Tooltip("Seconds to fade the panel in. Set to 0 to skip.")]
        [SerializeField] [Min(0f)] private float fadeInDuration = 0.4f;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            // Wire the restart button
            if (restartButton != null)
                restartButton.onClick.AddListener(RestartLevel);

            // The panel itself starts hidden (PlayerHealthUI manages this)
        }

        private void OnEnable()
        {
            // Subscribe so we know when to animate in
            PlayerHealthManager.OnPlayerDefeated += OnDefeated;
        }

        private void OnDisable()
        {
            PlayerHealthManager.OnPlayerDefeated -= OnDefeated;
        }

        // ── Defeat handler ────────────────────────────────────────────────────

        private void OnDefeated()
        {
            // Transition the GameManager into the Lose state so any future logic
            // that checks the round state behaves correctly.
            if (GameManager.Instance != null)
                GameManager.Instance.SwitchState(GameManager.Instance.Lose);

            // Kick off the optional fade
            if (canvasGroup != null && fadeInDuration > 0f)
            {
                canvasGroup.alpha = 0f;
                StartCoroutine(FadeIn());
            }
        }

        private System.Collections.IEnumerator FadeIn()
        {
            float elapsed = 0f;
            while (elapsed < fadeInDuration)
            {
                // Use unscaled time so the fade works even when the game is paused
                elapsed += Time.unscaledDeltaTime;
                if (canvasGroup != null)
                    canvasGroup.alpha = Mathf.Clamp01(elapsed / fadeInDuration);
                yield return null;
            }
            if (canvasGroup != null)
                canvasGroup.alpha = 1f;
        }

        // ── Restart ───────────────────────────────────────────────────────────

        /// <summary>
        /// Resets time scale and reloads the current scene, which reinitialises
        /// every manager, the grid, the pool, and all unit state from scratch.
        /// </summary>
        public void RestartLevel()
        {
            // Always restore timescale before loading — the game may have been
            // paused by PlayerHealthManager or TimeController on defeat.
            Time.timeScale = 1f;

            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
