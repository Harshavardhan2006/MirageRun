using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }
    public static bool Muted { get; private set; }

    [SerializeField] AudioClip coin;
    [SerializeField] AudioClip gem;
    [SerializeField] AudioClip crash;
    [SerializeField] AudioClip click;
    [SerializeField] AudioClip jump;
    [SerializeField] AudioClip wind;
    [SerializeField] AudioClip music;

    AudioSource sfx;
    AudioSource musicSource;
    AudioSource windSource;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        sfx = gameObject.AddComponent<AudioSource>();
        sfx.playOnAwake = false;
        musicSource = Loop(music, 0.35f);
        windSource = Loop(wind, 0.22f);

        Muted = PlayerPrefs.GetInt("MUTE", 0) == 1;
        AudioListener.volume = Muted ? 0f : 1f;
    }

    AudioSource Loop(AudioClip clip, float volume)
    {
        AudioSource source = gameObject.AddComponent<AudioSource>();
        source.clip = clip;
        source.loop = true;
        source.volume = volume;
        source.playOnAwake = false;
        if (clip != null) source.Play();
        return source;
    }

    static void Play(AudioClip clip, float volume)
    {
        if (Instance == null || clip == null) return;
        Instance.sfx.PlayOneShot(clip, volume);
    }

    public static void PlayCoin() { if (Instance != null) Play(Instance.coin, 0.7f); }
    public static void PlayGem() { if (Instance != null) Play(Instance.gem, 0.8f); }
    public static void PlayCrash() { if (Instance != null) Play(Instance.crash, 1f); }
    public static void PlayClick() { if (Instance != null) Play(Instance.click, 0.7f); }
    public static void PlayJump() { if (Instance != null) Play(Instance.jump, 0.5f); }

    public static void ToggleMute()
    {
        Muted = !Muted;
        PlayerPrefs.SetInt("MUTE", Muted ? 1 : 0);
        AudioListener.volume = Muted ? 0f : 1f;
    }
}
