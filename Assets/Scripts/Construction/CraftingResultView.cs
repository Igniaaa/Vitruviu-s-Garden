using Unity.Cinemachine;
using UnityEngine;

// Reframe the actual player camera, leaving the piece in the world and the UI transparent.
// Only the camera pose/lens and Brain state change; the player and its look target stay put.
[DisallowMultipleComponent]
public sealed class CraftingResultView : MonoBehaviour
{
    [Tooltip("Posizione della visuale rispetto all'uscita del banco: destra, alto, avanti.")]
    [SerializeField] private Vector3 viewOffset = new Vector3(1.4f, 1f, -2.4f);
    [SerializeField, Range(25, 75)] private float fieldOfView = 40;
    [Tooltip("Lascia il pezzo leggermente a sinistra per tenere liberi i comandi sulla destra.")]
    [SerializeField, Range(0, 1)] private float horizontalFraming = .5f;

    private Camera controlledCamera;
    private CinemachineBrain brain;
    private bool brainWasEnabled;
    private Vector3 savedLocalPosition;
    private Quaternion savedLocalRotation;
    private float savedFieldOfView;
    private DraggableObject piece;
    private Transform output;
    private Renderer[] pieceRenderers;

    public bool IsActive => controlledCamera != null;

    public bool Show(DraggableObject target, Transform workbenchOutput, Camera source = null)
    {
        if (!isActiveAndEnabled || target == null || workbenchOutput == null) return false;
        Camera camera = source != null ? source : Camera.main;
        if (camera == null) return false;
        // A rejected storage attempt can show the same result again. Keep the original snapshot.
        if (controlledCamera != camera)
        {
            Restore();
            controlledCamera = camera;
            savedLocalPosition = camera.transform.localPosition;
            savedLocalRotation = camera.transform.localRotation;
            savedFieldOfView = camera.fieldOfView;
            brain = camera.GetComponent<CinemachineBrain>();
            brainWasEnabled = brain != null && brain.enabled;
            if (brain != null) brain.enabled = false;
        }
        piece = target;
        output = workbenchOutput;
        pieceRenderers = target.GetComponentsInChildren<Renderer>();
        ApplyView();
        return true;
    }

    private void LateUpdate()
    {
        if (!IsActive) return;
        if (piece == null || output == null) { Restore(); return; }
        ApplyView();
    }

    private void ApplyView()
    {
        Vector3 center = piece.transform.position;
        bool found = false;
        var bounds = new Bounds(center, Vector3.zero);
        foreach (var renderer in pieceRenderers)
        {
            if (renderer == null) continue;
            if (!found) { bounds = renderer.bounds; found = true; }
            else bounds.Encapsulate(renderer.bounds);
        }
        if (found) center = bounds.center;
        Vector3 position = output.position + output.rotation * viewOffset;
        Vector3 direction = center - position;
        if (direction.sqrMagnitude < .001f) return;
        Quaternion facing = Quaternion.LookRotation(direction, Vector3.up);
        Vector3 target = center + facing * Vector3.right * horizontalFraming;
        controlledCamera.transform.SetPositionAndRotation(position, Quaternion.LookRotation(target - position, Vector3.up));
        controlledCamera.fieldOfView = fieldOfView;
    }

    public void Restore()
    {
        if (controlledCamera != null)
        {
            controlledCamera.transform.SetLocalPositionAndRotation(savedLocalPosition, savedLocalRotation);
            controlledCamera.fieldOfView = savedFieldOfView;
        }
        if (brain != null) brain.enabled = brainWasEnabled;
        controlledCamera = null;
        brain = null;
        piece = null;
        output = null;
        pieceRenderers = null;
    }

    private void OnDisable() => Restore();
}
