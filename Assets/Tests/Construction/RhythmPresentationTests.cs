using System;
using System.Linq;
using System.Reflection;
using Basilica.Crafting;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Basilica.Crafting.Tests
{
    public sealed class RhythmPresentationTests
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private Scene scene;
        private Component system, ui;
        private Type type;
        private CraftingRecipe recipe;
        private MaterialInventory inventory;
        private RhythmSequence sequence;

        [SetUp]
        public void Setup()
        {
            scene = EditorSceneManager.OpenPreviewScene("Assets/Scenes/GameScene.unity");
            var components = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Component>(true)).Where(c => c != null).ToArray();
            system = components.Single(c => c.GetType().Name == "ConstructionSystem"); type = system.GetType();
            ui = components.Single(c => c.GetType().Name == "ConstructionUI");
            recipe = AssetDatabase.LoadAssetAtPath<CraftingRecipe>("Assets/Data/Construction/Fondamenta.asset");
            inventory = new MaterialInventory(200);
            foreach (var cost in recipe.costs) Assert.That(inventory.TryBuy(cost.material, cost.quantity, out _), Is.True);
            Assert.That(inventory.TryReserve(recipe, out var reservation, out _), Is.True);
            type.GetProperty("Inventory").SetValue(system, inventory);
            Set("reservation", reservation); Set("activeRecipe", recipe); Set("activeCosts", (MaterialCost[])recipe.costs.Clone());
            Set("rhythmAudio", system.GetComponent("RhythmAudio")); Set("resultView", system.GetComponent("CraftingResultView"));
            Set("view", ui); Set("modalOpen", true);
            Set("station", components.First(c => c.GetType().Name == "ConstructionStation" && c.GetType().GetField("kind").GetValue(c).ToString() == "Workbench"));
            sequence = new RhythmSequence(new[] { 2d, 3, 4, 5, 6, 7, 8, 9 }, .15, 6);
            type.GetProperty("Sequence").SetValue(system, sequence);
            ui.GetType().GetMethod("Initialize").Invoke(ui, new object[] { system });
            ShowRhythm();
        }

        [TearDown]
        public void Cleanup() { if (scene.IsValid()) EditorSceneManager.ClosePreviewScene(scene); }
        private void Set(string name, object value) => type.GetField(name, Private).SetValue(system, value);
        private void ShowRhythm() => ui.GetType().GetMethod("ShowRhythm").Invoke(ui, new object[] { recipe, sequence });
        private bool IsCrafting => (bool)type.GetProperty("IsCrafting").GetValue(system);
        private GameObject CurrentScreen => (GameObject)ui.GetType().GetProperty("CurrentScreen").GetValue(ui);
        private CanvasGroup Group()
        {
            var property = new SerializedObject(ui).FindProperty("rhythmGroup");
            Assert.That(property, Is.Not.Null, "The rhythm panel needs an authored fade group.");
            var group = property.objectReferenceValue as CanvasGroup;
            Assert.That(group, Is.Not.Null); return group;
        }
        private void CompleteLastNote()
        {
            sequence.Advance(8.2); // Seven misses make the result fail without spawning a piece in EditMode.
            type.GetMethod("SubmitBeat").Invoke(system, new object[] { 9d });
        }
        private void Advance(float seconds)
        {
            var method = type.GetMethod("AdvanceRhythmFinish", Private);
            Assert.That(method, Is.Not.Null, "Closing needs a pause-aware presentation stage.");
            method.Invoke(system, new object[] { seconds });
        }

        [Test]
        public void LastJudgmentHoldsThenFadesAndSettlesOnlyOnce()
        {
            CompleteLastNote();
            Assert.That(IsCrafting, Is.True, "The last input must not immediately replace the rhythm screen.");
            Assert.That(CurrentScreen.name, Is.EqualTo("Ritmo"));
            Assert.That(sequence.Hits, Is.EqualTo(1));
            Assert.That(inventory.Count(recipe.costs[0].material), Is.Zero);
            Advance(.3f); Assert.That(Group().alpha, Is.EqualTo(1).Within(.001));
            type.GetMethod("CancelCraft").Invoke(system, null);
            Assert.That(IsCrafting, Is.True, "Cancel cannot discard an already judged result during its outro.");
            Advance(.3f); Assert.That(Group().alpha, Is.InRange(.01f, .99f));
            Advance(.3f);
            Assert.That(IsCrafting, Is.False);
            Assert.That(CurrentScreen.name, Is.EqualTo("Fallimento"));
            Assert.That(inventory.Count(recipe.costs[0].material), Is.EqualTo(2));
            Advance(5);
            Assert.That(inventory.Count(recipe.costs[0].material), Is.EqualTo(2), "Refund must not happen twice.");
        }

        [Test]
        public void PausingFreezesTheClosingPresentation()
        {
            CompleteLastNote();
            type.GetMethod("OnApplicationFocus", Private).Invoke(system, new object[] { false });
            Advance(5);
            Assert.That(IsCrafting, Is.True);
            Assert.That(Group().alpha, Is.EqualTo(1));
            type.GetMethod("OnApplicationFocus", Private).Invoke(system, new object[] { true });
            Advance(.6f);
            Assert.That(IsCrafting, Is.True);
            Assert.That(Group().alpha, Is.InRange(.01f, .99f));
        }

        [Test]
        public void NextAttemptRestoresOpacityAndCancelControl()
        {
            CompleteLastNote(); Advance(.6f); ShowRhythm();
            Assert.That(Group().alpha, Is.EqualTo(1));
            var button = (Component)new SerializedObject(ui).FindProperty("cancelButton").objectReferenceValue;
            Assert.That(button.GetType().GetProperty("interactable").GetValue(button), Is.True);
        }

        [Test]
        public void AuthoredLaneGivesAboutOneAndAHalfSecondsOfWarning()
        {
            var data = new SerializedObject(ui);
            var line = (RectTransform)data.FindProperty("hitLine").objectReferenceValue;
            float speed = data.FindProperty("noteSpeed").floatValue;
            float warning = (((RectTransform)line.parent).rect.width - line.anchoredPosition.x) / speed;
            Assert.That(warning, Is.InRange(1.3f, 1.7f));
        }
    }
}
