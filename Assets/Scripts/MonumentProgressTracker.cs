using System.Collections.Generic;
using UnityEngine;

// Tiene il conteggio totale dei pezzi piazzati correttamente (su tutti i PlaceholderSlot
// della scena) e fa partire un dialogo tra Vitruvio e l'Imperatore solo al raggiungimento
// delle soglie definite in milestoneSet, non ad ogni singolo pezzo.
public class MonumentProgressTracker : MonoBehaviour
{
    [SerializeField] private ArchitectDialoguePanel dialoguePanel;
    [SerializeField] private MonumentMilestoneSet milestoneSet;

    private int filledCount;
    private readonly HashSet<int> triggeredMilestones = new HashSet<int>();

    private void Start()
    {
        foreach (PlaceholderSlot slot in FindObjectsByType<PlaceholderSlot>(FindObjectsSortMode.None))
        {
            slot.OnSlotFilled += HandleSlotFilled;
        }
    }

    private void HandleSlotFilled(PlaceholderSlot slot)
    {
        if (milestoneSet == null)
        {
            return;
        }

        filledCount++;

        Milestone[] milestones = milestoneSet.milestones;
        for (int i = 0; i < milestones.Length; i++)
        {
            if (filledCount >= milestones[i].piecesRequired && triggeredMilestones.Add(i))
            {
                dialoguePanel.PlayDialogue(milestones[i].dialogue);
                DialogueLog.AddEntry(milestones[i]);
                return;
            }
        }
    }
}
