using System;
using UnityEngine;

namespace TowerDefenseTK
{
    /// <summary>
    /// Manages player health/lives. 
    /// Initialized by MapLoader with startingLives from MapData (set in Grid Map Editor).
    /// </summary>
    public class PlayerHealthManager : MonoBehaviour
    {
        public static PlayerHealthManager Instance;

        [Header("Current State")]
        [SerializeField] private int maxHealth = 20;
        [SerializeField] private int currentHealth;

        [Header("Debug")]
        [SerializeField] private bool logDamage = true;

        // Events
        public static event Action<int, int> OnHealthChanged; // current, max
        public static event Action OnPlayerDefeated;

        public int CurrentHealth => currentHealth;
        public int MaxHealth => maxHealth;
        public bool IsAlive => currentHealth > 0;
        public bool IsInitialized { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        // NOTE: No Start() auto-initialization
        // MapLoader.InitializePlayerResources() will call Initialize() with mapData.startingLives

        /// <summary>
        /// Initialize health with starting lives from MapData.
        /// Called by MapLoader with mapData.startingLives (set in Grid Map Editor).
        /// </summary>
        public void Initialize(int startingLives)
        {
            maxHealth = startingLives;
            currentHealth = startingLives;
            IsInitialized = true;

            OnHealthChanged?.Invoke(currentHealth, maxHealth);

            Debug.Log($"PlayerHealthManager: Initialized with {currentHealth}/{maxHealth} lives (from MapData)");
        }

        /// <summary>
        /// Take damage from an enemy reaching the exit
        /// </summary>
        public void TakeDamage(int amount)
        {
            if (!IsAlive) return;

            // Auto-initialize if somehow not initialized
            if (!IsInitialized)
            {
                Debug.LogWarning("PlayerHealthManager: TakeDamage called before Initialize! Using default maxHealth.");
                Initialize(maxHealth);
            }

            currentHealth = Mathf.Max(0, currentHealth - amount);

            if (logDamage)
                Debug.Log($"PlayerHealthManager: -{amount} lives! Remaining: {currentHealth}/{maxHealth}");

            OnHealthChanged?.Invoke(currentHealth, maxHealth);

            if (currentHealth <= 0)
            {
                HandleDefeat();
            }
        }

        /// <summary>
        /// Heal/add lives
        /// </summary>
        public void Heal(int amount)
        {
            if (!IsAlive) return;

            int previousHealth = currentHealth;
            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);

            if (currentHealth != previousHealth)
            {
                Debug.Log($"PlayerHealthManager: +{currentHealth - previousHealth} lives! Total: {currentHealth}/{maxHealth}");
                OnHealthChanged?.Invoke(currentHealth, maxHealth);
            }
        }

        /// <summary>
        /// Set health directly
        /// </summary>
        public void SetHealth(int health)
        {
            currentHealth = Mathf.Clamp(health, 0, maxHealth);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);

            if (currentHealth <= 0)
            {
                HandleDefeat();
            }
        }

        /// <summary>
        /// Set max health
        /// </summary>
        public void SetMaxHealth(int newMax, bool healToFull = false)
        {
            maxHealth = Mathf.Max(1, newMax);

            if (healToFull)
            {
                currentHealth = maxHealth;
            }
            else
            {
                currentHealth = Mathf.Min(currentHealth, maxHealth);
            }

            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        private void HandleDefeat()
        {
            Debug.Log("PlayerHealthManager: *** GAME OVER - NO LIVES REMAINING ***");
            OnPlayerDefeated?.Invoke();

            // Pause the game on defeat
            if (TimeController.Instance != null)
            {
                TimeController.Instance.Pause();
            }
        }

        /// <summary>
        /// Reset to max health (for restarting level)
        /// </summary>
        public void Reset()
        {
            currentHealth = maxHealth;
            IsInitialized = true;
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            Debug.Log($"PlayerHealthManager: Reset to {currentHealth}/{maxHealth} lives");
        }
    }
}