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

    // Estratto del De Architectura legato a questa milestone, mostrato insieme al dialogo
    // nella schermata del diario.
    [TextArea(2, 6)] public string deArchitecturaExcerpt;
}

// Una voce del diario dei dialoghi: il dialogo mostrato per una milestone raggiunta, insieme
// all'estratto del De Architectura associato. DialogueLog la crea quando la milestone scatta;
// la schermata dedicata la legge per presentare lo scambio e l'estratto.
[System.Serializable]
public struct DialogueLogEntry
{
    public string milestoneLabel;
    public DialogueLine[] dialogue;
    public string deArchitecturaExcerpt;

    public DialogueLogEntry(string milestoneLabel, DialogueLine[] dialogue, string deArchitecturaExcerpt)
    {
        this.milestoneLabel = milestoneLabel;
        this.dialogue = dialogue;
        this.deArchitecturaExcerpt = deArchitecturaExcerpt;
    }
}
