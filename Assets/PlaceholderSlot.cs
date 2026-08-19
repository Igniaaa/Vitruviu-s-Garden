using System;
using System.Collections.Generic;
using UnityEngine;

// Slot-guida trasparente che mostra dove va posizionato un pezzo del monumento.
// Funziona su qualunque forma: assegna la mesh/primitiva desiderata (cubo, cilindro,
// capitello custom...) come figlio o sullo stesso GameObject con un Renderer + Collider.
//
// Il Collider viene forzato a trigger e serve solo a rilevare l'ingresso/uscita del pezzo
// trascinabile; il Rigidbody kinematico è richiesto perché Unity generi eventi OnTrigger*
// anche se il pezzo trascinato non ha un proprio Rigidbody.
[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class PlaceholderSlot : MonoBehaviour
{
    [SerializeField] private string pieceId;
    [SerializeField] private Color placeholderColor = new Color(0.3f, 0.7f, 1f, 0.35f);
    [SerializeField] private Color highlightColor = new Color(0.3f, 1f, 0.4f, 0.6f);

    private Collider slotCollider;
    private Renderer[] renderers;
    private readonly HashSet<DraggableObject> candidates = new HashSet<DraggableObject>();
    private bool isFilled;

    public bool IsFilled => isFilled;
    public string PieceId => pieceId;

    // Notifica (es. a un GameManager che tiene traccia del monumento) che questo slot è stato completato.
    public event Action<PlaceholderSlot> OnSlotFilled;

    private void Awake()
    {
        slotCollider = GetComponent<Collider>();
        slotCollider.isTrigger = true;

        Rigidbody rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        renderers = GetComponentsInChildren<Renderer>();
        ApplyColor(placeholderColor);
    }

    // Il materiale assegnato deve già usare uno shader con Surface Type "Transparent"
    // (Standard/Fade oppure URP Lit in modalità Transparent), altrimenti l'alpha viene ignorato.
    private void ApplyColor(Color color)
    {
        foreach (Renderer r in renderers)
        {
            r.material.color = color;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isFilled)
        {
            return;
        }

        DraggableObject draggable = other.GetComponent<DraggableObject>();
        if (draggable == null || draggable.IsLocked || draggable.PieceId != pieceId)
        {
            return;
        }

        candidates.Add(draggable);
        draggable.OnReleased += HandleDraggableReleased;
        ApplyColor(highlightColor);
    }

    private void OnTriggerExit(Collider other)
    {
        DraggableObject draggable = other.GetComponent<DraggableObject>();
        if (draggable == null)
        {
            return;
        }

        if (candidates.Remove(draggable))
        {
            draggable.OnReleased -= HandleDraggableReleased;

            if (candidates.Count == 0)
            {
                ApplyColor(placeholderColor);
            }
        }
    }

    private void HandleDraggableReleased(DraggableObject draggable)
    {
        if (isFilled || !candidates.Contains(draggable))
        {
            return;
        }

        Fill(draggable);
    }

    private void Fill(DraggableObject draggable)
    {
        isFilled = true;

        foreach (DraggableObject candidate in candidates)
        {
            candidate.OnReleased -= HandleDraggableReleased;
        }
        candidates.Clear();

        draggable.LockAt(transform.position, transform.rotation);

        foreach (Renderer r in renderers)
        {
            r.enabled = false;
        }
        slotCollider.enabled = false;

        OnSlotFilled?.Invoke(this);
    }
}
