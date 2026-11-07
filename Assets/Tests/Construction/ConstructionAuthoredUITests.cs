using System;
using System.Linq;
using System.Reflection;
using Basilica.Crafting;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace Basilica.Crafting.Tests
{
    public sealed class ConstructionAuthoredUITests
    {
        private Scene scene;
        private Component ui, system;
        private GameObject uiObject;
        private Type uiType, systemType;
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

        [SetUp]
        public void Setup()
        {
            scene = EditorSceneManager.OpenPreviewScene("Assets/Scenes/GameScene.unity");
            uiObject = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true))
                .Select(t => t.gameObject).FirstOrDefault(g => g.name == "Construction UI");
            Assert.That(uiObject, Is.Not.Null, "Construction UI must exist in the saved Canvas before Play Mode.");
            ui = uiObject.GetComponent("ConstructionUI");
            uiType = ui.GetType();
            system = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Component>(true))
                .First(c => c != null && c.GetType().Name == "ConstructionSystem");
            systemType = system.GetType();
            systemType.GetProperty("Inventory").SetValue(system, new MaterialInventory(200));
            systemType.GetField("resultView", Private).SetValue(system, system.GetComponent("CraftingResultView"));
            systemType.GetField("view", Private).SetValue(system, ui);
        }

        [TearDown]
        public void Cleanup()
        {
            if (scene.IsValid()) EditorSceneManager.ClosePreviewScene(scene);
        }

        private void Initialize()
        {
            var method = uiType.GetMethod("Initialize");
            method.Invoke(ui, method.GetParameters().Length == 1 ? new object[] { system } : new object[] { system, null });
        }
        private void Call(string method, params object[] args) => uiType.GetMethod(method).Invoke(ui, args.Length == 0 ? null : args);
        private int ObjectCount() => uiObject.GetComponentsInChildren<Transform>(true).Length;

        [Test]
        public void SavedCanvasContainsAllScreensAndAssignedSystemReference()
        {
            Assert.That(uiObject.transform.parent.name, Is.EqualTo("Canvas"));
            var serialized = new SerializedObject(ui);
            foreach (string field in new[] { "screensRoot", "shopScreen", "workbenchScreen", "rhythmScreen", "completedScreen", "failedScreen" })
                Assert.That(serialized.FindProperty(field)?.objectReferenceValue, Is.Not.Null, field);
            Assert.That(new SerializedObject(system).FindProperty("view").objectReferenceValue, Is.EqualTo(ui));
        }

        [Test]
        public void SwitchingRuntimeScreensReusesAuthoredObjects()
        {
            int count = ObjectCount();
            Initialize();
            Call("ShowShop");
            Call("ShowWorkbench");
            var recipe = AssetDatabase.LoadAssetAtPath<CraftingRecipe>("Assets/Data/Construction/Fondamenta.asset");
            var sequence = new RhythmSequence(new[] { 2d, 3, 4, 5, 6, 7, 8, 9 }, .15, 6);
            Call("ShowRhythm", recipe, sequence);
            Call("UpdateRhythm", sequence, 2d);
            Call("ShowResult", CraftOutcome.Failure, 0, 8, null);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/INTERACTABLE/Cement_Sack_Piece Variant.prefab");
            Call("ShowResult", CraftOutcome.PerfectSuccess, 8, 8, prefab.GetComponent("DraggableObject"));
            Call("SetSuspended", true);
            Call("SetSuspended", false);
            Call("Hide");
            Assert.That(ObjectCount(), Is.EqualTo(count), "Runtime screens must not create or destroy UI elements.");
        }

        [Test]
        public void InspectorLayoutAndColorsSurviveMenuRefreshes()
        {
            var panel = (RectTransform)uiObject.transform.Find("Screens/Acquisto/Panel");
            var image = panel.GetComponents<Component>().First(c => c.GetType().Name == "Image");
            var colorProperty = image.GetType().GetProperty("color");
            var editedColor = new Color(.4f, .2f, .6f, .65f);
            var editedPosition = new Vector2(123, -45);
            panel.anchoredPosition = editedPosition;
            colorProperty.SetValue(image, editedColor);
            Initialize();
            Call("ShowShop");
            Call("ShowWorkbench");
            Call("ShowShop");
            Assert.That(panel.anchoredPosition, Is.EqualTo(editedPosition));
            Assert.That(colorProperty.GetValue(image), Is.EqualTo(editedColor));
        }

        [Test]
        public void SavedRhythmPoolSupportsEveryValidRecipeLength()
        {
            var serialized = new SerializedObject(ui);
            Assert.That(serialized.FindProperty("notes").arraySize, Is.GreaterThanOrEqualTo(64));
            Assert.That(serialized.FindProperty("tally").arraySize, Is.GreaterThanOrEqualTo(64));
        }

        [Test]
        public void ReinitializationDoesNotDoublePurchaseCallbacks()
        {
            Initialize();
            Initialize();
            var merchant = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Component>(true))
                .First(c => c != null && c.GetType().Name == "ConstructionStation" &&
                    c.GetType().GetField("kind").GetValue(c).ToString() == "Merchant");
            systemType.GetField("station", Private).SetValue(system, merchant);
            systemType.GetField("modalOpen", Private).SetValue(system, true);
            Call("ShowShop");
            var purchase = uiObject.transform.Find("Screens/Acquisto/Panel/Viewport/Content/Pietra/Compra 1")
                .GetComponents<Component>().First(c => c.GetType().Name == "Button");
            ((UnityEvent)purchase.GetType().GetProperty("onClick").GetValue(purchase)).Invoke();
            var inventory = (MaterialInventory)systemType.GetProperty("Inventory").GetValue(system);
            Assert.That(inventory.Coins, Is.EqualTo(197));
            Assert.That(inventory.Count(AssetDatabase.LoadAssetAtPath<MaterialDefinition>("Assets/Data/Construction/Pietra.asset")), Is.EqualTo(1));
        }
    }
}
