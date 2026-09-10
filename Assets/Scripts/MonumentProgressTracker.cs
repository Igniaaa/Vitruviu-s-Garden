using System.Collections.Generic;
using UnityEngine;

// Tiene il conteggio totale dei pezzi piazzati correttamente (su tutti i PlaceholderSlot
// della scena) e fa partire un dialogo tra Vitruvio e l'Imperatore solo al raggiungimento
// delle soglie definite in milestoneSet, non ad ogni singolo pezzo.
//
// Gestisce anche la sequenza costruttiva: gli slot sono raggruppati per PlaceholderSlot.BuildPhase
// (0 = fondamenta, 1 = colonne, ...) e solo quelli della fase corrente restano sbloccati
// (vedi PlaceholderSlot.SetUnlocked). Una fase si considera completa quando tutti i suoi
// slot sono pieni, e solo allora si sblocca la successiva — così l'ordine di costruzione è
// imposto qui, non solo suggerito dal level design.
public class MonumentProgressTracker : MonoBehaviour
{
    [SerializeField] private ArchitectDialoguePanel dialoguePanel;
    [SerializeField] private MonumentMilestoneSet milestoneSet;

    private int filledCount;
    private int nextMilestoneIndex;

    private readonly Dictionary<int, List<PlaceholderSlot>> slotsByPhase = new Dictionary<int, List<PlaceholderSlot>>();
    private int currentPhase;
    private int maxPhase;

    private void Start()
    {
        foreach (PlaceholderSlot slot in FindObjectsByType<PlaceholderSlot>(FindObjectsSortMode.None))
        {
            slot.OnSlotFilled += HandleSlotFilled;

            if (!slotsByPhase.TryGetValue(slot.BuildPhase, out List<PlaceholderSlot> slotsInPhase))
            {
                slotsInPhase = new List<PlaceholderSlot>();
                slotsByPhase[slot.BuildPhase] = slotsInPhase;
            }

            slotsInPhase.Add(slot);

            if (slot.BuildPhase > maxPhase)
            {
                maxPhase = slot.BuildPhase;
            }
        }

        AdvancePhaseIfComplete();
    }

    private void HandleSlotFilled(PlaceholderSlot slot)
    {
        filledCount++;

        if (milestoneSet != null)
        {
            Milestone[] milestones = milestoneSet.milestones;

            // Controlla solo la prossima milestone in ordine, mai le altre: così una soglia
            // impostata per errore più bassa di quella precedente (es. milestone 2 con
            // piecesRequired minore della 1) non può farla scattare prima del suo turno.
            if (nextMilestoneIndex < milestones.Length && filledCount >= milestones[nextMilestoneIndex].piecesRequired)
            {
                dialoguePanel.PlayDialogue(milestones[nextMilestoneIndex].dialogue);
                DialogueLog.AddEntry(milestones[nextMilestoneIndex]);
                nextMilestoneIndex++;
            }
        }

        AdvancePhaseIfComplete();
    }

    // Avanza la fase corrente finché quella presente risulta completa (o non ha slot), poi
    // applica lo sblocco risultante a tutti gli slot: un while, non un if, per saltare in un
    // colpo solo eventuali fasi vuote invece di fermarsi lì.
    private void AdvancePhaseIfComplete()
    {
        while (currentPhase <= maxPhase && IsPhaseComplete(currentPhase))
        {
            currentPhase++;
        }

        UpdatePhaseLocks();
    }

    private bool IsPhaseComplete(int phase)
    {
        if (!slotsByPhase.TryGetValue(phase, out List<PlaceholderSlot> slots))
        {
            return true;
        }

        foreach (PlaceholderSlot slot in slots)
        {
            if (!slot.IsFilled)
            {
                return false;
            }
        }

        return true;
    }

    private void UpdatePhaseLocks()
    {
        foreach (KeyValuePair<int, List<PlaceholderSlot>> entry in slotsByPhase)
        {
            bool unlocked = entry.Key <= currentPhase;
            foreach (PlaceholderSlot slot in entry.Value)
            {
                slot.SetUnlocked(unlocked);
            }
        }
    }
}
