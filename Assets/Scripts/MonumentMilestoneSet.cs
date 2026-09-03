using UnityEngine;

// Elenco delle soglie di un monumento come asset riutilizzabile: MonumentProgressTracker
// legge le milestone da qui invece di averle incorporate nel componente di scena.
[CreateAssetMenu(fileName = "MonumentMilestoneSet", menuName = "Vitruvio Garden/Monument Milestone Set")]
public class MonumentMilestoneSet : ScriptableObject
{
    public Milestone[] milestones;
}
