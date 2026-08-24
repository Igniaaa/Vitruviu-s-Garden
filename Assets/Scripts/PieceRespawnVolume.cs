using UnityEngine;

// Rete di sicurezza: collider trigger da piazzare ben sotto la mappa. Se un pezzo dovesse
// cadere fuori dal livello (clipping, bug fisici, ecc.) viene riportato al punto di spawn
// invece di cadere all'infinito.
[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class PieceRespawnVolume : MonoBehaviour
{
    [SerializeField] private Transform spawnPoint;

    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true;

        Rigidbody rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (spawnPoint == null)
        {
            return;
        }

        DraggableObject draggable = other.GetComponent<DraggableObject>();
        if (draggable == null)
        {
            return;
        }

        draggable.ResetToSpawn(spawnPoint.position, spawnPoint.rotation);
    }
}
