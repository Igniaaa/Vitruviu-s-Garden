using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class ConstructionBackpackUpgradeRow : MonoBehaviour
{
    [SerializeField] private Button purchaseButton;
    [SerializeField] private TMP_Text purchaseLabel;

    private ConstructionSystem system;

    private void Awake()
    {
        if (purchaseButton == null) purchaseButton = GetComponent<Button>();
        if (purchaseLabel == null) purchaseLabel = GetComponentInChildren<TMP_Text>(true);
    }

    public void Bind(ConstructionSystem owner)
    {
        system = owner;
        if (purchaseButton == null) purchaseButton = GetComponent<Button>();
        if (purchaseLabel == null) purchaseLabel = GetComponentInChildren<TMP_Text>(true);
        if (purchaseButton != null)
        {
            purchaseButton.onClick.RemoveListener(Buy);
            purchaseButton.onClick.AddListener(Buy);
        }
        Refresh();
    }

    public void Refresh()
    {
        if (system == null || purchaseButton == null || purchaseLabel == null) return;

        int slots = system.BackpackSlots;
        if (slots >= 3)
        {
            purchaseLabel.text = "ZAINO AL MASSIMO  ·  3/3 SLOT";
            purchaseButton.interactable = false;
            return;
        }

        purchaseLabel.text = "POTENZIA ZAINO  ·  +1 SLOT  ·  " + system.NextBackpackUpgradePrice + " MONETE";
        purchaseButton.interactable = system.CanBuyBackpackUpgrade;
    }

    private void Buy()
    {
        if (system != null) system.BuyBackpackUpgrade();
    }
}
