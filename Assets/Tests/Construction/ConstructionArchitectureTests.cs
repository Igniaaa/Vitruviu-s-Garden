using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Basilica.Crafting;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Basilica.Crafting.Tests
{
    public sealed class ConstructionArchitectureTests
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private Scene scene;
        private GameObject root;
        private readonly List<UnityEngine.Object> generated = new List<UnityEngine.Object>();
        private Type SlotType => Type.GetType("PlaceholderSlot, Assembly-CSharp", true);

        [SetUp]
        public void Setup()
        {
            scene = EditorSceneManager.OpenPreviewScene("Assets/Scenes/GameScene.unity");
            root = new GameObject("Architecture unit tests");
            SceneManager.MoveGameObjectToScene(root, scene);
            root.transform.position = new Vector3(10000, 0, 10000);
        }

        [TearDown]
        public void Cleanup()
        {
            foreach (var item in generated) if (item != null) UnityEngine.Object.DestroyImmediate(item);
            generated.Clear();
            if (scene.IsValid()) EditorSceneManager.ClosePreviewScene(scene);
        }

        private Component[] Components(string name) => scene.GetRootGameObjects()
            .SelectMany(r => r.GetComponentsInChildren<Component>(true)).Where(c => c != null && c.GetType().Name == name).ToArray();

        [Test]
        public void EveryArchitecturePhaseHasReadableMeshesAndAnOfferedRecipe()
        {
            var slots = Components("PlaceholderSlot");
            var system = Components("ConstructionSystem").Single();
            var recipes = (IReadOnlyList<CraftingRecipe>)system.GetType().GetProperty("Recipes").GetValue(system);
            var rows = Components("ConstructionRecipeRow");
            int[] expected = { 8, 2, 18, 16, 6, 10, 10, 4 };
            for (int phase = 2; phase <= 9; phase++)
            {
                var stage = slots.Where(s => (int)SlotType.GetProperty("BuildPhase").GetValue(s) == phase).ToArray();
                Assert.That(stage.Length, Is.EqualTo(expected[phase - 2]), "Missing architectural parts in phase " + phase);
                Assert.That(recipes.Any(r => r.buildPhase == phase && r.IsValid(out _)), Is.True, "Missing recipe for phase " + phase);
                Assert.That(rows.Any(r => ((CraftingRecipe)r.GetType().GetField("recipe").GetValue(r)).buildPhase == phase), Is.True);
                foreach (var slot in stage)
                {
                    var mesh = slot.GetComponent<MeshFilter>().sharedMesh;
                    Assert.That(mesh, Is.Not.Null);
                    Assert.That(mesh.isReadable, Is.True, "Wireframes and locking require CPU-readable meshes.");
                    Assert.That(mesh.vertexCount, Is.LessThan(50000), "A placeholder must not import the original multi-million-vertex capital.");
                    var collider = slot.GetComponent<BoxCollider>();
                    var renderer = slot.GetComponent<MeshRenderer>();
                    Assert.That(Vector3.Distance(collider.bounds.center, renderer.bounds.center), Is.LessThan(.003f), "The trigger must follow the structural mesh, including its height.");
                    Assert.That(Vector3.Distance(collider.bounds.size, renderer.bounds.size), Is.LessThan(.003f), "The trigger must fit the mesh dimensions.");
                }
            }
        }

        private Component NewSlot(string name)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            SceneManager.MoveGameObjectToScene(go, scene);
            go.name = name;
            go.transform.SetParent(root.transform, false);
            var slot = go.AddComponent(SlotType);
            var material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            generated.Add(material);
            go.GetComponent<Renderer>().sharedMaterial = material;
            SlotType.GetField("edgeMaterial", Private).SetValue(slot, material);
            SlotType.GetField("pieceId", Private).SetValue(slot, "C1");
            // These tests invoke the runtime initializer outside Play Mode. Unity
            // reports its intentional per-renderer material instance as an EditMode log.
            LogAssert.Expect(LogType.Error, "Instantiating material due to calling renderer.material during edit mode. This will leak materials into the scene. You most likely want to use renderer.sharedMaterial instead.");
            SlotType.GetMethod("Awake", Private).Invoke(slot, null);
            generated.Add(go.GetComponent<Renderer>().sharedMaterial);
            foreach (var filter in go.GetComponentsInChildren<MeshFilter>())
                if (filter.gameObject.name == "Wireframe") generated.Add(filter.sharedMesh);
            return slot;
        }

        private Component NewPiece()
        {
            var go = new GameObject("Test construction piece");
            SceneManager.MoveGameObjectToScene(go, scene);
            var mesh = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<MeshFilter>(true))
                .First(f => f.sharedMesh != null && f.sharedMesh.name == "Cube").sharedMesh;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>();
            go.AddComponent<MeshCollider>().sharedMesh = mesh;
            go.AddComponent<Rigidbody>();
            var type = Type.GetType("DraggableObject, Assembly-CSharp", true);
            var piece = go.AddComponent(type);
            type.GetField("pieceId", Private).SetValue(piece, "C1");
            type.GetMethod("Awake", Private).Invoke(piece, null);
            return piece;
        }

        private Mesh OffsetMesh(Vector3 center, Vector3 size)
        {
            var mesh = new Mesh { name = "Offset collider test mesh" };
            generated.Add(mesh);
            var points = new Vector3[8];
            for (int i = 0; i < 8; i++)
                points[i] = center + Vector3.Scale(size * .5f,
                    new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
            mesh.vertices = points;
            mesh.RecalculateBounds();
            return mesh;
        }

        private void Fit(Component slot)
        {
            var method = SlotType.GetMethod("FitColliderToMesh");
            Assert.That(method, Is.Not.Null, "A placeholder must fit its collider to its mesh.");
            method.Invoke(slot, null);
        }

        [Test]
        public void ColliderFollowsAnOffsetMeshWithoutMovingThePlaceholder()
        {
            var slot = NewSlot("Offset structure");
            var mesh = OffsetMesh(new Vector3(2, 8, -3), new Vector3(4, 6, 2));
            slot.GetComponent<MeshFilter>().sharedMesh = mesh;
            Vector3 position = slot.transform.position, scale = slot.transform.localScale;
            Fit(slot);
            var collider = slot.GetComponent<BoxCollider>();
            Assert.That(collider.center, Is.EqualTo(new Vector3(2, 8, -3)));
            Assert.That(collider.size, Is.EqualTo(new Vector3(4, 6, 2)));
            Assert.That(slot.transform.position, Is.EqualTo(position));
            Assert.That(slot.transform.localScale, Is.EqualTo(scale));
            Assert.That(slot.GetComponent<MeshFilter>().sharedMesh, Is.SameAs(mesh));
        }

        [Test]
        public void ColliderIncludesChildMeshRotationScaleAndOffset()
        {
            var slot = NewSlot("Child structure");
            slot.GetComponent<MeshFilter>().sharedMesh = null;
            var child = new GameObject("Structural mesh", typeof(MeshFilter));
            child.transform.SetParent(slot.transform, false);
            child.transform.localPosition = new Vector3(3, 4, 5);
            child.transform.localRotation = Quaternion.Euler(0, 90, 0);
            child.transform.localScale = new Vector3(2, 3, .5f);
            child.GetComponent<MeshFilter>().sharedMesh = OffsetMesh(new Vector3(2, 3, -4), new Vector3(2, 4, 6));
            Fit(slot);
            var collider = slot.GetComponent<BoxCollider>();
            Assert.That(Vector3.Distance(collider.center, new Vector3(1, 13, 1)), Is.LessThan(.003f));
            Assert.That(Vector3.Distance(collider.size, new Vector3(3, 12, 4)), Is.LessThan(.003f));
        }

        [Test]
        public void InspectorValidationRefitsColliderAfterMeshReplacement()
        {
            var slot = NewSlot("Editable structure");
            slot.GetComponent<MeshFilter>().sharedMesh = OffsetMesh(new Vector3(0, 12, 0), new Vector3(3, 2, 5));
            var validate = SlotType.GetMethod("OnValidate", Private);
            Assert.That(validate, Is.Not.Null, "Collider fitting must also work in the Editor.");
            validate.Invoke(slot, null);
            Assert.That(slot.GetComponent<BoxCollider>().center, Is.EqualTo(new Vector3(0, 12, 0)));
            Assert.That(slot.GetComponent<BoxCollider>().size, Is.EqualTo(new Vector3(3, 2, 5)));
        }

        private void ReleaseInto(Component slot, Component piece)
        {
            SlotType.GetMethod("OnTriggerEnter", Private).Invoke(slot, new object[] { piece.GetComponent<Collider>() });
            SlotType.GetMethod("HandleDraggableReleased", Private).Invoke(slot, new object[] { piece });
        }

        [Test]
        public void AReleasedPieceCannotFillTwoOverlappingSlots()
        {
            var first = NewSlot("First slot");
            var second = NewSlot("Second slot");
            var piece = NewPiece();
            SlotType.GetMethod("OnTriggerEnter", Private).Invoke(second, new object[] { piece.GetComponent<Collider>() });
            ReleaseInto(first, piece);
            SlotType.GetMethod("HandleDraggableReleased", Private).Invoke(second, new object[] { piece });
            Assert.That(SlotType.GetProperty("IsFilled").GetValue(first), Is.True);
            Assert.That(SlotType.GetProperty("IsFilled").GetValue(second), Is.False);
        }

        [Test]
        public void FilledSlotRemovesDeliveryTriggerAndKeepsPieceInConstructionHierarchy()
        {
            var slot = NewSlot("Delivery point");
            var piece = NewPiece();
            ReleaseInto(slot, piece);
            Assert.That(slot.GetComponent<Collider>().enabled, Is.False, "The delivery point must not become an invisible obstacle.");
            Assert.That(piece.transform.parent, Is.EqualTo(root.transform), "The completed building must be able to hide all placed pieces.");
        }

        [Test]
        public void CompletionReplacesConstructionOnlyOnce()
        {
            var manager = new GameObject("Test completion");
            SceneManager.MoveGameObjectToScene(manager, scene);
            var type = Type.GetType("BasilicaCompletionSpawner, Assembly-CSharp", true);
            var component = manager.AddComponent(type);
            var property = new SerializedObject(component).FindProperty("constructionRoot");
            Assert.That(property, Is.Not.Null, "Completion must hide the construction site.");
            var template = new GameObject("Architecture completion test model");
            SceneManager.MoveGameObjectToScene(template, scene);
            type.GetField("constructionRoot", Private).SetValue(component, root);
            type.GetField("spawnPoint", Private).SetValue(component, root.transform);
            type.GetField("basilicaPrefab", Private).SetValue(component, template);
            type.GetMethod("SpawnBasilica", Private).Invoke(component, null);
            type.GetMethod("SpawnBasilica", Private).Invoke(component, null);
            var clones = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(t => t.name == "Architecture completion test model(Clone)").ToArray();
            foreach (var clone in clones) generated.Add(clone.gameObject);
            Assert.That(clones.Length, Is.EqualTo(1));
            Assert.That(root.activeSelf, Is.False);
            Assert.That(clones[0].gameObject.activeSelf, Is.True);
        }
    }
}
