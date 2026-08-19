using System;
using UnityEngine;

// Richiede un Collider (non trigger) per ricevere gli eventi OnMouse*.
// In Project Settings > Player > Active Input Handling deve essere selezionato
// "Input Manager (Old)" oppure "Both", altrimenti OnMouseDown/Drag/Up non scattano.
[RequireComponent(typeof(Collider))]
public class DraggableObject : MonoBehaviour
{
    [SerializeField] private float dragSmoothing = 25f;

    // Identifica a quale PlaceholderSlot questo pezzo può agganciarsi (deve combaciare
    // con il pieceId impostato sul placeholder corrispondente).
    [SerializeField] private string pieceId;

    private Camera mainCamera;
    private Rigidbody rb;
    private Plane dragPlane;
    private Vector3 dragOffset;
    private Vector3 targetPosition;
    private bool isDragging;
    private bool isLocked;

    public bool IsDragging => isDragging;
    public bool IsLocked => isLocked;
    public string PieceId => pieceId;

    // Invocato dopo il rilascio del mouse, così un PlaceholderSlot nelle vicinanze
    // può verificare se agganciare il pezzo.
    public event Action<DraggableObject> OnReleased;

    private void Awake()
    {
        mainCamera = Camera.main;
        rb = GetComponent<Rigidbody>();
    }

    private void OnMouseDown()
    {
        if (isLocked)
        {
            return;
        }

        isDragging = true;

        if (rb != null)
        {
            rb.useGravity = false;
            rb.isKinematic = true;
        }

        // Piano rivolto verso la camera, passante per la posizione attuale dell'oggetto:
        // mantiene costante la profondità durante il trascinamento.
        dragPlane = new Plane(-mainCamera.transform.forward, transform.position);

        if (TryGetPlaneHit(out Vector3 hitPoint))
        {
            dragOffset = transform.position - hitPoint;
        }

        targetPosition = transform.position;
    }

    private void OnMouseDrag()
    {
        if (TryGetPlaneHit(out Vector3 hitPoint))
        {
            targetPosition = hitPoint + dragOffset;
        }
    }

    private void OnMouseUp()
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
        if (!isDragging)
        {
            return;
        }

        float t = Time.fixedDeltaTime * dragSmoothing;

        if (rb != null)
        {
            rb.MovePosition(Vector3.Lerp(rb.position, targetPosition, t));
        }
        else
        {
            transform.position = Vector3.Lerp(transform.position, targetPosition, t);
        }
    }

    private bool TryGetPlaneHit(out Vector3 hitPoint)
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        if (dragPlane.Raycast(ray, out float distance))
        {
            hitPoint = ray.GetPoint(distance);
            return true;
        }

        hitPoint = Vector3.zero;
        return false;
    }
}
