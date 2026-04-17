using System;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace TowerDefenseTK
{
    /// <summary>
    /// Controls game time speed - pause, normal, fast forward.
    /// Compatible with both the legacy Input Manager and the new Input System package.
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
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb == null) return;

            if (kb[ToKey(pauseKey)].wasPressedThisFrame)      TogglePause();
            if (kb[ToKey(speedUpKey)].wasPressedThisFrame)    SpeedUp();
            if (kb[ToKey(slowDownKey)].wasPressedThisFrame)   SlowDown();
            if (kb[ToKey(normalSpeedKey)].wasPressedThisFrame) SetNormalSpeed();
#else
            if (Input.GetKeyDown(pauseKey))      TogglePause();
            if (Input.GetKeyDown(speedUpKey))    SpeedUp();
            if (Input.GetKeyDown(slowDownKey))   SlowDown();
            if (Input.GetKeyDown(normalSpeedKey)) SetNormalSpeed();
#endif
        }

#if ENABLE_INPUT_SYSTEM
        /// <summary>Maps legacy KeyCode values to new Input System Key equivalents.</summary>
        private static Key ToKey(KeyCode kc)
        {
            return kc switch
            {
                KeyCode.Space        => Key.Space,
                KeyCode.Return       => Key.Enter,
                KeyCode.Escape       => Key.Escape,
                KeyCode.Tab          => Key.Tab,
                KeyCode.Backspace    => Key.Backspace,
                KeyCode.Delete       => Key.Delete,
                KeyCode.LeftArrow    => Key.LeftArrow,
                KeyCode.RightArrow   => Key.RightArrow,
                KeyCode.UpArrow      => Key.UpArrow,
                KeyCode.DownArrow    => Key.DownArrow,
                KeyCode.Home         => Key.Home,
                KeyCode.End          => Key.End,
                KeyCode.PageUp       => Key.PageUp,
                KeyCode.PageDown     => Key.PageDown,
                KeyCode.LeftShift    => Key.LeftShift,
                KeyCode.RightShift   => Key.RightShift,
                KeyCode.LeftControl  => Key.LeftCtrl,
                KeyCode.RightControl => Key.RightCtrl,
                KeyCode.LeftAlt      => Key.LeftAlt,
                KeyCode.RightAlt     => Key.RightAlt,
                KeyCode.A => Key.A,  KeyCode.B => Key.B,  KeyCode.C => Key.C,
                KeyCode.D => Key.D,  KeyCode.E => Key.E,  KeyCode.F => Key.F,
                KeyCode.G => Key.G,  KeyCode.H => Key.H,  KeyCode.I => Key.I,
                KeyCode.J => Key.J,  KeyCode.K => Key.K,  KeyCode.L => Key.L,
                KeyCode.M => Key.M,  KeyCode.N => Key.N,  KeyCode.O => Key.O,
                KeyCode.P => Key.P,  KeyCode.Q => Key.Q,  KeyCode.R => Key.R,
                KeyCode.S => Key.S,  KeyCode.T => Key.T,  KeyCode.U => Key.U,
                KeyCode.V => Key.V,  KeyCode.W => Key.W,  KeyCode.X => Key.X,
                KeyCode.Y => Key.Y,  KeyCode.Z => Key.Z,
                KeyCode.Alpha0 => Key.Digit0, KeyCode.Alpha1 => Key.Digit1,
                KeyCode.Alpha2 => Key.Digit2, KeyCode.Alpha3 => Key.Digit3,
                KeyCode.Alpha4 => Key.Digit4, KeyCode.Alpha5 => Key.Digit5,
                KeyCode.Alpha6 => Key.Digit6, KeyCode.Alpha7 => Key.Digit7,
                KeyCode.Alpha8 => Key.Digit8, KeyCode.Alpha9 => Key.Digit9,
                KeyCode.F1  => Key.F1,  KeyCode.F2  => Key.F2,  KeyCode.F3  => Key.F3,
                KeyCode.F4  => Key.F4,  KeyCode.F5  => Key.F5,  KeyCode.F6  => Key.F6,
                KeyCode.F7  => Key.F7,  KeyCode.F8  => Key.F8,  KeyCode.F9  => Key.F9,
                KeyCode.F10 => Key.F10, KeyCode.F11 => Key.F11, KeyCode.F12 => Key.F12,
                KeyCode.Comma        => Key.Comma,
                KeyCode.Period       => Key.Period,
                KeyCode.Slash        => Key.Slash,
                KeyCode.Semicolon    => Key.Semicolon,
                KeyCode.Quote        => Key.Quote,
                KeyCode.LeftBracket  => Key.LeftBracket,
                KeyCode.RightBracket => Key.RightBracket,
                KeyCode.Backslash    => Key.Backslash,
                KeyCode.Minus        => Key.Minus,
                KeyCode.Equals       => Key.Equals,
                KeyCode.BackQuote    => Key.Backquote,
                _ => Key.None
            };
        }
#endif

        #region Public Methods

        /// <summary>Toggle between paused and playing</summary>
        public void TogglePause()
        {
            if (isPaused) Resume();
            else          Pause();
        }

        /// <summary>Pause the game</summary>
        public void Pause()
        {
            isPaused = true;
            Time.timeScale = 0f;
            Debug.Log("TimeController: PAUSED");
            OnPauseStateChanged?.Invoke(true);
        }

        /// <summary>Resume the game at current speed</summary>
        public void Resume()
        {
            isPaused = false;
            ApplyTimeScale();
            Debug.Log($"TimeController: RESUMED at {CurrentSpeed}x");
            OnPauseStateChanged?.Invoke(false);
        }

        /// <summary>Increase game speed</summary>
        public void SpeedUp()
        {
            if (isPaused) { Resume(); return; }

            int newIndex = currentSpeedIndex + 1;
            if (newIndex < speedOptions.Length)
            {
                currentSpeedIndex = newIndex;
                ApplyTimeScale();
                Debug.Log($"TimeController: Speed Up: {CurrentSpeed}x");
                OnSpeedChanged?.Invoke(CurrentSpeed);
            }
        }

        /// <summary>Decrease game speed</summary>
        public void SlowDown()
        {
            if (isPaused) return;

            int newIndex = currentSpeedIndex - 1;
            if (newIndex >= 1)
            {
                currentSpeedIndex = newIndex;
                ApplyTimeScale();
                Debug.Log($"TimeController: Speed Down: {CurrentSpeed}x");
                OnSpeedChanged?.Invoke(CurrentSpeed);
            }
        }

        /// <summary>Set to normal speed (1x)</summary>
        public void SetNormalSpeed()
        {
            isPaused = false;
            currentSpeedIndex = defaultSpeedIndex;
            ApplyTimeScale();
            Debug.Log($"TimeController: Normal speed: {CurrentSpeed}x");
            OnSpeedChanged?.Invoke(CurrentSpeed);
            OnPauseStateChanged?.Invoke(false);
        }

        /// <summary>Set speed by index</summary>
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

        /// <summary>Set speed directly</summary>
        public void SetSpeed(float speed)
        {
            int closestIndex = 1;
            float closestDiff = float.MaxValue;
            for (int i = 0; i < speedOptions.Length; i++)
            {
                float diff = Mathf.Abs(speedOptions[i] - speed);
                if (diff < closestDiff) { closestDiff = diff; closestIndex = i; }
            }
            SetSpeedIndex(closestIndex);
        }

        /// <summary>Cycle through speed options (for UI button)</summary>
        public void CycleSpeed()
        {
            if (isPaused) { Resume(); return; }

            int newIndex = currentSpeedIndex + 1;
            if (newIndex >= speedOptions.Length) newIndex = 1;

            currentSpeedIndex = newIndex;
            ApplyTimeScale();
            Debug.Log($"TimeController: Speed: {CurrentSpeed}x");
            OnSpeedChanged?.Invoke(CurrentSpeed);
        }

        /// <summary>Get speed label for UI</summary>
        public string GetSpeedLabel()
        {
            if (isPaused) return "|| Paused";
            return CurrentSpeed switch
            {
                0f => "Paused",
                1f => ">",
                2f => ">>",
                3f => ">>>",
                _ => $"{CurrentSpeed}x"
            };
        }

        #endregion

        private void ApplyTimeScale()
        {
            if (!isPaused)
                Time.timeScale = speedOptions[currentSpeedIndex];
        }

        private void OnDestroy()
        {
            Time.timeScale = 1f;
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus && !isPaused)
                Pause();
        }
    }
}
