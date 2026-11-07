using System;
using System.Collections.Generic;
using UnityEngine;

// Genera i pezzi trascinabili all'avvio, così quelli necessari alla prima costruzione
// non restano bloccati in attesa che i placeholder siano già stati riempiti.
// In alternativa, lo spawn può restare subordinato al completamento delle fasi configurate.
//
// Va messo su un qualunque GameObject della scena (es. lo stesso di MonumentProgressTracker).
public class PiecePhaseSpawner : MonoBehaviour
{
    [Serializable]
    private class PieceSpawnEntry
    {
        [Tooltip("Fase associata alla notifica OnPieceSpawned. Non determina più quando compare il pezzo.")]
        public int buildPhase;

        [Tooltip("Prefab del DraggableObject da instanziare.")]
        public GameObject piecePrefab;

        [Tooltip("Obbligatorio: GameObject dedicato (deposito, carretto, ecc.) su cui il pezzo " +
                 "compare. Deve avere un Collider convesso (Box/Sphere/Capsule o Mesh convesso): " +
                 "viene scelto un punto casuale al suo interno invece del solo pivot, così più " +
                 "pezzi non nascono impilati nello stesso punto esatto e non si spingono a " +
                 "vicenda al primo frame di fisica.")]
        public Transform spawnPoint;

        [NonSerialized] public bool hasSpawned;
    }

    [SerializeField] private List<PieceSpawnEntry> spawnEntries = new List<PieceSpawnEntry>();

    [Tooltip("Genera i pezzi appena inizia la scena. Disattivalo per usare la soglia di fase qui sotto.")]
    [SerializeField] private bool spawnAtStart = true;

    [Tooltip("Se lo spawn all'avvio è disattivato, genera i pezzi quando sono stati riempiti tutti " +
             "i placeholder fino a questa fase inclusa.")]
    [SerializeField] private int spawnAfterBuildPhase = 1;

    // Nome opzionale di un SFX registrato in AudioManager (sfxList), riprodotto ogni volta
    // che un pezzo viene generato. Lascia vuoto per non riprodurre nulla.
    [SerializeField] private string spawnSfxName;

    // Notifica (es. per un effetto particellare o un highlight) che un pezzo è stato
    // generato: passa il numero di fase e l'istanza appena creata.
    public event Action<int, GameObject> OnPieceSpawned;

    private bool initialized;
    private bool hasSpawnedAllPieces;
    private List<PlaceholderSlot> constructionSlots;

    private void Start()
    {
        if (spawnAtStart)
        {
            SpawnAllPieces();
        }
    }

    private void Update()
    {
        if (spawnAtStart)
        {
            return;
        }

        if (!initialized)
        {
            initialized = true;
            constructionSlots = new List<PlaceholderSlot>(
                FindObjectsByType<PlaceholderSlot>(FindObjectsSortMode.None));
        }

        if (!hasSpawnedAllPieces && AreInitialConstructionPhasesComplete())
        {
            SpawnAllPieces();
        }
    }

    private void SpawnAllPieces()
    {
        if (hasSpawnedAllPieces)
        {
            return;
        }

        hasSpawnedAllPieces = true;
        foreach (PieceSpawnEntry entry in spawnEntries)
        {
            Spawn(entry);
        }
    }

    private bool AreInitialConstructionPhasesComplete()
    {
        bool foundRequiredSlot = false;

        foreach (PlaceholderSlot slot in constructionSlots)
        {
            if (slot.BuildPhase <= spawnAfterBuildPhase)
            {
                foundRequiredSlot = true;
                if (!slot.IsFilled)
                {
                    return false;
                }
            }
        }

        if (!foundRequiredSlot)
        {
            Debug.LogWarning("PiecePhaseSpawner: nessun PlaceholderSlot trovato nelle fasi fino alla fase " +
                              spawnAfterBuildPhase + "; i pezzi non verranno generati.", this);
            return false;
        }

        return true;
    }

    private void Spawn(PieceSpawnEntry entry)
    {
        entry.hasSpawned = true;

        if (entry.piecePrefab == null)
        {
            Debug.LogWarning("PiecePhaseSpawner: nessun piecePrefab assegnato per la fase " +
                              entry.buildPhase, this);
            return;
        }

        if (entry.spawnPoint == null)
        {
            Debug.LogWarning("PiecePhaseSpawner: nessuno spawnPoint assegnato per la fase " +
                              entry.buildPhase + ", il pezzo non verrà generato.", this);
            return;
        }

        Vector3 spawnPosition = GetRandomPointInSpawnCollider(entry.spawnPoint);

        GameObject instance = Instantiate(entry.piecePrefab, spawnPosition, entry.spawnPoint.rotation);

        if (!string.IsNullOrEmpty(spawnSfxName) && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(spawnSfxName);
        }

        OnPieceSpawned?.Invoke(entry.buildPhase, instance);
    }

    // Sceglie un punto casuale dentro il volume dello spawn point invece del solo pivot:
    // genera un punto uniforme nel bounding box del Collider e lo "proietta" dentro la
    // forma vera con ClosestPoint (per un punto già interno, ClosestPoint restituisce lo
    // stesso punto). Funziona con Collider convessi (Box/Sphere/Capsule/Mesh convesso);
    // se lo spawn point non ha un Collider, ricade sul suo pivot come prima.
    private static Vector3 GetRandomPointInSpawnCollider(Transform spawnPoint)
    {
        Collider spawnCollider = spawnPoint.GetComponent<Collider>();
        if (spawnCollider == null)
        {
            return spawnPoint.position;
        }

        Bounds bounds = spawnCollider.bounds;
        Vector3 randomPointInBounds = new Vector3(
            UnityEngine.Random.Range(bounds.min.x, bounds.max.x),
            UnityEngine.Random.Range(bounds.min.y, bounds.max.y),
            UnityEngine.Random.Range(bounds.min.z, bounds.max.z));

        return spawnCollider.ClosestPoint(randomPointInBounds);
    }
}
