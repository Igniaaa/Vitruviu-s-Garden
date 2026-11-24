using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections.Generic;

// Può stare su qualunque GameObject (es. sulla root del player): non richiede più di
// essere collocato sulla camera stessa, per evitare che Unity ne crei una nuova per errore.
// Assegna nell'Inspector la Camera reale (quella con Cinemachine Brain) nel campo Player Camera.
// Rileva col crosshair (centro schermo) i DraggableObject davanti al giocatore: un tasto
// afferra/rilascia il pezzo, che poi segue lo sguardo restando a distanza fissa dalla camera.
public class PlayerPieceInteractor : MonoBehaviour
{
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float interactRange = 3f;
    [SerializeField] private float holdDistance = 1.5f;
    [SerializeField] private Key interactKey = Key.E;

    [Header("Rotazione del pezzo in mano")]
    [SerializeField] private float heldPieceRotationSensitivity = 0.5f;

    [Header("Avviso a schermo")]
    // GameObject (es. un pannello/testo in un Canvas) mostrato solo quando è possibile interagire.
    [SerializeField] private GameObject interactionPrompt;
    [SerializeField] private GameObject rotationPrompt;
    // Opzionale: se assegnato, il testo cambia tra "afferra" e "rilascia" a seconda dello stato.
    // Se lasciato vuoto viene cercato automaticamente un Text tra i figli di interactionPrompt.
    [SerializeField] private TMP_Text promptText;
    [SerializeField] private string grabPromptMessage = "Premi E per afferrare";
    [SerializeField] private string releasePromptMessage = "Premi E per rilasciare";
    [SerializeField] private string addPiecePromptMessage = "Premi E per aggiungere un pezzo allo zaino";
    [SerializeField] private string backpackFullPromptMessage = "Zaino pieno: potenzialo nel negozio o premi E per rilasciare";
    [Header("Debug")]
    [SerializeField, Min(0.5f)] private float debugSpawnDistance = 1.5f;
    // Raggio dello SphereCast usato per il rilevamento (più tollerante di un raycast sottile
    // al leggero tremolio della mira dato dal rumore/shake della camera) e tempo minimo che deve
    // passare senza un pezzo inquadrato prima di nascondere l'avviso: evita che il piccolo jitter
    // della camera faccia accendere/spegnere di continuo il pannello UI (causa dello stutter).
    [SerializeField] private float aimRadius = 0.15f;
    [SerializeField] private float hidePromptDelay = 0.15f;

    private readonly List<DraggableObject> carriedPieces = new List<DraggableObject>();
    private BackpackHud backpackHud;
    private int selectedSlotIndex = -1;
    private float lastPieceSeenTime = -1f;
    private bool promptVisible;
    private ConstructionStation lookedAtStation;
    public bool IsHoldingPiece => carriedPieces.Count > 0;
    private int BackpackCapacity => ConstructionSystem.Instance != null
        ? Mathf.Clamp(ConstructionSystem.Instance.BackpackSlots, 1, 3)
        : 1;
    // Il FirstPersonController legge questo stato prima di applicare l'input della visuale.
    public bool IsRotatingHeldPiece
    {
        get
        {
            if (!IsHoldingPiece || Mouse.current == null || !Mouse.current.rightButton.isPressed)
            {
                return false;
            }

            return !(ArchitectDialoguePanel.Instance != null && ArchitectDialoguePanel.Instance.IsDialogueActive)
                && !(DialogueJournalPanel.Instance != null && DialogueJournalPanel.Instance.IsJournalActive)
                && !(PauseMenuController.Instance != null && PauseMenuController.Instance.IsPaused);
        }
    }

    private void Awake()
    {
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }

        if (promptText == null && interactionPrompt != null)
        {
            promptText = interactionPrompt.GetComponentInChildren<TMP_Text>(true);
        }

        if (interactionPrompt != null)
        {
            interactionPrompt.SetActive(false);
        }

        if (rotationPrompt != null)
        {
            rotationPrompt.SetActive(false);
        }

        backpackHud = FindFirstObjectByType<BackpackHud>(FindObjectsInactive.Include);
    }

    private void Update()
    {
        RefreshBackpackHud();
        if (playerCamera == null)
        {
            return;
        }

        if (ConstructionSystem.BlocksInput || ConstructionSystem.InteractionConsumedFrame == Time.frameCount)
        {
            UpdatePrompt(false, IsHoldingPiece);
            return;
        }

        if (ArchitectDialoguePanel.Instance != null && ArchitectDialoguePanel.Instance.IsDialogueActive)
        {
            UpdatePrompt(false, IsHoldingPiece);
            return;
        }

        if (DialogueJournalPanel.Instance != null && DialogueJournalPanel.Instance.IsJournalActive)
        {
            UpdatePrompt(false, IsHoldingPiece);
            return;
        }

        if (PauseMenuController.Instance != null && PauseMenuController.Instance.IsPaused)
        {
            UpdatePrompt(false, IsHoldingPiece);
            return;
        }

        if (Keyboard.current != null && Keyboard.current.f8Key.wasPressedThisFrame)
        {
            SpawnDebugPiece();
        }

        HandleBackpackSlotSelection();

        lookedAtStation = null;
        DraggableObject lookedAtPiece = FindInteractablePiece();

        if (IsHoldingPiece || lookedAtPiece != null || lookedAtStation != null)
        {
            lastPieceSeenTime = Time.unscaledTime;
        }

        bool visible = IsHoldingPiece || Time.unscaledTime - lastPieceSeenTime <= hidePromptDelay;
        UpdatePrompt(visible, IsHoldingPiece);
        if (visible && promptText != null && IsHoldingPiece)
        {
            int capacity = BackpackCapacity;
            if (lookedAtPiece == null) promptText.text = releasePromptMessage;
            else if (carriedPieces.Count >= capacity) promptText.text = backpackFullPromptMessage;
            else promptText.text = addPiecePromptMessage + " (" + carriedPieces.Count + "/" + capacity + ")";
        }
        if (lookedAtStation != null && promptText != null) promptText.text = lookedAtStation.Prompt;

        if (Keyboard.current != null && Keyboard.current[interactKey].wasPressedThisFrame)
        {
            if (!IsHoldingPiece)
            {
                if (lookedAtPiece != null) Grab(lookedAtPiece);
                else if (lookedAtStation != null && ConstructionSystem.Instance != null)
                    ConstructionSystem.Instance.Open(lookedAtStation);
            }
            else if (lookedAtPiece != null && carriedPieces.Count < BackpackCapacity)
            {
                AddToLoad(lookedAtPiece);
            }
            else if (IsHoldingPiece) ReleaseSelectedPiece();
        }

        if (IsHoldingPiece)
        {
            if (IsRotatingHeldPiece)
            {
                DraggableObject selectedPiece = GetSelectedPiece();
                if (selectedPiece != null)
                    selectedPiece.RotateWhileHeld(playerCamera.transform, Mouse.current.delta.ReadValue(), heldPieceRotationSensitivity);
            }

            Vector3 holdPoint = playerCamera.transform.position + playerCamera.transform.forward * holdDistance;
            UpdateCarriedPieceTargets(holdPoint);
        }

        RefreshBackpackHud();
    }

    private DraggableObject FindInteractablePiece()
    {
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);

        // Ignora i collider trigger: i PlaceholderSlot ne hanno sempre uno e non devono
        // intercettare la mira del giocatore al posto del pezzo vero.
        lookedAtStation = null;
        RaycastHit[] hits = Physics.SphereCastAll(ray, aimRadius, interactRange, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (RaycastHit hit in hits)
        {
            DraggableObject draggable = hit.collider.GetComponentInParent<DraggableObject>();
            if (draggable != null)
            {
                if (carriedPieces.Contains(draggable) || draggable.IsLocked || draggable.IsDragging) continue;
                return draggable;
            }

            ConstructionStation station = hit.collider.GetComponentInParent<ConstructionStation>();
            if (station != null)
            {
                lookedAtStation = station;
                return null;
            }
        }
        return null;
    }

    private void Grab(DraggableObject draggable)
    {
        carriedPieces.Clear();
        carriedPieces.Add(draggable);
        selectedSlotIndex = 0;
        draggable.BeginDrag();
    }

    private void AddToLoad(DraggableObject draggable)
    {
        if (draggable == null || carriedPieces.Contains(draggable)) return;
        DraggableObject currentPiece = GetSelectedPiece();
        if (currentPiece != null) currentPiece.StoreInBackpack();
        carriedPieces.Add(draggable);
        selectedSlotIndex = carriedPieces.Count - 1;
        draggable.BeginDrag();
    }

    private void HandleBackpackSlotSelection()
    {
        if (!IsHoldingPiece || Keyboard.current == null) return;
        if (Keyboard.current.digit1Key.wasPressedThisFrame || Keyboard.current.numpad1Key.wasPressedThisFrame) SelectBackpackSlot(0);
        else if (Keyboard.current.digit2Key.wasPressedThisFrame || Keyboard.current.numpad2Key.wasPressedThisFrame) SelectBackpackSlot(1);
        else if (Keyboard.current.digit3Key.wasPressedThisFrame || Keyboard.current.numpad3Key.wasPressedThisFrame) SelectBackpackSlot(2);
    }

    private void SelectBackpackSlot(int index)
    {
        if (index >= BackpackCapacity || index >= carriedPieces.Count || index == selectedSlotIndex) return;

        DraggableObject currentPiece = GetSelectedPiece();
        if (currentPiece != null) currentPiece.StoreInBackpack();
        selectedSlotIndex = index;
        DraggableObject selectedPiece = GetSelectedPiece();
        if (selectedPiece != null)
            selectedPiece.RetrieveFromBackpack(GetHoldPoint());
    }

    private DraggableObject GetSelectedPiece() =>
        selectedSlotIndex >= 0 && selectedSlotIndex < carriedPieces.Count ? carriedPieces[selectedSlotIndex] : null;

    private Vector3 GetHoldPoint() => playerCamera.transform.position + playerCamera.transform.forward * holdDistance;

    private void SpawnDebugPiece()
    {
        if (playerCamera == null) return;

        PiecePhaseSpawner[] spawners = FindObjectsByType<PiecePhaseSpawner>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        GameObject debugPiecePrefab = null;
        foreach (PiecePhaseSpawner spawner in spawners)
        {
            if (spawner != null && spawner.DebugPiecePrefab != null)
            {
                debugPiecePrefab = spawner.DebugPiecePrefab;
                break;
            }
        }
        if (debugPiecePrefab == null)
        {
            Debug.LogWarning("PlayerPieceInteractor: nessun prefab debug disponibile nel PiecePhaseSpawner.", this);
            return;
        }

        Transform cameraTransform = playerCamera.transform;
        Vector3 spawnPosition = cameraTransform.position + cameraTransform.forward * debugSpawnDistance - cameraTransform.up * 0.25f;
        Quaternion spawnRotation = Quaternion.Euler(0f, cameraTransform.eulerAngles.y, 0f);
        Instantiate(debugPiecePrefab, spawnPosition, spawnRotation);
    }

    private void ReleaseSelectedPiece()
    {
        DraggableObject releasedPiece = GetSelectedPiece();
        if (releasedPiece != null && releasedPiece.IsDragging) releasedPiece.EndDrag();
        if (selectedSlotIndex >= 0 && selectedSlotIndex < carriedPieces.Count)
            carriedPieces.RemoveAt(selectedSlotIndex);

        if (carriedPieces.Count == 0)
        {
            selectedSlotIndex = -1;
            return;
        }

        selectedSlotIndex = Mathf.Min(selectedSlotIndex, carriedPieces.Count - 1);
        DraggableObject nextPiece = GetSelectedPiece();
        if (nextPiece != null) nextPiece.RetrieveFromBackpack(GetHoldPoint());
    }

    private void UpdateCarriedPieceTargets(Vector3 holdPoint)
    {
        DraggableObject selectedPiece = GetSelectedPiece();
        if (selectedPiece != null) selectedPiece.UpdateDragTarget(holdPoint);
    }

    private void RefreshBackpackHud()
    {
        if (backpackHud == null) return;
        bool blocked = ConstructionSystem.BlocksInput ||
            ArchitectDialoguePanel.Instance != null && ArchitectDialoguePanel.Instance.IsDialogueActive ||
            DialogueJournalPanel.Instance != null && DialogueJournalPanel.Instance.IsJournalActive ||
            PauseMenuController.Instance != null && PauseMenuController.Instance.IsPaused;
        backpackHud.SetVisible(!blocked);
        backpackHud.Refresh(carriedPieces, selectedSlotIndex, BackpackCapacity);
    }

    private void UpdatePrompt(bool visible, bool isHolding)
    {
        if (interactionPrompt == null)
        {
            return;
        }

        if (visible != promptVisible)
        {
            promptVisible = visible;
            interactionPrompt.SetActive(visible);
        }

        if (visible && promptText != null)
        {
            promptText.text = isHolding ? releasePromptMessage : grabPromptMessage;
        }

        if (rotationPrompt != null)
        {
            bool showRotationPrompt = visible && isHolding;
            if (rotationPrompt.activeSelf != showRotationPrompt)
            {
                rotationPrompt.SetActive(showRotationPrompt);
            }
        }
    }
}
