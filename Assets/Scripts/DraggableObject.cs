using System;
using UnityEngine;

// Il posizionamento durante il trascinamento è pilotato dall'esterno (vedi PlayerPieceInteractor:
// raycast dal centro schermo + tasto di interazione), non dal mouse direttamente sull'oggetto.
[RequireComponent(typeof(Collider))]
public class DraggableObject : MonoBehaviour
{
    [SerializeField] private float dragSmoothing = 25f;

    // Velocità massima (m/s) con cui il pezzo insegue il punto di aggancio mentre è in mano:
    // evita uno strattone violento se il target è lontano appena dopo l'aggancio.
    [SerializeField] private float maxHoldSpeed = 10f;

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

        // Il pezzo viene spostato per velocità (non teletrasportato) mentre è in mano: la
        // Continuous Dynamic evita che attraversi pavimento/muri sottili se il giocatore
        // lo muove rapidamente.
        if (rb != null)
        {
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        }
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

        // Rimane non kinematico apposta: così le collisioni con l'ambiente restano attive
        // mentre viene trascinato (vedi FixedUpdate, che lo spinge per velocità verso il target
        // invece di teletrasportarlo con MovePosition). La rotazione fisica viene invece
        // bloccata: senza, un urto imprime velocità angolare che non si smorza mai da sola e lo
        // fa girare a oltranza, causando urti ripetuti a catena contro lo stesso ostacolo.
        if (rb != null)
        {
            rb.useGravity = false;
            rb.freezeRotation = true;
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

    // Chiamato dal PlayerPieceInteractor quando il giocatore rilascia il pezzo: inerziale,
    // mantiene la velocità con cui lo stava inseguendo mentre era in mano (vedi FixedUpdate).
    public void EndDrag()
    {
        isDragging = false;

        if (rb != null)
        {
            rb.useGravity = true;
            rb.freezeRotation = false;
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
            rb.freezeRotation = false;
            rb.isKinematic = true;
        }

        transform.SetPositionAndRotation(position, rotation);
    }

    // Chiamato da un PieceRespawnVolume quando il pezzo cade sotto la mappa: interrompe un
    // eventuale trascinamento in corso, azzera la velocità residua e lo riporta al punto di spawn.
    public void ResetToSpawn(Vector3 position, Quaternion rotation)
    {
        if (isLocked)
        {
            return;
        }

        isDragging = false;

        if (rb != null)
        {
            rb.useGravity = true;
            rb.isKinematic = false;
            rb.freezeRotation = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
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
                // Spinto per velocità verso il target (non teletrasportato): un ostacolo solido
                // sul percorso lo ferma naturalmente invece di lasciarlo attraversare.
                Vector3 desiredVelocity = (targetPosition - rb.position) * dragSmoothing;
                if (desiredVelocity.sqrMagnitude > maxHoldSpeed * maxHoldSpeed)
                {
                    desiredVelocity = desiredVelocity.normalized * maxHoldSpeed;
                }

                rb.linearVelocity = desiredVelocity;
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
