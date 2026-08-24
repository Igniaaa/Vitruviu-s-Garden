using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

// Esegue a schermo uno scambio di battute tra Vitruvio e l'Imperatore, una alla volta
// (si avanza premendo advanceKey). Non decide da sé quando partire: è MonumentProgressTracker
// a chiamare PlayDialogue() al raggiungimento di una soglia di pezzi piazzati.
//
// Mentre il dialogo è a schermo, IsDialogueActive è true: PlayerPieceInteractor lo controlla
// per mettere in pausa il grab/rilascio dei pezzi, così lo stesso tasto non fa doppio lavoro.
public class ArchitectDialoguePanel : MonoBehaviour
{
    public static ArchitectDialoguePanel Instance { get; private set; }

    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text speakerText;
    [SerializeField] private TMP_Text lineText;
    [SerializeField] private Key advanceKey = Key.E;

    private readonly Queue<DialogueLine> queue = new Queue<DialogueLine>();

    public bool IsDialogueActive { get; private set; }

    private void Awake()
    {
        Instance = this;

        if (panel != null)
        {
            panel.SetActive(false);
        }
    }

    private void Update()
    {
        if (!IsDialogueActive)
        {
            return;
        }

        if (Keyboard.current != null && Keyboard.current[advanceKey].wasPressedThisFrame)
        {
            ShowNextLine();
        }
    }

    public void PlayDialogue(DialogueLine[] lines)
    {
        if (lines == null || lines.Length == 0)
        {
            return;
        }

        queue.Clear();
        foreach (DialogueLine line in lines)
        {
            queue.Enqueue(line);
        }

        IsDialogueActive = true;

        if (panel != null)
        {
            panel.SetActive(true);
        }

        ShowNextLine();
    }

    private void ShowNextLine()
    {
        if (queue.Count == 0)
        {
            EndDialogue();
            return;
        }

        DialogueLine line = queue.Dequeue();

        if (speakerText != null)
        {
            speakerText.text = line.speaker.ToString();
        }

        if (lineText != null)
        {
            lineText.text = line.text;
        }
    }

    private void EndDialogue()
    {
        IsDialogueActive = false;

        if (panel != null)
        {
            panel.SetActive(false);
        }
    }
}
