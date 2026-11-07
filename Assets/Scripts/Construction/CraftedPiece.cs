using Basilica.Crafting;
using UnityEngine;

// Tracks unplaced output across all workbenches; filled slots account for placed pieces.
[DisallowMultipleComponent]
public sealed class CraftedPiece : MonoBehaviour
{
    public CraftingRecipe Recipe { get; private set; }
    public DraggableObject Piece { get; private set; }
    public void Initialize(CraftingRecipe recipe, DraggableObject piece)
    { Recipe = recipe; Piece = piece; }
}
