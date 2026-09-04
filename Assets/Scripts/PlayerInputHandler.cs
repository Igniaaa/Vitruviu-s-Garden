using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

// Wrapper centrale sul nuovo Input System per la action map "Player": clona l'asset assegnato
// nell'Inspector così le altre istanze/scene non condividono lo stesso stato a runtime, espone
// le azioni come proprietà/metodi tipizzati e si ri-abilita da solo quando viene riattivato
// (utile dopo un cambio di scena o la chiusura di un pannello modale).
[DisallowMultipleComponent]
public class PlayerInputHandler : MonoBehaviour
{
    [Header("Input Action Asset")]
    [SerializeField] private InputActionAsset playerControls;

    [Space]
    [Header("Action Map Name Reference")]
    [SerializeField] private string actionMapName = "Player";
    [Space]

    [Header("Action Name Reference")]
    [SerializeField] private string movement = "Move";
    [SerializeField] private string look = "Look";
    [SerializeField] private string jump = "Jump";
    [SerializeField] private string sprint = "Sprint";
    [SerializeField] private string openJournal = "OpenJournal";
    [SerializeField] private string nextJournalPage = "NextJournalPage";
    [SerializeField] private string previousJournalPage = "PreviouseJournalPage";

    private InputAction movementAction;
    private InputAction lookAction;
    private InputAction jumpAction;
    private InputAction sprintAction;
    private InputAction openJournalAction;
    private InputAction nextJournalPageAction;
    private InputAction previousJournalPageAction;

    public Vector2 MovementInput { get; private set; }
    public bool AnalogMovement { get; private set; }
    public Vector2 LookInput { get; private set; }
    public bool JumpTriggered { get; private set; }
    public bool SprintTriggered { get; private set; }

    public static PlayerInputHandler Instance { get; private set; }

    private void Awake()
    {
        Debug.Log("<color=yellow>[INPUT] Awake chiamato</color>");

        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Debug.LogWarning("[INPUT] Duplicate PlayerInputHandler rilevato → distruggo componente duplicato");
            Destroy(this);
            return;
        }

        playerControls = Instantiate(playerControls);

        InitializeActions();
        EnableInput();

        Cursor.visible = false;

    }

    private void InitializeActions()
    {
        InputActionMap mapReference = playerControls.FindActionMap(actionMapName);

        if (mapReference == null)
        {
            Debug.LogError("Action Map NOT FOUND: " + actionMapName);
            return;
        }

        movementAction = mapReference.FindAction(movement);
        lookAction = mapReference.FindAction(look);
        jumpAction = mapReference.FindAction(jump);
        sprintAction = mapReference.FindAction(sprint);
        openJournalAction = mapReference.FindAction(openJournal);
        nextJournalPageAction = mapReference.FindAction(nextJournalPage);
        previousJournalPageAction = mapReference.FindAction(previousJournalPage);

        SubscribeActionValueToInputEvent();
    }

    private void SubscribeActionValueToInputEvent()
    {
        movementAction.performed += inputInfo =>
        {
            MovementInput = inputInfo.ReadValue<Vector2>();
            AnalogMovement = inputInfo.control.device is Gamepad;
        };
        movementAction.canceled += inputInfo => MovementInput = Vector2.zero;

        lookAction.performed += inputInfo => LookInput = inputInfo.ReadValue<Vector2>();
        lookAction.canceled += inputInfo => LookInput = Vector2.zero;

        jumpAction.performed += inputInfo => JumpTriggered = true;
        jumpAction.canceled += inputInfo => JumpTriggered = false;

        sprintAction.performed += inputInfo => SprintTriggered = true;
        sprintAction.canceled += inputInfo => SprintTriggered = false;
    }

    #region input functions
    public bool JumpPressed()
    {
        return jumpAction.WasPerformedThisFrame();
    }

    public bool OpenJournalPressed()
    {
        return openJournalAction.WasPerformedThisFrame();
    }

    public bool NextJournalPagePressed()
    {
        return nextJournalPageAction.WasPerformedThisFrame();
    }

    public bool PreviousJournalPagePressed()
    {
        return previousJournalPageAction.WasPerformedThisFrame();
    }
    #endregion

    #region enable disable behavior
    private void OnEnable()
    {
        Debug.Log("<color=green>[INPUT] OnEnable chiamato → input abilitato</color>");
        StartCoroutine(EnableInputNextFrame());
    }

    private void OnDisable()
    {
        DisableInput();
    }

    private IEnumerator EnableInputNextFrame()
    {
        yield return null;
        EnableInput();
        EnableEventSystem();
    }

    private void EnableInput()
    {
        InputActionMap map = playerControls.FindActionMap(actionMapName);
        if (map != null)
        {
            map.Enable();
            Debug.Log("<color=cyan>[INPUT] ActionMap ENABLED</color>");
        }
    }

    private void DisableInput()
    {
        InputActionMap map = playerControls.FindActionMap(actionMapName);
        if (map != null)
        {
            map.Disable();
            Debug.Log("<color=magenta>[INPUT] ActionMap DISABLED</color>");
        }
    }

    private void EnableEventSystem()
    {
        var uiInputModule = FindAnyObjectByType<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        if (uiInputModule != null)
        {
            uiInputModule.enabled = false;
            uiInputModule.enabled = true;
            Debug.Log("<color=green>[UI] InputSystemUIInputModule resettato automaticamente con successo!</color>");
        }
        else
        {
            Debug.LogWarning("[UI] InputSystemUIInputModule non trovato. Assicurati che sia presente sull'EventSystem della scena di gioco.");
        }
    }

    public void ForceReEnable()
    {
        Debug.Log("<color=orange>[INPUT] ForceReEnable chiamato</color>");

        DisableInput();
        EnableInput();
    }
    #endregion

    private void OnDestroy()
    {
        if (playerControls != null)
        {
            Destroy(playerControls);
        }
    }
}
