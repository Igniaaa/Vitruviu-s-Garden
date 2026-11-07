using UnityEngine;

public enum ConstructionStationKind { Merchant, Workbench }

[DisallowMultipleComponent]
public sealed class ConstructionStation : MonoBehaviour
{
    public ConstructionStationKind kind;
    public Transform outputPoint;
    public Vector3 outputHalfExtents = new Vector3(.8f, .8f, .8f);
    public string Prompt => kind == ConstructionStationKind.Merchant
        ? "Premi E per acquistare materiali" : "Premi E per usare il banco da lavoro";

    public bool IsOutputBlocked()
    {
        if (outputPoint == null) return true;
        foreach (var col in Physics.OverlapBox(outputPoint.position, outputHalfExtents,
                     outputPoint.rotation, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            var piece = col.GetComponentInParent<DraggableObject>();
            if (piece != null && !piece.IsLocked) return true;
        }
        return false;
    }

    private void OnDrawGizmosSelected()
    {
        if (outputPoint == null) return;
        Gizmos.color = new Color(1, .7f, .2f, .5f);
        Gizmos.matrix = Matrix4x4.TRS(outputPoint.position, outputPoint.rotation, Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, outputHalfExtents * 2);
    }
}
