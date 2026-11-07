using System;
using System.Linq;
using Basilica.Crafting;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public sealed class ConstructionMaterialRow : MonoBehaviour
{
    [Serializable]
    public sealed class Purchase
    {
        [Min(1)] public int quantity = 1;
        public Button button;
        public TMP_Text label;
        [NonSerialized] public UnityAction handler;
    }

    public MaterialDefinition material;
    public TMP_Text nameText, stockText;
    public Purchase[] purchases = Array.Empty<Purchase>();
    private ConstructionSystem system;

    public void Bind(ConstructionSystem owner)
    {
        system = owner;
        foreach (var purchase in purchases)
        {
            if (purchase.button == null) continue;
            if (purchase.handler != null) purchase.button.onClick.RemoveListener(purchase.handler);
            var option = purchase;
            purchase.handler = () => system.Buy(material, option.quantity);
            purchase.button.onClick.AddListener(purchase.handler);
        }
    }

    public void Refresh()
    {
        bool offered = material != null && system.Materials.Contains(material);
        gameObject.SetActive(offered);
        if (!offered) return;
        nameText.text = material.displayName;
        stockText.text = "Posseduti: " + system.Inventory.Count(material) + "    |    " + material.unitPrice + " monete / unità";
        foreach (var purchase in purchases)
        {
            long price = (long)purchase.quantity * material.unitPrice;
            if (purchase.label != null) purchase.label.text = "+" + purchase.quantity + "  /  " + price + " m";
            if (purchase.button != null) purchase.button.interactable = purchase.quantity > 0 && material.unitPrice > 0 && price <= system.Inventory.Coins;
        }
    }
}
