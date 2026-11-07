using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Basilica.Crafting;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Unity.Cinemachine;
using UnityEngine.EventSystems;

public static class DeliverySmoke
{
    private static readonly Queue<Action> Steps = new Queue<Action>();
    private static string folder;
    private static ConstructionSystem system;
    private static ConstructionStation bench;
    private static ConstructionStorageArea storage;
    private static CraftingRecipe recipe;
    private static ConstructionUI ui;
    private static DraggableObject first, second, third;
    private static GameObject obstruction;
    private static Vector3 tablePosition;
    private static int pieceCount, coins;
    private static int cameraCount;
    private static int authoredObjectCount;
    private static Camera playerCamera;
    private static Vector3 playerCameraPosition;
    private static Quaternion playerCameraRotation;
    private static float playerFieldOfView;
    private static bool playerBrainEnabled;
    private static double next;
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

    public static string Run()
    {
        if (!EditorApplication.isPlaying) throw new InvalidOperationException("Play Mode required");
        folder = Path.GetFullPath(".ai-game-dev/verification/authored-ui");
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder,"play.txt"), "Crafting delivery Play Mode checks\n");
        system = ConstructionSystem.Instance;
        Application.runInBackground=true;
        system.SendMessage("OnApplicationFocus", true);
        bench = UnityEngine.Object.FindObjectsByType<ConstructionStation>(FindObjectsSortMode.None).First(s=>s.kind==ConstructionStationKind.Workbench);
        storage = UnityEngine.Object.FindFirstObjectByType<ConstructionStorageArea>();
        recipe = system.Recipes.First(r=>r.buildPhase==0);
        ui = UnityEngine.Object.FindFirstObjectByType<ConstructionUI>(FindObjectsInactive.Include);
        var gameView=typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
        EditorWindow.GetWindow(gameView).Show();
        authoredObjectCount=ui.GetComponentsInChildren<Transform>(true).Length;
        cameraCount=UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Length;
        if(system.Materials.Any(m=>system.Inventory.Count(m)<(m.displayName=="Pietra"?20:10)))
        {
            var merchant=UnityEngine.Object.FindObjectsByType<ConstructionStation>(FindObjectsSortMode.None).First(s=>s.kind==ConstructionStationKind.Merchant);
            if(!system.Open(merchant)) throw new Exception("Merchant unavailable");
            foreach(var material in system.Materials)
            {
                int quantity=(material.displayName=="Pietra"?20:10)-system.Inventory.Count(material);
                if(quantity>0 && !system.Buy(material,quantity)) throw new Exception(system.Message);
            }
            system.Close();
        }
        Steps.Clear();
        Steps.Enqueue(()=>
        {
            var merchant=UnityEngine.Object.FindObjectsByType<ConstructionStation>(FindObjectsSortMode.None).First(s=>s.kind==ConstructionStationKind.Merchant);
            Check("OpenSavedShopScreen",system.Open(merchant) && ui.CurrentScreen.name=="Acquisto");
        });
        // Graphics acquire raycast depth only after the newly enabled screen has rendered.
        Steps.Enqueue(()=>
        {
            var stone=system.Materials.First(m=>m.displayName=="Pietra");
            int before=system.Inventory.Count(stone);
            var buy=ui.transform.Find("Screens/Acquisto/Panel/Viewport/Content/Pietra/Compra 1").GetComponent<Button>();
            Check("SavedShopButtonReceivesPointer",ReceivesPointer(buy));
            buy.onClick.Invoke();
            Check("AuthoredPurchaseButtonBuysOnce",system.Inventory.Count(stone)==before+1);
            Capture("authored-shop.png");
        });
        Steps.Enqueue(()=>system.Close());
        Steps.Enqueue(()=>
        {
            Check("StorageConfiguredInExistingEnclosure",storage!=null && storage.volume!=null && storage.name=="PieceRespawnPoint");
            Check("OpenWorkbench",system.Open(bench));
            Check("SavedRecipeScreenSelected",ui.CurrentScreen.name=="Ricette");
            Capture("authored-recipes.png");
        });
        Steps.Enqueue(()=>
        {
            Produce();
            first=system.CreatedPiece;
            tablePosition=first.transform.position;
            Check("SuccessKeepsActualCreatedPiece",first!=null && !first.IsLocked);
            Check("SuccessOffersStorageButton",StorageButton()!=null && StorageButton().interactable);
        });
        Steps.Enqueue(()=>
        {
            Check("CompletedResultHasTransparentBackground",TransparentResult());
            Check("SavedStorageButtonReceivesPointer",ReceivesPointer(StorageButton()));
            Check("CompletedResultShowsNoPicture",ui.GetComponentsInChildren<RawImage>(true).Length==0);
            Check("CompletedResultCreatesNoExtraCamera",UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Length==cameraCount);
            var position=playerCamera.WorldToViewportPoint(first.GetComponentInChildren<Renderer>().bounds.center);
            Check("ResultViewFramesPieceLeftOfControls",position.z>0 && position.x>.05f && position.x<.65f && position.y>.05f && position.y<.95f);
            Check("ResultViewUsesPlayerCameraDirectly",system.GetComponent<CraftingResultView>().IsActive && Camera.main==playerCamera && playerCamera.targetTexture==null);
            Check("ResultViewSuspendsCinemachineControl",!playerCamera.GetComponent<CinemachineBrain>().enabled);
            Capture("dedicated-result-view.png");
        });
        Steps.Enqueue(()=>
        {
            PauseMenuController.Instance.Pause();
            Check("PauseHidesCompletedResult",!ui.transform.Find("Screens").gameObject.activeSelf);
            Check("PausePreservesDedicatedView",system.GetComponent<CraftingResultView>().IsActive);
        });
        Steps.Enqueue(()=>
        {
            PauseMenuController.Instance.Resume();
            Check("ResumeRestoresTransparentResult",ui.transform.Find("Screens").gameObject.activeSelf && TransparentResult());
        });
        Steps.Enqueue(()=>
        {
            var close=ui.GetComponentsInChildren<Button>().First(b=>b.name.StartsWith("CHIUDI"));
            close.onClick.Invoke();
            Check("CloseLeavesSamePieceOnTable",!system.IsOpen && first!=null && Vector2.Distance(new Vector2(first.transform.position.x,first.transform.position.z),new Vector2(tablePosition.x,tablePosition.z))<.2f);
            Check("CloseHasNoAuxiliaryCamera",UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Length==cameraCount);
            Check("CloseRestoresPlayerCamera",PlayerViewRestored());
        });
        Steps.Enqueue(()=>
        {
            Check("ManualPickupAlternativeCanClearTable",storage.TryStore(first,out _));
            Check("FirstPieceStoredInsideFence",Inside(first));
            Check("WorkbenchOutputCleared",!bench.IsOutputBlocked());
            Check("ReopenWorkbench",system.Open(bench));
            Produce();
            second=system.CreatedPiece;
            pieceCount=UnityEngine.Object.FindObjectsByType<CraftedPiece>(FindObjectsSortMode.None).Length;
            coins=system.Inventory.Coins;
            StorageButton().onClick.Invoke();
            Check("StorageButtonMovesSamePiece",second!=null && Inside(second));
            Check("StoredPieceRemainsGrabbable",!second.IsLocked && !second.IsDragging && second.GetComponent<Rigidbody>().useGravity);
            Check("StorageReturnsToRecipeMenu",system.IsOpen && system.CreatedPiece==null && Preview()==null);
            Check("StorageRestoresPlayerCamera",PlayerViewRestored());
            Check("RecipeMenuRestoresOpaqueBackground",ui.CurrentScreen.name=="Ricette" && ui.CurrentScreen.GetComponent<Image>().color.a>.7f && ui.CurrentScreen.transform.Find("Panel").GetComponent<Image>().color.a==1);
            Check("StorageDoesNotDuplicatePiecesOrSpendCoins",pieceCount==UnityEngine.Object.FindObjectsByType<CraftedPiece>(FindObjectsSortMode.None).Length && coins==system.Inventory.Coins);
            Check("DuplicateStorageClickRejected",!system.StoreCreatedPiece());
            Check("StoredPiecesStillCountTowardRecipeLimit",system.Remaining(recipe)==2);
        });
        Steps.Enqueue(()=>
        {
            Capture("stored-and-ready.png");
            Check("SecondTransferClearsWorkbench",!bench.IsOutputBlocked());
            Check("StoredPiecesDoNotOverlap",!first.GetComponent<Collider>().bounds.Intersects(second.GetComponent<Collider>().bounds));
            obstruction=GameObject.CreatePrimitive(PrimitiveType.Cube);
            obstruction.name="Delivery smoke obstruction";
            obstruction.transform.position=storage.volume.bounds.center;
            obstruction.transform.localScale=storage.volume.bounds.size+new Vector3(2,8,2);
            Physics.SyncTransforms();
            Produce();
            third=system.CreatedPiece;
            tablePosition=third.transform.position;
        });
        Steps.Enqueue(()=>
        {
            tablePosition=third.transform.position;
            StorageButton().onClick.Invoke();
            Check("FullEnclosureKeepsPieceOnTable",system.CreatedPiece==third && Vector3.Distance(third.transform.position,tablePosition)<.01f && system.Message.Contains("recinto"));
            Check("FullEnclosureKeepsTransparentResult",TransparentResult() && system.IsOpen);
            Check("FullEnclosurePreservesResultView",system.GetComponent<CraftingResultView>().IsActive);
            Capture("enclosure-full.png");
            UnityEngine.Object.Destroy(obstruction);
        });
        Steps.Enqueue(()=>
        {
            Check("StorageWorksAfterFreeingSpace",system.StoreCreatedPiece());
            Check("ThirdPieceStored",Inside(third));
            Check("BeginFailureCase",system.BeginCraft(recipe));
            Check("SavedRhythmScreenSelected",ui.CurrentScreen.name=="Ritmo");
            Capture("authored-rhythm.png");
        });
        Steps.Enqueue(()=>
        {
            ui.GetComponentsInChildren<Button>().First(b=>b.name=="ANNULLA").onClick.Invoke();
            Check("SavedCancelButtonShowsFailure",!system.IsCrafting && ui.CurrentScreen.name=="Fallimento");
            Check("FailureShowsNoPiecePreviewOrStorageAction",system.CreatedPiece==null && Preview()==null && StorageButton()==null);
            Check("FailureDoesNotOverridePlayerView",PlayerViewRestored());
            Capture("failure-result.png");
        });
        Steps.Enqueue(()=>
        {
            ui.GetComponentsInChildren<Button>().First(b=>b.name=="TORNA AL BANCO").onClick.Invoke();
            Check("SavedBackButtonReturnsToRecipes",ui.CurrentScreen.name=="Ricette");
            system.Close();
            Check("FinalCloseRestoresPlayerInput",!ConstructionSystem.BlocksInput && Cursor.lockState==CursorLockMode.Locked);
            Check("NoPreviewCameraLeaked",UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).All(c=>c.name!="Camera anteprima banco"));
            Check("NoUIObjectsCreatedOrDestroyed",ui.GetComponentsInChildren<Transform>(true).Length==authoredObjectCount);
            Application.runInBackground=false;
            File.AppendAllText(Path.Combine(folder,"play.txt"),"DONE\n");
        });
        next=EditorApplication.timeSinceStartup+.7;
        EditorApplication.update+=Tick;
        return "Delivery verification started; report: "+Path.Combine(folder,"play.txt");
    }

    private static void Tick()
    {
        if (!EditorApplication.isPlaying) { EditorApplication.update-=Tick; return; }
        EditorApplication.QueuePlayerLoopUpdate();
        if (EditorApplication.timeSinceStartup<next) return;
        next=EditorApplication.timeSinceStartup+1;
        try
        {
            if(Steps.Count==0) { EditorApplication.update-=Tick; EditorApplication.isPlaying=false; return; }
            system.SendMessage("OnApplicationFocus",true);
            Steps.Dequeue()();
        }
        catch(Exception ex)
        {
            File.AppendAllText(Path.Combine(folder,"play.txt"),"ERROR "+ex+"\n");
            EditorApplication.update-=Tick;
            EditorApplication.isPlaying=false;
        }
    }
    private static Camera Preview() => UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).FirstOrDefault(c=>c.name=="Camera anteprima banco");
    private static bool TransparentResult() => ui.CurrentScreen!=null && ui.CurrentScreen.name=="Completato" && ui.CurrentScreen.GetComponent<Image>().color.a==0 && ui.CurrentScreen.transform.Find("Panel").GetComponent<Image>().color.a<=.2f;
    private static Button StorageButton() => ui.GetComponentsInChildren<Button>().FirstOrDefault(b=>b.name=="SPOSTA NEL RECINTO");
    private static bool ReceivesPointer(Button button)
    {
        Canvas.ForceUpdateCanvases();
        var rect=(RectTransform)button.transform;
        var pointer=new PointerEventData(EventSystem.current) { position=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center)) };
        var hits=new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointer,hits);
        return hits.Count>0 && (hits[0].gameObject==button.gameObject || hits[0].gameObject.transform.IsChildOf(button.transform));
    }
    private static bool Inside(DraggableObject p)
    {
        var b=storage.volume.bounds; var c=p.GetComponent<Collider>().bounds.center;
        return c.x>=b.min.x && c.x<=b.max.x && c.z>=b.min.z && c.z<=b.max.z;
    }
    private static void Produce()
    {
        SnapshotPlayerView();
        var row=ui.GetComponentsInChildren<ConstructionRecipeRow>().First(r=>r.recipe==recipe);
        Check("SavedCraftButtonEnabled",row.craftButton.interactable);
        row.craftButton.onClick.Invoke();
        Check("BeginCraft",system.IsCrafting);
        foreach(var time in system.Sequence.NoteTimes) system.SubmitBeat(time);
        Check("PerfectCraftSettled",!system.IsCrafting && system.LastOutcome==CraftOutcome.PerfectSuccess);
    }
    private static void SnapshotPlayerView()
    {
        playerCamera=Camera.main;
        playerCameraPosition=playerCamera.transform.localPosition;
        playerCameraRotation=playerCamera.transform.localRotation;
        playerFieldOfView=playerCamera.fieldOfView;
        playerBrainEnabled=playerCamera.GetComponent<CinemachineBrain>().enabled;
    }
    private static bool PlayerViewRestored() => !system.GetComponent<CraftingResultView>().IsActive &&
        Vector3.Distance(playerCamera.transform.localPosition,playerCameraPosition)<.001f &&
        Quaternion.Angle(playerCamera.transform.localRotation,playerCameraRotation)<.05f &&
        Mathf.Abs(playerCamera.fieldOfView-playerFieldOfView)<.001f &&
        playerCamera.GetComponent<CinemachineBrain>().enabled==playerBrainEnabled;
    private static void Check(string label,bool passed)
    {
        File.AppendAllText(Path.Combine(folder,"play.txt"),(passed?"PASS ":"FAIL ")+label+"\n");
        if(!passed) throw new Exception(label);
    }
    private static void Capture(string file) => ScreenCapture.CaptureScreenshot(Path.Combine(folder,file));
    private static void SavePreview(RenderTexture render)
    {
        var previous=RenderTexture.active;
        var texture=new Texture2D(render.width,render.height,TextureFormat.RGB24,false);
        try
        {
            RenderTexture.active=render;
            texture.ReadPixels(new Rect(0,0,render.width,render.height),0,0);
            texture.Apply();
            var colors=texture.GetPixels();
            Check("PreviewContainsRenderedScene",colors.Max(c=>c.grayscale)-colors.Min(c=>c.grayscale)>.1f);
            File.WriteAllBytes(Path.Combine(folder,"table-preview.png"),texture.EncodeToPNG());
        }
        finally { RenderTexture.active=previous; UnityEngine.Object.Destroy(texture); }
    }
}
