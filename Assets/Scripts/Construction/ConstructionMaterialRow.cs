using System.Globalization;
using System.Linq;
using Basilica.Crafting;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class ConstructionMaterialRow : MonoBehaviour
{
    public MaterialDefinition material;
    public TMP_Text nameText, stockText;
    public TMP_InputField quantityInput;
    public Button decreaseButton, increaseButton, purchaseButton;
    public TMP_Text purchaseLabel;
    private ConstructionSystem system;

    public void Bind(ConstructionSystem owner)
    {
        system = owner;
        quantityInput.onValueChanged.RemoveListener(QuantityChanged);
        quantityInput.onValueChanged.AddListener(QuantityChanged);
        decreaseButton.onClick.RemoveListener(Decrease);
        decreaseButton.onClick.AddListener(Decrease);
        increaseButton.onClick.RemoveListener(Increase);
        increaseButton.onClick.AddListener(Increase);
        purchaseButton.onClick.RemoveListener(Buy);
        purchaseButton.onClick.AddListener(Buy);
    }

    public void Refresh()
    {
        bool offered = system != null && material != null && system.Materials.Contains(material);
        gameObject.SetActive(offered);
        if (!offered) return;
        nameText.text = material.displayName;
        stockText.text = "Posseduti: " + system.Inventory.Count(material) + "    |    " + material.unitPrice + " monete / unità";
        bool valid = TryQuantity(out int quantity);
        long price = (long)quantity * material.unitPrice;
        purchaseLabel.text = valid ? "ACQUISTA · " + price + " m" : "ACQUISTA";
        purchaseButton.interactable = valid && CanBuy(quantity);
        decreaseButton.interactable = !valid || quantity > 1;
        increaseButton.interactable = !valid || quantity < int.MaxValue;
    }

    private bool TryQuantity(out int quantity) => int.TryParse(quantityInput.text,
        NumberStyles.None, CultureInfo.InvariantCulture, out quantity) && quantity > 0;

    private bool CanBuy(int quantity) => system != null && material != null && material.unitPrice > 0 &&
        (long)quantity * material.unitPrice <= system.Inventory.Coins &&
        (long)system.Inventory.Count(material) + quantity <= int.MaxValue;

    private void QuantityChanged(string value) => Refresh();
    private void Decrease() => quantityInput.text = (TryQuantity(out int value) ? System.Math.Max(1, value - 1) : 1).ToString(CultureInfo.InvariantCulture);
    private void Increase() => quantityInput.text = (TryQuantity(out int value) ? (int)System.Math.Min(int.MaxValue, (long)value + 1) : 1).ToString(CultureInfo.InvariantCulture);
    private void Buy()
    {
        if (TryQuantity(out int quantity) && CanBuy(quantity)) system.Buy(material, quantity);
    }
}
