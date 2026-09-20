using System;
using System.Collections.Generic;
using UnityEngine;

// Genera dinamicamente i pezzi trascinabili (DraggableObject) di una fase costruttiva nel
// momento in cui MonumentProgressTracker la sblocca, invece di tenerli tutti già presenti
// in scena fin dall'inizio.
//
// Non modifica PlaceholderSlot.cs né MonumentProgressTracker.cs: per sapere quando una fase
// si sblocca legge PlaceholderSlot.IsUnlocked (già pubblica) su UNO slot rappresentante di
// quella fase — non serve un riferimento per ogni pezzo, ne basta uno per numero di fase,
// perché MonumentProgressTracker sblocca sempre tutti gli slot della stessa fase insieme
// (vedi UpdatePhaseLocks). Lo script raccoglie da sé quel rappresentante cercando in scena
// tutti i PlaceholderSlot e raggruppandoli per BuildPhase.
//
// Va messo su un qualunque GameObject della scena (es. lo stesso di MonumentProgressTracker).
public class PiecePhaseSpawner : MonoBehaviour
{
    [Serializable]
    private class PieceSpawnEntry
    {
        [Tooltip("Numero di fase (deve combaciare col Build Phase impostato sui PlaceholderSlot " +
                 "di quella fase, non su questo componente).")]
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

    // Nome opzionale di un SFX registrato in AudioManager (sfxList), riprodotto ogni volta
    // che un pezzo viene generato. Lascia vuoto per non riprodurre nulla.
    [SerializeField] private string spawnSfxName;

    // Notifica (es. per un effetto particellare o un highlight) che un pezzo è stato
    // generato: passa il numero di fase e l'istanza appena creata.
    public event Action<int, GameObject> OnPieceSpawned;

    // Uno slot "rappresentante" per ciascun numero di fase, usato solo per leggere
    // IsUnlocked: non è il pezzo che verrà piazzato lì, serve solo a capire quando quella
    // fase è stata sbloccata.
    private readonly Dictionary<int, PlaceholderSlot> phaseUnlockReference = new Dictionary<int, PlaceholderSlot>();

    // Rimandato al primo Update invece che a Start: Unity garantisce che TUTTI gli Start()
    // della scena (incluso quello di MonumentProgressTracker, che sblocca gli slot della
    // fase iniziale) siano già stati eseguiti prima che parta il primo Update di chiunque,
    // indipendentemente dall'ordine di esecuzione degli script.
    private bool initialized;

    private void Update()
    {
        if (!initialized)
        {
            initialized = true;
            BuildPhaseUnlockReference();

            foreach (PieceSpawnEntry entry in spawnEntries)
            {
                CheckAndSpawn(entry);
            }

            return;
        }

        foreach (PieceSpawnEntry entry in spawnEntries)
        {
            if (!entry.hasSpawned)
            {
                CheckAndSpawn(entry);
            }
        }
    }

    private void BuildPhaseUnlockReference()
    {
        foreach (PlaceholderSlot slot in FindObjectsByType<PlaceholderSlot>(FindObjectsSortMode.None))
        {
            if (!phaseUnlockReference.ContainsKey(slot.BuildPhase))
            {
                phaseUnlockReference[slot.BuildPhase] = slot;
            }
        }
    }

    private void CheckAndSpawn(PieceSpawnEntry entry)
    {
        if (entry.hasSpawned)
        {
            return;
        }

        if (!phaseUnlockReference.TryGetValue(entry.buildPhase, out PlaceholderSlot referenceSlot))
        {
            Debug.LogWarning("PiecePhaseSpawner: nessun PlaceholderSlot trovato in scena con BuildPhase " +
                              entry.buildPhase + ", impossibile capire quando generare questo pezzo.", this);
            entry.hasSpawned = true;
            return;
        }

        if (!referenceSlot.IsUnlocked)
        {
            return;
        }

        Spawn(entry);
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
