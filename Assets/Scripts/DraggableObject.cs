using System;
using UnityEngine;

// Il posizionamento durante il trascinamento è pilotato dall'esterno (vedi PlayerPieceInteractor:
// raycast dal centro schermo + tasto di interazione), non dal mouse direttamente sull'oggetto.
[RequireComponent(typeof(Collider))]
public class DraggableObject : MonoBehaviour
{
    [SerializeField] private float dragSmoothing = 25f;

    // Frazione della dimensione originale a cui rimpicciolisce il pezzo mentre è in mano
    // (1 = nessun rimpicciolimento). Torna alla dimensione originale al rilascio o allo snap.
    [SerializeField] [Range(0.1f, 1f)] private float heldScale = 0.6f;

    // Identifica a quale PlaceholderSlot questo pezzo può agganciarsi (deve combaciare
    // con il pieceId impostato sul placeholder corrispondente).
    [SerializeField] private string pieceId;

    private Rigidbody rb;
    private Vector3 targetPosition;
    private Vector3 originalScale;
    private bool isDragging;
    private bool isLocked;

    public bool IsDragging => isDragging;
    public bool IsLocked => isLocked;
    public string PieceId => pieceId;

    // Invocato al rilascio del pezzo, così un PlaceholderSlot nelle vicinanze
    // può verificare se agganciare il pezzo.
    public event Action<DraggableObject> OnReleased;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        originalScale = transform.localScale;
    }

    // Chiamato dal PlayerPieceInteractor quando il giocatore afferra il pezzo.
    public void BeginDrag()
    {
        if (isLocked)
        {
            return;
        }

        isDragging = true;
        targetPosition = transform.position;

        if (rb != null)
        {
            rb.useGravity = false;
            rb.isKinematic = true;
        }
    }

    // Chiamato ad ogni frame dal PlayerPieceInteractor mentre il pezzo è tenuto in mano,
    // con il punto davanti alla camera verso cui il pezzo deve muoversi.
    public void UpdateDragTarget(Vector3 worldPosition)
    {
        if (!isDragging)
        {
            return;
        }

        targetPosition = worldPosition;
    }

    // Chiamato dal PlayerPieceInteractor quando il giocatore rilascia il pezzo.
    public void EndDrag()
    {
        isDragging = false;

        if (rb != null)
        {
            rb.useGravity = true;
            rb.isKinematic = false;
        }

        // Un eventuale PlaceholderSlot in ascolto decide qui se agganciare il pezzo
        // (vedi PlaceholderSlot.HandleDraggableReleased -> LockAt).
        OnReleased?.Invoke(this);
    }

    // Chiamato da un PlaceholderSlot quando il pezzo viene agganciato correttamente:
    // lo blocca in posizione/rotazione esatte e disattiva ulteriori trascinamenti.
    public void LockAt(Vector3 position, Quaternion rotation)
    {
        isDragging = false;
        isLocked = true;

        if (rb != null)
        {
            rb.useGravity = false;
            rb.isKinematic = true;
        }

        transform.SetPositionAndRotation(position, rotation);
    }

    private void FixedUpdate()
    {
        float t = Time.fixedDeltaTime * dragSmoothing;

        if (isDragging)
        {
            if (rb != null)
            {
                rb.MovePosition(Vector3.Lerp(rb.position, targetPosition, t));
            }
            else
            {
                transform.position = Vector3.Lerp(transform.position, targetPosition, t);
            }
        }

        // Rimpicciolito mentre è in mano; torna alla dimensione originale sia al rilascio
        // libero che allo snap nel placeholder (in entrambi i casi isDragging torna false).
        Vector3 targetScale = isDragging ? originalScale * heldScale : originalScale;
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, t);
    }
}
