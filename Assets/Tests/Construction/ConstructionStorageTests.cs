using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Basilica.Crafting.Tests
{
    public sealed class ConstructionStorageTests
    {
        private GameObject root, storageObject, pieceObject;
        private Component storage, piece;
        private Type storageType;
        private BoxCollider area;
        private static readonly Vector3 Origin = new Vector3(10000, 0, 10000);

        [SetUp]
        public void Setup()
        {
            storageType = Type.GetType("ConstructionStorageArea, Assembly-CSharp");
            Assert.That(storageType, Is.Not.Null, "A storage area must transfer real crafted pieces into a free place in the enclosure.");
            root = new GameObject("Storage tests");
            root.transform.position = Origin;
            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.transform.SetParent(root.transform, false);
            ground.transform.localPosition = new Vector3(0, -.1f, 0);
            ground.transform.localScale = new Vector3(10, .2f, 10);
            storageObject = new GameObject("Enclosure");
            storageObject.transform.SetParent(root.transform, false);
            area = storageObject.AddComponent<BoxCollider>();
            area.isTrigger = true;
            area.center = new Vector3(0, .8f, 0);
            area.size = new Vector3(4, 1.6f, 3);
            storage = storageObject.AddComponent(storageType);
            storageType.GetField("volume").SetValue(storage, area);
            pieceObject = NewPiece(new Vector3(8, 1, 0));
            piece = pieceObject.GetComponent(Type.GetType("DraggableObject, Assembly-CSharp", true));
            Physics.SyncTransforms();
        }

        [TearDown]
        public void Cleanup()
        {
            if (root != null) UnityEngine.Object.DestroyImmediate(root);
        }

        private GameObject NewPiece(Vector3 position)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.transform.SetParent(root.transform, false);
            go.transform.localPosition = position;
            go.transform.localScale = new Vector3(.7f, .9f, .5f);
            go.AddComponent<Rigidbody>();
            var draggableType = Type.GetType("DraggableObject, Assembly-CSharp", true);
            var draggable = go.AddComponent(draggableType);
            // EditMode does not run the component's normal initialization.
            draggableType.GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(draggable, null);
            return go;
        }

        private bool Store(Component target)
        {
            var args = new object[] { target, null };
            return (bool)storageType.GetMethod("TryStore").Invoke(storage, args);
        }

        [Test]
        public void TransferMovesSamePieceInsideVolumeAndClearsVelocity()
        {
            var body = pieceObject.GetComponent<Rigidbody>();
            body.linearVelocity = Vector3.right * 4;
            body.angularVelocity = Vector3.up * 3;
            int identity = pieceObject.GetInstanceID();
            Assert.That(Store(piece), Is.True);
            Assert.That(pieceObject.GetInstanceID(), Is.EqualTo(identity));
            Assert.That(area.bounds.Contains(pieceObject.GetComponent<Collider>().bounds.center), Is.True);
            Assert.That(pieceObject.GetComponent<Collider>().bounds.min.y, Is.GreaterThan(0));
            Assert.That(body.linearVelocity, Is.EqualTo(Vector3.zero));
            Assert.That(body.angularVelocity, Is.EqualTo(Vector3.zero));
            Assert.That(body.isKinematic, Is.False);
        }

        [Test]
        public void SuccessivePiecesUseDifferentNonOverlappingPlaces()
        {
            Assert.That(Store(piece), Is.True);
            var second = NewPiece(new Vector3(8, 1, 2));
            Physics.SyncTransforms();
            Assert.That(Store(second.GetComponent(piece.GetType())), Is.True);
            Assert.That(pieceObject.GetComponent<Collider>().bounds.Intersects(second.GetComponent<Collider>().bounds), Is.False);
        }

        [Test]
        public void FullStorageLeavesPieceOnWorkbench()
        {
            var obstruction = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obstruction.transform.SetParent(root.transform, false);
            obstruction.transform.localPosition = new Vector3(0, 3, 0);
            obstruction.transform.localScale = new Vector3(6, 6, 6);
            Physics.SyncTransforms();
            var before = pieceObject.transform.position;
            Assert.That(Store(piece), Is.False);
            Assert.That(pieceObject.transform.position, Is.EqualTo(before));
        }

        [Test]
        public void MissingVolumeDoesNotMovePiece()
        {
            storageType.GetField("volume").SetValue(storage, null);
            var before = pieceObject.transform.position;
            Assert.That(Store(piece), Is.False);
            Assert.That(pieceObject.transform.position, Is.EqualTo(before));
        }

        [Test]
        public void HeldPieceCannotBeTransferred()
        {
            piece.GetType().GetMethod("BeginDrag").Invoke(piece, null);
            var before = pieceObject.transform.position;
            Assert.That(Store(piece), Is.False);
            Assert.That(pieceObject.transform.position, Is.EqualTo(before));
        }

        [Test]
        public void LockedPieceCannotBeTransferred()
        {
            piece.GetType().GetField("isLocked", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(piece, true);
            Assert.That(Store(piece), Is.False);
        }
    }
}
