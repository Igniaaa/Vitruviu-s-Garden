using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections.Generic;

// Può stare su qualunque GameObject (es. sulla root del player): non richiede più di
// essere collocato sulla camera stessa, per evitare che Unity ne crei una nuova per errore.
// Assegna nell'Inspector la Camera reale (quella con Cinemachine Brain) nel campo Player Camera.
// Rileva col crosshair (centro schermo) i DraggableObject davanti al giocatore: E afferra
// o rilascia il pezzo, i tasti 1-3 lo ripongono o lo estraggono dagli slot dello zaino.
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
    [SerializeField] private string storePiecePromptMessage = "riponi o scambia";
    [Header("Debug")]
    [SerializeField, Min(0.5f)] private float debugSpawnDistance = 1.5f;
    // Raggio dello SphereCast usato per il rilevamento (più tollerante di un raycast sottile
    // al leggero tremolio della mira dato dal rumore/shake della camera) e tempo minimo che deve
    // passare senza un pezzo inquadrato prima di nascondere l'avviso: evita che il piccolo jitter
    // della camera faccia accendere/spegnere di continuo il pannello UI (causa dello stutter).
    [SerializeField] private float aimRadius = 0.15f;
    [SerializeField] private float hidePromptDelay = 0.15f;

    // La lista rappresenta tre slot fissi: null indica uno slot vuoto.
    private readonly List<DraggableObject> carriedPieces = new List<DraggableObject> { null, null, null };
    private DraggableObject heldPiece;
    private BackpackHud backpackHud;
    private int selectedSlotIndex = -1;
    private float lastPieceSeenTime = -1f;
    private bool promptVisible;
    private ConstructionStation lookedAtStation;
    public bool IsHoldingPiece => heldPiece != null && heldPiece.IsDragging;
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

        HandleBackpackSlotKeys();

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
            string slotKeys = BackpackCapacity == 1 ? "1" : "1-" + BackpackCapacity;
            promptText.text = "[" + interactKey + "] " + releasePromptMessage + " · [" + slotKeys + "] " + storePiecePromptMessage;
        }
        if (!IsHoldingPiece && lookedAtStation != null && promptText != null) promptText.text = lookedAtStation.Prompt;

        if (Keyboard.current != null && Keyboard.current[interactKey].wasPressedThisFrame)
        {
            if (!IsHoldingPiece)
            {
                if (lookedAtPiece != null) Grab(lookedAtPiece);
                else if (lookedAtStation != null && ConstructionSystem.Instance != null)
                    ConstructionSystem.Instance.Open(lookedAtStation);
            }
            else if (IsHoldingPiece) ReleaseSelectedPiece();
        }

        if (IsHoldingPiece)
        {
            if (IsRotatingHeldPiece)
            {
                if (heldPiece != null)
                    heldPiece.RotateWhileHeld(playerCamera.transform, Mouse.current.delta.ReadValue(), heldPieceRotationSensitivity);
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
        heldPiece = draggable;
        selectedSlotIndex = -1;
        draggable.BeginDrag();
    }

    private void HandleBackpackSlotKeys()
    {
        if (Keyboard.current == null) return;
        if (Keyboard.current.digit1Key.wasPressedThisFrame || Keyboard.current.numpad1Key.wasPressedThisFrame) UseBackpackSlot(0);
        else if (Keyboard.current.digit2Key.wasPressedThisFrame || Keyboard.current.numpad2Key.wasPressedThisFrame) UseBackpackSlot(1);
        else if (Keyboard.current.digit3Key.wasPressedThisFrame || Keyboard.current.numpad3Key.wasPressedThisFrame) UseBackpackSlot(2);
    }

    private void UseBackpackSlot(int index)
    {
        if (index >= BackpackCapacity) return;

        DraggableObject storedPiece = carriedPieces[index];
        if (IsHoldingPiece)
        {
            DraggableObject pieceToStore = heldPiece;
            if (storedPiece == pieceToStore) return;

            pieceToStore.StoreInBackpack();
            carriedPieces[index] = pieceToStore;
            if (storedPiece != null)
            {
                heldPiece = storedPiece;
                storedPiece.RetrieveFromBackpack(GetHoldPoint());
                selectedSlotIndex = -1;
            }
            else
            {
                heldPiece = null;
                selectedSlotIndex = index;
            }
            return;
        }

        if (storedPiece == null) return;
        carriedPieces[index] = null;
        heldPiece = storedPiece;
        selectedSlotIndex = -1;
        storedPiece.RetrieveFromBackpack(GetHoldPoint());
    }

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
        DraggableObject releasedPiece = heldPiece;
        heldPiece = null;
        selectedSlotIndex = -1;
        if (releasedPiece != null && releasedPiece.IsDragging) releasedPiece.EndDrag();
    }

    private void UpdateCarriedPieceTargets(Vector3 holdPoint)
    {
        if (heldPiece != null) heldPiece.UpdateDragTarget(holdPoint);
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
            promptText.text = isHolding
                ? "[" + interactKey + "] " + releasePromptMessage
                : "[" + interactKey + "] " + grabPromptMessage;
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
