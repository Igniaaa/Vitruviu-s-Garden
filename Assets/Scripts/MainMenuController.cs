using UnityEngine;
using UnityEngine.SceneManagement;

// Menu principale: i tre pannelli (principale, impostazioni, conferma uscita) li costruisci
// tu in Editor (Canvas/Image/Button) e li assegni nell'Inspector, come per ArchitectDialoguePanel
// e DialogueJournalPanel — questo script si limita ad attivarli/disattivarli e a collegare
// i bottoni. Ogni metodo pubblico va agganciato all'evento OnClick() del bottone corrispondente.
public class MainMenuController : MonoBehaviour
{
    [SerializeField] private string gameSceneName = "GameScene";

    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject quitConfirmationPanel;

    private void Awake()
    {
        ShowMainPanel();
    }

    public void StartGame()
    {
        SceneManager.LoadScene(gameSceneName);
    }

    public void OpenSettings()
    {
        SetActivePanel(settingsPanel);
    }

    public void CloseSettings()
    {
        ShowMainPanel();
    }

    public void RequestQuit()
    {
        SetActivePanel(quitConfirmationPanel);
    }

    public void CancelQuit()
    {
        ShowMainPanel();
    }

    public void ConfirmQuit()
    {
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    private void ShowMainPanel()
    {
        SetActivePanel(mainPanel);
    }

    private void SetActivePanel(GameObject panelToShow)
    {
        if (mainPanel != null) mainPanel.SetActive(panelToShow == mainPanel);
        if (settingsPanel != null) settingsPanel.SetActive(panelToShow == settingsPanel);
        if (quitConfirmationPanel != null) quitConfirmationPanel.SetActive(panelToShow == quitConfirmationPanel);
    }
}
