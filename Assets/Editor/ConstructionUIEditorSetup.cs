using System;
using System.Collections.Generic;
using System.Linq;
using Basilica.Crafting;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// One-time authoring tool. It never runs automatically or in a build, and never
// regenerates an existing UI: all subsequent visual edits belong to the scene.
public static class ConstructionUIEditorSetup
{
    private static readonly Color Ink = new Color(.065f, .085f, .09f);
    private static readonly Color Paper = new Color(.94f, .91f, .82f);
    private static readonly Color Gold = new Color(.91f, .68f, .32f);
    private static readonly Color Muted = new Color(.64f, .7f, .68f);
    private static TMP_FontAsset font;

    private sealed class Screen
    {
        public GameObject root;
        public RectTransform panel, content;
        public TMP_Text title, summary, message;
        public Button close;
    }

    [MenuItem("Tools/Vitruvio/Prepare editable construction UI")]
    public static void FromMenu() => Debug.Log(Configure());

    public static string Configure()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path != "Assets/Scenes/GameScene.unity") throw new InvalidOperationException("Open GameScene first.");
        var system = UnityEngine.Object.FindFirstObjectByType<ConstructionSystem>();
        if (system == null) throw new InvalidOperationException("ConstructionSystem is missing.");
        var canvas = GameObject.Find("Canvas");
        if (canvas == null) canvas = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .FirstOrDefault(c => c.isRootCanvas && c.gameObject.scene == scene)?.gameObject;
        if (canvas == null || canvas.GetComponent<Canvas>() == null) throw new InvalidOperationException("The scene Canvas is missing.");
        var savedUI = canvas.GetComponentInChildren<ConstructionUI>(true);
        var existing = savedUI != null ? savedUI.transform : canvas.transform.Find("Construction UI");
        if (existing != null)
        {
            var authored = existing.GetComponent<ConstructionUI>();
            if (authored == null || !authored.IsConfigured)
                throw new InvalidOperationException("Construction UI already exists but has missing references. Repair its Inspector fields; it will not be overwritten.");
            Set(system, "view", authored);
            EditorSceneManager.SaveScene(scene);
            return "Existing editable UI linked; manual layout preserved.";
        }

        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
        if (font == null) font = TMP_Settings.defaultFontAsset;
        var root = Rect("Construction UI", canvas.transform);
        Stretch(root);
        Undo.RegisterCreatedObjectUndo(root.gameObject, "Prepare editable crafting UI");
        var nestedCanvas = root.gameObject.AddComponent<Canvas>();
        nestedCanvas.overrideSorting = true;
        nestedCanvas.sortingOrder = 30;
        root.gameObject.AddComponent<GraphicRaycaster>();
        var ui = root.gameObject.AddComponent<ConstructionUI>();
        var screens = Rect("Screens", root);
        Stretch(screens);

        var shop = Standard("Acquisto", screens, "Il mercante dei materiali",
            "Pietra e calce restano nell'inventario.\nIl costo viene mostrato su ogni acquisto.");
        var bench = Standard("Ricette", screens, "Banco da lavoro",
            "Riuscita: costo intero. Perfetto: metà costo.\nFallimento o annullamento: restituisce metà dei materiali.");
        var rhythm = Standard("Ritmo", screens, "Al ritmo del cantiere",
            "Premere in anticipo fa perdere la nota.\nESC mette in pausa, senza penalità.", false);
        var failed = Standard("Fallimento", screens, "Lavorazione non riuscita", "Chiudi il banco per tornare al cantiere.");
        var complete = Completed(screens);
        var config = new SerializedObject(system);
        var materials = config.FindProperty("materials");
        var recipes = config.FindProperty("recipes");
        ListLayout(shop.content);
        ListLayout(bench.content);
        for (int i = 0; i < materials.arraySize; i++)
        {
            var material = materials.GetArrayElementAtIndex(i).objectReferenceValue as MaterialDefinition;
            if (material != null) MaterialRow(shop.content, material);
        }
        for (int i = 0; i < recipes.arraySize; i++)
        {
            var recipe = recipes.GetArrayElementAtIndex(i).objectReferenceValue as CraftingRecipe;
            if (recipe != null) RecipeRow(bench.content, recipe);
        }

        var score = Label("Punteggio", "Preparati…", rhythm.content, 0, -4, 1032, 44, 27, Gold);
        var lane = Box("Corsia", rhythm.content, new Color(.11f, .15f, .16f)).rectTransform;
        Place(lane, 0, -70, 1032, 130);
        lane.gameObject.AddComponent<RectMask2D>();
        var window = Box("Finestra colpo", lane, new Color(Gold.r, Gold.g, Gold.b, .16f)).rectTransform;
        MiddleLeft(window, 200, 0, 66, 114);
        var line = Box("Linea colpo", lane, Gold).rectTransform;
        MiddleLeft(line, 200, 0, 3, 114);
        var notes = new List<Image>();
        var judgments = new List<Image>();
        var tallyRoot = Rect("Giudizi", rhythm.content);
        Place(tallyRoot, 0, -216, 1032, 42);
        var grid = tallyRoot.gameObject.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(26, 16);
        grid.spacing = new Vector2(4, 4);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 32;
        for (int i = 0; i < 64; i++)
        {
            var note = Box("Nota " + (i + 1).ToString("00"), lane, Paper);
            MiddleLeft(note.rectTransform, 200 + i * 120, 0, 28, 40);
            note.gameObject.SetActive(i < 8);
            notes.Add(note);
            var judgment = Box("Giudizio " + (i + 1).ToString("00"), tallyRoot, Muted);
            judgment.gameObject.SetActive(i < 8);
            judgments.Add(judgment);
        }
        var cancel = Button("ANNULLA", "ANNULLA  ·  rimborso 50%", rhythm.content, 658, -260, 374, 48);
        var failedMessage = Label("Dettagli esito", "Qui appariranno esito e materiali rimborsati.", failed.content, 0, -20, 1000, 190, 25, Paper);
        var back = Button("TORNA AL BANCO", "TORNA AL BANCO", failed.content, 0, -246, 330, 52);
        var completeMessage = Label("Dettagli pezzo", "Il pezzo è pronto sul tavolo.", complete.content, 0, 0, 544, 124, 20, Paper);
        Outline(completeMessage);
        var storage = Button("SPOSTA NEL RECINTO", "SPOSTA NEL RECINTO", complete.panel, 252, -394, 320, 48);

        Set(ui, "screensRoot", screens.gameObject);
        Set(ui, "shopScreen", shop.root); Set(ui, "workbenchScreen", bench.root);
        Set(ui, "rhythmScreen", rhythm.root); Set(ui, "completedScreen", complete.root); Set(ui, "failedScreen", failed.root);
        Set(ui, "shopSummary", shop.summary); Set(ui, "shopMessage", shop.message);
        Set(ui, "workbenchSummary", bench.summary); Set(ui, "workbenchMessage", bench.message);
        Set(ui, "rhythmSummary", rhythm.summary); Set(ui, "rhythmMessage", rhythm.message); Set(ui, "score", score);
        Set(ui, "completedTitle", complete.title); Set(ui, "completedSummary", complete.summary); Set(ui, "completedMessage", completeMessage);
        Set(ui, "failedTitle", failed.title); Set(ui, "failedSummary", failed.summary); Set(ui, "failedMessage", failedMessage);
        Set(ui, "cancelButton", cancel); Set(ui, "backButton", back); Set(ui, "storageButton", storage);
        Set(ui, "hitLine", line); Set(ui, "hitWindow", window);
        SetArray(ui, "closeButtons", new UnityEngine.Object[] { shop.close, bench.close, complete.close, failed.close });
        SetArray(ui, "notes", notes.Cast<UnityEngine.Object>().ToArray());
        SetArray(ui, "tally", judgments.Cast<UnityEngine.Object>().ToArray());
        Set(system, "view", ui);
        foreach (var screen in new[] { shop, bench, rhythm, complete, failed }) screen.root.SetActive(screen == shop);
        if (!ui.IsConfigured) throw new InvalidOperationException("Saved UI references are incomplete.");
        Canvas.ForceUpdateCanvases();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Selection.activeGameObject = root.gameObject;
        return "Editable UI saved at Canvas/Construction UI: 5 screens, material/recipe rows, 64 notes and judgments. Select a screen in Edit Mode to customize it.";
    }

    private static Screen Standard(string name, Transform parent, string title, string instructions, bool close = true)
    {
        var overlay = Box(name, parent, new Color(.01f, .02f, .02f, .75f)).rectTransform;
        Stretch(overlay);
        var panel = Box("Panel", overlay, Ink).rectTransform;
        Place(panel, 0, 0, 1120, 780, new Vector2(.5f, .5f), new Vector2(.5f, .5f));
        Place(Box("Linea dorata", panel, Gold).rectTransform, 0, 0, 1120, 5);
        Label("Intestazione", "CANTIERE DELLA BASILICA", panel, 44, -34, 1000, 28, 18, Gold);
        var heading = Label("Titolo", title, panel, 44, -80, 1000, 62, 46, Paper);
        var summary = Label("Riepilogo", "Credito di cantiere: 200 monete", panel, 44, -153, 1000, 72, 23, Muted);
        var viewport = Box("Viewport", panel, Color.clear).rectTransform;
        Place(viewport, 44, -243, 1032, 310);
        viewport.gameObject.AddComponent<RectMask2D>();
        var content = Rect("Content", viewport);
        Place(content, 0, 0, 1032, 310);
        var scroll = viewport.gameObject.AddComponent<ScrollRect>();
        scroll.viewport = viewport; scroll.content = content; scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 28;
        var message = Label("Messaggio", "", panel, 44, -565, 1000, 82, 21, Gold);
        Label("Istruzioni", instructions, panel, 44, -665, 760, 73, 18, Muted);
        return new Screen { root = overlay.gameObject, panel = panel, content = content, title = heading, summary = summary,
            message = message, close = close ? Button("CHIUDI [E]", "CHIUDI  [E]", panel, 850, -686, 226, 52) : null };
    }

    private static Screen Completed(Transform parent)
    {
        var overlay = Box("Completato", parent, Color.clear).rectTransform;
        Stretch(overlay);
        var panel = Box("Panel", overlay, new Color(Ink.r, Ink.g, Ink.b, .18f)).rectTransform;
        Place(panel, -32, 32, 600, 466, new Vector2(1, 0), new Vector2(1, 0));
        Place(Box("Linea dorata", panel, Gold).rectTransform, 0, 0, 600, 4);
        var brand = Label("Intestazione", "CANTIERE DELLA BASILICA", panel, 28, -24, 544, 26, 18, Gold);
        var title = Label("Titolo", "Pezzo completato", panel, 28, -62, 544, 48, 34, Paper);
        var summary = Label("Riepilogo", "Note corrette: 8 / 8    |    Credito: 200 monete", panel, 28, -122, 544, 56, 20, Paper);
        var content = Rect("Content", panel);
        Place(content, 28, -190, 544, 124);
        var instructions = Label("Istruzioni", "Chiudi per lasciare il pezzo sul tavolo e ritirarlo a mano,\noppure spostalo nel recinto per liberare il banco.", panel, 28, -330, 544, 52, 17, Paper);
        foreach (var label in new[] { brand, title, summary, instructions }) Outline(label);
        return new Screen { root = overlay.gameObject, panel = panel, content = content, title = title, summary = summary,
            close = Button("CHIUDI [E]", "CHIUDI  [E]", panel, 28, -394, 210, 48) };
    }

    public static void MaterialRow(Transform parent, MaterialDefinition material)
    {
        if(font == null) font = TMP_Settings.defaultFontAsset;
        var card = Box(material.displayName, parent, new Color(.12f, .16f, .17f)).rectTransform;
        Place(card, 0, 0, 1032, 112);
        var row = card.gameObject.AddComponent<ConstructionMaterialRow>();
        row.material = material;
        row.nameText = Label("Nome materiale", material.displayName, card, 22, -16, 420, 39, 29, Paper);
        row.stockText = Label("Scorte e prezzo", "Posseduti: 0    |    " + material.unitPrice + " monete / unità", card, 22, -61, 450, 32, 20, Muted);
        row.purchases = new[] { 1, 5, 10 }.Select((quantity, index) =>
        {
            var button = Button("Compra " + quantity, "+" + quantity + "  /  " + (long)quantity * material.unitPrice + " m", card,
                498 + index * 164, -31, 152, 52);
            return new ConstructionMaterialRow.Purchase { quantity = quantity, button = button, label = button.GetComponentInChildren<TMP_Text>() };
        }).ToArray();
    }

    private static void RecipeRow(Transform parent, CraftingRecipe recipe)
    {
        var card = Box(recipe.displayName, parent, new Color(.12f, .16f, .17f)).rectTransform;
        Place(card, 0, 0, 1032, 134);
        var row = card.gameObject.AddComponent<ConstructionRecipeRow>();
        row.recipe = recipe;
        row.nameText = Label("Nome ricetta", recipe.displayName, card, 22, -12, 690, 38, 27, Paper);
        row.costText = Label("Materiali richiesti", "Materiali richiesti dalla ricetta", card, 22, -53, 690, 31, 21, Muted);
        row.statusText = Label("Disponibilita", "Disponibilità aggiornata in gioco", card, 22, -90, 720, 30, 18, Gold);
        row.craftButton = Button("CREA", "CREA  " + recipe.minimumHits + "/" + recipe.noteCount, card, 790, -40, 218, 58);
        row.actionText = row.craftButton.GetComponentInChildren<TMP_Text>();
    }

    private static void ListLayout(RectTransform content)
    {
        var group = content.gameObject.AddComponent<VerticalLayoutGroup>();
        group.spacing = 14; group.childAlignment = TextAnchor.UpperLeft;
        group.childControlWidth = false; group.childControlHeight = false;
        group.childForceExpandWidth = false; group.childForceExpandHeight = false;
        content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    private static RectTransform Rect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = LayerMask.NameToLayer("UI");
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }
    private static Image Box(string name, Transform parent, Color color)
    {
        var rect = Rect(name, parent);
        var image = rect.gameObject.AddComponent<Image>(); image.color = color; return image;
    }
    private static TMP_Text Label(string name, string text, Transform parent, float x, float y, float width, float height, float size, Color color)
    {
        var rect = Rect(name, parent); Place(rect, x, y, width, height);
        var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
        label.font = font; label.text = text; label.fontSize = size; label.color = color;
        label.raycastTarget = false; label.textWrappingMode = TextWrappingModes.Normal; return label;
    }
    private static Button Button(string name, string text, Transform parent, float x, float y, float width, float height)
    {
        var image = Box(name, parent, Gold); Place(image.rectTransform, x, y, width, height);
        var button = image.gameObject.AddComponent<Button>(); button.targetGraphic = image;
        var colors = button.colors; colors.highlightedColor = new Color(1, .92f, .72f);
        colors.disabledColor = new Color(.4f, .43f, .42f, .6f); button.colors = colors;
        Label("Etichetta", text, image.transform, 8, 0, width - 16, height, 19, Ink).alignment = TextAlignmentOptions.Center;
        return button;
    }
    private static void Place(RectTransform rect, float x, float y, float width, float height, Vector2? anchor = null, Vector2? pivot = null)
    {
        rect.anchorMin = rect.anchorMax = anchor ?? new Vector2(0, 1);
        rect.pivot = pivot ?? new Vector2(0, 1); rect.anchoredPosition = new Vector2(x, y); rect.sizeDelta = new Vector2(width, height);
    }
    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
    }
    private static void MiddleLeft(RectTransform rect, float x, float y, float width, float height) =>
        Place(rect, x, y, width, height, new Vector2(0, .5f), new Vector2(.5f, .5f));
    private static void Outline(TMP_Text text)
    {
        text.outlineColor = new Color(0, 0, 0, .9f); text.outlineWidth = .18f;
        var material = text.fontMaterial; material.EnableKeyword("OUTLINE_ON"); material.EnableKeyword("UNDERLAY_ON");
        material.SetColor("_UnderlayColor", new Color(0, 0, 0, .9f));
        material.SetFloat("_UnderlayOffsetX", .5f); material.SetFloat("_UnderlayOffsetY", -.5f);
        material.SetFloat("_UnderlayDilate", .15f); material.SetFloat("_UnderlaySoftness", .15f); text.UpdateMeshPadding();
    }
    private static void Set(UnityEngine.Object target, string field, UnityEngine.Object value)
    {
        var so = new SerializedObject(target); so.FindProperty(field).objectReferenceValue = value; so.ApplyModifiedProperties();
    }
    private static void SetArray(UnityEngine.Object target, string field, UnityEngine.Object[] values)
    {
        var so = new SerializedObject(target); var array = so.FindProperty(field); array.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        so.ApplyModifiedProperties();
    }
}
