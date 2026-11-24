using System;
using System.Linq;

namespace Basilica.Crafting
{
    public sealed class RhythmSequence
    {
        public double[] NoteTimes { get; private set; }
        public int[] Judgments { get; private set; }
        public int Hits { get; private set; }
        public int Errors { get; private set; }
        public bool IsComplete { get; private set; }
        private readonly double window;
        private readonly int minimumHits;
        private int next;
        // Half-beat intervals: straight beats, pickups, syncopation, bursts and rests.
        private static readonly int[][] Motifs =
        {
            new[] { 2, 2, 2, 2 }, new[] { 1, 1, 2, 2 }, new[] { 2, 1, 1, 2 }, new[] { 2, 2, 1, 1 },
            new[] { 1, 2, 1, 2 }, new[] { 3, 1, 2, 2 }, new[] { 2, 3, 1, 2 }, new[] { 1, 3, 1, 3 },
            new[] { 4, 1, 1, 2 }, new[] { 2, 1, 1, 4 }, new[] { 1, 1, 1, 1, 2, 2 }, new[] { 1, 2, 1, 4 },
            new[] { 3, 3, 1, 1 }, new[] { 1, 1, 3, 3 }, new[] { 2, 4, 1, 1 }, new[] { 4, 2, 1, 1 }
        };
        public CraftOutcome Outcome => Hits == NoteTimes.Length && Errors == 0 ? CraftOutcome.PerfectSuccess :
            Hits >= minimumHits ? CraftOutcome.NormalSuccess : CraftOutcome.Failure;

        public RhythmSequence(double[] noteTimes, double window, int minimumHits)
        {
            if (noteTimes == null || noteTimes.Length == 0 || minimumHits < 1 || minimumHits > noteTimes.Length ||
                double.IsNaN(window) || double.IsInfinity(window) || window <= 0) throw new ArgumentException("Invalid rhythm");
            for (int i = 0; i < noteTimes.Length; i++)
                if (double.IsNaN(noteTimes[i]) || double.IsInfinity(noteTimes[i]) || noteTimes[i] < 0 ||
                    i > 0 && noteTimes[i] <= noteTimes[i - 1]) throw new ArgumentException("Notes must be ordered");
            NoteTimes = (double[])noteTimes.Clone();
            Judgments = new int[noteTimes.Length];
            this.window = window;
            this.minimumHits = minimumHits;
        }

        public bool Press(double time)
        {
            if (double.IsNaN(time) || double.IsInfinity(time) || time < 0 || IsComplete) return false;
            Advance(time);
            if (IsComplete) return false;
            bool hit = Math.Abs(time - NoteTimes[next]) <= window + 1e-7;
            Judgments[next++] = hit ? 1 : -1;
            if (hit) Hits++; else Errors++;
            IsComplete = next == NoteTimes.Length;
            return hit;
        }

        public void Advance(double time)
        {
            if (double.IsNaN(time) || double.IsInfinity(time)) return;
            while (next < NoteTimes.Length && time > NoteTimes[next] + window + 1e-7)
            { Judgments[next++] = -1; Errors++; }
            IsComplete = next == NoteTimes.Length;
        }

        public static double[] GeneratePattern(int count, double bpm, Random random, double[] previous = null)
        {
            if (count < 2 || count > 64 || double.IsNaN(bpm) || bpm < 40 || bpm > 180 || random == null)
                throw new ArgumentException("Invalid pattern settings");
            double step = 30d / bpm;
            var times = new double[count];
            double beat = step * 2;
            times[0] = Math.Ceiling(2.4 / beat - 1e-9) * beat;
            int motif = random.Next(Motifs.Length), offset = 0;
            for (int i = 1; i < count; i++)
            {
                times[i] = times[i - 1] + step * Motifs[motif][offset++];
                if (offset == Motifs[motif].Length)
                {
                    motif = (motif + 1 + random.Next(Motifs.Length - 1)) % Motifs.Length;
                    offset = 0;
                }
            }
            if (previous != null && times.SequenceEqual(previous))
                times[count - 1] = times[count - 2] + step * ((int)Math.Round((times[count - 1] - times[count - 2]) / step) % 4 + 1);
            return times;
        }
    }
}
