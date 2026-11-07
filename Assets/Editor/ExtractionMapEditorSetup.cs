using System;
using System.Linq;
using Basilica.Crafting;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Editor-only authoring. Existing UI and balancing assets are preserved on repeat runs.
public static class ExtractionMapEditorSetup
{
    static readonly Color Paper=new Color(.94f,.91f,.82f), Gold=new Color(.91f,.68f,.32f);
    [MenuItem("Tools/Vitruvio/Prepare editable extraction map")]
    public static void FromMenu() => Debug.Log(Configure());
    public static string Configure()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
        var system=UnityEngine.Object.FindFirstObjectByType<ConstructionSystem>();
        var canvas=GameObject.Find("Canvas");
        if(system==null || canvas==null) throw new InvalidOperationException("Open GameScene with its Canvas and ConstructionSystem.");
        var wood=Material("Legno",2); var iron=Material("Ferro",4);
        var so=new SerializedObject(system); var materials=so.FindProperty("materials");
        foreach(var material in new[]{wood,iron})
            if(!Enumerable.Range(0,materials.arraySize).Any(i=>materials.GetArrayElementAtIndex(i).objectReferenceValue==material))
            { int index=materials.arraySize++; materials.GetArrayElementAtIndex(index).objectReferenceValue=material; }
        so.ApplyModifiedProperties();
        var rows=canvas.GetComponentsInChildren<ConstructionMaterialRow>(true);
        foreach(var material in new[]{wood,iron})
            if(!rows.Any(r=>r.material==material)) ConstructionUIEditorSetup.MaterialRow(rows[0].transform.parent,material);
        var existing=canvas.GetComponentInChildren<ExtractionMapSystem>(true);
        if(existing!=null) { Save(); return "Existing extraction map preserved; materials linked."; }
        const string assetPath="Assets/Data/Construction/MacchinaVitruvio.asset";
        var machine=AssetDatabase.LoadAssetAtPath<ExtractionMachineDefinition>(assetPath);
        if(machine==null)
        {
            machine=ScriptableObject.CreateInstance<ExtractionMachineDefinition>();
            machine.costs=new[]{new MaterialCost(wood,4),new MaterialCost(iron,2)};
            AssetDatabase.CreateAsset(machine,assetPath);
        }
        var root=Rect("Extraction Map",canvas.transform,0,0,1920,1080); Stretch(root);
        Undo.RegisterCreatedObjectUndo(root.gameObject,"Prepare extraction map");
        var overlay=root.gameObject.AddComponent<Canvas>(); overlay.overrideSorting=true; overlay.sortingOrder=25;
        root.gameObject.AddComponent<GraphicRaycaster>();
        var map=root.gameObject.AddComponent<ExtractionMapSystem>(); map.machine=machine;
        map.openButton=Button("Apri mappa","MAPPA ESTRAZIONI  [M]",root,1540,-28,340,56);
        var screen=Box("Schermata mappa",root,new Color(.035f,.055f,.06f,.98f),0,0,1920,1080); Stretch(screen.rectTransform); map.screen=screen.gameObject;
        Text("Titolo","LE MACCHINE DI VITRUVIO",screen.transform,80,-52,1450,65,42,Paper);
        Text("Sottotitolo","PUNTI DI ESTRAZIONE  /  Installa, produci, costruisci",screen.transform,84,-127,1420,42,23,Gold);
        map.closeButton=Button("Chiudi mappa","CHIUDI  [M]",screen.transform,1560,-66,280,60);
        map.summary=Text("Risorse","Credito e materiali",screen.transform,84,-195,1750,55,27,Paper);
        var terrain=Box("Territorio",screen.transform,new Color(.13f,.23f,.21f),80,-280,1100,680);
        Text("Nome territorio","AGRO DI VITRUVIO",terrain.transform,32,-24,1000,45,28,Gold);
        // Editable flat map silhouette and road, built from ordinary UI images.
        Box("Fiume",terrain.transform,new Color(.16f,.34f,.39f),460,-110,90,505).rectTransform.localRotation=Quaternion.Euler(0,0,-14);
        Box("Sentiero",terrain.transform,new Color(.41f,.37f,.26f),95,-305,870,14);
        Text("Legenda","Seleziona un punto per installare una macchina.\nLe macchine attive producono anche a mappa chiusa.",terrain.transform,32,-575,1000,78,23,Paper);
        string[] names={"Bosco settentrionale","Deposito delle colline","Argine del fiume"};
        Vector2[] positions={new Vector2(90,-160),new Vector2(720,-155),new Vector2(490,-400)};
        map.points=new ExtractionMapSystem.Point[3];
        for(int i=0;i<3;i++)
        {
            var button=Button("Punto "+(i+1),names[i]+"\nPunto libero",terrain.transform,positions[i].x,positions[i].y,285,110);
            map.points[i]=new ExtractionMapSystem.Point{name=names[i],button=button,label=button.GetComponentInChildren<TMP_Text>()};
        }
        var side=Box("Dettagli punto",screen.transform,new Color(.09f,.13f,.14f),1220,-280,620,680);
        Text("Intestazione","OFFICINA DI ESTRAZIONE",side.transform,30,-24,560,46,28,Gold);
        map.details=Text("Costo e produzione","Scegli un punto sulla mappa.",side.transform,30,-105,560,310,28,Paper);
        map.installButton=Button("Installa macchina","INSTALLA MACCHINA",side.transform,30,-440,560,72); map.installButton.interactable=false;
        map.message=Text("Messaggio","Acquista legno e ferro dal mercante dei materiali.",side.transform,30,-540,560,115,23,Paper);
        screen.gameObject.SetActive(false); Save();
        return "Saved editable extraction map with 3 points, wood/iron shop rows and machine definition.";
    }
    static MaterialDefinition Material(string name,int price)
    {
        string path="Assets/Data/Construction/"+name+".asset";
        var material=AssetDatabase.LoadAssetAtPath<MaterialDefinition>(path);
        if(material==null) { material=ScriptableObject.CreateInstance<MaterialDefinition>(); material.displayName=name; material.unitPrice=price; AssetDatabase.CreateAsset(material,path); }
        return material;
    }
    static void Save() { Canvas.ForceUpdateCanvases(); EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene()); EditorSceneManager.SaveOpenScenes(); AssetDatabase.SaveAssets(); }
    static RectTransform Rect(string name,Transform parent,float x,float y,float w,float h)
    {
        var go=new GameObject(name,typeof(RectTransform)); go.layer=LayerMask.NameToLayer("UI"); go.transform.SetParent(parent,false);
        var r=(RectTransform)go.transform; r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1); r.anchoredPosition=new Vector2(x,y); r.sizeDelta=new Vector2(w,h); return r;
    }
    static void Stretch(RectTransform r) { r.anchorMin=Vector2.zero; r.anchorMax=Vector2.one; r.offsetMin=r.offsetMax=Vector2.zero; }
    static Image Box(string name,Transform parent,Color color,float x,float y,float w,float h)
    { var r=Rect(name,parent,x,y,w,h); var image=r.gameObject.AddComponent<Image>(); image.color=color; return image; }
    static TMP_Text Text(string name,string text,Transform parent,float x,float y,float w,float h,float size,Color color)
    {
        var r=Rect(name,parent,x,y,w,h); var t=r.gameObject.AddComponent<TextMeshProUGUI>();
        t.font=TMP_Settings.defaultFontAsset; t.text=text; t.fontSize=size; t.color=color; t.raycastTarget=false; return t;
    }
    static Button Button(string name,string text,Transform parent,float x,float y,float w,float h)
    {
        var image=Box(name,parent,Gold,x,y,w,h); var b=image.gameObject.AddComponent<Button>(); b.targetGraphic=image;
        var colors=b.colors; colors.disabledColor=new Color(.42f,.44f,.4f); b.colors=colors;
        Text("Etichetta",text,image.transform,12,0,w-24,h,24,new Color(.06f,.09f,.09f)).alignment=TextAlignmentOptions.Center;
        return b;
    }
}
