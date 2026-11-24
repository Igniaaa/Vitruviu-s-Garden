using System;
using System.Collections.Generic;
using System.Linq;
using Basilica.Crafting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

[RequireComponent(typeof(RhythmAudio))]
[DisallowMultipleComponent]
public sealed class ConstructionSystem : MonoBehaviour
{
    public static ConstructionSystem Instance { get; private set; }
    public static bool BlocksInput => Instance != null && Instance.IsOpen || ExtractionMapSystem.IsMapOpen;
    public static int InteractionConsumedFrame { get; private set; } = -1;
    [SerializeField, Min(0)] private int startingCoins = 200;
    [SerializeField] private MaterialDefinition[] materials = Array.Empty<MaterialDefinition>();
    [SerializeField] private CraftingRecipe[] recipes = Array.Empty<CraftingRecipe>();
    [SerializeField] private MonumentProgressTracker progressTracker;
    [SerializeField] private ConstructionUI view;
    [SerializeField] private ConstructionStorageArea storageArea;
    [Header("Potenziamenti dello zaino")]
    [SerializeField, Min(1)] private int secondBackpackSlotPrice = 50;
    [SerializeField, Min(1)] private int thirdBackpackSlotPrice = 100;
    [Header("Chiusura del minigioco ritmico")]
    [SerializeField, Min(0)] private float rhythmResultHold = .45f;
    [SerializeField, Min(.01f)] private float rhythmFadeDuration = .35f;

    public MaterialInventory Inventory { get; private set; }
    public IReadOnlyList<MaterialDefinition> Materials => materials;
    public IReadOnlyList<CraftingRecipe> Recipes => recipes;
    public bool IsOpen => modalOpen;
    public bool IsCrafting => reservation != null;
    public bool IsFinishingRhythm => rhythmFinishing;
    public RhythmSequence Sequence { get; private set; }
    public string Message { get; private set; }
    public CraftOutcome LastOutcome { get; private set; }
    public int CurrentPhase => progressTracker != null ? progressTracker.CurrentPhase : -1;
    public int BackpackSlots { get; private set; } = 1;
    public int NextBackpackUpgradePrice => BackpackSlots == 1 ? secondBackpackSlotPrice :
        BackpackSlots == 2 ? thirdBackpackSlotPrice : 0;
    public bool CanBuyBackpackUpgrade => IsOpen && !IsCrafting && !Paused && station != null &&
        station.kind == ConstructionStationKind.Merchant && BackpackSlots < 3 &&
        Inventory.Coins >= NextBackpackUpgradePrice;
    public Sprite GetBackpackIcon(DraggableObject piece) => view != null ? view.GetPieceIcon(piece) : null;
    public DraggableObject CreatedPiece { get; private set; }
    public bool CanStoreCreatedPiece => IsOpen && !IsCrafting && !Paused && storageArea != null &&
        storageArea.isActiveAndEnabled && CreatedPiece != null && !CreatedPiece.IsLocked && !CreatedPiece.IsDragging;

    private ConstructionStation station;
    private CraftingRecipe activeRecipe;
    private MaterialCost[] activeCosts;
    private CraftingReservation reservation;
    private RhythmAudio rhythmAudio;
    private CraftingResultView resultView;
    private InputAction beatAction;
    private double[] previousPattern;
    private readonly System.Random random = new System.Random();
    private int openedFrame, startedFrame;
    private bool focusPaused;
    private bool modalOpen;
    private bool rhythmFinishing;
    private bool waitingForFirstCraftDialogue;
    private float rhythmFinishElapsed;
    private int rhythmFinishFrame;
    private bool Paused => focusPaused || PauseMenuController.Instance != null && PauseMenuController.Instance.IsPaused;

    private void Awake()
    {
        Instance = this;
        Inventory = new MaterialInventory(Math.Max(0, startingCoins));
        rhythmAudio = GetComponent<RhythmAudio>();
        resultView = GetComponent<CraftingResultView>();
        if (resultView == null) resultView = gameObject.AddComponent<CraftingResultView>();
        if (progressTracker == null) progressTracker = FindFirstObjectByType<MonumentProgressTracker>();
        if (view == null) view = FindFirstObjectByType<ConstructionUI>(FindObjectsInactive.Include);
        if (view == null || !view.IsConfigured)
        {
            Debug.LogError("ConstructionSystem: assegna l'interfaccia salvata sotto Canvas/Construction UI.", this);
            enabled = false;
            return;
        }
        view.Initialize(this);
        beatAction = new InputAction("Construction beat", InputActionType.Button, "<Keyboard>/space");
        beatAction.performed += context =>
        {
            if (!IsCrafting || Paused || rhythmFinishing) return;
            rhythmAudio.SetPaused(false);
            // Input event timestamps preserve precision during a slow rendering frame.
            double eventDsp = AudioSettings.dspTime + context.time - Time.realtimeSinceStartupAsDouble;
            if (Time.frameCount > startedFrame) SubmitBeat(rhythmAudio.ElapsedAt(eventDsp));
        };
    }

    private void OnEnable() => beatAction?.Enable();
    private void Start() => beatAction?.Enable();

    private void Update()
    {
        if (IsOpen && (station == null || view == null || !view.isActiveAndEnabled))
        {
            if (IsCrafting) FinishCraft(CraftOutcome.TechnicalAbort, "Banco rimosso: materiali restituiti interamente.");
            Close();
            return;
        }
        if (reservation != null && (station == null || !station.isActiveAndEnabled || station.outputPoint == null))
        { FinishCraft(CraftOutcome.TechnicalAbort, "Banco non disponibile: materiali restituiti interamente."); }
        rhythmAudio.SetPaused(Paused);
        view.SetSuspended(Paused);
        if (!IsOpen || Paused) return;
        if (IsCrafting)
        {
            if (rhythmFinishing)
            {
                // Input callbacks run before Update: do not count time from
                // before the final input, especially after a slow frame.
                if (Time.frameCount != rhythmFinishFrame) AdvanceRhythmFinish(Time.unscaledDeltaTime);
            }
            else
            {
                Sequence.Advance(rhythmAudio.Elapsed);
                view.UpdateRhythm(Sequence, rhythmAudio.Elapsed);
                if (Sequence.IsComplete) BeginRhythmFinish();
            }
        }
        else if (waitingForFirstCraftDialogue)
        {
            if (ArchitectDialoguePanel.Instance != null && ArchitectDialoguePanel.Instance.IsDialogueActive) return;
            waitingForFirstCraftDialogue = false;
            return;
        }
        else if (Time.frameCount > openedFrame && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            Close();
    }

    private void OnApplicationFocus(bool focus)
    {
        focusPaused = !focus;
        RefreshPauseState();
    }

    public void RefreshPauseState()
    {
        if (rhythmAudio != null) rhythmAudio.SetPaused(Paused);
        if (view != null) view.SetSuspended(Paused);
    }

    public bool Open(ConstructionStation target)
    {
        if (ExtractionMapSystem.IsMapOpen || !isActiveAndEnabled || view == null || !view.isActiveAndEnabled || target == null || !target.isActiveAndEnabled || IsOpen || Paused ||
            InteractionConsumedFrame == Time.frameCount ||
            ArchitectDialoguePanel.Instance != null && ArchitectDialoguePanel.Instance.IsDialogueActive ||
            DialogueJournalPanel.Instance != null && DialogueJournalPanel.Instance.IsJournalActive ||
            FindFirstObjectByType<PlayerPieceInteractor>()?.IsHoldingPiece == true) return false;
        station = target;
        modalOpen = true;
        InteractionConsumedFrame = Time.frameCount;
        openedFrame = Time.frameCount;
        Message = null;
        CreatedPiece = null;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        PlayerInputHandler.Instance?.ResetUIInputModule();
        EventSystem.current?.SetSelectedGameObject(null);
        ShowMenu();
        return true;
    }

    public void ShowMenu()
    {
        if (!IsOpen || IsCrafting) return;
        resultView.Restore();
        if (station.kind == ConstructionStationKind.Merchant) view.ShowShop(); else view.ShowWorkbench();
    }

    public bool Buy(MaterialDefinition material, int quantity)
    {
        if (!IsOpen || station.kind != ConstructionStationKind.Merchant || IsCrafting || Paused || !materials.Contains(material)) return false;
        bool result = Inventory.TryBuy(material, quantity, out string error);
        Message = result ? "Acquistato: " + material.displayName + " × " + quantity + "." : error;
        ShowMenu();
        return result;
    }

    public bool BuyBackpackUpgrade()
    {
        if (!IsOpen || station == null || station.kind != ConstructionStationKind.Merchant || IsCrafting || Paused)
            return false;
        if (BackpackSlots >= 3)
        {
            Message = "Lo zaino ha già tutti e 3 gli slot.";
            ShowMenu();
            return false;
        }

        int price = NextBackpackUpgradePrice;
        if (!Inventory.TrySpendCoins(price))
        {
            Message = "Monete insufficienti per potenziare lo zaino.";
            ShowMenu();
            return false;
        }

        BackpackSlots++;
        Message = "Zaino potenziato: ora ha " + BackpackSlots + " slot.";
        ShowMenu();
        return true;
    }

    public int Remaining(CraftingRecipe recipe)
    {
        if (recipe == null || recipe.piecePrefab == null) return 0;
        var prefab = recipe.piecePrefab.GetComponent<DraggableObject>();
        if (prefab == null) return 0;
        int empty = FindObjectsByType<PlaceholderSlot>(FindObjectsSortMode.None)
            .Count(s => s.BuildPhase == recipe.buildPhase && s.PieceId == prefab.PieceId && !s.IsFilled);
        int available = FindObjectsByType<CraftedPiece>(FindObjectsSortMode.None)
            .Count(p => p.Recipe != null && p.Recipe.buildPhase == recipe.buildPhase && p.Piece != null &&
                p.Piece.PieceId == prefab.PieceId && !p.Piece.IsLocked);
        return Math.Max(0, empty - available);
    }

    public bool HasCrafted(CraftingRecipe recipe) => recipe != null && PlayerPrefs.GetInt(CraftedRecipeKey(recipe), 0) != 0;

    private static string CraftedRecipeKey(CraftingRecipe recipe) => "Vitruvio.CraftedRecipe." + recipe.name;
    private static string CraftedDialogueKey(CraftingRecipe recipe) => "Vitruvio.CraftedDialogue." + recipe.name;

    public bool CanCraft(CraftingRecipe recipe, out string reason)
    {
        reason = null;
        if (recipe == null || !recipes.Contains(recipe)) reason = "Ricetta non disponibile.";
        else if (!recipe.IsValid(out reason)) return false;
        else if (recipe.piecePrefab.GetComponent<DraggableObject>() == null) reason = "Il prefab deve avere DraggableObject.";
        else if (recipe.buildPhase != CurrentPhase) reason = recipe.buildPhase > CurrentPhase ? "Completa prima la fase precedente." : "Fase già completata.";
        else if (Remaining(recipe) == 0) reason = "Tutti i pezzi necessari sono già stati prodotti.";
        else if (station == null || station.kind != ConstructionStationKind.Workbench || station.outputPoint == null) reason = "Banco non disponibile.";
        else if (station.IsOutputBlocked()) reason = "Ritira prima il pezzo dal banco.";
        else if (IsCrafting) reason = "Una lavorazione è già in corso.";
        else
            foreach (var cost in recipe.costs)
                if (Inventory.Count(cost.material) < cost.quantity)
                { reason = "Materiali insufficienti: " + cost.material.displayName + "."; break; }
        return reason == null;
    }

    public bool BeginCraft(CraftingRecipe recipe)
    {
        string error = null;
        if (Paused || !CanCraft(recipe, out error))
        { Message = Paused ? "Riprendi la partita." : error; ShowMenu(); return false; }
        if (!Inventory.TryReserve(recipe, out reservation, out error))
        { Message = error; ShowMenu(); return false; }
        activeRecipe = recipe;
        rhythmFinishing = false;
        rhythmFinishElapsed = 0;
        activeCosts = (MaterialCost[])recipe.costs.Clone();
        try
        {
            var pattern = RhythmSequence.GeneratePattern(recipe.noteCount, recipe.bpm, random, previousPattern);
            previousPattern = pattern;
            Sequence = new RhythmSequence(pattern, recipe.hitWindow, recipe.minimumHits);
            rhythmAudio.Play(pattern, recipe.bpm);
            startedFrame = Time.frameCount;
            view.ShowRhythm(recipe, Sequence);
        }
        catch (Exception exception)
        { FinishCraft(CraftOutcome.TechnicalAbort, "Impossibile avviare la lavorazione: rimborso completo."); Debug.LogException(exception, this); return false; }
        return true;
    }

    public void SubmitBeat(double elapsed)
    {
        if (!IsCrafting || Paused || rhythmFinishing || elapsed < 0) return;
        Sequence.Press(elapsed);
        view.UpdateRhythm(Sequence, elapsed);
        if (Sequence.IsComplete) BeginRhythmFinish();
    }

    private void BeginRhythmFinish()
    {
        if (reservation == null || rhythmFinishing) return;
        rhythmFinishing = true;
        rhythmFinishElapsed = 0;
        rhythmFinishFrame = Time.frameCount;
        view.BeginRhythmFinish(Sequence);
    }

    private void AdvanceRhythmFinish(float seconds)
    {
        if (!rhythmFinishing || reservation == null || Paused) return;
        rhythmFinishElapsed += Mathf.Max(0, seconds);
        float hold = Mathf.Max(0, rhythmResultHold), duration = Mathf.Max(.01f, rhythmFadeDuration);
        float progress = Mathf.Clamp01((rhythmFinishElapsed - hold) / duration);
        view.UpdateRhythmFinish(progress);
        rhythmAudio.SetOutroProgress(progress);
        if (rhythmFinishElapsed >= hold + duration) FinishCraft(Sequence.Outcome);
    }

    public void CancelCraft()
    {
        if (IsCrafting && !Paused && !rhythmFinishing) FinishCraft(CraftOutcome.Failure, "Lavorazione annullata. Nessun pezzo prodotto.");
    }

    private void FinishCraft(CraftOutcome outcome, string detail = null)
    {
        if (reservation == null) return;
        rhythmFinishing = false;
        rhythmFinishElapsed = 0;
        CreatedPiece = null;
        GameObject instance = null;
        bool shouldPlayFirstCraftDialogue = false;
        if (outcome == CraftOutcome.NormalSuccess || outcome == CraftOutcome.PerfectSuccess)
        {
            try
            {
                if (station == null || station.outputPoint == null || station.IsOutputBlocked())
                    throw new InvalidOperationException("Zona di uscita non disponibile.");
                instance = Instantiate(activeRecipe.piecePrefab, station.outputPoint.position, station.outputPoint.rotation);
                var piece = instance.GetComponent<DraggableObject>();
                if (piece == null) throw new InvalidOperationException("Il prefab non è trascinabile.");
                Mesh previewMesh = FindCraftPreviewMesh(activeRecipe, piece.PieceId);
                if (previewMesh != null) piece.SetCraftPreview(previewMesh, activeRecipe.outputMaterial);
                instance.AddComponent<CraftedPiece>().Initialize(activeRecipe, piece);
                CreatedPiece = piece;
                shouldPlayFirstCraftDialogue = PlayerPrefs.GetInt(CraftedDialogueKey(activeRecipe), 0) == 0;
                if (PlayerPrefs.GetInt(CraftedRecipeKey(activeRecipe), 0) == 0)
                {
                    PlayerPrefs.SetInt(CraftedRecipeKey(activeRecipe), 1);
                    PlayerPrefs.Save();
                }
            }
            catch (Exception exception)
            {
                if (instance != null) Destroy(instance);
                CreatedPiece = null;
                outcome = CraftOutcome.TechnicalAbort;
                detail = "Produzione non disponibile: tutti i materiali restituiti. " + exception.Message;
            }
        }
        reservation.Settle(outcome);
        reservation = null;
        rhythmAudio.Stop();
        LastOutcome = outcome;
        int divisor = outcome == CraftOutcome.NormalSuccess ? 1 : 2;
        string used = string.Join(" · ", activeCosts.Select(c => (outcome == CraftOutcome.TechnicalAbort ? 0 : c.quantity / divisor) + " " + c.material.displayName));
        string refund = outcome == CraftOutcome.NormalSuccess ? "Nessun rimborso." :
            "Restituiti: " + string.Join(" · ", activeCosts.Select(c => (outcome == CraftOutcome.TechnicalAbort ? c.quantity : c.quantity / 2) + " " + c.material.displayName));
        Message = (detail ?? (outcome == CraftOutcome.PerfectSuccess ? "Tutte le note corrette! Ritira il pezzo dal banco." :
            outcome == CraftOutcome.NormalSuccess ? "Pezzo pronto: ritiralo dal banco e portalo al cantiere." : "Non hai raggiunto la soglia. Puoi riprovare.")) +
            "\nConsumati: " + used + ".\n" + refund;
        view.ShowResult(outcome, Sequence != null ? Sequence.Hits : 0, activeRecipe.noteCount, CreatedPiece);
        if (CreatedPiece != null)
        {
            resultView.Show(CreatedPiece, station.outputPoint);
            if (shouldPlayFirstCraftDialogue && PlayFirstCraftDialogue(activeRecipe))
            {
                PlayerPrefs.SetInt(CraftedDialogueKey(activeRecipe), 1);
                PlayerPrefs.Save();
            }
        }
        else resultView.Restore();
    }

    private static Mesh FindCraftPreviewMesh(CraftingRecipe recipe, string id)
    {
        PlaceholderSlot[] slots = FindObjectsByType<PlaceholderSlot>(FindObjectsSortMode.None);
        Mesh[] candidates = slots
            .Where(slot => slot != null && !slot.IsFilled && slot.BuildPhase == recipe.buildPhase &&
                slot.PieceId == id && slot.SlotMesh != null)
            .Select(slot => slot.SlotMesh)
            .ToArray();

        return candidates.Length == 0 ? null : candidates[UnityEngine.Random.Range(0, candidates.Length)];
    }

    private bool PlayFirstCraftDialogue(CraftingRecipe recipe)
    {
        ArchitectDialoguePanel dialoguePanel = ArchitectDialoguePanel.Instance;
        if (recipe == null || dialoguePanel == null ||
            string.IsNullOrWhiteSpace(recipe.firstCraftVitruvioLine) ||
            string.IsNullOrWhiteSpace(recipe.firstCraftImperatoreLine)) return false;

        dialoguePanel.PlayDialogue(new[]
        {
            new DialogueLine { speaker = Speaker.Vitruvio, text = recipe.firstCraftVitruvioLine },
            new DialogueLine { speaker = Speaker.Imperatore, text = recipe.firstCraftImperatoreLine }
        });
        waitingForFirstCraftDialogue = dialoguePanel.IsDialogueActive;
        return waitingForFirstCraftDialogue;
    }

    public bool StoreCreatedPiece()
    {
        if (!CanStoreCreatedPiece) return false;
        if (!storageArea.TryStore(CreatedPiece, out string reason))
        {
            Message = reason;
            view.ShowResult(LastOutcome, Sequence != null ? Sequence.Hits : 0, activeRecipe.noteCount, CreatedPiece);
            return false;
        }
        CreatedPiece = null;
        Message = "Pezzo inviato al recinto. Il tavolo è libero: puoi continuare a costruire.";
        ShowMenu();
        return true;
    }

    public void Close()
    {
        if (Paused) return;
        if (IsCrafting) { CancelCraft(); return; }
        resultView.Restore();
        if (modalOpen) InteractionConsumedFrame = Time.frameCount;
        modalOpen = false;
        CreatedPiece = null;
        station = null;
        view.Hide();
        EventSystem.current?.SetSelectedGameObject(null);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void OnDisable()
    {
        beatAction?.Disable();
        reservation?.Settle(CraftOutcome.TechnicalAbort);
        reservation = null;
        rhythmFinishing = false;
        rhythmFinishElapsed = 0;
        if (rhythmAudio != null) rhythmAudio.Stop();
        if (resultView != null) resultView.Restore();
        if (view != null) view.Hide();
        bool wasOpen = modalOpen;
        modalOpen = false;
        CreatedPiece = null;
        station = null;
        if (wasOpen && !(PauseMenuController.Instance != null && PauseMenuController.Instance.IsPaused))
        { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }
    }

    private void OnDestroy()
    {
        beatAction?.Dispose();
        if (Instance == this) Instance = null;
    }
}
