using System.Collections.Generic;

// Registro di tutti i dialoghi già mostrati al raggiungimento delle milestone, in ordine
// cronologico. MonumentProgressTracker aggiunge una voce ogni volta che fa partire il
// dialogo di una milestone; la schermata dedicata al diario legge Entries per presentare
// gli scambi tra Vitruvio e l'Imperatore insieme ai relativi estratti del De Architectura.
public static class DialogueLog
{
    private static readonly List<DialogueLogEntry> entries = new List<DialogueLogEntry>();

    public static IReadOnlyList<DialogueLogEntry> Entries => entries;

    public static void AddEntry(Milestone milestone)
    {
        entries.Add(new DialogueLogEntry(milestone.label, milestone.dialogue, milestone.deArchitecturaExcerpt));
    }
}
