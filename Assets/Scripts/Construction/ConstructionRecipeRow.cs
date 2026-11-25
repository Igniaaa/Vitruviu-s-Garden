using System.Linq;
using Basilica.Crafting;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class ConstructionRecipeRow : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public CraftingRecipe recipe;
    public ConstructionPieceCategory category;
    public Image silhouette;
    public TMP_Text nameText, costText, statusText, actionText;
    public Button craftButton;
    public Color availableColor = new Color(.36f, .81f, .64f);
    public Color blockedColor = new Color(.91f, .68f, .32f);
    public Color craftedSilhouetteColor = new Color(.36f, .81f, .64f);
    private ConstructionSystem system;
    private ConstructionUI owner;
    private string availabilityText;
    private Color availabilityColor;

    public void Bind(ConstructionSystem owner, ConstructionUI ui)
    {
        system = owner;
        this.owner = ui;
        craftButton.onClick.RemoveListener(Craft);
        craftButton.onClick.AddListener(Craft);
    }

    public void Refresh() => Refresh(category);

    public void Refresh(ConstructionPieceCategory selectedCategory)
    {
        bool offered = recipe != null && system.Recipes.Contains(recipe) && category == selectedCategory;
        gameObject.SetActive(offered);
        if (!offered) return;
        silhouette.color = system.HasCrafted(recipe) ? craftedSilhouetteColor : Color.white;
        nameText.text = recipe.displayName;
        costText.text = recipe.costs == null ? "Ricetta non configurata" : string.Join("    ", recipe.costs.Select(c =>
            c.material != null ? c.material.displayName + " " + system.Inventory.Count(c.material) + "/" + c.quantity : "Materiale mancante"));
        bool can = system.CanCraft(recipe, out string reason);
        availabilityText = can ? "Ancora da produrre: " + system.Remaining(recipe) : reason;
        availabilityColor = can ? availableColor : blockedColor;
        statusText.text = availabilityText;
        statusText.color = availabilityColor;
        craftButton.interactable = can;
        actionText.text = "CREA  " + recipe.minimumHits + "/" + recipe.noteCount;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (recipe != null) owner?.ShowRecipeTooltip(recipe, eventData.position);
    }

    public void OnPointerExit(PointerEventData eventData) => owner?.HideRecipeTooltip();

    private void OnDisable() => owner?.HideRecipeTooltip();

    private void Craft() => system.BeginCraft(recipe);
}
