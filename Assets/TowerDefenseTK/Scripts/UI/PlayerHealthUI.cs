using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace TowerDefenseTK
{
    /// <summary>
    /// Displays player health/lives in the UI.
    /// Listens to PlayerHealthManager events and optionally drives a Slider fill bar.
    /// Attach to a Canvas GameObject and wire up fields in the Inspector.
    /// </summary>
    public class PlayerHealthUI : MonoBehaviour
    {
        [Header("Text")]
        [Tooltip("Shows current/max lives e.g. '15 / 20'")]
        [SerializeField] private TMP_Text healthText;

        [Header("Health Bar (optional)")]
        [Tooltip("Slider or Image with fill — set Image Type to Filled in the Inspector")]
        [SerializeField] private Slider healthSlider;
        [SerializeField] private Image healthFillImage;

        [Header("Fill Colors")]
        [SerializeField] private Color fullColor = Color.green;
        [SerializeField] private Color midColor = Color.yellow;
        [SerializeField] private Color lowColor = Color.red;

        [Header("Thresholds")]
        [Tooltip("Below this fraction the bar turns yellow")]
        [SerializeField] [Range(0f, 1f)] private float midThreshold = 0.5f;
        [Tooltip("Below this fraction the bar turns red")]
        [SerializeField] [Range(0f, 1f)] private float lowThreshold = 0.25f;

        [Header("Defeated UI (optional)")]
        [Tooltip("GameObject to show when the player is defeated (e.g. Game Over panel)")]
        [SerializeField] private GameObject defeatedPanel;

        private void OnEnable()
        {
            PlayerHealthManager.OnHealthChanged += UpdateUI;
            PlayerHealthManager.OnPlayerDefeated += ShowDefeatedPanel;
        }

        private void OnDisable()
        {
            PlayerHealthManager.OnHealthChanged -= UpdateUI;
            PlayerHealthManager.OnPlayerDefeated -= ShowDefeatedPanel;
        }

        private void Start()
        {
            if (defeatedPanel != null)
                defeatedPanel.SetActive(false);

            // Sync to current state if manager is already initialized
            if (PlayerHealthManager.Instance != null && PlayerHealthManager.Instance.IsInitialized)
                UpdateUI(PlayerHealthManager.Instance.CurrentHealth, PlayerHealthManager.Instance.MaxHealth);
        }

        private void UpdateUI(int current, int max)
        {
            // Text
            if (healthText != null)
                healthText.text = $"{current} / {max}";

            float fraction = max > 0 ? (float)current / max : 0f;

            // Slider
            if (healthSlider != null)
                healthSlider.value = fraction;

            // Fill color
            Color targetColor = fraction > midThreshold ? fullColor
                              : fraction > lowThreshold ? midColor
                              : lowColor;

            if (healthFillImage != null)
                healthFillImage.color = targetColor;
        }

        private void ShowDefeatedPanel()
        {
            if (defeatedPanel != null)
                defeatedPanel.SetActive(true);
        }
    }
}
