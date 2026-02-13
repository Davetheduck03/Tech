using System;
using UnityEngine;

namespace TowerDefenseTK
{
    /// <summary>
    /// Controls game time speed - pause, normal, fast forward.
    /// </summary>
    public class TimeController : MonoBehaviour
    {
        public static TimeController Instance;

        [Header("Speed Settings")]
        [SerializeField] private float[] speedOptions = { 0f, 1f, 2f, 3f };
        [SerializeField] private int defaultSpeedIndex = 1; // Normal speed

        [Header("Hotkeys")]
        [SerializeField] private KeyCode pauseKey = KeyCode.Space;
        [SerializeField] private KeyCode speedUpKey = KeyCode.Period;      // >
        [SerializeField] private KeyCode slowDownKey = KeyCode.Comma;      // <
        [SerializeField] private KeyCode normalSpeedKey = KeyCode.Slash;   // /

        [Header("Current State")]
        [SerializeField] private int currentSpeedIndex = 1;
        [SerializeField] private bool isPaused = false;

        // Events
        public static event Action<float> OnSpeedChanged;      // new speed
        public static event Action<bool> OnPauseStateChanged;  // isPaused

        // Properties
        public float CurrentSpeed => speedOptions[currentSpeedIndex];
        public bool IsPaused => isPaused;
        public int SpeedIndex => currentSpeedIndex;
        public float[] SpeedOptions => speedOptions;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            currentSpeedIndex = defaultSpeedIndex;
            ApplyTimeScale();
        }

        private void Update()
        {
            HandleInput();
        }

        private void HandleInput()
        {
            // Pause/Resume
            if (Input.GetKeyDown(pauseKey))
            {
                TogglePause();
            }

            // Speed Up
            if (Input.GetKeyDown(speedUpKey))
            {
                SpeedUp();
            }

            // Slow Down
            if (Input.GetKeyDown(slowDownKey))
            {
                SlowDown();
            }

            // Normal Speed
            if (Input.GetKeyDown(normalSpeedKey))
            {
                SetNormalSpeed();
            }
        }

        #region Public Methods

        /// <summary>
        /// Toggle between paused and playing
        /// </summary>
        public void TogglePause()
        {
            if (isPaused)
                Resume();
            else
                Pause();
        }

        /// <summary>
        /// Pause the game
        /// </summary>
        public void Pause()
        {
            isPaused = true;
            Time.timeScale = 0f;
            
            Debug.Log("TimeController: ⏸ PAUSED");
            OnPauseStateChanged?.Invoke(true);
        }

        /// <summary>
        /// Resume the game at current speed
        /// </summary>
        public void Resume()
        {
            isPaused = false;
            ApplyTimeScale();
            
            Debug.Log($"TimeController: ▶ RESUMED at {CurrentSpeed}x");
            OnPauseStateChanged?.Invoke(false);
        }

        /// <summary>
        /// Increase game speed
        /// </summary>
        public void SpeedUp()
        {
            if (isPaused)
            {
                Resume();
                return;
            }

            // Find next non-zero speed
            int newIndex = currentSpeedIndex + 1;
            
            // Skip index 0 (pause) when speeding up
            if (newIndex < speedOptions.Length)
            {
                currentSpeedIndex = newIndex;
                ApplyTimeScale();
                
                Debug.Log($"TimeController: ⏩ Speed: {CurrentSpeed}x");
                OnSpeedChanged?.Invoke(CurrentSpeed);
            }
        }

        /// <summary>
        /// Decrease game speed
        /// </summary>
        public void SlowDown()
        {
            if (isPaused) return;

            int newIndex = currentSpeedIndex - 1;
            
            // Don't go below index 1 (normal speed) with slow down
            // Use Pause() explicitly for pausing
            if (newIndex >= 1)
            {
                currentSpeedIndex = newIndex;
                ApplyTimeScale();
                
                Debug.Log($"TimeController: ⏪ Speed: {CurrentSpeed}x");
                OnSpeedChanged?.Invoke(CurrentSpeed);
            }
        }

        /// <summary>
        /// Set to normal speed (1x)
        /// </summary>
        public void SetNormalSpeed()
        {
            isPaused = false;
            currentSpeedIndex = defaultSpeedIndex;
            ApplyTimeScale();
            
            Debug.Log($"TimeController: ▶ Normal speed: {CurrentSpeed}x");
            OnSpeedChanged?.Invoke(CurrentSpeed);
            OnPauseStateChanged?.Invoke(false);
        }

        /// <summary>
        /// Set speed by index
        /// </summary>
        public void SetSpeedIndex(int index)
        {
            if (index < 0 || index >= speedOptions.Length) return;

            currentSpeedIndex = index;
            
            if (speedOptions[index] == 0f)
            {
                Pause();
            }
            else
            {
                isPaused = false;
                ApplyTimeScale();
                OnSpeedChanged?.Invoke(CurrentSpeed);
                OnPauseStateChanged?.Invoke(false);
            }
        }

        /// <summary>
        /// Set speed directly
        /// </summary>
        public void SetSpeed(float speed)
        {
            // Find closest speed option
            int closestIndex = 1;
            float closestDiff = float.MaxValue;

            for (int i = 0; i < speedOptions.Length; i++)
            {
                float diff = Mathf.Abs(speedOptions[i] - speed);
                if (diff < closestDiff)
                {
                    closestDiff = diff;
                    closestIndex = i;
                }
            }

            SetSpeedIndex(closestIndex);
        }

        /// <summary>
        /// Cycle through speed options (for UI button)
        /// </summary>
        public void CycleSpeed()
        {
            if (isPaused)
            {
                Resume();
                return;
            }

            // Cycle through non-zero speeds
            int newIndex = currentSpeedIndex + 1;
            
            // Wrap around, skipping 0 (pause)
            if (newIndex >= speedOptions.Length)
            {
                newIndex = 1;
            }

            currentSpeedIndex = newIndex;
            ApplyTimeScale();
            
            Debug.Log($"TimeController: Speed: {CurrentSpeed}x");
            OnSpeedChanged?.Invoke(CurrentSpeed);
        }

        /// <summary>
        /// Get speed label for UI
        /// </summary>
        public string GetSpeedLabel()
        {
            if (isPaused) return "⏸";
            
            return CurrentSpeed switch
            {
                0f => "⏸",
                1f => "▶",
                2f => "▶▶",
                3f => "▶▶▶",
                _ => $"{CurrentSpeed}x"
            };
        }

        #endregion

        private void ApplyTimeScale()
        {
            if (!isPaused)
            {
                Time.timeScale = speedOptions[currentSpeedIndex];
            }
        }

        private void OnDestroy()
        {
            // Reset time scale when destroyed
            Time.timeScale = 1f;
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            // Pause game when app loses focus (mobile)
            if (pauseStatus && !isPaused)
            {
                Pause();
            }
        }
    }
}
