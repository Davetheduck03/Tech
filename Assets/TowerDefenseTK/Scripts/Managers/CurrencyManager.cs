using System;
using UnityEngine;

public class CurrencyManager : MonoBehaviour
{
    public static CurrencyManager Instance;

    public static event Action<int> OnCurrencyChanged;

    [SerializeField] private int startingCurrency = 1500;

    private int currentCurrency;
    public int CurrentCurrency => currentCurrency;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        currentCurrency = startingCurrency;
        OnCurrencyChanged?.Invoke(currentCurrency);
    }

    public bool CanAfford(int amount)
    {
        return currentCurrency >= amount;
    }

    public bool TrySpend(int amount)
    {
        if (!CanAfford(amount)) return false;

        currentCurrency -= amount;
        OnCurrencyChanged?.Invoke(currentCurrency);
        return true;
    }

    public void Add(int amount)
    {
        currentCurrency += amount;
        OnCurrencyChanged?.Invoke(currentCurrency);
    }

    public void Set(int amount)
    {
        currentCurrency = amount;
        OnCurrencyChanged?.Invoke(currentCurrency);
    }
}