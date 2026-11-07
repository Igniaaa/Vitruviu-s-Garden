using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Basilica.Crafting.Tests
{
    public sealed class CraftingResultViewTests
    {
        private GameObject root, target, bench;
        private Component view, piece, brain;
        private Type viewType;
        private Camera camera;
        private Vector3 originalPosition;
        private Quaternion originalRotation;

        [SetUp]
        public void Setup()
        {
            viewType = Type.GetType("CraftingResultView, Assembly-CSharp");
            Assert.That(viewType, Is.Not.Null, "The completed piece needs a dedicated scene view with a reversible camera override.");
            root = new GameObject("Result camera tests");
            root.transform.position = new Vector3(10000, 0, 10000);
            var cameraObject = new GameObject("Test player camera");
            cameraObject.transform.SetParent(root.transform, false);
            camera = cameraObject.AddComponent<Camera>();
            camera.transform.localPosition = new Vector3(0, 1.6f, -6);
            camera.transform.localRotation = Quaternion.Euler(0, 12, 0);
            camera.fieldOfView = 65;
            camera.aspect = 16f / 9;
            var brainType = AppDomain.CurrentDomain.GetAssemblies()
                .Select(a => a.GetType("Unity.Cinemachine.CinemachineBrain")).First(t => t != null);
            brain = cameraObject.AddComponent(brainType);
            target = GameObject.CreatePrimitive(PrimitiveType.Cube);
            target.transform.SetParent(root.transform, false);
            target.transform.localPosition = new Vector3(0, 2, 0);
            target.transform.localScale = new Vector3(.7f, .9f, .5f);
            piece = target.AddComponent(Type.GetType("DraggableObject, Assembly-CSharp", true));
            bench = new GameObject("Workbench output");
            bench.transform.SetParent(root.transform, false);
            bench.transform.localPosition = target.transform.localPosition;
            view = root.AddComponent(viewType);
            originalPosition = camera.transform.localPosition;
            originalRotation = camera.transform.localRotation;
        }

        [TearDown]
        public void Cleanup()
        {
            if (view != null) viewType.GetMethod("Restore").Invoke(view, null);
            if (root != null) UnityEngine.Object.DestroyImmediate(root);
        }

        private bool Show(Component value = null) => (bool)viewType.GetMethod("Show")
            .Invoke(view, new object[] { value ?? piece, bench.transform, camera });
        private void Restore() => viewType.GetMethod("Restore").Invoke(view, null);

        [Test]
        public void ViewFramesActualPieceWithoutMovingPlayerOrAddingCamera()
        {
            int count = UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Length;
            var playerPosition = root.transform.position;
            Assert.That(Show(), Is.True);
            var screen = camera.WorldToViewportPoint(target.GetComponent<Renderer>().bounds.center);
            Assert.That(screen.z, Is.GreaterThan(0));
            Assert.That(screen.x, Is.InRange(.05f, .65f));
            Assert.That(screen.y, Is.InRange(.05f, .95f));
            Assert.That(root.transform.position, Is.EqualTo(playerPosition));
            Assert.That(UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Length, Is.EqualTo(count));
            Assert.That(((Behaviour)brain).enabled, Is.False);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void RestorePreservesOriginalPoseLensAndBrainState(bool wasEnabled)
        {
            ((Behaviour)brain).enabled = wasEnabled;
            Assert.That(Show(), Is.True);
            Restore();
            Assert.That(camera.transform.localPosition, Is.EqualTo(originalPosition));
            Assert.That(Quaternion.Angle(camera.transform.localRotation, originalRotation), Is.LessThan(.01f));
            Assert.That(camera.fieldOfView, Is.EqualTo(65));
            Assert.That(((Behaviour)brain).enabled, Is.EqualTo(wasEnabled));
        }

        [Test]
        public void RepeatedResultDoesNotOverwritePlayerSnapshot()
        {
            Assert.That(Show(), Is.True);
            Assert.That(Show(), Is.True);
            Restore();
            Assert.That(camera.transform.localPosition, Is.EqualTo(originalPosition));
            Assert.That(camera.fieldOfView, Is.EqualTo(65));
        }

        [Test]
        public void DisablingViewRestoresCameraControl()
        {
            Assert.That(Show(), Is.True);
            viewType.GetMethod("OnDisable", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(view, null);
            Assert.That(camera.transform.localPosition, Is.EqualTo(originalPosition));
            Assert.That(((Behaviour)brain).enabled, Is.True);
        }

        [Test]
        public void MissingPieceLeavesPlayerViewIntact()
        {
            Assert.That((bool)viewType.GetMethod("Show").Invoke(view, new object[] { null, bench.transform, camera }), Is.False);
            Assert.That(camera.transform.localPosition, Is.EqualTo(originalPosition));
            Assert.That(((Behaviour)brain).enabled, Is.True);
        }
    }
}
