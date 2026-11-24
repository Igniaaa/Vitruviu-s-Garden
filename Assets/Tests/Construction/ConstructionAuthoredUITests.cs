using System;
using System.Linq;
using System.Reflection;
using System.IO;
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

        private Component[] RecipeRows() => uiObject.GetComponentsInChildren<Component>(true)
            .Where(c => c != null && c.GetType().Name == "ConstructionRecipeRow").ToArray();

        private void SelectCategory(string name)
        {
            var method = uiType.GetMethod("SelectCategory");
            Assert.That(method, Is.Not.Null, "The workbench must let the player select a piece category.");
            method.Invoke(ui, new[] { Enum.Parse(method.GetParameters()[0].ParameterType, name) });
        }

        [Test]
        public void CategoriesFilterRecipesAndSurviveWorkbenchRefresh()
        {
            Initialize();
            Call("ShowWorkbench");
            SelectCategory("Roofs");
            Call("ShowWorkbench");
            var visible = RecipeRows().Where(row => row.gameObject.activeSelf).Select(row =>
                (CraftingRecipe)row.GetType().GetField("recipe").GetValue(row)).ToArray();
            Assert.That(visible.Select(r => r.buildPhase), Is.EquivalentTo(new[] { 7, 8 }));
            Assert.That(visible.All(r => r.buildPhase != 0), Is.True);
            SelectCategory("Floors");
            Assert.That(RecipeRows().Where(r => r.gameObject.activeSelf).Select(r =>
                ((CraftingRecipe)r.GetType().GetField("recipe").GetValue(r)).buildPhase), Is.EquivalentTo(new[] { 1, 6 }));
        }

        [Test]
        public void SwitchingCategoriesDoesNotCreateOrRebindRecipeObjects()
        {
            Initialize();
            Initialize();
            Call("ShowWorkbench");
            int count = ObjectCount();
            foreach (string category in new[] { "Foundations", "Floors", "Columns", "Walls", "RoofSupports", "Roofs", "Stairs" })
            {
                SelectCategory(category);
                Assert.That(RecipeRows().Count(r => r.gameObject.activeSelf), Is.GreaterThan(0), category);
                Assert.That(ObjectCount(), Is.EqualTo(count));
            }
            Call("Hide");
            Call("ShowWorkbench");
            Assert.That(RecipeRows().Where(r => r.gameObject.activeSelf).Select(r =>
                ((CraftingRecipe)r.GetType().GetField("recipe").GetValue(r)).buildPhase), Is.EquivalentTo(new[] { 9 }));
        }

        [Test]
        public void CategoryButtonClickFiltersTheCatalogAndKeepsFutureRecipesBlocked()
        {
            Initialize();
            Initialize();
            Call("ShowWorkbench");
            var tabs = uiObject.GetComponentsInChildren<Component>(true)
                .Where(c => c != null && c.GetType().Name == "ConstructionCategoryButton").ToArray();
            Assert.That(tabs.Length, Is.EqualTo(7));
            var tab = tabs.Single(t => t.GetType().GetField("category").GetValue(t).ToString() == "Walls");
            var button = (Component)tab.GetType().GetField("button").GetValue(tab);
            ((UnityEvent)button.GetType().GetProperty("onClick").GetValue(button)).Invoke();
            var visible = RecipeRows().Where(r => r.gameObject.activeSelf).ToArray();
            Assert.That(visible.Length, Is.EqualTo(1));
            Assert.That(((CraftingRecipe)visible[0].GetType().GetField("recipe").GetValue(visible[0])).buildPhase, Is.EqualTo(4));
            var craft = (Component)visible[0].GetType().GetField("craftButton").GetValue(visible[0]);
            Assert.That(craft.GetType().GetProperty("interactable").GetValue(craft), Is.False);
        }

        [Test]
        public void AdvancingConstructionOpensTheCategoryOfTheNewPhase()
        {
            Initialize();
            Call("ShowWorkbench");
            SelectCategory("Walls");
            var tracker = (Component)systemType.GetField("progressTracker", Private).GetValue(system);
            tracker.GetType().GetField("currentPhase", Private).SetValue(tracker, 7);
            Call("ShowWorkbench");
            Assert.That(uiType.GetProperty("SelectedCategory").GetValue(ui).ToString(), Is.EqualTo("Roofs"));
            Assert.That(RecipeRows().Where(r => r.gameObject.activeSelf).Select(r =>
                ((CraftingRecipe)r.GetType().GetField("recipe").GetValue(r)).buildPhase), Is.EquivalentTo(new[] { 7, 8 }));
        }

        [Test]
        public void EveryRecipeHasABlackSilhouetteWithATransparentBackground()
        {
            foreach (var row in RecipeRows())
            {
                var field = row.GetType().GetField("silhouette");
                Assert.That(field, Is.Not.Null, "Each recipe card needs a silhouette image.");
                var graphic = (Component)field.GetValue(row);
                Assert.That(graphic, Is.Not.Null, row.name);
                var sprite = (Sprite)graphic.GetType().GetProperty("sprite").GetValue(graphic);
                Assert.That(sprite, Is.Not.Null, row.name);
                string path = AssetDatabase.GetAssetPath(sprite.texture);
                var texture = new Texture2D(2, 2);
                try
                {
                    Assert.That(ImageConversion.LoadImage(texture, File.ReadAllBytes(path)), Is.True);
                    var pixels = texture.GetPixels32();
                    Assert.That(pixels.Count(p => p.a > 200), Is.GreaterThan(100), row.name + " must show a shape.");
                    Assert.That(pixels.Count(p => p.a == 0), Is.GreaterThan(100), row.name + " needs transparent margins.");
                    Assert.That(pixels.Where(p => p.a > 200).All(p => p.r < 10 && p.g < 10 && p.b < 10), Is.True, row.name + " must be black.");
                }
                finally { UnityEngine.Object.DestroyImmediate(texture); }
            }
        }

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

        private void OpenShop()
        {
            Initialize();
            var merchant = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Component>(true))
                .First(c => c != null && c.GetType().Name == "ConstructionStation" &&
                    c.GetType().GetField("kind").GetValue(c).ToString() == "Merchant");
            systemType.GetField("station", Private).SetValue(system, merchant);
            systemType.GetField("modalOpen", Private).SetValue(system, true);
            Call("ShowShop");
        }

        private Component StoneControl(string name, string type)
        {
            var control = uiObject.transform.Find("Screens/Acquisto/Panel/Viewport/Content/Pietra/" + name);
            Assert.That(control, Is.Not.Null, "Missing shop control: " + name);
            return control.GetComponents<Component>().First(c => c != null && c.GetType().Name == type);
        }

        private void SetQuantity(string value)
        {
            var input = StoneControl("Quantita", "TMP_InputField");
            input.GetType().GetProperty("text").SetValue(input, value);
        }

        private void Click(string name)
        {
            var purchase = StoneControl(name, "Button");
            ((UnityEvent)purchase.GetType().GetProperty("onClick").GetValue(purchase)).Invoke();
        }

        [Test]
        public void ReinitializationBuysSelectedQuantityOnlyOnce()
        {
            Initialize();
            OpenShop();
            SetQuantity("7");
            Click("ACQUISTA");
            var inventory = (MaterialInventory)systemType.GetProperty("Inventory").GetValue(system);
            Assert.That(inventory.Coins, Is.EqualTo(179));
            Assert.That(inventory.Count(AssetDatabase.LoadAssetAtPath<MaterialDefinition>("Assets/Data/Construction/Pietra.asset")), Is.EqualTo(7));
            var input = StoneControl("Quantita", "TMP_InputField");
            Assert.That(input.GetType().GetProperty("text").GetValue(input), Is.EqualTo("7"));
        }

        [TestCase("")]
        [TestCase("0")]
        [TestCase("-3")]
        [TestCase("abc")]
        [TestCase("2147483648")]
        [TestCase("67")]
        [TestCase("2147483647")]
        public void InvalidOrUnaffordableQuantityCannotBePurchased(string quantity)
        {
            OpenShop();
            SetQuantity(quantity);
            var purchase = StoneControl("ACQUISTA", "Button");
            Assert.That(purchase.GetType().GetProperty("interactable").GetValue(purchase), Is.False);
            Click("ACQUISTA");
            var inventory = (MaterialInventory)systemType.GetProperty("Inventory").GetValue(system);
            Assert.That(inventory.Coins, Is.EqualTo(200));
        }

        [Test]
        public void QuantityButtonsStepByOneAndStopAtOne()
        {
            OpenShop();
            Click("Aumenta");
            var input = StoneControl("Quantita", "TMP_InputField");
            Assert.That(input.GetType().GetProperty("text").GetValue(input), Is.EqualTo("2"));
            Click("Diminuisci");
            Click("Diminuisci");
            Assert.That(input.GetType().GetProperty("text").GetValue(input), Is.EqualTo("1"));
            SetQuantity("7");
            Call("ShowShop");
            Assert.That(input.GetType().GetProperty("text").GetValue(input), Is.EqualTo("7"));
        }

        [Test]
        public void TotalPriceAndAvailabilityFollowQuantityAndWallet()
        {
            OpenShop();
            SetQuantity("40");
            var row = uiObject.transform.Find("Screens/Acquisto/Panel/Viewport/Content/Pietra").GetComponent("ConstructionMaterialRow");
            var label = (Component)row.GetType().GetField("purchaseLabel").GetValue(row);
            Assert.That(label.GetType().GetProperty("text").GetValue(label), Does.Contain("120"));
            var purchase = StoneControl("ACQUISTA", "Button");
            Assert.That(purchase.GetType().GetProperty("interactable").GetValue(purchase), Is.True);
            Click("ACQUISTA");
            Assert.That(purchase.GetType().GetProperty("interactable").GetValue(purchase), Is.False);
            SetQuantity("20");
            Assert.That(purchase.GetType().GetProperty("interactable").GetValue(purchase), Is.True);
            Click("ACQUISTA");
            var inventory = (MaterialInventory)systemType.GetProperty("Inventory").GetValue(system);
            Assert.That(inventory.Coins, Is.EqualTo(20));
            Assert.That(inventory.Count(AssetDatabase.LoadAssetAtPath<MaterialDefinition>("Assets/Data/Construction/Pietra.asset")), Is.EqualTo(60));
        }
    }
}
