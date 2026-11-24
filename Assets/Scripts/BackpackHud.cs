using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class BackpackHud : MonoBehaviour
{
    [SerializeField] private Image[] slotFrames = new Image[3];
    [SerializeField] private Image[] slotIcons = new Image[3];
    [SerializeField] private TMP_Text[] slotNumbers = new TMP_Text[3];
    [SerializeField] private Color selectedColor = new Color(.91f, .68f, .32f, 1f);
    [SerializeField] private Color idleColor = new Color(.64f, .7f, .68f, .72f);
    [SerializeField] private Color lockedColor = new Color(.28f, .32f, .32f, .48f);

    private Sprite[] displayedSprites = new Sprite[3];
    private DraggableObject[] displayedPieces = new DraggableObject[3];
    private int displayedSelection = int.MinValue;
    private int displayedCapacity = int.MinValue;

    public void SetVisible(bool value)
    {
        if (gameObject.activeSelf != value) gameObject.SetActive(value);
    }

    public void Refresh(IReadOnlyList<DraggableObject> pieces, int selectedSlot, int capacity)
    {
        capacity = Mathf.Clamp(capacity, 1, 3);
        for (int i = 0; i < 3; i++)
        {
            if (slotFrames == null || i >= slotFrames.Length || slotFrames[i] == null ||
                slotIcons == null || i >= slotIcons.Length || slotIcons[i] == null ||
                slotNumbers == null || i >= slotNumbers.Length || slotNumbers[i] == null) continue;

            DraggableObject piece = pieces != null && i < pieces.Count ? pieces[i] : null;
            Sprite sprite = piece != null && ConstructionSystem.Instance != null
                ? ConstructionSystem.Instance.GetBackpackIcon(piece)
                : null;
            bool slotChanged = displayedPieces[i] != piece || displayedSprites[i] != sprite;
            bool stateChanged = displayedSelection != selectedSlot || displayedCapacity != capacity;

            if (slotChanged)
            {
                displayedPieces[i] = piece;
                displayedSprites[i] = sprite;
                slotIcons[i].sprite = sprite;
                slotIcons[i].enabled = piece != null && sprite != null;
            }

            if (slotChanged || stateChanged)
            {
                bool locked = i >= capacity;
                slotFrames[i].color = locked ? lockedColor : i == selectedSlot ? selectedColor : idleColor;
                slotNumbers[i].color = locked ? lockedColor : i == selectedSlot ? Color.black : Color.white;
            }
        }

        displayedSelection = selectedSlot;
        displayedCapacity = capacity;
    }
}
