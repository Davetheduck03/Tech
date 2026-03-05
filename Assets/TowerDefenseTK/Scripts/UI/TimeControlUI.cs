using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace TowerDefenseTK
{
    /// <summary>
    /// UI controller for time management buttons (Pause, Play, Fast Forward).
    /// Wire up buttons and speed label in the Inspector.
    /// Requires TimeController to be present in the scene.
    /// </summary>
    public class TimeControlUI : MonoBehaviour
    {
        [Header("Buttons")]
        [SerializeField] private Button pauseButton;
        [SerializeField] private Button playButton;
        [SerializeField] private Button fastForwardButton;

        [Header("Display")]
        [SerializeField] private TMP_Text speedLabel;

        private void OnEnable()
        {
            TimeController.OnSpeedChanged += UpdateSpeedLabel;
            TimeController.OnPauseStateChanged += UpdateButtonStates;
        }

        private void OnDisable()
        {
            TimeController.OnSpeedChanged -= UpdateSpeedLabel;
            TimeController.OnPauseStateChanged -= UpdateButtonStates;
        }

        private void Start()
        {
            // Wire button clicks
            if (pauseButton != null)
                pauseButton.onClick.AddListener(() => TimeController.Instance.Pause());

            if (playButton != null)
                playButton.onClick.AddListener(() => TimeController.Instance.Resume());

            if (fastForwardButton != null)
                fastForwardButton.onClick.AddListener(() => TimeController.Instance.CycleSpeed());

            // Set initial state
            if (TimeController.Instance != null)
            {
                UpdateSpeedLabel(TimeController.Instance.CurrentSpeed);
                UpdateButtonStates(TimeController.Instance.IsPaused);
            }
        }

        private void UpdateSpeedLabel(float speed)
        {
            if (speedLabel != null && TimeController.Instance != null)
                speedLabel.text = TimeController.Instance.GetSpeedLabel();
        }

        private void UpdateButtonStates(bool isPaused)
        {
            if (pauseButton != null)
                pauseButton.interactable = !isPaused;

            if (playButton != null)
                playButton.interactable = isPaused;

            if (speedLabel != null && TimeController.Instance != null)
                speedLabel.text = TimeController.Instance.GetSpeedLabel();
        }
    }
}
