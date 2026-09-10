using UnityEngine;
using UnityEngine.UI;

// Va messo sia sul pannello Impostazioni di MainMenu sia su quello di GameScene: ogni volta
// che il pannello si attiva, inizializza gli slider con il volume attuale letto da
// AudioManager invece di lasciarli al valore serializzato in Editor. Il volume vero vive
// nell'AudioMixer condiviso tramite AudioManager (DontDestroyOnLoad tra le scene), quindi
// senza questo passaggio un cambiamento fatto nel menu principale non si vedrebbe
// riflesso riaprendo il pannello nella scena di gioco (pur essendo comunque applicato).
//
// Gli OnValueChanged() degli slider vanno agganciati ai metodi SetXVolume QUI, non
// direttamente su un AudioManager della scena: in una scena diversa da quella in cui vive
// l'istanza persistente (DontDestroyOnLoad), l'unico AudioManager trascinabile in Inspector
// è una copia locale che si autodistrugge al suo Awake — un riferimento diretto a lei
// resta agganciato a un oggetto ormai distrutto. Passando da qui, che risolve sempre
// AudioManager.Instance al momento della chiamata, si punta sempre a quella viva.
public class SettingsPanelController : MonoBehaviour
{
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;
    [SerializeField] private Slider uiSfxVolumeSlider;

    private void OnEnable()
    {
        if (AudioManager.Instance == null)
        {
            return;
        }

        SetSliderWithoutNotify(masterVolumeSlider, AudioManager.Instance.GetMasterVolume());
        SetSliderWithoutNotify(musicVolumeSlider, AudioManager.Instance.GetMusicVolume());
        SetSliderWithoutNotify(sfxVolumeSlider, AudioManager.Instance.GetSFXVolume());
        SetSliderWithoutNotify(uiSfxVolumeSlider, AudioManager.Instance.GetUISFXVolume());
    }

    public void SetMasterVolume(float value) => AudioManager.Instance?.SetMasterVolume(value);
    public void SetMusicVolume(float value) => AudioManager.Instance?.SetMusicVolume(value);
    public void SetSFXVolume(float value) => AudioManager.Instance?.SetSFXVolume(value);
    public void SetUISFXVolume(float value) => AudioManager.Instance?.SetUISFXVolume(value);

    // SetValueWithoutNotify evita di richiamare AudioManager.SetXVolume durante
    // l'inizializzazione: altrimenti ogni apertura del pannello riscriverebbe nel mixer un
    // valore già suo, inutilmente (e con un giro dB->lineare->dB che nel tempo potrebbe
    // accumulare una piccola deriva).
    private static void SetSliderWithoutNotify(Slider slider, float value)
    {
        if (slider != null)
        {
            slider.SetValueWithoutNotify(value);
        }
    }
}
