using TMPro;
using UnityEngine;

// Quaderno che raccoglie le voci di DialogueLog una alla volta, come una doppia pagina:
// a sinistra il dialogo della milestone, a destra l'estratto del De Architectura.
// L'apertura/chiusura e lo scorrimento pagine passano dalle action OpenJournal,
// NextJournalPage e PreviouseJournalPage lette tramite PlayerInputHandler (Tab, E e Q
// nella action map "Player").
//
// Mentre il quaderno è aperto, IsJournalActive è true: PlayerPieceInteractor lo controlla
// per mettere in pausa il grab/rilascio dei pezzi, così E non fa doppio lavoro.
public class DialogueJournalPanel : MonoBehaviour
{
    public static DialogueJournalPanel Instance { get; private set; }

    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text leftPageText;
    [SerializeField] private TMP_Text rightPageText;
    [SerializeField] private TMP_Text pageIndicatorText;

    private int currentPage;

    public bool IsJournalActive { get; private set; }

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
        if (PlayerInputHandler.Instance == null)
        {
            return;
        }

        if (PlayerInputHandler.Instance.OpenJournalPressed() && CanToggle())
        {
            if (IsJournalActive)
            {
                Close();
            }
            else
            {
                Open();
            }
        }

        if (!IsJournalActive)
        {
            return;
        }

        if (PlayerInputHandler.Instance.NextJournalPagePressed())
        {
            TurnPage(1);
        }
        else if (PlayerInputHandler.Instance.PreviousJournalPagePressed())
        {
            TurnPage(-1);
        }
    }

    private bool CanToggle()
    {
        return IsJournalActive || ArchitectDialoguePanel.Instance == null || !ArchitectDialoguePanel.Instance.IsDialogueActive;
    }

    private void Open()
    {
        if (DialogueLog.Entries.Count == 0)
        {
            Debug.Log("no dialoge");

            return;
        }

        IsJournalActive = true;
        currentPage = DialogueLog.Entries.Count - 1;

        if (panel != null)
        {
            panel.SetActive(true);
        }

        ShowCurrentPage();
    }

    private void Close()
    {
        IsJournalActive = false;

        if (panel != null)
        {
            panel.SetActive(false);
        }
    }

    private void TurnPage(int direction)
    {
        currentPage = Mathf.Clamp(currentPage + direction, 0, DialogueLog.Entries.Count - 1);
        ShowCurrentPage();
    }

    private void ShowCurrentPage()
    {
        DialogueLogEntry entry = DialogueLog.Entries[currentPage];

        if (leftPageText != null)
        {
            string[] lines = new string[entry.dialogue.Length];
            for (int i = 0; i < entry.dialogue.Length; i++)
            {
                lines[i] = $"{entry.dialogue[i].speaker}: {entry.dialogue[i].text}";
            }

            leftPageText.text = entry.milestoneLabel + "\n\n" + string.Join("\n", lines);
        }

        if (rightPageText != null)
        {
            rightPageText.text = entry.deArchitecturaExcerpt;
        }

        if (pageIndicatorText != null)
        {
            pageIndicatorText.text = $"{currentPage + 1} / {DialogueLog.Entries.Count}";
        }
    }
}
