using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Quaderno che raccoglie le voci di DialogueLog una alla volta, come una doppia pagina:
// a sinistra il dialogo della milestone, a destra l'estratto del De Architectura e/o
// l'immagine della pagina corrispondente (se una milestone non ha immagine assegnata,
// lo slot resta semplicemente nascosto).
// L'apertura/chiusura e lo scorrimento pagine passano dalle action OpenJournal,
// NextJournalPage e PreviouseJournalPage lette tramite PlayerInputHandler (Tab, E e Q
// nella action map "Player").
//
// Mentre il quaderno è aperto, IsJournalActive è true: PlayerPieceInteractor e
// FirstPersonController lo controllano per mettere in pausa grab/rilascio, movimento e
// visuale, così E non fa doppio lavoro e il giocatore non si muove leggendo. All'apertura
// il cursore torna visibile e sbloccato (per poter interagire con l'immagine della pagina),
// e viene ri-agganciato alla chiusura.
public class DialogueJournalPanel : MonoBehaviour
{
    public static DialogueJournalPanel Instance { get; private set; }

    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text leftPageText;
    [SerializeField] private TMP_Text rightPageText;
    [SerializeField] private Image rightPageImage;
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
        if (IsJournalActive)
        {
            return true;
        }

        bool dialogueActive = ArchitectDialoguePanel.Instance != null && ArchitectDialoguePanel.Instance.IsDialogueActive;
        bool paused = PauseMenuController.Instance != null && PauseMenuController.Instance.IsPaused;

        return !dialogueActive && !paused;
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

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        ShowCurrentPage();
    }

    private void Close()
    {
        IsJournalActive = false;

        if (panel != null)
        {
            panel.SetActive(false);
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
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

        if (rightPageImage != null)
        {
            rightPageImage.sprite = entry.deArchitecturaPageImage;
            rightPageImage.gameObject.SetActive(entry.deArchitecturaPageImage != null);
        }

        if (pageIndicatorText != null)
        {
            pageIndicatorText.text = $"{currentPage + 1} / {DialogueLog.Entries.Count}";
        }
    }
}
