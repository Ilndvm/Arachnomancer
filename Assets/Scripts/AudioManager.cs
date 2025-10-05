using UnityEngine;
using UnityEngine.Rendering;
public class AudioManager : MonoBehaviour
{
    #region Instance
    public static AudioManager Instance { get; private set; }
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else Destroy(gameObject);
    }
    #endregion

    [Header("Audio Sources")]
    [SerializeField] private AudioSource soundFXObject;
    [SerializeField] private AudioSource soundUIObject;
    [SerializeField] private AudioSource musicSource;

    [Header("Music Clips")]
    [SerializeField] private AudioClip[] musicClips;

    private int currentMusicIndex = 0;

    public enum Sound
    {
        HitEnemy,
        ShieldActivate,
        PickUp,
        ButtonClick
    }

    #region soundAudioClipArray
    [SerializeField] private SoundAudioClip[] soundAudioClipArray;

    [System.Serializable]
    public class SoundAudioClip
    {
        public Sound sound;
        public AudioClip audioClip;
    }
    #endregion



    void Start()
    {
        if (musicClips.Length > 0 && musicSource != null)
        {
            PlayCurrentMusic(0.5f);
        }
        else
        {
            Debug.LogWarning("MusicPlayer: Missing AudioSource or no clips assigned.");
        }
    }

    //void Update()
    //{
    //    if (!musicSource.isPlaying)
    //    {
    //        PlayNextMusic();
    //    }
    //}

    private void PlayCurrentMusic(float volume)
    {
        musicSource.clip = musicClips[currentMusicIndex];
        musicSource.volume = volume;
        musicSource.Play();
    }
    
    public void ChangeMusic(int currentMusicIndex, float volume)
    {
        musicSource.clip = musicClips[currentMusicIndex];
        musicSource.volume = volume;
        musicSource.Play();
    }

    private void PlayNextMusic()
    {
        currentMusicIndex = (currentMusicIndex + 1) % musicClips.Length;
        PlayCurrentMusic(0.5f);
    }

    public void PlaySound(Sound sound)
    {
        AudioSource audioSource = Instantiate(soundFXObject, this.transform);

        audioSource.clip = GetAudioClip(sound);
        audioSource.volume = 1;
        audioSource.Play();
        float clipLength = audioSource.clip.length;

        Destroy(audioSource.gameObject, clipLength);
    }

    public void PlaySound(Sound sound, float volume)
    {
        AudioSource audioSource = Instantiate(soundFXObject, this.transform);

        audioSource.clip = GetAudioClip(sound);
        audioSource.volume = volume;
        audioSource.Play();
        float clipLength = audioSource.clip.length;

        Destroy(audioSource.gameObject, clipLength);
    }
    public void PlayUISound(Sound sound)
    {
        AudioSource audioSource = Instantiate(soundUIObject, this.transform);

        audioSource.clip = GetAudioClip(sound);
        audioSource.volume = 1;
        audioSource.Play();
        float clipLength = audioSource.clip.length;

        Destroy(audioSource.gameObject, clipLength);
    }

    public void PlayUISound(Sound sound, float volume)
    {
        AudioSource audioSource = Instantiate(soundUIObject, this.transform);

        audioSource.clip = GetAudioClip(sound);
        audioSource.volume = volume;
        audioSource.Play();
        float clipLength = audioSource.clip.length;

        Destroy(audioSource.gameObject, clipLength);
    }

    public void PlaySound(AudioClip clip)
    {
        // Create a temporary AudioSource
        AudioSource audioSource = Instantiate(soundUIObject, this.transform);
        audioSource.clip = clip;
        audioSource.volume = 1f;
        audioSource.Play();

        // Destroy the temporary object after the clip finishes
        Destroy(audioSource.gameObject, clip.length);
    }
    public AudioClip GetAudioClip(Sound sound)
    {

        foreach (SoundAudioClip soundAudioClip in soundAudioClipArray)
        {
            if (soundAudioClip.sound == sound)
            {
                return soundAudioClip.audioClip;
            }
        }
        Debug.LogError("Sound " + sound + " is not found");
        return null;
    }
}