using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

// Singleton persistente tra le scene (MainMenu -> GameScene): riproduce musica, SFX di
// gioco e SFX di UI cercandoli per nome nelle tre liste sotto, e cambia traccia da sola
// quando si carica una scena il cui nome coincide con quello di un brano in musicList.
[RequireComponent(typeof(AudioSource))]
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Lists")]
    [SerializeField] private List<Sound> musicList = new();
    [SerializeField] private List<Sound> sfxList = new();
    [SerializeField] private List<Sound> uiList = new();

    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource uiSource;

    [Header("Mixer Settings")]
    [SerializeField] private AudioMixer mixer;
    [SerializeField] private string masterVolumeParam = "MasterVolume";
    [SerializeField] private string musicVolumeParam = "MusicVolume";
    [SerializeField] private string sfxVolumeParam = "SFXVolume";
    [SerializeField] private string uiSfxVolumeParam = "UISFXVolume";

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        if (sfxSource == null)
        {
            sfxSource = GetComponent<AudioSource>();
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // Le tracce in musicList vanno chiamate come la scena a cui appartengono
    // (es. "MainMenu", "GameScene") per essere fatte partire automaticamente qui.
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        PlayMusic(scene.name);
    }

    public void PlayMusic(string musicName)
    {
        Sound s = musicList.Find(music => music.name == musicName);

        if (s == null || musicSource.clip == s.clip)
        {
            return;
        }

        musicSource.clip = s.clip;
        musicSource.Play();
    }

    public void PlaySFX(string soundName)
    {
        Sound s = sfxList.Find(sound => sound.name == soundName);

        if (s != null)
        {
            sfxSource.PlayOneShot(s.clip);
        }
        else
        {
            Debug.LogWarning("SFX non trovato: " + soundName);
        }
    }

    public void PlayUISFX(string soundName)
    {
        Sound s = uiList.Find(sound => sound.name == soundName);

        if (s != null)
        {
            uiSource.PlayOneShot(s.clip);
        }
        else
        {
            Debug.LogWarning("UI SFX non trovato: " + soundName);
        }
    }

    public void FadeMusicOut(float duration)
    {
        StopAllCoroutines();
        StartCoroutine(FadeMusicRoutine(duration));
    }

    private IEnumerator FadeMusicRoutine(float duration)
    {
        float startVolume = musicSource.volume;
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(startVolume, 0f, t / duration);
            yield return null;
        }

        musicSource.volume = 0f;
    }

    // Pensati per gli slider del pannello Impostazioni: volume lineare 0-1, convertito in dB
    // per il parametro esposto dall'AudioMixer.
    #region Volume
    public void SetMasterVolume(float linearVolume) => SetMixerVolume(masterVolumeParam, linearVolume);
    public void SetMusicVolume(float linearVolume) => SetMixerVolume(musicVolumeParam, linearVolume);
    public void SetSFXVolume(float linearVolume) => SetMixerVolume(sfxVolumeParam, linearVolume);
    public void SetUISFXVolume(float linearVolume) => SetMixerVolume(uiSfxVolumeParam, linearVolume);

    private void SetMixerVolume(string exposedParam, float linearVolume)
    {
        if (mixer == null)
        {
            return;
        }

        float dB = linearVolume > 0.0001f ? Mathf.Log10(linearVolume) * 20f : -80f;
        mixer.SetFloat(exposedParam, dB);
    }
    #endregion
}

[System.Serializable]
public class Sound
{
    public string name;
    public AudioClip clip;
}
