using System;
using System.Collections.Generic;
using System.Linq;
using Basilica.Crafting;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

// Explicit authoring only: source FBXs contain left-side structural parts and
// unrelated modelling samples. Bake the selected parts into readable, metre-sized
// meshes rather than instantiating the entire export with millions of leaf vertices.
public static class BasilicaArchitectureEditorSetup
{
    private const string Folder = "Assets/Data/Construction/Architecture";
    private static readonly string[] StageNames = { "Colonnati", "Capitelli", "Pareti", "Architravi", "PianoSuperiore", "TettiInferiori", "TettiSuperiori", "Scale" };
    private static readonly string[] Titles = { "Colonnati e colonne", "Capitelli", "Pareti e ingressi", "Architravi", "Piano superiore", "Tetti inferiori", "Tetti superiori", "Scale e gradini" };

    [MenuItem("Tools/Vitruvio/Prepare basilica architecture placeholders")]
    public static void FromMenu() => Debug.Log(Configure());

    public static string Configure()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path != "Assets/Scenes/GameScene.unity" || scene.isDirty)
            throw new InvalidOperationException("Open and save GameScene first.");
        var site = GameObject.Find("Basilica - Cantiere");
        var system = UnityEngine.Object.FindFirstObjectByType<ConstructionSystem>();
        var spawner = UnityEngine.Object.FindFirstObjectByType<BasilicaCompletionSpawner>();
        if (site == null || system == null || spawner == null || site.transform.lossyScale != Vector3.one)
            throw new InvalidOperationException("The aligned construction site, at unit scale, is required.");
        var point = (Transform)new SerializedObject(spawner).FindProperty("spawnPoint").objectReferenceValue;
        var original = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Meshes/BASILICA FIN 2026.fbx");
        var source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Meshes/ColonnatoInfSX1.fbx");
        var entrance = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Meshes/MuroIngresso.fbx");
        var column = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Meshes/Column.fbx");
        var capital = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Meshes/Capitol.fbx");
        if (new[] { original, source, entrance, column, capital }.Any(p => p == null))
            throw new InvalidOperationException("Missing basilica source FBX.");
        var alignment = point.localToWorldMatrix * original.transform.worldToLocalMatrix;
        float mirrorX = original.GetComponent<Renderer>().bounds.center.x;
        var reflection = Matrix4x4.Translate(new Vector3(mirrorX * 2, 0, 0)) * Matrix4x4.Scale(new Vector3(-1, 1, 1));
        float floor = site.GetComponentsInChildren<PlaceholderSlot>().Where(s => s.BuildPhase == 1)
            .Max(s => s.GetComponent<Renderer>().bounds.max.y);
        var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_placeholder.mat");
        EnsureFolder(Folder);
        Undo.IncrementCurrentGroup();
        int undo = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Prepare basilica architecture");
        var root = site.transform.Find("Architettura");
        if (root == null)
        {
            root = new GameObject("Architettura").transform;
            root.SetParent(site.transform, false);
            Undo.RegisterCreatedObjectUndo(root.gameObject, "Create architectural placeholders");
        }
        var stages = new Transform[StageNames.Length];
        for (int i = 0; i < stages.Length; i++)
        {
            stages[i] = root.Find((i + 2).ToString("00") + " - " + Titles[i]);
            if (stages[i] != null) continue;
            stages[i] = new GameObject((i + 2).ToString("00") + " - " + Titles[i]).transform;
            stages[i].SetParent(root, false);
            Undo.RegisterCreatedObjectUndo(stages[i].gameObject, "Create architecture stage");
        }
        foreach (var filter in source.GetComponentsInChildren<MeshFilter>(true))
        {
            int phase = Phase(filter.name);
            if (phase < 2) continue;
            MeshFilter[] filters;
            if (filter.name == "Cube") filters = column.GetComponentsInChildren<MeshFilter>(true);
            else if (filter.name == "Circle.004" || filter.name == "Cube.005" || filter.name == "Cube.006")
                filters = new[] { entrance.GetComponentsInChildren<MeshFilter>(true).Single(f => f.name == filter.name) };
            else filters = new[] { filter };
            AddPair(filter.name, phase, filters, 0);
        }
        AddPair("Capitol", 3, capital.GetComponentsInChildren<MeshFilter>(true), .025f);
        foreach (var slot in root.GetComponentsInChildren<PlaceholderSlot>(true))
        {
            slot.FitColliderToMesh();
            foreach (var child in slot.GetComponentsInChildren<Transform>(true))
                if (child.name.StartsWith("Punto di posa", StringComparison.Ordinal)) child.gameObject.SetActive(false);
        }
        ConfigureRecipes(system);
        var spawnerData = new SerializedObject(spawner);
        spawnerData.FindProperty("constructionRoot").objectReferenceValue = site;
        spawnerData.ApplyModifiedProperties();
        Undo.CollapseUndoOperations(undo);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Selection.activeGameObject = root.gameObject;
        return "Saved architecture: " + root.GetComponentsInChildren<PlaceholderSlot>(true).Length + " slots in 8 stages, readable meshes, fitted colliders and workbench recipes.";

        void AddPair(string key, int phase, MeshFilter[] filters, float simplify)
        {
            for (int side = 0; side < 2; side++)
            {
                string id = key + (side == 0 ? "_SX" : "_DX");
                var parent = stages[phase - 2];
                if (parent.Find(id) != null) continue;
                var mapping = side == 0 ? alignment : alignment * reflection;
                Bounds bounds = WorldBounds(filters, mapping);
                // Keep the authored mesh origin so existing parts still snap to
                // their saved pose; collider.center follows the actual geometry.
                var position = new Vector3(bounds.center.x, floor + 1, bounds.center.z);
                var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(Folder + "/" + id + ".asset");
                if (mesh == null)
                {
                    mesh = Bake(filters, mapping, position, simplify);
                    mesh.name = id;
                    AssetDatabase.CreateAsset(mesh, Folder + "/" + id + ".asset");
                }
                var go = new GameObject(id, typeof(MeshFilter), typeof(MeshRenderer), typeof(BoxCollider), typeof(Rigidbody));
                go.transform.SetParent(parent, false);
                go.transform.position = position;
                go.transform.rotation = Quaternion.identity;
                go.GetComponent<MeshFilter>().sharedMesh = mesh;
                go.GetComponent<MeshRenderer>().sharedMaterial = material;
                var collider = go.GetComponent<BoxCollider>();
                collider.isTrigger = true;
                var body = go.GetComponent<Rigidbody>(); body.isKinematic = true; body.useGravity = false;
                var slot = go.AddComponent<PlaceholderSlot>();
                var data = new SerializedObject(slot);
                data.FindProperty("pieceId").stringValue = "C1";
                data.FindProperty("buildPhase").intValue = phase;
                data.ApplyModifiedPropertiesWithoutUndo();
                slot.FitColliderToMesh();
                Undo.RegisterCreatedObjectUndo(go, "Create architectural placeholder");
            }
        }
    }

    private static int Phase(string name)
    {
        if (name.StartsWith("Colonnato", StringComparison.Ordinal) || name == "Cube") return 2;
        if (name == "Circle.004") return 4;
        if (name.StartsWith("ColonnaQuadrata.", StringComparison.Ordinal)) return 5;
        if (name.StartsWith("UpperFloor", StringComparison.Ordinal)) return 6;
        if (name.StartsWith("TettoInf", StringComparison.Ordinal) || name == "TettoFronteSx") return 7;
        if (name.StartsWith("TettoSup", StringComparison.Ordinal)) return 8;
        if (!name.StartsWith("Cube.", StringComparison.Ordinal) || !int.TryParse(name.Substring(5), out int index)) return -1;
        if (index == 1 || index == 2 || index == 4) return 8;
        if (index >= 5 && index <= 12) return 4;
        if (index >= 13 && index <= 17) return 5;
        return index == 18 || index == 19 ? 9 : -1;
    }

    private static Bounds WorldBounds(MeshFilter[] filters, Matrix4x4 mapping)
    {
        bool first = true;
        var result = new Bounds();
        foreach (var filter in filters)
        {
            var transform = mapping * filter.transform.localToWorldMatrix;
            var bounds = filter.sharedMesh.bounds;
            for (int i = 0; i < 8; i++)
            {
                var p = transform.MultiplyPoint3x4(bounds.center + Vector3.Scale(bounds.extents,
                    new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1)));
                if (first) { result = new Bounds(p, Vector3.zero); first = false; } else result.Encapsulate(p);
            }
        }
        return result;
    }

    private static Mesh Bake(MeshFilter[] filters, Matrix4x4 mapping, Vector3 origin, float cell)
    {
        var vertices = new List<Vector3>();
        var triangles = new List<int>();
        var cells = new Dictionary<Vector3Int, int>();
        foreach (var filter in filters)
        {
            var source = filter.sharedMesh;
            var transform = mapping * filter.transform.localToWorldMatrix;
            bool reverse = transform.determinant < 0;
            var positions = source.vertices;
            var remap = new int[positions.Length];
            for (int i = 0; i < positions.Length; i++)
            {
                var p = transform.MultiplyPoint3x4(positions[i]) - origin;
                int index;
                if (cell > 0)
                {
                    var key = new Vector3Int(Mathf.RoundToInt(p.x / cell), Mathf.RoundToInt(p.y / cell), Mathf.RoundToInt(p.z / cell));
                    if (!cells.TryGetValue(key, out index)) { index = vertices.Count; cells.Add(key, index); vertices.Add(p); }
                }
                else { index = vertices.Count; vertices.Add(p); }
                remap[i] = index;
            }
            var indices = source.triangles;
            for (int i = 0; i < indices.Length; i += 3)
            {
                int a = remap[indices[i]], b = remap[indices[i + 1]], c = remap[indices[i + 2]];
                if (a == b || b == c || a == c) continue;
                triangles.Add(a); triangles.Add(reverse ? c : b); triangles.Add(reverse ? b : c);
            }
        }
        var mesh = new Mesh { indexFormat = vertices.Count > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
        mesh.SetVertices(vertices); mesh.SetTriangles(triangles, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
        return mesh;
    }

    private static void ConfigureRecipes(ConstructionSystem system)
    {
        var stone = AssetDatabase.LoadAssetAtPath<MaterialDefinition>("Assets/Data/Construction/Pietra.asset");
        var lime = AssetDatabase.LoadAssetAtPath<MaterialDefinition>("Assets/Data/Construction/Calce.asset");
        var wood = AssetDatabase.LoadAssetAtPath<MaterialDefinition>("Assets/Data/Construction/Legno.asset");
        var iron = AssetDatabase.LoadAssetAtPath<MaterialDefinition>("Assets/Data/Construction/Ferro.asset");
        var piece = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/INTERACTABLE/Cement_Sack_Piece Variant.prefab");
        var ui = UnityEngine.Object.FindFirstObjectByType<ConstructionUI>(FindObjectsInactive.Include);
        var content = ui.transform.Find("Screens/Ricette/Panel/Viewport/Content");
        var data = new SerializedObject(system);
        var offered = data.FindProperty("recipes");
        for (int i = 0; i < StageNames.Length; i++)
        {
            string path = Folder + "/" + StageNames[i] + ".asset";
            var recipe = AssetDatabase.LoadAssetAtPath<CraftingRecipe>(path);
            if (recipe == null)
            {
                recipe = ScriptableObject.CreateInstance<CraftingRecipe>();
                recipe.displayName = Titles[i]; recipe.buildPhase = i + 2; recipe.piecePrefab = piece;
                recipe.costs = i == 5 || i == 6 ? new[] { new MaterialCost(wood, i == 5 ? 4 : 6), new MaterialCost(iron, 2) } :
                    new[] { new MaterialCost(stone, i == 1 || i == 7 ? 2 : i == 2 ? 6 : 4), new MaterialCost(lime, 2) };
                AssetDatabase.CreateAsset(recipe, path);
            }
            if (!Enumerable.Range(0, offered.arraySize).Any(n => offered.GetArrayElementAtIndex(n).objectReferenceValue == recipe))
                offered.GetArrayElementAtIndex(offered.arraySize++).objectReferenceValue = recipe;
            if (!content.GetComponentsInChildren<ConstructionRecipeRow>(true).Any(row => row.recipe == recipe))
                ConstructionUIEditorSetup.RecipeRow(content, recipe);
        }
        data.ApplyModifiedProperties();
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        int slash = path.LastIndexOf('/');
        EnsureFolder(path.Substring(0, slash));
        AssetDatabase.CreateFolder(path.Substring(0, slash), path.Substring(slash + 1));
    }
}
