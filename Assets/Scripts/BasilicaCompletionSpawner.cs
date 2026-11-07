using UnityEngine;

[DisallowMultipleComponent]
public sealed class BasilicaCompletionSpawner : MonoBehaviour
{
    [SerializeField] private MonumentProgressTracker progressTracker;
    [SerializeField] private GameObject basilicaPrefab;
    [SerializeField] private Transform spawnPoint;

    private bool hasSpawned;

    private void OnEnable()
    {
        if (progressTracker == null)
        {
            progressTracker = GetComponent<MonumentProgressTracker>();
        }

        if (progressTracker != null)
        {
            progressTracker.OnReconstructionCompleted += SpawnBasilica;
        }
    }

    private void Start()
    {
        if (progressTracker == null)
        {
            progressTracker = FindFirstObjectByType<MonumentProgressTracker>();
        }

        if (progressTracker == null)
        {
            Debug.LogError("BasilicaCompletionSpawner: nessun MonumentProgressTracker trovato.", this);
            enabled = false;
            return;
        }

        if (basilicaPrefab == null || spawnPoint == null)
        {
            Debug.LogError("BasilicaCompletionSpawner: assegna il prefab della basilica e il punto di spawn.", this);
            enabled = false;
            return;
        }

        progressTracker.OnReconstructionCompleted -= SpawnBasilica;
        progressTracker.OnReconstructionCompleted += SpawnBasilica;

        if (progressTracker.IsReconstructionComplete)
        {
            SpawnBasilica();
        }
    }

    private void OnDisable()
    {
        if (progressTracker != null)
        {
            progressTracker.OnReconstructionCompleted -= SpawnBasilica;
        }
    }

    private void SpawnBasilica()
    {
        if (hasSpawned || basilicaPrefab == null || spawnPoint == null)
        {
            return;
        }

        hasSpawned = true;
        Instantiate(basilicaPrefab, spawnPoint.position, spawnPoint.rotation);
    }
}
