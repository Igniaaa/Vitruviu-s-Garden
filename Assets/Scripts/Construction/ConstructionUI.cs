using System;
using Basilica.Crafting;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Static graphics are authored under Canvas/Construction UI; this controller binds
// them and creates the transient recipe tooltip while the pointer is over a recipe.
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
    [SerializeField] private TMP_Text coinCounter;
    [SerializeField] private TMP_Text rhythmSummary, rhythmMessage, score;
    [SerializeField] private TMP_Text completedTitle, completedSummary, completedMessage;
    [SerializeField] private TMP_Text failedTitle, failedSummary, failedMessage;

    [Header("Tooltip ricette")]
    [SerializeField] private Vector2 recipeTooltipSize = new Vector2(300, 300);
    [SerializeField] private Color recipeTooltipBackground = new Color(.08f, .12f, .13f, .98f);
    [SerializeField] private Color recipeTooltipTextColor = new Color(.94f, .91f, .82f);

    [Header("Catalogo dei pezzi")]
    [SerializeField] private TMP_Text categoryTitle;
    [SerializeField] private ScrollRect recipeScroll;
    [SerializeField] private ConstructionPieceCategory selectedCategory;

    [Header("Pulsanti esistenti")]
    [SerializeField] private Button[] closeButtons = Array.Empty<Button>();
    [SerializeField] private Button cancelButton, backButton, storageButton;

    [Header("Ritmo: elementi esistenti e parametri")]
    [SerializeField] private RectTransform hitLine, hitWindow;
    [SerializeField] private Image[] notes = Array.Empty<Image>();
    [SerializeField] private Image[] tally = Array.Empty<Image>();
    [SerializeField, Min(1)] private float noteSpeed = 340;
    [SerializeField] private CanvasGroup rhythmGroup;
    [SerializeField] private Color pendingColor = new Color(.64f, .7f, .68f);
    [SerializeField] private Color hitColor = new Color(.36f, .81f, .64f);
    [SerializeField] private Color missColor = new Color(.95f, .4f, .34f);

    private ConstructionSystem system;
    private ConstructionMaterialRow[] materialRows;
    private ConstructionBackpackUpgradeRow[] backpackUpgradeRows;
    private ConstructionRecipeRow[] recipeRows;
    private ConstructionCategoryButton[] categoryButtons;
    private int lastWorkbenchPhase = -1;
    private CraftingRecipe rhythmRecipe;
    private GameObject currentScreen;
    private bool visible, suspended;
    private RectTransform recipeTooltip;
    private TMP_Text recipeTooltipText;
    private RectTransform canvasRect;
    private Canvas uiCanvas;

    public GameObject CurrentScreen => currentScreen;
    public ConstructionPieceCategory SelectedCategory => selectedCategory;

    private void OnEnable()
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            var child = transform.GetChild(i);
            if (child.name.StartsWith("Tooltip pezzo", StringComparison.Ordinal))
                child.gameObject.SetActive(false);
        }
    }

    public bool IsConfigured => screensRoot != null && shopScreen != null && workbenchScreen != null &&
        rhythmScreen != null && completedScreen != null && failedScreen != null &&
        shopSummary != null && shopMessage != null && workbenchMessage != null &&
        rhythmSummary != null && rhythmMessage != null && score != null && completedTitle != null &&
        completedSummary != null && completedMessage != null && failedTitle != null && failedSummary != null &&
        failedMessage != null && coinCounter != null && hitLine != null && hitWindow != null && notes.Length >= 64 && tally.Length >= 64 &&
        storageButton != null && cancelButton != null && backButton != null;

    public Sprite GetPieceIcon(DraggableObject piece)
    {
        if (piece == null || recipeRows == null) return null;
        var craftedPiece = piece.GetComponent<CraftedPiece>();
        CraftingRecipe recipe = craftedPiece != null ? craftedPiece.Recipe : null;
        for (int i = 0; i < recipeRows.Length; i++)
        {
            var row = recipeRows[i];
            if (row == null || row.recipe == null) continue;
            if (recipe != null && row.recipe == recipe && row.silhouette != null) return row.silhouette.sprite;
            var prefabPiece = row.recipe.piecePrefab != null ? row.recipe.piecePrefab.GetComponent<DraggableObject>() : null;
            if (prefabPiece != null && prefabPiece.PieceId == piece.PieceId && row.silhouette != null)
                return row.silhouette.sprite;
        }
        return null;
    }

    public void Initialize(ConstructionSystem owner)
    {
        if (!IsConfigured) throw new InvalidOperationException("Collega le schermate e gli elementi di Construction UI nel Canvas.");
        system = owner;
        EnsureRecipeTooltip();
        materialRows = shopScreen.GetComponentsInChildren<ConstructionMaterialRow>(true);
        backpackUpgradeRows = shopScreen.GetComponentsInChildren<ConstructionBackpackUpgradeRow>(true);
        recipeRows = workbenchScreen.GetComponentsInChildren<ConstructionRecipeRow>(true);
        foreach (var row in materialRows) row.Bind(owner);
        foreach (var row in backpackUpgradeRows) row.Bind(owner);
        foreach (var row in recipeRows) row.Bind(owner, this);
        categoryButtons = workbenchScreen.GetComponentsInChildren<ConstructionCategoryButton>(true);
        int visibleCategoryIndex = 0;
        foreach (var button in categoryButtons)
        {
            button.Bind(this);
            bool visible = button.category != ConstructionPieceCategory.Walls;
            button.gameObject.SetActive(visible);
            if (!visible) continue;

            var rect = button.transform as RectTransform;
            if (rect != null)
            {
                var position = rect.anchoredPosition;
                position.y = -visibleCategoryIndex * (rect.rect.height + 10f);
                rect.anchoredPosition = position;
            }
            visibleCategoryIndex++;
        }
        lastWorkbenchPhase = -1;
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
        shopSummary.text = "Acquista risorse e potenzia qui lo zaino fino a 3 slot.";
        shopMessage.text = system.Message ?? "";
        foreach (var row in materialRows) row.Refresh();
        foreach (var row in backpackUpgradeRows) row.Refresh();
    }

    public void ShowWorkbench()
    {
        Show(workbenchScreen);
        if (workbenchSummary != null)
            workbenchSummary.text = "Scegli il pezzo da creare con i materiali acquistati.";
        workbenchMessage.text = system.Message ?? "";
        bool phaseChanged = lastWorkbenchPhase != system.CurrentPhase;
        if (phaseChanged) selectedCategory = ConstructionPieceCategories.ForPhase(system.CurrentPhase);
        lastWorkbenchPhase = system.CurrentPhase;
        RefreshCatalog(phaseChanged);
    }

    public void SelectCategory(ConstructionPieceCategory category)
    {
        bool changed = selectedCategory != category;
        selectedCategory = category;
        RefreshCatalog(changed);
    }

    private void RefreshCatalog(bool resetScroll)
    {
        if (recipeRows == null) return;
        foreach (var row in recipeRows) row.Refresh(selectedCategory);
        foreach (var button in categoryButtons) button.Refresh(selectedCategory);
        if (categoryTitle != null) categoryTitle.text = ConstructionPieceCategories.Title(selectedCategory).ToUpperInvariant();
        if (resetScroll && recipeScroll != null)
        {
            Canvas.ForceUpdateCanvases();
            recipeScroll.StopMovement();
            recipeScroll.verticalNormalizedPosition = 1;
        }
    }

    public void ShowRhythm(CraftingRecipe recipe, RhythmSequence sequence)
    {
        if (sequence.NoteTimes.Length > notes.Length || sequence.NoteTimes.Length > tally.Length)
            throw new InvalidOperationException("Il Canvas non contiene abbastanza note e giudizi per questa ricetta.");
        rhythmRecipe = recipe;
        if (rhythmGroup != null) rhythmGroup.alpha = 1;
        cancelButton.interactable = true;
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
        string summary = "Note corrette: " + hits + " / " + total;
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

    public void BeginRhythmFinish(RhythmSequence sequence)
    {
        UpdateRhythm(sequence, sequence.NoteTimes[sequence.NoteTimes.Length - 1]);
        cancelButton.interactable = false;
        rhythmMessage.text = "Sequenza completata · risultati in arrivo…";
    }

    public void UpdateRhythmFinish(float progress)
    {
        if (rhythmGroup != null) rhythmGroup.alpha = 1 - Mathf.SmoothStep(0, 1, Mathf.Clamp01(progress));
    }

    private void Show(GameObject selected)
    {
        if (selected != workbenchScreen) HideRecipeTooltip();
        currentScreen = selected;
        foreach (var screen in new[] { shopScreen, workbenchScreen, rhythmScreen, completedScreen, failedScreen })
            screen.SetActive(screen == selected);
        visible = true;
        screensRoot.SetActive(!suspended);
    }

    private void LateUpdate()
    {
        if (recipeTooltip != null && recipeTooltip.gameObject.activeSelf)
            PositionRecipeTooltip(Input.mousePosition);
        if (coinCounter != null && system != null)
        {
            string balance = system.Inventory.Coins.ToString();
            if (coinCounter.text != balance) coinCounter.text = balance;
        }
        if (visible && !suspended && currentScreen == completedScreen)
            storageButton.interactable = system != null && system.CanStoreCreatedPiece;
        if (visible && !suspended && currentScreen == shopScreen && backpackUpgradeRows != null)
            foreach (var row in backpackUpgradeRows) row.Refresh();
    }

    public void SetSuspended(bool value)
    {
        suspended = value;
        if (suspended) HideRecipeTooltip();
        if (screensRoot != null) screensRoot.SetActive(visible && !suspended);
    }

    public void Hide()
    {
        HideRecipeTooltip();
        visible = false;
        currentScreen = null;
        if (screensRoot != null) screensRoot.SetActive(false);
    }

    public void ShowRecipeTooltip(CraftingRecipe recipe, Vector2 screenPosition)
    {
        if (recipe == null || currentScreen != workbenchScreen || suspended) return;
        if (string.IsNullOrWhiteSpace(recipe.description))
        {
            HideRecipeTooltip();
            return;
        }
        EnsureRecipeTooltip();

        recipeTooltipText.text = recipe.description;
        recipeTooltip.gameObject.SetActive(true);
        recipeTooltip.SetAsLastSibling();
        PositionRecipeTooltip(screenPosition);
    }

    public void HideRecipeTooltip()
    {
        if (recipeTooltip != null) recipeTooltip.gameObject.SetActive(false);
    }

    private void EnsureRecipeTooltip()
    {
        uiCanvas = GetComponent<Canvas>();
        canvasRect = transform as RectTransform;
        if (recipeTooltip != null)
        {
            HideDuplicateRecipeTooltips();
            return;
        }

        var existingTooltip = transform.Find("Tooltip pezzo");
        if (existingTooltip != null)
        {
            recipeTooltip = existingTooltip as RectTransform;
            recipeTooltipText = existingTooltip.GetComponentInChildren<TMP_Text>(true);
            if (recipeTooltip != null && recipeTooltipText != null)
            {
                recipeTooltip.sizeDelta = recipeTooltipSize;
                var existingImage = recipeTooltip.GetComponent<Image>();
                if (existingImage != null) existingImage.raycastTarget = false;
                recipeTooltipText.raycastTarget = false;
                HideDuplicateRecipeTooltips();
                return;
            }
            recipeTooltip = null;
            recipeTooltipText = null;
        }

        var panel = new GameObject("Tooltip pezzo", typeof(RectTransform), typeof(Image), typeof(Outline));
        panel.layer = gameObject.layer;
        panel.transform.SetParent(transform, false);
        recipeTooltip = panel.GetComponent<RectTransform>();
        recipeTooltip.anchorMin = recipeTooltip.anchorMax = new Vector2(0, 1);
        recipeTooltip.pivot = new Vector2(0, 1);
        recipeTooltip.sizeDelta = recipeTooltipSize;
        var background = panel.GetComponent<Image>();
        background.color = recipeTooltipBackground;
        background.raycastTarget = false;
        var outline = panel.GetComponent<Outline>();
        outline.effectColor = new Color(.91f, .68f, .32f, 1);
        outline.effectDistance = new Vector2(2, -2);
        outline.useGraphicAlpha = true;

        var textObject = new GameObject("Dettagli", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.layer = gameObject.layer;
        textObject.transform.SetParent(panel.transform, false);
        var textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(16, 16);
        textRect.offsetMax = new Vector2(-16, -16);
        recipeTooltipText = textObject.GetComponent<TextMeshProUGUI>();
        recipeTooltipText.font = TMP_Settings.defaultFontAsset;
        recipeTooltipText.fontSize = 18;
        recipeTooltipText.color = recipeTooltipTextColor;
        recipeTooltipText.alignment = TextAlignmentOptions.TopLeft;
        recipeTooltipText.textWrappingMode = TextWrappingModes.Normal;
        recipeTooltipText.raycastTarget = false;

        panel.SetActive(false);
        HideDuplicateRecipeTooltips();
    }

    private void HideDuplicateRecipeTooltips()
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            var child = transform.GetChild(i);
            if (child != recipeTooltip && child.name.StartsWith("Tooltip pezzo", StringComparison.Ordinal))
                child.gameObject.SetActive(false);
        }
    }

    private void PositionRecipeTooltip(Vector2 screenPosition)
    {
        if (recipeTooltip == null || canvasRect == null) return;
        Camera eventCamera = uiCanvas != null && uiCanvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? uiCanvas.worldCamera
            : null;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPosition, eventCamera, out var localPoint))
            return;
        recipeTooltip.anchoredPosition = localPoint - new Vector2(canvasRect.rect.xMin, canvasRect.rect.yMax);
    }

    private void Close() => system.Close();
    private void Cancel() => system.CancelCraft();
    private void Back() => system.ShowMenu();
    private void Store() => system.StoreCreatedPiece();
}
