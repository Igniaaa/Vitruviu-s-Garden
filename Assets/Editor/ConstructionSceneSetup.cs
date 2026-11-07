using System;
using System.Linq;
using Basilica.Crafting;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Audio;

// Explicit, repeatable setup. It never runs automatically when the project loads.
public static class ConstructionSceneSetup
{
    private const string Folder = "Assets/Data/Construction";

    [MenuItem("Tools/Vitruvio/Configure construction crafting")]
    public static void ConfigureFromMenu() => Debug.Log(Configure());

    public static string Configure()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path != "Assets/Scenes/GameScene.unity") throw new InvalidOperationException("Open GameScene before configuring.");
        EnsureFolder(Folder);
        var stone = Material("Pietra", 3);
        var lime = Material("Calce", 2);
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/INTERACTABLE/Cement_Sack_Piece Variant.prefab");
        if (prefab == null || prefab.GetComponent<DraggableObject>() == null) throw new InvalidOperationException("Missing construction piece prefab.");
        var foundations = Recipe("Fondamenta", 0, prefab, stone, lime, 4);
        var pavement = Recipe("Pavimento", 1, prefab, stone, lime, 6);
        var manager = UnityEngine.Object.FindFirstObjectByType<MonumentProgressTracker>();
        if (manager == null) throw new InvalidOperationException("Missing progress tracker.");
        var system = manager.GetComponent<ConstructionSystem>();
        if (system == null)
        {
            system = Undo.AddComponent<ConstructionSystem>(manager.gameObject);
            var so = new SerializedObject(system);
            SetArray(so.FindProperty("materials"), new UnityEngine.Object[] { stone, lime });
            SetArray(so.FindProperty("recipes"), new UnityEngine.Object[] { foundations, pavement });
            so.FindProperty("progressTracker").objectReferenceValue = manager;
            so.ApplyModifiedProperties();
            var audio = new SerializedObject(system.GetComponent<RhythmAudio>());
            var mixerGroup = AssetDatabase.FindAssets("t:AudioMixerGroup").Select(AssetDatabase.GUIDToAssetPath)
                .SelectMany(AssetDatabase.LoadAllAssetsAtPath).OfType<AudioMixerGroup>()
                .FirstOrDefault(g => g.name.IndexOf("SFX", StringComparison.OrdinalIgnoreCase) >= 0);
            if (mixerGroup != null) { audio.FindProperty("output").objectReferenceValue = mixerGroup; audio.ApplyModifiedProperties(); }
        }
        foreach (var old in UnityEngine.Object.FindObjectsByType<PiecePhaseSpawner>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        { Undo.RecordObject(old, "Disable automatic construction pieces"); old.enabled = false; EditorUtility.SetDirty(old); }

        var root = GameObject.Find("ConstructionStations");
        if (root == null)
        {
            root = new GameObject("ConstructionStations");
            Undo.RegisterCreatedObjectUndo(root, "Create merchant and workbench");
            var wood = Surface("Legno", new Color(.28f, .16f, .085f));
            var top = Surface("Banco", new Color(.48f, .32f, .17f));
            var rock = Surface("Pietra chiara", new Color(.62f, .6f, .5f));
            var gold = Surface("Insegna", new Color(.73f, .47f, .16f));
            Station(root.transform, "Mercante materiali", ConstructionStationKind.Merchant, new Vector3(464, 0, 518), wood, top, rock, gold);
            Station(root.transform, "Banco da lavoro", ConstructionStationKind.Workbench, new Vector3(457, 0, 518), wood, top, rock, gold);
        }
        ConstructionUIEditorSetup.Configure();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        return "Configured merchant, workbench, 2 materials and 2 recipes. Automatic piece spawn disabled. Scene saved.";
    }

    private static void Station(Transform parent, string name, ConstructionStationKind kind, Vector3 position,
        UnityEngine.Material wood, UnityEngine.Material top, UnityEngine.Material rock, UnityEngine.Material gold)
    {
        if (Terrain.activeTerrain != null)
            position.y = Terrain.activeTerrain.SampleHeight(position) + Terrain.activeTerrain.transform.position.y;
        else position.y = 3;
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = position;
        var station = go.AddComponent<ConstructionStation>();
        station.kind = kind;
        var box = go.AddComponent<BoxCollider>();
        box.center = new Vector3(0, .7f, 0);
        box.size = new Vector3(3, 1.4f, 1.4f);
        Cube("Piano", go.transform, new Vector3(0, 1.3f, 0), new Vector3(3.2f, .22f, 1.5f), top);
        foreach (float x in new[] { -1.3f, 1.3f })
            foreach (float z in new[] { -.5f, .5f }) Cube("Gamba", go.transform, new Vector3(x, .6f, z), new Vector3(.22f, 1.2f, .22f), wood);
        Cube("Supporto insegna", go.transform, new Vector3(0, 2, .62f), new Vector3(.12f, 1.5f, .12f), wood);
        Cube("Insegna", go.transform, new Vector3(0, 2.6f, .6f), new Vector3(3, .65f, .14f), gold);
        var sign = new GameObject("Nome", typeof(TextMeshPro));
        sign.transform.SetParent(go.transform, false);
        sign.transform.localPosition = new Vector3(0, 2.6f, .51f);
        var label = sign.GetComponent<TextMeshPro>();
        label.text = kind == ConstructionStationKind.Merchant ? "MATERIALI" : "BANCO DA LAVORO";
        label.fontSize = 3;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
        label.rectTransform.sizeDelta = new Vector2(2.9f, .58f);
        if (kind == ConstructionStationKind.Merchant)
        {
            for (int i = 0; i < 3; i++)
                Cube("Scorta pietra", go.transform, new Vector3(-.9f + i * .55f, 1.6f, .1f), new Vector3(.42f, .38f, .45f), rock);
            Cube("Calce", go.transform, new Vector3(1, 1.65f, .1f), new Vector3(.5f, .5f, .5f), rock);
        }
        else
        {
            var output = new GameObject("Uscita pezzo");
            output.transform.SetParent(go.transform, false);
            output.transform.localPosition = new Vector3(0, 1.95f, 0);
            station.outputPoint = output.transform;
            Cube("Martello", go.transform, new Vector3(1.15f, 1.48f, 0), new Vector3(.12f, .12f, .7f), wood);
            Cube("Testa martello", go.transform, new Vector3(1.15f, 1.5f, .25f), new Vector3(.42f, .2f, .2f), rock);
        }
    }

    private static void Cube(string name, Transform parent, Vector3 position, Vector3 scale, UnityEngine.Material material)
    {
        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = name;
        cube.transform.SetParent(parent, false);
        cube.transform.localPosition = position;
        cube.transform.localScale = scale;
        cube.GetComponent<Renderer>().sharedMaterial = material;
        UnityEngine.Object.DestroyImmediate(cube.GetComponent<Collider>());
    }

    private static MaterialDefinition Material(string label, int price)
    {
        string path = Folder + "/" + label + ".asset";
        var asset = AssetDatabase.LoadAssetAtPath<MaterialDefinition>(path);
        if (asset != null) return asset;
        asset = ScriptableObject.CreateInstance<MaterialDefinition>();
        asset.displayName = label; asset.unitPrice = price;
        AssetDatabase.CreateAsset(asset, path);
        return asset;
    }

    private static CraftingRecipe Recipe(string label, int phase, GameObject prefab, MaterialDefinition stone, MaterialDefinition lime, int stones)
    {
        string path = Folder + "/" + label + ".asset";
        var asset = AssetDatabase.LoadAssetAtPath<CraftingRecipe>(path);
        if (asset != null) return asset;
        asset = ScriptableObject.CreateInstance<CraftingRecipe>();
        asset.displayName = label; asset.buildPhase = phase; asset.piecePrefab = prefab;
        asset.costs = new[] { new MaterialCost(stone, stones), new MaterialCost(lime, 2) };
        AssetDatabase.CreateAsset(asset, path);
        return asset;
    }

    private static UnityEngine.Material Surface(string label, Color color)
    {
        string path = Folder + "/" + label + ".mat";
        var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Material>(path);
        if (asset != null) return asset;
        asset = new UnityEngine.Material(Shader.Find("Universal Render Pipeline/Lit"));
        asset.color = color;
        AssetDatabase.CreateAsset(asset, path);
        return asset;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        int slash = path.LastIndexOf('/');
        EnsureFolder(path.Substring(0, slash));
        AssetDatabase.CreateFolder(path.Substring(0, slash), path.Substring(slash + 1));
    }

    private static void SetArray(SerializedProperty property, UnityEngine.Object[] values)
    {
        property.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
    }
}
