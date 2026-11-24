using Basilica.Crafting;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class ConstructionCategoryButton : MonoBehaviour
{
    public ConstructionPieceCategory category;
    public Button button;
    public Image background;
    public TMP_Text label;
    public Color selectedBackground = new Color(.91f, .68f, .32f);
    public Color idleBackground = new Color(.12f, .16f, .17f);
    public Color selectedText = new Color(.065f, .085f, .09f);
    public Color idleText = new Color(.94f, .91f, .82f);
    private ConstructionUI owner;

    public void Bind(ConstructionUI ui)
    {
        owner = ui;
        button.onClick.RemoveListener(Select);
        button.onClick.AddListener(Select);
    }

    public void Refresh(ConstructionPieceCategory selected)
    {
        bool active = category == selected;
        background.color = active ? selectedBackground : idleBackground;
        label.color = active ? selectedText : idleText;
    }

    private void Select() => owner.SelectCategory(category);
}
