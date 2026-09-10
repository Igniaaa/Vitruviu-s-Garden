using System;
using System.Collections.Generic;
using UnityEngine;

// Il posizionamento durante il trascinamento è pilotato dall'esterno (vedi PlayerPieceInteractor:
// raycast dal centro schermo + tasto di interazione), non dal mouse direttamente sull'oggetto.
[RequireComponent(typeof(Collider))]
public class DraggableObject : MonoBehaviour
{
    [SerializeField] private float dragSmoothing = 25f;

    // Velocità massima (m/s) con cui il pezzo insegue il punto di aggancio mentre è in mano:
    // evita uno strattone violento se il target è lontano appena dopo l'aggancio. Deve
    // restare comunque alta: con holdDistance corto, anche un giro normale della visuale
    // sposta il target lungo un arco a diversi m/s, e un limite troppo basso lo fa restare
    // indietro (sembra incastrato mentre in realtà insegue un bersaglio più veloce di lui).
    [SerializeField] private float maxHoldSpeed = 40f;

    // Frazione della dimensione originale a cui rimpicciolisce il pezzo mentre è in mano
    // (1 = nessun rimpicciolimento). Torna alla dimensione originale al rilascio o allo snap.
    [SerializeField] [Range(0.1f, 1f)] private float heldScale = 0.6f;

    // Identifica a quale PlaceholderSlot questo pezzo può agganciarsi (deve combaciare
    // con il pieceId impostato sul placeholder corrispondente).
    [SerializeField] private string pieceId;

    // Tutti i pezzi già agganciati a un PlaceholderSlot: usato per sospendere la collisione
    // tra il pezzo in mano e la struttura già costruita mentre viene trascinato (vedi
    // BeginDrag/EndDrag), evitando che ci si incastri contro un pezzo fermo mentre il
    // giocatore continua a muoversi.
    private static readonly List<DraggableObject> lockedPieces = new List<DraggableObject>();

    private Rigidbody rb;
    private Collider col;
    private Collider playerCollider;
    private Vector3 targetPosition;
    private Vector3 originalScale;
    private Vector3 lockedScale;
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
        col = GetComponent<Collider>();
        originalScale = transform.localScale;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerCollider = player.GetComponent<Collider>();
        }

        // Il pezzo viene spostato per velocità (non teletrasportato) mentre è in mano: la
        // Continuous Dynamic evita che attraversi pavimento/muri sottili se il giocatore
        // lo muove rapidamente. L'interpolazione invece è quella che elimina lo scatto visivo:
        // senza, la posizione resta ferma all'ultimo FixedUpdate fino al successivo, ben
        // visibile su un oggetto tenuto vicino alla camera.
        if (rb != null)
        {
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
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

        SetHeldCollisionIgnored(true);
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

        SetHeldCollisionIgnored(false);

        // Un eventuale PlaceholderSlot in ascolto decide qui se agganciare il pezzo
        // (vedi PlaceholderSlot.HandleDraggableReleased -> LockAt).
        OnReleased?.Invoke(this);
    }

    // Chiamato da un PlaceholderSlot quando il pezzo viene agganciato correttamente: lo
    // blocca in posizione/rotazione esatte e disattiva ulteriori trascinamenti. La scala
    // passata (quella del placeholder) diventa il nuovo target di FixedUpdate, che la
    // raggiunge con lo stesso lerp già usato per lo shrink/ripristino mentre è in mano.
    public void LockAt(Vector3 position, Quaternion rotation, Vector3 scale)
    {
        isDragging = false;
        isLocked = true;
        lockedScale = scale;

        if (rb != null)
        {
            rb.useGravity = false;
            rb.freezeRotation = false;
            rb.isKinematic = true;
        }

        transform.SetPositionAndRotation(position, rotation);

        if (!lockedPieces.Contains(this))
        {
            lockedPieces.Add(this);
        }
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

        SetHeldCollisionIgnored(false);

        transform.SetPositionAndRotation(position, rotation);
    }

    // Sospende o ripristina la collisione tra questo pezzo e tutti quelli già agganciati,
    // oltre che con la capsula del giocatore: chiamato all'inizio/fine del trascinamento.
    // Senza ignorare anche il giocatore, un giro veloce della visuale fa passare il target
    // (che segue la camera) attraverso la capsula stessa: il pezzo, da Rigidbody, non riesce
    // a spingerla via e ci resta incastrato contro finché il giocatore non si allontana.
    private void SetHeldCollisionIgnored(bool ignore)
    {
        if (col == null)
        {
            return;
        }

        if (playerCollider != null)
        {
            Physics.IgnoreCollision(col, playerCollider, ignore);
        }

        foreach (DraggableObject locked in lockedPieces)
        {
            if (locked != this && locked.col != null)
            {
                Physics.IgnoreCollision(col, locked.col, ignore);
            }
        }
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

        // Rimpicciolito mentre è in mano; una volta agganciato prende la scala del
        // placeholder (lockedScale), altrimenti (rilasciato ma non agganciato) torna alla
        // propria dimensione originale.
        Vector3 targetScale;
        if (isLocked)
        {
            targetScale = lockedScale;
        }
        else if (isDragging)
        {
            targetScale = originalScale * heldScale;
        }
        else
        {
            targetScale = originalScale;
        }

        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, t);
    }
}
