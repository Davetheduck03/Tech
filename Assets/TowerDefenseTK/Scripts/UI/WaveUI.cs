using UnityEngine;
using TMPro;
using TowerDefenseTK;

/// <summary>
/// Displays the current wave number and a countdown to the next wave.
///
/// Attach to a Canvas GameObject and wire up the text fields in the Inspector.
///
/// Layout suggestion:
///   WaveUI (this script)
///   ├── WaveLabel     (TMP_Text)  →  "Wave 3"
///   └── StatusLabel   (TMP_Text)  →  "Spawning..." | "Next wave in 5s"
/// </summary>
public class WaveUI : MonoBehaviour
{
    [Header("Text References")]
    [Tooltip("Displays the current wave number, e.g. 'Wave 1'.")]
    [SerializeField] private TMP_Text waveText;

    [Tooltip("Displays spawning status or countdown to next wave.")]
    [SerializeField] private TMP_Text statusText;

    [Header("Labels")]
    [SerializeField] private string wavePrefix = "Wave ";
    [SerializeField] private string spawningLabel = "Spawning...";
    [SerializeField] private string countdownPrefix = "Next wave in ";
    [SerializeField] private string countdownSuffix = "s";

    // ── Runtime state ──────────────────────────────────────────────────────
    private int currentWave = 0;
    private float cooldownRemaining = 0f;
    private bool inCooldown = false;

    // ──────────────────────────────────────────────────────────────────────
    //  Lifecycle
    // ──────────────────────────────────────────────────────────────────────
    private void OnEnable()
    {
        EnemySpawner.OnWaveStarted       += HandleWaveStarted;
        EnemySpawner.OnWaveCooldownStarted += HandleCooldownStarted;
    }

    private void OnDisable()
    {
        EnemySpawner.OnWaveStarted       -= HandleWaveStarted;
        EnemySpawner.OnWaveCooldownStarted -= HandleCooldownStarted;
    }

    private void Update()
    {
        if (!inCooldown) return;

        cooldownRemaining -= Time.deltaTime;

        if (cooldownRemaining <= 0f)
        {
            cooldownRemaining = 0f;
            inCooldown = false;
            SetStatus(spawningLabel);   // next wave is about to fire
        }
        else
        {
            SetStatus($"{countdownPrefix}{Mathf.CeilToInt(cooldownRemaining)}{countdownSuffix}");
        }
    }

    // ──────────────────────────────────────────────────────────────────────
    //  Event handlers
    // ──────────────────────────────────────────────────────────────────────
    private void HandleWaveStarted(int waveNumber)
    {
        currentWave   = waveNumber;
        inCooldown    = false;

        SetWave(waveNumber);
        SetStatus(spawningLabel);
    }

    private void HandleCooldownStarted(int waveNumber, float duration)
    {
        cooldownRemaining = duration;
        inCooldown        = true;

        SetStatus($"{countdownPrefix}{Mathf.CeilToInt(duration)}{countdownSuffix}");
    }

    // ──────────────────────────────────────────────────────────────────────
    //  Helpers
    // ──────────────────────────────────────────────────────────────────────
    private void SetWave(int wave)
    {
        if (waveText != null)
            waveText.text = $"{wavePrefix}{wave}";
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;
    }
}
