using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Background Music (BGM)")]
    public AudioClip menuBGM;
    public AudioClip gameBGM;

    [Header("UI Sound Effects")]
    public AudioClip btnHover;
    public AudioClip btnClick;
    public AudioClip menuBtnClick;
    public AudioClip transitionSound;

    [Header("Gameplay Sound Effects")]
    public AudioClip dealCard;
    public AudioClip playCard;
    public AudioClip winRound;
    public AudioClip loseRound;
    public AudioClip cardHover;
    public AudioClip highlightSound;
    public AudioClip dimensionBreakSound;
    public AudioClip grayscaleSound;

    [Header("Volume Settings")]
    [Range(0f, 1f)] public float masterVolume = 1f;
    [Range(0f, 1f)] public float musicVolume = 1f;
    [Range(0f, 1f)] public float gameVolume = 1f;

    [Header("Game Settings")]
    public int botDifficulty = 1; // 0: Easy, 1: Medium, 2: Hard
    public bool predictionMode = true; 

    private Coroutine _fadeCoroutine;
    private float _maxMusicVolume = 1f;
    private float _maxSfxVolume = 1f;

    private float TargetMusicVolume => _maxMusicVolume * masterVolume * musicVolume;
    private float TargetSfxVolume => _maxSfxVolume * masterVolume * gameVolume;

    private void Awake()
    {
        // Singleton pattern to keep AudioManager alive across scenes
        if (Instance == null)
        {
            Instance = this;
            if (musicSource != null) _maxMusicVolume = musicSource.volume;
            if (sfxSource != null) _maxSfxVolume = sfxSource.volume;

            masterVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
            musicVolume = PlayerPrefs.GetFloat("MusicVolume", 1f);
            gameVolume = PlayerPrefs.GetFloat("GameVolume", 1f);
            botDifficulty = PlayerPrefs.GetInt("BotDifficulty", 1);
            predictionMode = PlayerPrefs.GetInt("PredictionMode", 1) == 1;
            ApplyVolumes();

            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SetMasterVolume(float value)
    {
        masterVolume = value;
        PlayerPrefs.SetFloat("MasterVolume", value);
        PlayerPrefs.Save();
        ApplyVolumes();
    }

    public void SetMusicVolume(float value)
    {
        musicVolume = value;
        PlayerPrefs.SetFloat("MusicVolume", value);
        PlayerPrefs.Save();
        ApplyVolumes();
    }

    public void SetGameVolume(float value)
    {
        gameVolume = value;
        PlayerPrefs.SetFloat("GameVolume", value);
        PlayerPrefs.Save();
        ApplyVolumes();
    }

    public void SetBotDifficulty(int level)
    {
        botDifficulty = level;
        PlayerPrefs.SetInt("BotDifficulty", level);
        PlayerPrefs.Save();
    }

    public void SetPredictionMode(bool isEnabled)
    {
        predictionMode = isEnabled;
        PlayerPrefs.SetInt("PredictionMode", isEnabled ? 1 : 0);
        PlayerPrefs.Save();
    }

    private void ApplyVolumes()
    {
        if (musicSource != null && _fadeCoroutine == null)
        {
            musicSource.volume = TargetMusicVolume;
        }
        if (sfxSource != null)
        {
            sfxSource.volume = TargetSfxVolume;
        }
    }

    private void OnEnable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        if (scene.name == "MainMenu")
        {
            PlayMusic(menuBGM);
        }
        else if (scene.name == "SampleScene")
        {
            PlayMusic(gameBGM);
        }
    }

    private void Start()
    {
        // Khi chạy game lần đầu (đặc biệt trong Editor), event sceneLoaded có thể đã trôi qua.
        // Cần tự kiểm tra Scene hiện tại để bật đúng nhạc.
        UnityEngine.SceneManagement.Scene currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        OnSceneLoaded(currentScene, UnityEngine.SceneManagement.LoadSceneMode.Single);
    }

    /// <summary>
    /// Phát một hiệu ứng âm thanh (SFX). Có thể phát chồng lên nhau.
    /// </summary>
    public void PlaySFX(AudioClip clip)
    {
        if (clip != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(clip);
        }
    }

    /// <summary>
    /// Phát nhạc nền (BGM). Sẽ ghi đè nhạc đang phát.
    /// </summary>
    public void PlayMusic(AudioClip clip)
    {
        if (clip != null && musicSource != null)
        {
            // Nếu nhạc này đang phát rồi thì không chạy lại từ đầu để tránh bị giật nhạc
            if (musicSource.clip == clip && musicSource.isPlaying) return;

            if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = StartCoroutine(FadeMusicRoutine(clip, 1.0f)); // Fade cross 1 giây
        }
    }

    private System.Collections.IEnumerator FadeMusicRoutine(AudioClip newClip, float duration)
    {
        // 1. Fade Out nhạc cũ (nếu đang phát)
        if (musicSource.isPlaying)
        {
            float startVolume = musicSource.volume;
            float elapsed = 0f;
            while (elapsed < duration / 2f)
            {
                elapsed += Time.deltaTime;
                musicSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / (duration / 2f));
                yield return null;
            }
        }
        
        // 2. Đổi bài nhạc
        musicSource.volume = 0f;
        musicSource.clip = newClip;
        musicSource.loop = true;
        musicSource.Play();

        // 3. Fade In nhạc mới
        float elapsedIn = 0f;
        while (elapsedIn < duration / 2f)
        {
            elapsedIn += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(0f, TargetMusicVolume, elapsedIn / (duration / 2f));
            yield return null;
        }

        musicSource.volume = TargetMusicVolume;
        _fadeCoroutine = null;
    }

    // Các hàm tiện ích để gọi nhanh từ Unity Events (ví dụ: OnClick của Button)
    
    public void PlayBtnClick()
    {
        PlaySFX(btnClick);
    }

    public void PlayMenuBtnClick()
    {
        PlaySFX(menuBtnClick);
    }

    public void PlayBtnHover()
    {
        PlaySFX(btnHover);
    }

    public void PlayTransitionSound()
    {
        PlaySFX(transitionSound);
    }

    private float _lastDealTime = 0f;

    public void PlayDealCard()
    {
        // Tránh bị "dồn dập" (machine gun effect) khi tốc độ chia quá nhanh
        // Chỉ cho phép phát âm thanh tối đa 1 lần mỗi 0.08 giây
        if (Time.time - _lastDealTime >= 0.08f)
        {
            PlaySFX(dealCard);
            _lastDealTime = Time.time;
        }
    }

    public void PlayCardSound()
    {
        PlaySFX(playCard);
    }

    public void PlayWinRound()
    {
        // Sử dụng PlayMusic để nhạc thắng thay thế nhạc nền. 
        // Khi Restart game, OnSceneLoaded sẽ tự động tắt nhạc này và bật lại nhạc Game.
        PlayMusic(winRound);
    }

    public void PlayLoseRound()
    {
        PlayMusic(loseRound);
    }

    public void PlayCardHover()
    {
        PlaySFX(cardHover);
    }
    
    public void PlayHighlightSound()
    {
        PlaySFX(highlightSound);
    }

    public void PlayDimensionBreakSound()
    {
        PlaySFX(dimensionBreakSound);
    }

    public void PlayGrayscaleSound()
    {
        PlaySFX(grayscaleSound);
    }
}
