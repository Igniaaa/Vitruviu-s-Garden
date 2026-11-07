using System;
using System.Linq;
using Basilica.Crafting;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// The map and all its controls are saved in the Canvas. Runtime state is per session.
public sealed class ExtractionMapSystem : MonoBehaviour
{
    [Serializable] public sealed class Point
    {
        public string name;
        public Button button;
        public TMP_Text label;
        [NonSerialized] public bool installed;
        [NonSerialized] public double elapsed;
    }
    public static ExtractionMapSystem Instance { get; private set; }
    public static bool IsMapOpen => Instance != null && Instance.open;
    public GameObject screen;
    public Button openButton, closeButton, installButton;
    public TMP_Text summary, details, message;
    public ExtractionMachineDefinition machine;
    public Point[] points = Array.Empty<Point>();
    private Point selected;
    private bool open;
    private MaterialInventory Inventory => ConstructionSystem.Instance != null ? ConstructionSystem.Instance.Inventory : null;
    private bool Paused => PauseMenuController.Instance != null && PauseMenuController.Instance.IsPaused;

    private void Awake()
    {
        Instance=this;
        openButton.onClick.AddListener(Toggle);
        closeButton.onClick.AddListener(Close);
        installButton.onClick.AddListener(InstallSelected);
        foreach(var point in points) { var p=point; p.button.onClick.AddListener(()=>Select(p)); }
        screen.SetActive(false);
    }
    private void Update()
    {
        if(Keyboard.current!=null && Keyboard.current.mKey.wasPressedThisFrame) Toggle();
        bool available=Inventory!=null && !Paused && !(ConstructionSystem.Instance!=null && ConstructionSystem.Instance.IsOpen) &&
            !(DialogueJournalPanel.Instance!=null && DialogueJournalPanel.Instance.IsJournalActive) &&
            !(ArchitectDialoguePanel.Instance!=null && ArchitectDialoguePanel.Instance.IsDialogueActive);
        openButton.gameObject.SetActive(!open && available);
        screen.SetActive(open && !Paused);
        if(!Paused && machine!=null && machine.IsValid && Inventory!=null)
            foreach(var point in points)
                if(point.installed)
                {
                    point.elapsed+=Time.deltaTime;
                    int cycles=(int)Math.Min(100000,Math.Floor(point.elapsed/machine.cycleSeconds));
                    if(cycles>0)
                    {
                        point.elapsed-=cycles*machine.cycleSeconds;
                        long payout=(long)cycles*machine.coinsPerCycle;
                        Inventory.TryCredit((int)Math.Min(payout,int.MaxValue));
                    }
                }
        if(open && !Paused) Refresh();
    }
    public void Toggle()
    {
        if(open) { Close(); return; }
        if(Inventory==null || Paused || ConstructionSystem.BlocksInput ||
            DialogueJournalPanel.Instance!=null && DialogueJournalPanel.Instance.IsJournalActive ||
            ArchitectDialoguePanel.Instance!=null && ArchitectDialoguePanel.Instance.IsDialogueActive ||
            UnityEngine.Object.FindFirstObjectByType<PlayerPieceInteractor>()?.IsHoldingPiece==true) return;
        open=true; screen.SetActive(true); message.text="Seleziona un punto di estrazione sulla mappa.";
        Cursor.lockState=CursorLockMode.None; Cursor.visible=true;
        PlayerInputHandler.Instance?.ResetUIInputModule(); Refresh();
    }
    private void Select(Point point) { selected=point; message.text=""; Refresh(); }
    public void InstallSelected()
    {
        if(!open || Paused || selected==null || selected.installed || machine==null || !machine.IsValid || Inventory==null) return;
        if(!Inventory.TrySpend(machine.costs,out string error)) { message.text=error; return; }
        selected.installed=true; selected.elapsed=0;
        message.text="Macchina di Vitruvio installata: produzione avviata."; Refresh();
    }
    private void Refresh()
    {
        if(Inventory==null || machine==null || !machine.IsValid)
        {
            details.text="Configura una macchina valida nell'Inspector.";
            installButton.interactable=false; return;
        }
        summary.text="Credito: "+Inventory.Coins+" monete    |    "+string.Join("    ",machine.costs.Select(c=>c.material!=null?c.material.displayName+": "+Inventory.Count(c.material):"Materiale mancante"));
        foreach(var p in points) p.label.text=p.name+"\n"+(p.installed?"Macchina attiva":"Punto libero");
        if(selected==null) details.text="Scegli un punto sulla mappa.\n\nAcquista legno e ferro dal mercante dei materiali.";
        else details.text=selected.name+"\n"+machine.displayName+"\n\n"+(selected.installed?
            "Attiva · prossima produzione tra "+Math.Ceiling(machine.cycleSeconds-selected.elapsed)+" s":
            "Installazione: "+string.Join(" · ",machine.costs.Select(c=>c.quantity+" "+(c.material!=null?c.material.displayName:"?"))))+
            "\nProduzione: +"+machine.coinsPerCycle+" monete ogni "+machine.cycleSeconds+" s";
        installButton.interactable=selected!=null && !selected.installed && machine.IsValid && machine.costs.All(c=>c.material!=null && Inventory.Count(c.material)>=c.quantity);
    }
    public void Close()
    {
        open=false; if(screen!=null) screen.SetActive(false);
        if(!Paused) { Cursor.lockState=CursorLockMode.Locked; Cursor.visible=false; }
    }
    private void OnDisable() { if(open) Close(); }
    private void OnDestroy() { if(Instance==this) Instance=null; }
}
