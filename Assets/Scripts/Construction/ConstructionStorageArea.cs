using System.Linq;
using UnityEngine;

// The trigger describes the enclosure footprint; a raycast finds its actual ground.
// Transfer the existing piece, so CraftedPiece, inventory and slot counts remain intact.
[DisallowMultipleComponent]
public sealed class ConstructionStorageArea : MonoBehaviour
{
    public BoxCollider volume;
    [SerializeField, Min(.02f)] private float spacing = .15f;

    public bool TryStore(DraggableObject piece, out string reason)
    {
        reason = null;
        if (volume == null || !volume.enabled || !volume.gameObject.activeInHierarchy || !isActiveAndEnabled)
            reason = "Area di stoccaggio non disponibile. Il pezzo resta sul tavolo.";
        else if (piece == null || piece.IsLocked || piece.IsDragging)
            reason = "Questo pezzo non può essere trasferito.";
        if (reason != null) return false;

        Physics.SyncTransforms();
        var colliders = piece.GetComponentsInChildren<Collider>()
            .Where(c => c.enabled && !c.isTrigger).ToArray();
        if (colliders.Length == 0)
        { reason = "Il pezzo non ha un ingombro fisico valido."; return false; }
        Bounds pieceBounds = colliders[0].bounds;
        foreach (var collider in colliders.Skip(1)) pieceBounds.Encapsulate(collider.bounds);
        Vector3 half = pieceBounds.extents;
        Vector3 offset = pieceBounds.center - piece.transform.position;
        Bounds bounds = volume.bounds;
        float gap = Mathf.Max(.02f, spacing);
        int attempts = 0;
        for (float z = bounds.min.z + half.z + gap; z <= bounds.max.z - half.z - gap; z += half.z * 2 + gap)
        {
            for (float x = bounds.min.x + half.x + gap; x <= bounds.max.x - half.x - gap; x += half.x * 2 + gap)
            {
                if (++attempts > 256) break;
                if (!ContainsFootprint(x, z, half, bounds.center.y)) continue;
                var origin = new Vector3(x, bounds.max.y + 3, z);
                var hits = Physics.RaycastAll(origin, Vector3.down, bounds.size.y + 6,
                    Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore).OrderBy(h => h.distance);
                var surface = hits.FirstOrDefault(h => !h.collider.transform.IsChildOf(piece.transform));
                if (surface.collider == null || surface.normal.y < .7f ||
                    surface.collider.GetComponentInParent<DraggableObject>() != null ||
                    surface.point.y > bounds.max.y) continue;
                Vector3 center = new Vector3(x, surface.point.y + half.y + .08f, z);
                bool occupied = Physics.OverlapBox(center, half + Vector3.one * .04f, Quaternion.identity,
                        Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
                    .Any(c => !c.transform.IsChildOf(piece.transform));
                if (occupied) continue;
                piece.ResetToSpawn(center - offset, piece.transform.rotation);
                Physics.SyncTransforms();
                return true;
            }
            if (attempts > 256) break;
        }
        reason = "Il recinto è pieno o non ha un punto libero. Il pezzo resta sul tavolo.";
        return false;
    }

    private bool ContainsFootprint(float x, float z, Vector3 half, float y)
    {
        for (int ix = -1; ix <= 1; ix += 2)
            for (int iz = -1; iz <= 1; iz += 2)
            {
                Vector3 corner = new Vector3(x + ix * half.x, y, z + iz * half.z);
                if ((volume.ClosestPoint(corner) - corner).sqrMagnitude > .0001f) return false;
            }
        return true;
    }
}
