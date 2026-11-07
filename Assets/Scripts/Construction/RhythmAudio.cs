using System;
using UnityEngine;
using UnityEngine.Audio;

// A single scheduled clip keeps the metronome, accompaniment and target cues on the DSP clock.
[RequireComponent(typeof(AudioSource))]
public sealed class RhythmAudio : MonoBehaviour
{
    [SerializeField] private AudioMixerGroup output;
    [Range(0, 1)] public float volume = .45f;
    [Header("Accompagnamento ritmico")]
    [Tooltip("Tic regolare al BPM della ricetta, con un accento ogni quattro battiti.")]
    [Range(0, 1)] public float metronomeVolume = .65f;
    [Tooltip("Motivetto di accompagnamento. Imposta 0 per sentire soltanto tic e note bersaglio.")]
    [Range(0, 1)] public float melodyVolume = .2f;
    [Tooltip("Accenti acuti corrispondenti alle note da colpire con Spazio.")]
    [Range(0, 1)] public float noteVolume = .5f;
    // Original four-bar phrase in D minor pentatonic, played with a soft plucked timbre.
    private static readonly int[] Melody = { 62, 69, 72, 69, 65, 69, 67, 65, 62, 65, 69, 72, 69, 67, 65, 62 };
    private AudioSource source;
    private AudioClip generated;
    private double start, pausedAt, pausedDuration;
    private bool paused;
    public double Elapsed => ElapsedAt(paused ? pausedAt : AudioSettings.dspTime);
    public double ElapsedAt(double dspTime) => dspTime - start - pausedDuration;

    public void Play(double[] notes, double bpm)
    {
        Stop();
        if (source == null) source = GetComponent<AudioSource>();
        const int rate = 44100;
        var samples = new float[(int)Math.Ceiling((notes[notes.Length - 1] + .6) * rate)];
        double beat = 60d / bpm;
        for (int index = 0; index * beat < notes[notes.Length - 1] + .3; index++)
        {
            double time = index * beat;
            bool downbeat = index % 4 == 0;
            AddClick(samples, rate, time, downbeat ? 1100 : 750,
                metronomeVolume * (downbeat ? 1f : .65f));
            if (melodyVolume > 0)
            {
                double frequency = 440 * Math.Pow(2, (Melody[index % Melody.Length] - 69) / 12d);
                AddPluck(samples, rate, time, frequency, Math.Min(.48, beat * .8), melodyVolume);
            }
        }
        foreach (double t in notes) AddClick(samples, rate, t, 1760, noteVolume);
        // Coincident ticks, melody and target accents must not overload the mono mix.
        float peak = 0;
        foreach (float sample in samples) peak = Mathf.Max(peak, Mathf.Abs(sample));
        if (peak > .9f)
        {
            float gain = .9f / peak;
            for (int i = 0; i < samples.Length; i++) samples[i] *= gain;
        }
        generated = AudioClip.Create("Metronomo e melodia del banco", samples.Length, 1, rate, false);
        generated.SetData(samples, 0);
        source.clip = generated;
        source.outputAudioMixerGroup = output;
        source.volume = volume;
        source.spatialBlend = 0;
        source.playOnAwake = false;
        source.loop = false;
        start = AudioSettings.dspTime + .15;
        source.PlayScheduled(start);
    }

    private static void AddClick(float[] data, int rate, double time, double frequency, float gain)
    {
        int startSample = (int)Math.Round(time * rate);
        int length = (int)(rate * .045);
        for (int j = 0; j < length && startSample + j < data.Length; j++)
            data[startSample + j] += gain * (float)(Math.Sin(2 * Math.PI * frequency * j / rate) * Math.Exp(-j / (rate * .008)));
    }

    private static void AddPluck(float[] data, int rate, double time, double frequency, double duration, float gain)
    {
        int startSample = (int)Math.Round(time * rate);
        int length = (int)(duration * rate);
        for (int j = 0; j < length && startSample + j < data.Length; j++)
        {
            double age = j / (double)rate;
            double phase = 2 * Math.PI * frequency * age;
            double attack = Math.Min(1, age / .005);
            double release = Math.Min(1, (length - j) / (rate * .025));
            double envelope = attack * release * Math.Exp(-4 * age / duration);
            double tone = .74 * Math.Sin(phase) + .2 * Math.Sin(phase * 2) + .06 * Math.Sin(phase * 3);
            data[startSample + j] += gain * (float)(tone * envelope);
        }
    }

    public void SetPaused(bool value)
    {
        if (value == paused || generated == null) return;
        if (value) { pausedAt = AudioSettings.dspTime; source.Pause(); }
        else { pausedDuration += AudioSettings.dspTime - pausedAt; source.UnPause(); }
        paused = value;
    }

    public void Stop()
    {
        if (source != null) { source.Stop(); source.clip = null; }
        if (generated != null) Destroy(generated);
        generated = null;
        paused = false;
        pausedDuration = 0;
    }

    private void OnDisable() => Stop();
}
