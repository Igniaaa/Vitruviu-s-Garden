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
            times[0] = 2.4;
            for (int i = 1; i < count; i++) times[i] = times[i - 1] + step * random.Next(1, 4);
            if (previous != null && times.SequenceEqual(previous))
                times[count - 1] = times[count - 2] + step * (times[count - 1] - times[count - 2] < 2.5 * step ? 3 : 1);
            return times;
        }
    }
}
