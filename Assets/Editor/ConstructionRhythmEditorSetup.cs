using System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class ConstructionRhythmEditorSetup
{
    [MenuItem("Tools/Vitruvio/Prepare compact rhythm panel")]
    public static void FromMenu() => Debug.Log(Configure());

    public static string Configure()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path != "Assets/Scenes/GameScene.unity") throw new InvalidOperationException("Open GameScene first.");
        var ui = UnityEngine.Object.FindFirstObjectByType<ConstructionUI>(FindObjectsInactive.Include);
        if (ui == null) throw new InvalidOperationException("The authored construction UI is missing.");
        Undo.IncrementCurrentGroup(); int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Compact rhythm panel");
        Undo.RegisterFullObjectHierarchyUndo(ui.transform.Find("Screens/Ritmo").gameObject, "Compact rhythm panel");
        Apply(ui);
        Undo.CollapseUndoOperations(group);
        Canvas.ForceUpdateCanvases();
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        return "Saved 900x620 rhythm panel, 640px lane, 340px/s notes and authored fade group (1.51s lookahead).";
    }

    public static void Apply(ConstructionUI ui)
    {
        var screen = ui.transform.Find("Screens/Ritmo");
        var panel = screen.Find("Panel");
        var viewport = panel.Find("Viewport");
        var content = viewport.Find("Content");
        var lane = content.Find("Corsia");
        var group = screen.GetComponent<CanvasGroup>();
        if (group == null) group = Undo.AddComponent<CanvasGroup>(screen.gameObject);
        group.alpha = 1;
        var panelRect = (RectTransform)panel;
        panelRect.sizeDelta = new Vector2(900, 620);
        Place(panel.Find("Linea dorata"), 0, 0, 900, 5);
        Label(panel.Find("Intestazione"), 36, -24, 828, 24, 16);
        Label(panel.Find("Titolo"), 36, -60, 828, 52, 38);
        Label(panel.Find("Riepilogo"), 36, -122, 828, 56, 20);
        Place(viewport, 36, -192, 828, 252);
        Place(content, 0, 0, 828, 252);
        var scroll = viewport.GetComponent<ScrollRect>();
        scroll.StopMovement(); scroll.vertical = false; scroll.horizontal = false;
        Label(content.Find("Punteggio"), 0, -4, 828, 32, 22);
        Place(lane, 94, -54, 640, 100);
        foreach (string name in new[] { "Linea colpo", "Finestra colpo" })
        {
            var rect = (RectTransform)lane.Find(name);
            rect.anchoredPosition = new Vector2(128, 0);
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 90);
        }
        Place(content.Find("Giudizi"), 94, -178, 640, 32);
        var tally = content.Find("Giudizi").GetComponent<GridLayoutGroup>();
        tally.cellSize = new Vector2(16, 12); tally.spacing = new Vector2(3, 3);
        var data = new SerializedObject(ui);
        var cancel = (Button)data.FindProperty("cancelButton").objectReferenceValue;
        Undo.SetTransformParent(cancel.transform, panel, "Place rhythm cancel button");
        Place(cancel.transform, 550, -548, 314, 44);
        Label(cancel.GetComponentInChildren<TMP_Text>().transform, 8, 0, 298, 44, 18);
        Label(panel.Find("Messaggio"), 36, -462, 828, 54, 18);
        Label(panel.Find("Istruzioni"), 36, -540, 480, 58, 16);
        data.FindProperty("noteSpeed").floatValue = 340;
        data.FindProperty("rhythmGroup").objectReferenceValue = group;
        data.ApplyModifiedProperties();
    }

    private static void Place(Transform target, float x, float y, float w, float h)
    {
        var rect = (RectTransform)target;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(x, y); rect.sizeDelta = new Vector2(w, h);
    }

    private static void Label(Transform target, float x, float y, float w, float h, float size)
    {
        Place(target, x, y, w, h); target.GetComponent<TMP_Text>().fontSize = size;
    }
}
