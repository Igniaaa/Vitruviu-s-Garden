using UnityEngine;

// Una singola battuta di uno scambio educativo tra Vitruvio e l'Imperatore, mostrata da
// ArchitectDialoguePanel quando un PlaceholderSlot viene riempito correttamente.
[System.Serializable]
public struct DialogueLine
{
    public Speaker speaker;
    [TextArea(2, 4)] public string text;
}

public enum Speaker
{
    Vitruvio,
    Imperatore
}

// Una fase/traguardo del monumento: al raggiungimento di piecesRequired pezzi piazzati
// correttamente (in totale, non su un singolo placeholder), MonumentProgressTracker fa
// partire questo scambio di battute tramite ArchitectDialoguePanel.
[System.Serializable]
public struct Milestone
{
    // Solo per riconoscere la voce nell'Inspector (es. "Fine colonnato"), non usato in gioco.
    public string label;
    public int piecesRequired;
    public DialogueLine[] dialogue;
}
