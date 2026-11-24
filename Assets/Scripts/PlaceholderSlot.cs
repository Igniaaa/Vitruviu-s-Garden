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
    private static Material dustMaterial;

    [SerializeField] private string pieceId;

    // Fase costruttiva a cui appartiene (0 = fondamenta, 1 = colonne, ...): MonumentProgressTracker
    // tiene visibili/interagibili solo gli slot della fase corrente, e passa a quella
    // successiva solo quando tutti gli slot della fase presente sono pieni. Più slot possono
    // condividere la stessa fase (si riempiono in un ordine libero tra loro).
    [SerializeField] private int buildPhase;

    [SerializeField] private Color placeholderColor = new Color(0.3f, 0.7f, 1f, 0.35f);
    [SerializeField] private Color highlightColor = new Color(0.3f, 1f, 0.4f, 0.6f);
    [SerializeField] private Color wrongColor = new Color(0.3f, 1f, 0.4f, 0.6f);

    // Materiale Unlit (opaco) per i bordi della mesh: rende visibile la separazione tra
    // placeholder che si sovrappongono. Se non assegnato, viene cercato uno shader Unlit di default.
    [SerializeField] private Material edgeMaterial;
    [SerializeField] private Color edgeColor = Color.white;

    [Header("Nuvola di polvere")]
    [SerializeField, Min(16)] private int dustParticleCount = 300;
    [SerializeField, Min(0.1f)] private float dustRadiusMultiplier = 1.45f;
    [SerializeField, Min(0.01f)] private float dustParticleSizeMin = 0.3f;
    [SerializeField, Min(0.01f)] private float dustParticleSizeMax = 0.48f;
    [SerializeField, Min(0.05f)] private float dustLifetimeMin = 1.05f;
    [SerializeField, Min(0.05f)] private float dustLifetimeMax = 1.55f;
    [SerializeField] private Color dustColor = new Color(0.88f, 0.82f, 0.7f, 1f);
    [SerializeField, Range(0f, 1f)] private float dustPeakOpacity = 0.95f;

    private Collider slotCollider;
    private Renderer[] renderers;
    private MeshFilter slotMeshFilter;
    private readonly List<Renderer> wireframeRenderers = new List<Renderer>();
    private readonly HashSet<DraggableObject> candidates = new HashSet<DraggableObject>();
    private readonly HashSet<DraggableObject> wrongCandidates = new HashSet<DraggableObject>();
    private bool isFilled;
    private bool isUnlocked = true;

    public bool IsFilled => isFilled;
    public bool IsUnlocked => isUnlocked;
    public string PieceId => pieceId;
    public int BuildPhase => buildPhase;

    // Mesh "definitiva" di questo slot: il pezzo che ci si aggancia la adotta al posto della
    // propria (vedi Fill/DraggableObject.LockAt), qualunque forma avesse in mano.
    public Mesh SlotMesh => slotMeshFilter != null ? slotMeshFilter.sharedMesh : null;

    // Notifica (es. a un GameManager che tiene traccia del monumento) che questo slot è stato completato.
    public event Action<PlaceholderSlot> OnSlotFilled;

    private void OnValidate() => FitColliderToMesh();

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) FitColliderToMesh();
    }
#endif

    [ContextMenu("Fit collider to mesh")]
    public void FitColliderToMesh()
    {
        var box = GetComponent<BoxCollider>();
        if (box == null) return;
        MeshFilter source = null;
        foreach (var filter in GetComponentsInChildren<MeshFilter>(true))
        {
            if (filter.sharedMesh == null || filter.name == "Wireframe" ||
                filter.name.StartsWith("Punto di posa", StringComparison.Ordinal)) continue;
            source = filter;
            break;
        }
        if (source == null) return;
        slotMeshFilter = source;
        var bounds = source.sharedMesh.bounds;
        if (source.transform != transform)
        {
            var relative = transform.worldToLocalMatrix * source.transform.localToWorldMatrix;
            var fitted = new Bounds();
            for (int i = 0; i < 8; i++)
            {
                var corner = bounds.center + Vector3.Scale(bounds.extents,
                    new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                var point = relative.MultiplyPoint3x4(corner);
                if (i == 0) fitted = new Bounds(point, Vector3.zero); else fitted.Encapsulate(point);
            }
            bounds = fitted;
        }
        var size = Vector3.Max(bounds.size, Vector3.one * .001f);
        if (box.center != bounds.center) box.center = bounds.center;
        if (box.size != size) box.size = size;
    }

    private void Awake()
    {
        slotCollider = GetComponent<Collider>();
        slotCollider.isTrigger = true;

        Rigidbody rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        renderers = GetComponentsInChildren<Renderer>();

        // Catturata prima di CreateWireframes(): dopo, GetComponentInChildren rischierebbe di
        // trovare il MeshFilter del wireframe generato invece di quello della forma vera.
        slotMeshFilter = GetComponentInChildren<MeshFilter>();
        FitColliderToMesh();

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
        if (isFilled || !isUnlocked || draggable == null || draggable.IsLocked || !candidates.Contains(draggable))
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

        draggable.LockAt(transform.position, transform.rotation, transform.localScale, SlotMesh);
        draggable.transform.SetParent(transform.parent, true);

        SpawnPlacementDust();

        foreach (Renderer r in renderers)
        {
            r.enabled = false;
        }

        foreach (Renderer r in wireframeRenderers)
        {
            r.enabled = false;
        }

        // The locked piece owns the structural collider. Delivery triggers must not
        // become invisible solid boxes at ground level after the mesh is hidden.
        slotCollider.enabled = false;

        OnSlotFilled?.Invoke(this);
    }

    // Una raffica di particelle occupa il volume della mesh, così il pezzo viene
    // coperto per un istante anche se lo slot ha una forma o dimensione insolita.
    private void SpawnPlacementDust()
    {
        Bounds bounds = GetSlotLocalBounds();
        if (bounds.size.sqrMagnitude <= 0f)
        {
            return;
        }

        GameObject dustObject = new GameObject("Placement Dust");
        dustObject.SetActive(false);
        dustObject.transform.SetParent(transform, false);
        dustObject.transform.localPosition = bounds.center;
        dustObject.transform.localRotation = Quaternion.identity;
        dustObject.transform.localScale = Vector3.one;

        ParticleSystem particles = dustObject.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = particles.main;
        float lifetimeMin = Mathf.Max(0.05f, dustLifetimeMin);
        float lifetimeMax = Mathf.Max(lifetimeMin, dustLifetimeMax);
        float sizeMin = Mathf.Max(0.01f, dustParticleSizeMin);
        float sizeMax = Mathf.Max(sizeMin, dustParticleSizeMax);
        int particleCount = Mathf.Max(16, dustParticleCount);
        main.duration = lifetimeMax;
        main.loop = false;
        main.playOnAwake = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(lifetimeMin, lifetimeMax);
        float maxDimension = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
        main.startSize3D = false;
        main.startSize = new ParticleSystem.MinMaxCurve(maxDimension * sizeMin, maxDimension * sizeMax);
        main.startSpeed = new ParticleSystem.MinMaxCurve(maxDimension * 0.1f, maxDimension * 0.22f);
        main.startColor = dustColor;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        main.maxParticles = particleCount;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.enabled = false;

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = Mathf.Max(bounds.extents.magnitude * dustRadiusMultiplier, maxDimension * 0.8f);
        shape.radiusThickness = 0.55f;
        shape.position = Vector3.zero;
        shape.rotation = Vector3.zero;
        shape.scale = Vector3.one;
        shape.randomDirectionAmount = 0.9f;

        ParticleSystem.ColorOverLifetimeModule colorOverLifetime = particles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient fade = new Gradient();
        fade.SetKeys(
            new[]
            {
                new GradientColorKey(dustColor, 0f),
                new GradientColorKey(dustColor * 0.82f, 1f)
            },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(dustPeakOpacity, 0.1f),
                new GradientAlphaKey(dustPeakOpacity * 0.76f, 0.58f),
                new GradientAlphaKey(0f, 1f)
            });
        colorOverLifetime.color = fade;

        ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = particles.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve puffCurve = new AnimationCurve(
            new Keyframe(0f, 0.65f),
            new Keyframe(0.25f, 1f),
            new Keyframe(1f, 1.35f));
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, puffCurve);

        ParticleSystemRenderer particleRenderer = dustObject.GetComponent<ParticleSystemRenderer>();
        particleRenderer.renderMode = ParticleSystemRenderMode.Billboard;
        particleRenderer.alignment = ParticleSystemRenderSpace.View;
        particleRenderer.material = GetDustMaterial();
        particleRenderer.sortingFudge = 1f;

        dustObject.SetActive(true);
        particles.Emit(particleCount);
        particles.Play();
        Destroy(dustObject, lifetimeMax + 0.3f);
    }

    private Bounds GetSlotLocalBounds()
    {
        if (slotMeshFilter == null || slotMeshFilter.sharedMesh == null)
        {
            return new Bounds(Vector3.zero, Vector3.one * 0.5f);
        }

        Bounds meshBounds = slotMeshFilter.sharedMesh.bounds;
        Matrix4x4 toSlotSpace = transform.worldToLocalMatrix * slotMeshFilter.transform.localToWorldMatrix;
        Bounds result = new Bounds(toSlotSpace.MultiplyPoint3x4(meshBounds.center), Vector3.zero);
        for (int i = 0; i < 8; i++)
        {
            Vector3 corner = meshBounds.center + Vector3.Scale(meshBounds.extents,
                new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
            result.Encapsulate(toSlotSpace.MultiplyPoint3x4(corner));
        }

        result.size = Vector3.Max(result.size, Vector3.one * 0.05f);
        return result;
    }

    private static Material GetDustMaterial()
    {
        if (dustMaterial != null)
        {
            return dustMaterial;
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null)
        {
            shader = Shader.Find("Particles/Standard Unlit");
        }
        if (shader == null)
        {
            shader = Shader.Find("Sprites/Default");
        }
        if (shader == null)
        {
            Debug.LogWarning("PlaceholderSlot: nessuno shader trasparente disponibile per la polvere.");
            return null;
        }

        dustMaterial = new Material(shader) { name = "Runtime Placement Dust" };
        dustMaterial.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        dustMaterial.SetOverrideTag("RenderType", "Transparent");
        if (dustMaterial.HasProperty("_Surface")) dustMaterial.SetFloat("_Surface", 1f);
        if (dustMaterial.HasProperty("_ZWrite")) dustMaterial.SetFloat("_ZWrite", 0f);
        Texture2D softDisc = new Texture2D(64, 64, TextureFormat.RGBA32, false)
        {
            name = "Runtime Soft Dust Particle",
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };
        Color[] pixels = new Color[64 * 64];
        for (int y = 0; y < 64; y++)
        {
            for (int x = 0; x < 64; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), new Vector2(31.5f, 31.5f)) / 31.5f;
                float alpha = 1f - Mathf.SmoothStep(0.68f, 1f, distance);
                pixels[y * 64 + x] = new Color(1f, 1f, 1f, alpha);
            }
        }
        softDisc.SetPixels(pixels);
        softDisc.Apply();

        if (dustMaterial.HasProperty("_BaseMap")) dustMaterial.SetTexture("_BaseMap", softDisc);
        if (dustMaterial.HasProperty("_MainTex")) dustMaterial.SetTexture("_MainTex", softDisc);
        if (dustMaterial.HasProperty("_BaseColor")) dustMaterial.SetColor("_BaseColor", Color.white);
        if (dustMaterial.HasProperty("_Color")) dustMaterial.SetColor("_Color", Color.white);
        return dustMaterial;
    }

    // Chiamato da MonumentProgressTracker quando la fase costruttiva di questo slot diventa
    // (o smette di essere) quella corrente: uno slot bloccato è invisibile e il suo Collider
    // trigger è disattivato, quindi OnTriggerEnter non scatta affatto e non può ricevere pezzi
    // fuori sequenza, anche se il pieceId corrisponderebbe.
    public void SetUnlocked(bool unlocked)
    {
        isUnlocked = unlocked;

        if (isFilled)
        {
            return;
        }

        foreach (Renderer r in renderers)
        {
            r.enabled = unlocked;
        }

        foreach (Renderer r in wireframeRenderers)
        {
            r.enabled = unlocked;
        }

        slotCollider.enabled = unlocked;
    }
}
