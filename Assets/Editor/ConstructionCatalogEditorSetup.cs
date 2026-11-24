using System;
using System.IO;
using System.Linq;
using Basilica.Crafting;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Explicit authoring: all tabs, cards and images are saved before Play Mode.
public static class ConstructionCatalogEditorSetup
{
    private const string Icons = "Assets/UI/Construction/Silhouettes";
    private static readonly Color Ink = new Color(.065f, .085f, .09f);
    private static readonly Color Card = new Color(.12f, .16f, .17f);
    private static readonly Color Paper = new Color(.88f, .85f, .77f);
    private static readonly Color Gold = new Color(.91f, .68f, .32f);
    private static readonly Color Muted = new Color(.64f, .7f, .68f);
    private static TMP_FontAsset font;

    [MenuItem("Tools/Vitruvio/Prepare construction categories and silhouettes")]
    public static void FromMenu() => Debug.Log(Configure());

    public static string RegenerateFoundationAndFloorSackSilhouettes()
    {
        foreach (string recipeName in new[] { "Fondamenta", "Pavimento" })
        {
            var recipe = AssetDatabase.LoadAssetAtPath<CraftingRecipe>("Assets/Data/Construction/" + recipeName + ".asset");
            if (recipe == null || recipe.piecePrefab == null)
                throw new InvalidOperationException("Recipe or lime sack prefab missing: " + recipeName);
            GenerateSilhouette(recipe.piecePrefab, Icons + "/" + recipeName + ".png");
        }
        return "Regenerated foundation and floor silhouettes from their lime sack prefab.";
    }

    public static string Configure()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path != "Assets/Scenes/GameScene.unity" || scene.isDirty) throw new InvalidOperationException("Open and save GameScene first.");
        var ui = UnityEngine.Object.FindFirstObjectByType<ConstructionUI>(FindObjectsInactive.Include);
        if (ui == null) throw new InvalidOperationException("The authored construction UI is missing.");
        var screen = ui.transform.Find("Screens/Ricette");
        var panel = screen.Find("Panel");
        var viewport = panel.Find("Viewport");
        var rows = screen.GetComponentsInChildren<ConstructionRecipeRow>(true);
        if (rows.Length == 0) throw new InvalidOperationException("No authored recipe cards found.");
        font = panel.Find("Titolo").GetComponent<TMP_Text>().font;
        Undo.IncrementCurrentGroup();
        int undo = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Prepare construction categories and silhouettes");
        Undo.RegisterFullObjectHierarchyUndo(screen.gameObject, "Prepare construction catalog");
        Place((RectTransform)panel, 0, 0, 1440, 880, new Vector2(.5f, .5f), new Vector2(.5f, .5f));
        Place(panel.Find("Linea dorata").GetComponent<RectTransform>(), 0, 0, 1440, 5);
        Place(panel.Find("Intestazione").GetComponent<RectTransform>(), 40, -28, 1356, 28);
        Place(panel.Find("Titolo").GetComponent<RectTransform>(), 40, -68, 1356, 62);
        Place(panel.Find("Riepilogo").GetComponent<RectTransform>(), 40, -140, 1356, 64);
        Place((RectTransform)viewport, 300, -245, 1096, 436);
        Place(panel.Find("Messaggio").GetComponent<RectTransform>(), 300, -688, 1096, 76);
        Place(panel.Find("Istruzioni").GetComponent<RectTransform>(), 40, -770, 1050, 74);
        panel.Find("Istruzioni").GetComponent<TMP_Text>().text = "Scegli una categoria e consulta le sagome dei pezzi.\nCompleta la fase corrente per sbloccare quelle successive.";
        Place(panel.Find("CHIUDI [E]").GetComponent<RectTransform>(), 1158, -788, 226, 52);
        var content = viewport.Find("Content").GetComponent<RectTransform>();
        if (content.GetComponent<VerticalLayoutGroup>() != null)
        {
            var previous = content;
            previous.name = "Elenco precedente";
            content = Rect("Content", viewport);
            Place(content, 0, 0, 1096, 416);
            foreach (var row in rows) Undo.SetTransformParent(row.transform, content, "Move recipe to catalog");
            previous.gameObject.SetActive(false);
        }
        var grid = content.GetComponent<GridLayoutGroup>() ?? Undo.AddComponent<GridLayoutGroup>(content.gameObject);
        grid.cellSize = new Vector2(536, 416); grid.spacing = new Vector2(16, 16);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount; grid.constraintCount = 2;
        grid.childAlignment = TextAnchor.UpperLeft;
        var fitter = content.GetComponent<ContentSizeFitter>() ?? Undo.AddComponent<ContentSizeFitter>(content.gameObject);
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        var scroll = viewport.GetComponent<ScrollRect>(); scroll.content = content;
        var sidebar = panel.Find("Categorie") as RectTransform ?? Rect("Categorie", panel);
        Place(sidebar, 40, -245, 230, 436);
        Text("Intestazione categorie", "CATEGORIE", panel, 40, -211, 230, 25, 16, Muted);
        var title = Text("Categoria selezionata", "FONDAMENTA", panel, 300, -211, 1096, 25, 18, Gold);
        var categories = (ConstructionPieceCategory[])Enum.GetValues(typeof(ConstructionPieceCategory));
        for (int i = 0; i < categories.Length; i++)
        {
            var category = categories[i];
            var rect = sidebar.Find(category.ToString()) as RectTransform ?? Rect(category.ToString(), sidebar);
            Place(rect, 0, -i * 62, 230, 52);
            var background = rect.GetComponent<Image>() ?? Undo.AddComponent<Image>(rect.gameObject); background.color = Card;
            var button = rect.GetComponent<Button>() ?? Undo.AddComponent<Button>(rect.gameObject); button.targetGraphic = background;
            var label = Text("Etichetta", ConstructionPieceCategories.Title(category).ToUpperInvariant(), rect, 16, 0, 198, 52, 19, Paper);
            label.alignment = TextAlignmentOptions.MidlineLeft;
            var tab = rect.GetComponent<ConstructionCategoryButton>() ?? Undo.AddComponent<ConstructionCategoryButton>(rect.gameObject);
            tab.category = category; tab.button = button; tab.background = background; tab.label = label;
            tab.Refresh(ConstructionPieceCategory.Foundations);
            EditorUtility.SetDirty(tab);
        }
        EnsureFolder(Icons);
        var slots = GameObject.Find("Basilica - Cantiere").GetComponentsInChildren<PlaceholderSlot>(true);
        string[] preferred = { "", "", "ColonnatoInferioreSX1_SX", "Capitol_SX", "Circle.004_SX", "ColonnaQuadrata.001_SX", "UpperFloorL1_SX", "TettoFronteSx_SX", "TettoSupSx_SX", "Cube.018_SX" };
        foreach (var row in rows)
        {
            int phase = row.recipe.buildPhase;
            var slot = slots.FirstOrDefault(s => s.BuildPhase == phase && phase < preferred.Length && s.name == preferred[phase])
                ?? slots.First(s => s.BuildPhase == phase);
            row.category = ConstructionPieceCategories.ForPhase(phase);
            Place((RectTransform)row.transform, 0, 0, 536, 416);
            Place(row.nameText.rectTransform, 18, -222, 500, 36); row.nameText.fontSize = 25;
            Place(row.costText.rectTransform, 18, -264, 500, 54); row.costText.fontSize = 19;
            Place(row.statusText.rectTransform, 18, -326, 295, 70); row.statusText.fontSize = 18;
            Place((RectTransform)row.craftButton.transform, 336, -346, 182, 52);
            Place(row.actionText.rectTransform, 8, 0, 166, 52); row.actionText.fontSize = 19;
            var frame = Box("Riquadro silhouette", row.transform, Paper, 18, -16, 500, 190);
            frame.raycastTarget = false;
            var image = Box("Silhouette", frame.transform, Color.white, 16, -8, 468, 174);
            image.preserveAspect = true; image.raycastTarget = false;
            image.sprite = phase <= 1
                ? GenerateSilhouette(row.recipe.piecePrefab, Icons + "/" + row.recipe.name + ".png")
                : GenerateSilhouette(slot, Icons + "/" + row.recipe.name + ".png");
            row.silhouette = image;
            row.gameObject.SetActive(row.category == ConstructionPieceCategory.Foundations);
            EditorUtility.SetDirty(row);
        }
        var data = new SerializedObject(ui);
        data.FindProperty("categoryTitle").objectReferenceValue = title;
        data.FindProperty("recipeScroll").objectReferenceValue = scroll;
        data.FindProperty("selectedCategory").enumValueIndex = (int)ConstructionPieceCategory.Foundations;
        data.ApplyModifiedProperties();
        Canvas.ForceUpdateCanvases();
        Undo.CollapseUndoOperations(undo);
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        Selection.activeGameObject = screen.gameObject;
        return "Saved 7 categories and " + rows.Length + " recipe cards with black mesh silhouettes.";
    }

    private static Sprite GenerateSilhouette(PlaceholderSlot slot, string path)
    {
        var preview = new PreviewRenderUtility();
        var material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        material.SetColor("_BaseColor", Color.black); material.SetFloat("_Cull", 0);
        Texture2D image = null;
        try
        {
            var mesh = slot.GetComponent<MeshFilter>().sharedMesh;
            var go = new GameObject("Silhouette source", typeof(MeshFilter), typeof(MeshRenderer));
            go.GetComponent<MeshFilter>().sharedMesh = mesh; go.GetComponent<MeshRenderer>().sharedMaterial = material;
            go.transform.rotation = slot.transform.rotation; go.transform.localScale = slot.transform.lossyScale;
            go.transform.position = -go.transform.TransformVector(mesh.bounds.center);
            preview.AddSingleGO(go);
            var bounds = go.GetComponent<Renderer>().bounds;
            var camera = preview.camera;
            float distance = bounds.size.magnitude * 2 + 1;
            camera.transform.position = bounds.center + new Vector3(1.2f, .9f, -1.3f).normalized * distance;
            camera.transform.LookAt(bounds.center);
            camera.orthographic = true; camera.aspect = 2;
            camera.nearClipPlane = .01f; camera.farClipPlane = distance * 3;
            // Static preview readback discards alpha. Render a monochrome mask
            // against white, then put its coverage into a fresh RGBA texture.
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.white;
            camera.allowHDR = false;
            float x = 0, y = 0;
            for (int i = 0; i < 8; i++)
            {
                var corner = Vector3.Scale(bounds.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                var view = camera.transform.InverseTransformVector(corner);
                x = Mathf.Max(x, Mathf.Abs(view.x)); y = Mathf.Max(y, Mathf.Abs(view.y));
            }
            camera.orthographicSize = Mathf.Max(.05f, y, x / 2) * 1.12f;
            preview.BeginStaticPreview(new Rect(0, 0, 512, 256));
            preview.Render(true, false);
            image = preview.EndStaticPreview();
            var pixels = image.GetPixels32();
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = new Color32(0, 0, 0, (byte)(255 - pixels[i].r));
            var mask = new Texture2D(image.width, image.height, TextureFormat.RGBA32, false);
            mask.SetPixels32(pixels); mask.Apply();
            UnityEngine.Object.DestroyImmediate(image); image = mask;
            File.WriteAllBytes(path, image.EncodeToPNG());
        }
        finally
        {
            if (image != null) UnityEngine.Object.DestroyImmediate(image);
            preview.Cleanup(); UnityEngine.Object.DestroyImmediate(material);
        }
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true; importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static Sprite GenerateSilhouette(GameObject prefab, string path)
    {
        var preview = new PreviewRenderUtility();
        var material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        material.SetColor("_BaseColor", Color.black); material.SetFloat("_Cull", 0);
        GameObject source = null;
        Texture2D image = null;
        try
        {
            source = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            source.name = "Silhouette source";
            source.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            source.transform.localScale = Vector3.one;
            var renderers = source.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) throw new InvalidOperationException("The recipe prefab has no renderers.");
            Bounds bounds = renderers[0].bounds;
            foreach (var renderer in renderers)
            {
                bounds.Encapsulate(renderer.bounds);
                renderer.sharedMaterials = Enumerable.Repeat(material, Mathf.Max(1, renderer.sharedMaterials.Length)).ToArray();
            }
            preview.AddSingleGO(source);
            var camera = preview.camera;
            float distance = bounds.size.magnitude * 2 + 1;
            camera.transform.position = bounds.center + new Vector3(1.2f, .9f, -1.3f).normalized * distance;
            camera.transform.LookAt(bounds.center);
            camera.orthographic = true; camera.aspect = 2;
            camera.nearClipPlane = .01f; camera.farClipPlane = distance * 3;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.white;
            camera.allowHDR = false;
            float x = 0, y = 0;
            for (int i = 0; i < 8; i++)
            {
                var corner = Vector3.Scale(bounds.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                var view = camera.transform.InverseTransformVector(corner);
                x = Mathf.Max(x, Mathf.Abs(view.x)); y = Mathf.Max(y, Mathf.Abs(view.y));
            }
            camera.orthographicSize = Mathf.Max(.05f, y, x / 2) * 1.12f;
            preview.BeginStaticPreview(new Rect(0, 0, 512, 256));
            preview.Render(true, false);
            image = preview.EndStaticPreview();
            var pixels = image.GetPixels32();
            for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color32(0, 0, 0, (byte)(255 - pixels[i].r));
            var mask = new Texture2D(image.width, image.height, TextureFormat.RGBA32, false);
            mask.SetPixels32(pixels); mask.Apply();
            UnityEngine.Object.DestroyImmediate(image); image = mask;
            File.WriteAllBytes(path, image.EncodeToPNG());
        }
        finally
        {
            if (image != null) UnityEngine.Object.DestroyImmediate(image);
            if (source != null) UnityEngine.Object.DestroyImmediate(source);
            preview.Cleanup(); UnityEngine.Object.DestroyImmediate(material);
        }
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true; importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static RectTransform Rect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform)); go.layer = LayerMask.NameToLayer("UI");
        go.transform.SetParent(parent, false);
        Undo.RegisterCreatedObjectUndo(go, "Create construction catalog element");
        return (RectTransform)go.transform;
    }

    private static Image Box(string name, Transform parent, Color color, float x, float y, float w, float h)
    {
        var rect = parent.Find(name) as RectTransform ?? Rect(name, parent); Place(rect, x, y, w, h);
        var image = rect.GetComponent<Image>() ?? Undo.AddComponent<Image>(rect.gameObject); image.color = color; return image;
    }

    private static TMP_Text Text(string name, string value, Transform parent, float x, float y, float w, float h, float size, Color color)
    {
        var rect = parent.Find(name) as RectTransform ?? Rect(name, parent); Place(rect, x, y, w, h);
        var label = rect.GetComponent<TextMeshProUGUI>() ?? Undo.AddComponent<TextMeshProUGUI>(rect.gameObject);
        label.font = font; label.text = value; label.fontSize = size; label.color = color; label.raycastTarget = false;
        return label;
    }

    private static void Place(RectTransform rect, float x, float y, float w, float h, Vector2? anchor = null, Vector2? pivot = null)
    {
        rect.anchorMin = rect.anchorMax = anchor ?? new Vector2(0, 1);
        rect.pivot = pivot ?? new Vector2(0, 1); rect.anchoredPosition = new Vector2(x, y); rect.sizeDelta = new Vector2(w, h);
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        int slash = path.LastIndexOf('/'); EnsureFolder(path.Substring(0, slash));
        AssetDatabase.CreateFolder(path.Substring(0, slash), path.Substring(slash + 1));
    }
}
