using UnityEngine;
using TMPro;

public class CurrencyUI : MonoBehaviour
{
    [SerializeField] private TMP_Text currencyText;

    private void OnEnable()
    {
            CurrencyManager.OnCurrencyChanged += UpdateUI;
    }

    private void OnDisable()
    {
            CurrencyManager.OnCurrencyChanged -= UpdateUI;
    }

    private void UpdateUI(int amount)
    {
        currencyText.text = $"${amount}";
    }
}