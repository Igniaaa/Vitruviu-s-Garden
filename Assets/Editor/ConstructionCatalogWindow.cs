using System;
using System.Linq;
using Basilica.Crafting;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// A small authoring surface for the saved piece catalog. It edits the scene's
// existing card objects and never rebuilds or restyles the catalog.
public sealed class ConstructionCatalogWindow : EditorWindow
{
    private ConstructionUI ui;
    private ConstructionSystem system;
    private CraftingRecipe recipeToAdd;
    private Vector2 scroll;
    private bool showAddCard = true;
    private bool showAllCategories = true;
    private ConstructionPieceCategory categoryFilter;

    [MenuItem("Tools/Vitruvio/Modifica schermata crafting")]
    public static void Open() => GetWindow<ConstructionCatalogWindow>("Crafting pezzi");

    private void OnEnable() => RefreshReferences();

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Schermata crafting dei pezzi", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Modifica le schede salvate nella scena. Le modifiche alla grafica si fanno nel Canvas; questa finestra aiuta a gestire ricette, categorie e sagome.", MessageType.Info);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Trova schermata", GUILayout.Height(26))) RefreshReferences();
            using (new EditorGUI.DisabledScope(ui == null))
                if (GUILayout.Button("Seleziona catalogo nel Canvas", GUILayout.Height(26))) SelectCatalog();
        }

        if (ui == null || system == null)
        {
            EditorGUILayout.HelpBox("Apri Assets/Scenes/GameScene.unity in Edit Mode per modificare il catalogo.", MessageType.Warning);
            return;
        }

        EditorGUILayout.Space(6);
        showAddCard = EditorGUILayout.Foldout(showAddCard, "Aggiungi scheda da ricetta", true);
        if (showAddCard)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                recipeToAdd = (CraftingRecipe)EditorGUILayout.ObjectField(recipeToAdd, typeof(CraftingRecipe), false);
                using (new EditorGUI.DisabledScope(recipeToAdd == null || FindRow(recipeToAdd) != null))
                    if (GUILayout.Button("Crea scheda", GUILayout.Width(100))) AddCard(recipeToAdd);
            }
        }

        EditorGUILayout.Space(6);
        using (new EditorGUILayout.HorizontalScope())
        {
            showAllCategories = EditorGUILayout.ToggleLeft("Tutte le categorie", showAllCategories, GUILayout.Width(150));
            using (new EditorGUI.DisabledScope(showAllCategories))
                categoryFilter = (ConstructionPieceCategory)EditorGUILayout.EnumPopup("Filtra", categoryFilter);
        }
        EditorGUILayout.LabelField("Ricette nella schermata", EditorStyles.boldLabel);
        scroll = EditorGUILayout.BeginScrollView(scroll);
        foreach (var recipe in system.Recipes.Where(r => r != null)) DrawRecipe(recipe);
        EditorGUILayout.EndScrollView();
    }

    private void RefreshReferences()
    {
        var scene = SceneManager.GetActiveScene();
        ui = null;
        system = null;
        if (scene.path != "Assets/Scenes/GameScene.unity") { Repaint(); return; }
        ui = FindFirstObjectByType<ConstructionUI>(FindObjectsInactive.Include);
        system = FindFirstObjectByType<ConstructionSystem>(FindObjectsInactive.Include);
        Repaint();
    }

    private void SelectCatalog()
    {
        var catalog = ui.transform.Find("Screens/Ricette");
        Selection.activeObject = catalog != null ? catalog.gameObject : ui.gameObject;
        EditorGUIUtility.PingObject(Selection.activeObject);
        SceneView.lastActiveSceneView?.FrameSelected();
    }

    private ConstructionRecipeRow FindRow(CraftingRecipe recipe) => ui == null ? null :
        ui.GetComponentsInChildren<ConstructionRecipeRow>(true).FirstOrDefault(row => row.recipe == recipe);

    private void DrawRecipe(CraftingRecipe recipe)
    {
        var row = FindRow(recipe);
        if (row == null)
        {
            EditorGUILayout.HelpBox(recipe.displayName + " · scheda mancante", MessageType.Warning);
            return;
        }
        if (!showAllCategories && row.category != categoryFilter) return;

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(recipe.displayName, EditorStyles.boldLabel);
                if (GUILayout.Button("Seleziona", GUILayout.Width(76)))
                {
                    Selection.activeGameObject = row.gameObject;
                    EditorGUIUtility.PingObject(row.gameObject);
                    SceneView.lastActiveSceneView?.FrameSelected();
                }
            }

            EditorGUI.BeginChangeCheck();
            var category = (ConstructionPieceCategory)EditorGUILayout.EnumPopup("Categoria", row.category);
            var sprite = row.silhouette != null ? (Sprite)EditorGUILayout.ObjectField("Sagoma", row.silhouette.sprite, typeof(Sprite), false) : null;
            if (row.silhouette == null)
                EditorGUILayout.HelpBox("La scheda non ha un'immagine silhouette assegnata.", MessageType.Warning);
            else if (row.silhouette.sprite == null)
                EditorGUILayout.HelpBox("Trascina qui sopra la silhouette del pezzo per mostrarla nella scheda.", MessageType.None);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(row, "Edit piece catalog card");
                row.category = category;
                if (row.silhouette != null)
                {
                    Undo.RecordObject(row.silhouette, "Edit piece catalog silhouette");
                    row.silhouette.sprite = sprite;
                    EditorUtility.SetDirty(row.silhouette);
                }
                EditorUtility.SetDirty(row);
                EditorSceneManager.MarkSceneDirty(row.gameObject.scene);
            }
        }
    }

    private void AddCard(CraftingRecipe recipe)
    {
        var template = ui.GetComponentsInChildren<ConstructionRecipeRow>(true).FirstOrDefault();
        var content = ui.transform.Find("Screens/Ricette/Panel/Viewport/Content");
        if (template == null || content == null)
        {
            EditorUtility.DisplayDialog("Catalogo incompleto", "Serve una scheda modello e la cartella Content nel catalogo.", "OK");
            return;
        }

        var copy = Instantiate(template.gameObject, content, false);
        Undo.RegisterCreatedObjectUndo(copy, "Add piece catalog card");
        copy.name = "Scheda - " + recipe.displayName;
        copy.SetActive(true);
        var row = copy.GetComponent<ConstructionRecipeRow>();
        row.recipe = recipe;
        row.category = ConstructionPieceCategories.ForPhase(recipe.buildPhase);
        if (row.silhouette != null) row.silhouette.sprite = null;
        if (row.nameText != null) row.nameText.text = recipe.displayName;
        EditorUtility.SetDirty(row);
        EditorSceneManager.MarkSceneDirty(copy.scene);
        Selection.activeGameObject = copy;
        recipeToAdd = null;
        Repaint();
    }
}
