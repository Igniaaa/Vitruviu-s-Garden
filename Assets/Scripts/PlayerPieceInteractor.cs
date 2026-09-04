using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

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

    [Header("Avviso a schermo")]
    // GameObject (es. un pannello/testo in un Canvas) mostrato solo quando è possibile interagire.
    [SerializeField] private GameObject interactionPrompt;
    // Opzionale: se assegnato, il testo cambia tra "afferra" e "rilascia" a seconda dello stato.
    // Se lasciato vuoto viene cercato automaticamente un Text tra i figli di interactionPrompt.
    [SerializeField] private TMP_Text promptText;
    [SerializeField] private string grabPromptMessage = "Premi E per afferrare";
    [SerializeField] private string releasePromptMessage = "Premi E per rilasciare";
    // Raggio dello SphereCast usato per il rilevamento (più tollerante di un raycast sottile
    // al leggero tremolio della mira dato dal rumore/shake della camera) e tempo minimo che deve
    // passare senza un pezzo inquadrato prima di nascondere l'avviso: evita che il piccolo jitter
    // della camera faccia accendere/spegnere di continuo il pannello UI (causa dello stutter).
    [SerializeField] private float aimRadius = 0.15f;
    [SerializeField] private float hidePromptDelay = 0.15f;

    private DraggableObject heldPiece;
    private float lastPieceSeenTime = -1f;
    private bool promptVisible;

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
    }

    private void Update()
    {
        if (playerCamera == null)
        {
            return;
        }

        if (ArchitectDialoguePanel.Instance != null && ArchitectDialoguePanel.Instance.IsDialogueActive)
        {
            UpdatePrompt(false, heldPiece != null);
            return;
        }

        if (DialogueJournalPanel.Instance != null && DialogueJournalPanel.Instance.IsJournalActive)
        {
            UpdatePrompt(false, heldPiece != null);
            return;
        }

        DraggableObject lookedAtPiece = heldPiece == null ? FindInteractablePiece() : null;

        if (heldPiece != null || lookedAtPiece != null)
        {
            lastPieceSeenTime = Time.unscaledTime;
        }

        bool visible = heldPiece != null || Time.unscaledTime - lastPieceSeenTime <= hidePromptDelay;
        UpdatePrompt(visible, heldPiece != null);

        if (Keyboard.current != null && Keyboard.current[interactKey].wasPressedThisFrame)
        {
            if (heldPiece != null)
            {
                Release();
            }
            else if (lookedAtPiece != null)
            {
                Grab(lookedAtPiece);
            }
        }

        if (heldPiece != null)
        {
            Vector3 holdPoint = playerCamera.transform.position + playerCamera.transform.forward * holdDistance;
            heldPiece.UpdateDragTarget(holdPoint);
        }
    }

    private DraggableObject FindInteractablePiece()
    {
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);

        // Ignora i collider trigger: i PlaceholderSlot ne hanno sempre uno e non devono
        // intercettare la mira del giocatore al posto del pezzo vero.
        if (!Physics.SphereCast(ray, aimRadius, out RaycastHit hit, interactRange, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            return null;
        }

        DraggableObject draggable = hit.collider.GetComponent<DraggableObject>();
        if (draggable == null || draggable.IsLocked || draggable.IsDragging)
        {
            return null;
        }

        return draggable;
    }

    private void Grab(DraggableObject draggable)
    {
        heldPiece = draggable;
        heldPiece.BeginDrag();
    }

    private void Release()
    {
        heldPiece.EndDrag();
        heldPiece = null;
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
    }
}
