using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Basilica.Crafting.Tests
{
    // The test assembly references Core; resolve the scene component from Assembly-CSharp
    // to exercise its generated clip without introducing a circular assembly reference.
    public sealed class RhythmAudioTests
    {
        private GameObject owner;
        private Component audio;
        private Type audioType;

        [SetUp]
        public void Setup()
        {
            audioType = Type.GetType("RhythmAudio, Assembly-CSharp", true);
            owner = new GameObject("Rhythm audio test");
            audio = owner.AddComponent(audioType);
        }

        [TearDown]
        public void Cleanup()
        {
            // Release the generated clip immediately in EditMode; normal gameplay uses Destroy.
            var source = owner.GetComponent<AudioSource>();
            var clip = source.clip;
            source.Stop();
            source.clip = null;
            if (clip != null) UnityEngine.Object.DestroyImmediate(clip);
            UnityEngine.Object.DestroyImmediate(owner);
        }

        private float[] Play(double bpm = 100)
        {
            audioType.GetMethod("Play").Invoke(audio, new object[] { new[] { 3d, 4d }, bpm });
            var clip = owner.GetComponent<AudioSource>().clip;
            var samples = new float[clip.samples];
            Assert.That(clip.GetData(samples, 0), Is.True);
            return samples;
        }

        private static double Energy(float[] samples, double start, double end)
        {
            int first = (int)(start * 44100), last = (int)(end * 44100);
            double sum = 0;
            for (int i = first; i < last; i++) sum += samples[i] * samples[i];
            return Math.Sqrt(sum / (last - first));
        }

        [Test]
        public void MelodyPlaysDuringPreparationBetweenMetronomeTicks()
        {
            var samples = Play();
            Assert.That(Energy(samples, .12, .24), Is.GreaterThan(.005),
                "Preparation should contain a melody between the short metronome ticks.");
        }

        [Test]
        public void FirstBeatOfEachBarIsAccented()
        {
            var samples = Play();
            Assert.That(Energy(samples, 0, .04), Is.GreaterThan(Energy(samples, .6, .64) * 1.2));
            Assert.That(Energy(samples, 2.4, 2.44), Is.GreaterThan(Energy(samples, 1.8, 1.84) * 1.2));
        }

        [Test]
        public void MelodyCanBeMutedWithoutRemovingMetronomeOrTargetCues()
        {
            var melody = audioType.GetField("melodyVolume");
            Assert.That(melody, Is.Not.Null, "The accompaniment needs its own volume control.");
            melody.SetValue(audio, 0f);
            var samples = Play();
            Assert.That(Energy(samples, .12, .24), Is.LessThan(.00001));
            Assert.That(Energy(samples, 0, .04), Is.GreaterThan(.03));
            Assert.That(Energy(samples, 4, 4.04), Is.GreaterThan(.03));
        }

        [TestCase(40)]
        [TestCase(100)]
        [TestCase(180)]
        public void MixedSoundtrackHasNoClippedOrInvalidSamples(double bpm)
        {
            foreach (string name in new[] { "metronomeVolume", "melodyVolume", "noteVolume" })
            {
                var field = audioType.GetField(name);
                Assert.That(field, Is.Not.Null);
                field.SetValue(audio, 1f);
            }
            var samples = Play(bpm);
            Assert.That(samples.All(s => !float.IsNaN(s) && !float.IsInfinity(s) && Math.Abs(s) <= .95f), Is.True);
            Assert.That(Energy(samples, 0, .04), Is.GreaterThan(.03));
        }
    }

    public sealed class CraftingCoreTests
    {
        private MaterialDefinition stone, lime;
        private CraftingRecipe recipe;
        private GameObject prefab;

        [SetUp]
        public void Setup()
        {
            stone = ScriptableObject.CreateInstance<MaterialDefinition>();
            stone.displayName = "Pietra"; stone.unitPrice = 3;
            lime = ScriptableObject.CreateInstance<MaterialDefinition>();
            lime.displayName = "Calce"; lime.unitPrice = 2;
            prefab = new GameObject("Test piece");
            recipe = ScriptableObject.CreateInstance<CraftingRecipe>();
            recipe.displayName = "Fondamenta";
            recipe.piecePrefab = prefab;
            recipe.costs = new[] { new MaterialCost(stone, 4), new MaterialCost(lime, 2) };
        }

        [TearDown]
        public void Cleanup()
        {
            UnityEngine.Object.DestroyImmediate(recipe);
            UnityEngine.Object.DestroyImmediate(stone);
            UnityEngine.Object.DestroyImmediate(lime);
            UnityEngine.Object.DestroyImmediate(prefab);
        }

        private MaterialInventory Stocked()
        {
            var bag = new MaterialInventory(200);
            Assert.That(bag.TryBuy(stone, 8, out _), Is.True);
            Assert.That(bag.TryBuy(lime, 4, out _), Is.True);
            return bag;
        }

        [TestCase(CraftOutcome.NormalSuccess, 4, 2)]
        [TestCase(CraftOutcome.PerfectSuccess, 6, 3)]
        [TestCase(CraftOutcome.Failure, 6, 3)]
        [TestCase(CraftOutcome.TechnicalAbort, 8, 4)]
        public void SettlementConsumesCorrectFraction(CraftOutcome outcome, int stones, int limes)
        {
            var bag = Stocked();
            Assert.That(bag.TryReserve(recipe, out var payment, out _), Is.True);
            Assert.That(bag.Count(stone), Is.EqualTo(4));
            Assert.That(payment.Settle(outcome), Is.True);
            Assert.That(bag.Count(stone), Is.EqualTo(stones));
            Assert.That(bag.Count(lime), Is.EqualTo(limes));
            Assert.That(bag.Coins, Is.EqualTo(168));
        }

        [Test]
        public void SettlementAndConcurrentReservationCannotDuplicateRefunds()
        {
            var bag = Stocked();
            bag.TryReserve(recipe, out var payment, out _);
            Assert.That(bag.TryReserve(recipe, out _, out _), Is.False);
            Assert.That(payment.Settle(CraftOutcome.Failure), Is.True);
            Assert.That(payment.Settle(CraftOutcome.TechnicalAbort), Is.False);
            Assert.That(bag.Count(stone), Is.EqualTo(6));
            Assert.That(bag.TryReserve(recipe, out _, out _), Is.True);
        }

        [Test]
        public void MissingOneIngredientDoesNotConsumeTheOther()
        {
            var bag = new MaterialInventory(200);
            bag.TryBuy(stone, 8, out _);
            Assert.That(bag.TryReserve(recipe, out _, out _), Is.False);
            Assert.That(bag.Count(stone), Is.EqualTo(8));
            Assert.That(bag.Coins, Is.EqualTo(176));
        }

        [TestCase(0)] [TestCase(-2)] [TestCase(3)]
        public void InvalidRecipeQuantitiesCannotReserve(int amount)
        {
            var bag = Stocked();
            recipe.costs[0] = new MaterialCost(stone, amount);
            Assert.That(recipe.IsValid(out _), Is.False);
            Assert.That(bag.TryReserve(recipe, out _, out _), Is.False);
            Assert.That(bag.Count(stone), Is.EqualTo(8));
        }

        [Test]
        public void DuplicateIngredientsAreRejectedAtomically()
        {
            var bag = Stocked();
            recipe.costs = new[] { new MaterialCost(stone, 4), new MaterialCost(stone, 4) };
            Assert.That(bag.TryReserve(recipe, out _, out _), Is.False);
            Assert.That(bag.Count(stone), Is.EqualTo(8));
        }

        [TestCase(0)] [TestCase(-1)] [TestCase(int.MaxValue)]
        public void InvalidOrUnaffordablePurchaseLeavesWalletUntouched(int quantity)
        {
            var bag = new MaterialInventory(200);
            Assert.That(bag.TryBuy(stone, quantity, out _), Is.False);
            Assert.That(bag.Coins, Is.EqualTo(200));
            Assert.That(bag.Count(stone), Is.Zero);
        }

        [Test]
        public void ReservationSnapshotsCostsAgainstAssetEdits()
        {
            var bag = Stocked();
            bag.TryReserve(recipe, out var payment, out _);
            recipe.costs[0] = new MaterialCost(stone, 20);
            payment.Settle(CraftOutcome.Failure);
            Assert.That(bag.Count(stone), Is.EqualTo(6));
        }

        [Test]
        public void EightCorrectHitsArePerfectAndSixAreNormal()
        {
            var times = Enumerable.Range(0, 8).Select(i => 2.0 + i * .6).ToArray();
            var perfect = new RhythmSequence(times, .15, 6);
            foreach (var t in times) Assert.That(perfect.Press(t), Is.True);
            Assert.That(perfect.Outcome, Is.EqualTo(CraftOutcome.PerfectSuccess));
            var normal = new RhythmSequence(times, .15, 6);
            foreach (var t in times.Take(6)) normal.Press(t);
            normal.Advance(10);
            Assert.That(normal.Outcome, Is.EqualTo(CraftOutcome.NormalSuccess));
            Assert.That(normal.Errors, Is.EqualTo(2));
        }

        [Test]
        public void FiveHitsFailAndLateFramesCountAllMissedNotes()
        {
            var sequence = new RhythmSequence(new[] { 2d, 3, 4, 5, 6, 7, 8, 9 }, .15, 6);
            for (int i = 2; i < 7; i++) sequence.Press(i);
            sequence.Advance(100);
            Assert.That(sequence.Hits, Is.EqualTo(5));
            Assert.That(sequence.Errors, Is.EqualTo(3));
            Assert.That(sequence.Outcome, Is.EqualTo(CraftOutcome.Failure));
        }

        [TestCase(-.15)] [TestCase(.15)]
        public void HitWindowIncludesBothBoundaries(double offset)
        {
            var sequence = new RhythmSequence(new[] { 2d, 3d }, .15, 1);
            Assert.That(sequence.Press(2 + offset), Is.True);
        }

        [Test]
        public void EarlyAndRepeatedPressesConsumeNotesInsteadOfRewardingSpam()
        {
            var sequence = new RhythmSequence(new[] { 2d, 3d, 4d, 5d }, .15, 3);
            sequence.Press(.5);
            sequence.Press(2);
            sequence.Press(2);
            sequence.Press(2);
            Assert.That(sequence.IsComplete, Is.True);
            Assert.That(sequence.Hits, Is.Zero);
            Assert.That(sequence.Outcome, Is.EqualTo(CraftOutcome.Failure));
        }

        [Test]
        public void PatternUsesHalfBeatGridAndNeverRepeatsPrevious()
        {
            var first = RhythmSequence.GeneratePattern(8, 100, new System.Random(123));
            var next = RhythmSequence.GeneratePattern(8, 100, new System.Random(123), first);
            Assert.That(first.Length, Is.EqualTo(8));
            Assert.That(first[0], Is.GreaterThanOrEqualTo(2));
            Assert.That(first.SequenceEqual(next), Is.False);
            for (int i = 1; i < next.Length; i++)
            {
                double steps = (next[i] - next[i - 1]) / .3;
                Assert.That(steps, Is.EqualTo(Math.Round(steps)).Within(.00001));
                Assert.That(steps, Is.InRange(.9999, 3.0001));
            }
        }

        [Test]
        public void PausedClockDoesNotAdvanceJudgments()
        {
            var sequence = new RhythmSequence(new[] { 2d, 3d }, .15, 1);
            sequence.Advance(1);
            sequence.Advance(1); // World time does not enter the sequence; the caller freezes its clock.
            Assert.That(sequence.Errors, Is.Zero);
            Assert.That(sequence.Press(2), Is.True);
        }
    }
}
