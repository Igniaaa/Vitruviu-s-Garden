using System.Linq;
using Basilica.Crafting;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class ConstructionRecipeRow : MonoBehaviour
{
    public CraftingRecipe recipe;
    public TMP_Text nameText, costText, statusText, actionText;
    public Button craftButton;
    public Color availableColor = new Color(.36f, .81f, .64f);
    public Color blockedColor = new Color(.91f, .68f, .32f);
    private ConstructionSystem system;

    public void Bind(ConstructionSystem owner)
    {
        system = owner;
        craftButton.onClick.RemoveListener(Craft);
        craftButton.onClick.AddListener(Craft);
    }

    public void Refresh()
    {
        bool offered = recipe != null && system.Recipes.Contains(recipe);
        gameObject.SetActive(offered);
        if (!offered) return;
        nameText.text = recipe.displayName + "  ·  fase " + (recipe.buildPhase + 1);
        costText.text = recipe.costs == null ? "Ricetta non configurata" : string.Join("    ", recipe.costs.Select(c =>
            c.material != null ? c.material.displayName + " " + system.Inventory.Count(c.material) + "/" + c.quantity : "Materiale mancante"));
        bool can = system.CanCraft(recipe, out string reason);
        statusText.text = can ? "Ancora da produrre: " + system.Remaining(recipe) : reason;
        statusText.color = can ? availableColor : blockedColor;
        craftButton.interactable = can;
        actionText.text = "CREA  " + recipe.minimumHits + "/" + recipe.noteCount;
    }

    private void Craft() => system.BeginCraft(recipe);
}
