using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

// Pausa la partita: Time.timeScale a 0 congela fisica/movimento (a parte lo sguardo del
// mouse, che FirstPersonController non scala per deltaTime apposta — per questo controlla
// anche IsPaused direttamente). L'action Pause (Esc) passa da PlayerInputHandler come tutto
// il resto dell'input, e resta leggibile anche a timeScale 0 perché l'Input System non
// dipende dal time scale.
public class PauseMenuController : MonoBehaviour
{
    public static PauseMenuController Instance { get; private set; }

    [SerializeField] private string mainMenuSceneName = "MainMenu";

    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject settingsPanel;

    public bool IsPaused { get; private set; }

    private void Awake()
    {
        Instance = this;

        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }

        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }
    }

    private void Update()
    {
        if (PlayerInputHandler.Instance == null)
        {
            return;
        }

        if (PlayerInputHandler.Instance.PausePressed() && CanTogglePause())
        {
            if (IsPaused)
            {
                Resume();
            }
            else
            {
                Pause();
            }
        }
    }

    // Non si può mettere in pausa mentre un dialogo o il quaderno sono a schermo (e
    // viceversa: DialogueJournalPanel.CanToggle già rifiuta di aprirsi durante la pausa),
    // per non sovrapporre due sistemi a schermo intero.
    private bool CanTogglePause()
    {
        if (IsPaused)
        {
            return true;
        }

        bool dialogueActive = ArchitectDialoguePanel.Instance != null && ArchitectDialoguePanel.Instance.IsDialogueActive;
        bool journalActive = DialogueJournalPanel.Instance != null && DialogueJournalPanel.Instance.IsJournalActive;

        return !dialogueActive && !journalActive;
    }

    public void Pause()
    {
        IsPaused = true;
        Time.timeScale = 0f;

        if (pausePanel != null)
        {
            pausePanel.SetActive(true);
        }

        // Il pannello si riattiva spesso proprio mentre il cursore è ancora fermo sopra
        // "Riprendi" (l'ultimo bottone premuto la volta precedente): senza questo reset,
        // l'EventSystem non gli rimanda OnPointerEnter e resta cliccabile ma sordo all'hover.
        PlayerInputHandler.Instance?.ResetUIInputModule();

        // Il click su "Riprendi" lo marca anche come "selezionato" nell'EventSystem (per la
        // navigazione da tastiera/gamepad), stato che il reset sopra non tocca: Selectable
        // controlla "è selezionato?" prima di "ci passa sopra il mouse?", quindi finché resta
        // selezionato mostra il colore Selected invece di Highlighted, sembrando "sordo" al
        // passaggio del mouse anche se l'hover in sé funziona.
        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Resume()
    {
        IsPaused = false;
        Time.timeScale = 1f;

        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }

        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void OpenSettings()
    {
        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }

        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);
        }
    }

    public void CloseSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }

        if (pausePanel != null)
        {
            pausePanel.SetActive(true);
        }
    }

    public void QuitToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }
}
