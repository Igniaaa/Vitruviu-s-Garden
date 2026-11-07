using System;
using Basilica.Crafting;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// All graphics are authored and saved under Canvas/Construction UI. This controller
// only binds data, actions and visibility; it never builds or restyles the interface.
public sealed class ConstructionUI : MonoBehaviour
{
    [Header("Schermate salvate nel Canvas")]
    [SerializeField] private GameObject screensRoot;
    [SerializeField] private GameObject shopScreen;
    [SerializeField] private GameObject workbenchScreen;
    [SerializeField] private GameObject rhythmScreen;
    [SerializeField] private GameObject completedScreen;
    [SerializeField] private GameObject failedScreen;

    [Header("Testi dinamici")]
    [SerializeField] private TMP_Text shopSummary, shopMessage;
    [SerializeField] private TMP_Text workbenchSummary, workbenchMessage;
    [SerializeField] private TMP_Text rhythmSummary, rhythmMessage, score;
    [SerializeField] private TMP_Text completedTitle, completedSummary, completedMessage;
    [SerializeField] private TMP_Text failedTitle, failedSummary, failedMessage;

    [Header("Pulsanti esistenti")]
    [SerializeField] private Button[] closeButtons = Array.Empty<Button>();
    [SerializeField] private Button cancelButton, backButton, storageButton;

    [Header("Ritmo: elementi esistenti e parametri")]
    [SerializeField] private RectTransform hitLine, hitWindow;
    [SerializeField] private Image[] notes = Array.Empty<Image>();
    [SerializeField] private Image[] tally = Array.Empty<Image>();
    [SerializeField, Min(1)] private float noteSpeed = 220;
    [SerializeField] private Color pendingColor = new Color(.64f, .7f, .68f);
    [SerializeField] private Color hitColor = new Color(.36f, .81f, .64f);
    [SerializeField] private Color missColor = new Color(.95f, .4f, .34f);

    private ConstructionSystem system;
    private ConstructionMaterialRow[] materialRows;
    private ConstructionRecipeRow[] recipeRows;
    private CraftingRecipe rhythmRecipe;
    private GameObject currentScreen;
    private bool visible, suspended;

    public GameObject CurrentScreen => currentScreen;
    public bool IsConfigured => screensRoot != null && shopScreen != null && workbenchScreen != null &&
        rhythmScreen != null && completedScreen != null && failedScreen != null &&
        shopSummary != null && shopMessage != null && workbenchSummary != null && workbenchMessage != null &&
        rhythmSummary != null && rhythmMessage != null && score != null && completedTitle != null &&
        completedSummary != null && completedMessage != null && failedTitle != null && failedSummary != null &&
        failedMessage != null && hitLine != null && hitWindow != null && notes.Length >= 64 && tally.Length >= 64 &&
        storageButton != null && cancelButton != null && backButton != null;

    public void Initialize(ConstructionSystem owner)
    {
        if (!IsConfigured) throw new InvalidOperationException("Collega le schermate e gli elementi di Construction UI nel Canvas.");
        system = owner;
        materialRows = shopScreen.GetComponentsInChildren<ConstructionMaterialRow>(true);
        recipeRows = workbenchScreen.GetComponentsInChildren<ConstructionRecipeRow>(true);
        foreach (var row in materialRows) row.Bind(owner);
        foreach (var row in recipeRows) row.Bind(owner);
        foreach (var button in closeButtons)
        {
            if (button == null) continue;
            button.onClick.RemoveListener(Close);
            button.onClick.AddListener(Close);
        }
        cancelButton.onClick.RemoveListener(Cancel);
        cancelButton.onClick.AddListener(Cancel);
        backButton.onClick.RemoveListener(Back);
        backButton.onClick.AddListener(Back);
        storageButton.onClick.RemoveListener(Store);
        storageButton.onClick.AddListener(Store);
        Hide();
    }

    public void ShowShop()
    {
        Show(shopScreen);
        shopSummary.text = "Credito di cantiere: " + system.Inventory.Coins + " monete\nAcquista le risorse, poi lavorale al banco.";
        shopMessage.text = system.Message ?? "";
        foreach (var row in materialRows) row.Refresh();
    }

    public void ShowWorkbench()
    {
        Show(workbenchScreen);
        workbenchSummary.text = "Fase costruttiva: " + (system.CurrentPhase + 1) + "    |    Credito: " + system.Inventory.Coins +
            " monete\nScegli il pezzo da creare con i materiali acquistati.";
        workbenchMessage.text = system.Message ?? "";
        foreach (var row in recipeRows) row.Refresh();
    }

    public void ShowRhythm(CraftingRecipe recipe, RhythmSequence sequence)
    {
        if (sequence.NoteTimes.Length > notes.Length || sequence.NoteTimes.Length > tally.Length)
            throw new InvalidOperationException("Il Canvas non contiene abbastanza note e giudizi per questa ricetta.");
        rhythmRecipe = recipe;
        Show(rhythmScreen);
        rhythmSummary.text = recipe.displayName + "    |    " + recipe.bpm + " BPM\nPremi SPAZIO quando una nota attraversa la linea dorata.";
        rhythmMessage.text = "Perfetto: tutte le note corrette. Riuscita: almeno " + recipe.minimumHits + " su " + recipe.noteCount + ".";
        for (int i = 0; i < notes.Length; i++) notes[i].gameObject.SetActive(i < sequence.NoteTimes.Length);
        for (int i = 0; i < tally.Length; i++) tally[i].gameObject.SetActive(i < sequence.NoteTimes.Length);
        // Width represents the current recipe tolerance; the authored height and styling stay intact.
        hitWindow.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, recipe.hitWindow * noteSpeed * 2);
        var windowPosition = hitWindow.anchoredPosition;
        windowPosition.x = hitLine.anchoredPosition.x;
        hitWindow.anchoredPosition = windowPosition;
        UpdateRhythm(sequence, -.15);
    }

    public void UpdateRhythm(RhythmSequence sequence, double elapsed)
    {
        if (currentScreen != rhythmScreen || sequence == null || rhythmRecipe == null) return;
        score.text = elapsed < 0 ? "Preparati…" : elapsed < sequence.NoteTimes[0] - rhythmRecipe.hitWindow
            ? "Preparati  ·  " + Mathf.CeilToInt((float)(sequence.NoteTimes[0] - elapsed))
            : "Corretti: " + sequence.Hits + " / " + sequence.NoteTimes.Length + "    Errori: " + sequence.Errors;
        for (int i = 0; i < sequence.NoteTimes.Length; i++)
        {
            notes[i].gameObject.SetActive(sequence.Judgments[i] == 0);
            var position = notes[i].rectTransform.anchoredPosition;
            position.x = hitLine.anchoredPosition.x + (float)(sequence.NoteTimes[i] - elapsed) * noteSpeed;
            notes[i].rectTransform.anchoredPosition = position;
            tally[i].color = sequence.Judgments[i] > 0 ? hitColor : sequence.Judgments[i] < 0 ? missColor : pendingColor;
        }
    }

    public void ShowResult(CraftOutcome outcome, int hits, int total, DraggableObject piece = null)
    {
        bool succeeded = (outcome == CraftOutcome.NormalSuccess || outcome == CraftOutcome.PerfectSuccess) && piece != null;
        Show(succeeded ? completedScreen : failedScreen);
        string summary = "Note corrette: " + hits + " / " + total + "    |    Credito: " + system.Inventory.Coins + " monete";
        if (succeeded)
        {
            completedTitle.text = outcome == CraftOutcome.PerfectSuccess ? "Lavorazione perfetta" : "Pezzo completato";
            completedSummary.text = summary;
            completedMessage.text = system.Message ?? "";
            storageButton.interactable = system.CanStoreCreatedPiece;
        }
        else
        {
            failedTitle.text = outcome == CraftOutcome.TechnicalAbort ? "Lavorazione interrotta" : "Lavorazione non riuscita";
            failedSummary.text = summary;
            failedMessage.text = system.Message ?? "";
        }
    }

    private void Show(GameObject selected)
    {
        currentScreen = selected;
        foreach (var screen in new[] { shopScreen, workbenchScreen, rhythmScreen, completedScreen, failedScreen })
            screen.SetActive(screen == selected);
        visible = true;
        screensRoot.SetActive(!suspended);
    }

    private void LateUpdate()
    {
        if (visible && !suspended && currentScreen == completedScreen)
            storageButton.interactable = system != null && system.CanStoreCreatedPiece;
    }

    public void SetSuspended(bool value)
    {
        suspended = value;
        if (screensRoot != null) screensRoot.SetActive(visible && !suspended);
    }

    public void Hide()
    {
        visible = false;
        currentScreen = null;
        if (screensRoot != null) screensRoot.SetActive(false);
    }

    private void Close() => system.Close();
    private void Cancel() => system.CancelCraft();
    private void Back() => system.ShowMenu();
    private void Store() => system.StoreCreatedPiece();
}