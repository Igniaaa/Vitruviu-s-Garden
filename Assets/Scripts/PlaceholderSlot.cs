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
    [SerializeField] private Color wrongColor = new Color(0.3f, 1f, 0.4f, 0.6f);

    // Materiale Unlit (opaco) per i bordi della mesh: rende visibile la separazione tra
    // placeholder che si sovrappongono. Se non assegnato, viene cercato uno shader Unlit di default.
    [SerializeField] private Material edgeMaterial;
    [SerializeField] private Color edgeColor = Color.white;

    private Collider slotCollider;
    private Renderer[] renderers;
    private readonly List<Renderer> wireframeRenderers = new List<Renderer>();
    private readonly HashSet<DraggableObject> candidates = new HashSet<DraggableObject>();
    private readonly HashSet<DraggableObject> wrongCandidates = new HashSet<DraggableObject>();
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
        CreateWireframes();
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

    // Genera, per ogni mesh del placeholder, un mesh "a linee" con gli spigoli del solido
    // (deduplicati) e lo disegna in overlay: rende visibile il contorno anche quando più
    // placeholder trasparenti si sovrappongono, indipendentemente dalla forma assegnata.
    private void CreateWireframes()
    {
        Material material = edgeMaterial;
        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
            {
                Debug.LogWarning("PlaceholderSlot: nessun edgeMaterial assegnato e shader Unlit di default non trovato; i bordi non verranno disegnati.", this);
                return;
            }
            material = new Material(shader);
        }
        material.color = edgeColor;

        foreach (Renderer r in renderers)
        {
            MeshFilter meshFilter = r.GetComponent<MeshFilter>();
            if (meshFilter == null || meshFilter.sharedMesh == null)
            {
                continue;
            }

            GameObject wireframeGo = new GameObject("Wireframe");
            wireframeGo.transform.SetParent(meshFilter.transform, false);

            MeshFilter wireframeFilter = wireframeGo.AddComponent<MeshFilter>();
            wireframeFilter.mesh = BuildEdgeMesh(meshFilter.sharedMesh);

            MeshRenderer wireframeRenderer = wireframeGo.AddComponent<MeshRenderer>();
            wireframeRenderer.sharedMaterial = material;
            wireframeRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            wireframeRenderer.receiveShadows = false;

            wireframeRenderers.Add(wireframeRenderer);
        }
    }

    private static Mesh BuildEdgeMesh(Mesh source)
    {
        var edges = new HashSet<(int a, int b)>();
        int[] triangles = source.triangles;

        for (int i = 0; i < triangles.Length; i += 3)
        {
            AddEdge(edges, triangles[i], triangles[i + 1]);
            AddEdge(edges, triangles[i + 1], triangles[i + 2]);
            AddEdge(edges, triangles[i + 2], triangles[i]);
        }

        int[] lineIndices = new int[edges.Count * 2];
        int index = 0;
        foreach ((int a, int b) in edges)
        {
            lineIndices[index++] = a;
            lineIndices[index++] = b;
        }

        Mesh edgeMesh = new Mesh { name = source.name + "_Wireframe" };
        edgeMesh.vertices = source.vertices;
        edgeMesh.SetIndices(lineIndices, MeshTopology.Lines, 0);
        return edgeMesh;
    }

    private static void AddEdge(HashSet<(int a, int b)> edges, int a, int b)
    {
        if (a > b)
        {
            (a, b) = (b, a);
        }

        edges.Add((a, b));
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isFilled)
        {
            return;
        }

        DraggableObject draggable = other.GetComponent<DraggableObject>();
        if (draggable == null || draggable.IsLocked)
        {
            return;
        }

        if (draggable.PieceId == pieceId)
        {
            candidates.Add(draggable);
            draggable.OnReleased += HandleDraggableReleased;
        }
        else
        {
            wrongCandidates.Add(draggable);
        }

        UpdateVisual();
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
        }

        wrongCandidates.Remove(draggable);

        UpdateVisual();
    }

    // Verde se dentro c'è il pezzo giusto, rosso se ce n'è uno sbagliato, altrimenti colore normale.
    private void UpdateVisual()
    {
        if (candidates.Count > 0)
        {
            ApplyColor(highlightColor);
        }
        else if (wrongCandidates.Count > 0)
        {
            ApplyColor(wrongColor);
        }
        else
        {
            ApplyColor(placeholderColor);
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

        foreach (Renderer r in wireframeRenderers)
        {
            r.enabled = false;
        }

        slotCollider.enabled = false;

        OnSlotFilled?.Invoke(this);
    }
}
