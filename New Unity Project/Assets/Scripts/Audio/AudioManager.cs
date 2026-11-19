using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Game.UI;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("音频播放器")]
    public AudioSource bgmSource;
    public AudioSource sfxSource;

    [Header("背景音乐淡入淡出")]
    [Range(0.1f, 5f)]
    public float bgmFadeDuration = 0.8f;

    private Coroutine bgmFadeCoroutine;
    private float bgmFadeLevel;
    private float musicVolume = 1f;
    private float soundVolume = 1f;

    private void Awake()
    {
        // 如果已经存在另一个 AudioManager，就销毁重复的对象
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        // 注册当前实例
        Instance = this;

        // 切换场景时保留这个对象
        DontDestroyOnLoad(gameObject);

        if (bgmSource != null)
        {
            bgmFadeLevel = 0f;
            bgmSource.volume = 0f;
        }
    }

    private void Start()
    {
        // 读取保存值；SettingsManager 在用户拖动滑块时会直接应用新音量。
        SettingsManager settings = SettingsManager.Instance;
        if (settings == null)
            return;

        SetMusicVolume(settings.MusicVolume);
        SetSoundVolume(settings.SoundVolume);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 保持 BGM 过渡不中断：切场景时不重置当前 AudioSource 的播放状态
        if (bgmSource == null)
        {
            return;
        }

        if (bgmSource.clip != null && bgmSource.isPlaying)
        {
            bgmSource.loop = true;
            ApplyBGMVolume();
        }
    }

    // 播放背景音乐，可选淡入时间
    public void PlayBGM(AudioClip clip, float duration = -1f)
    {
        if (clip == null) return;

        if (bgmSource == null)
        {
            Debug.LogWarning("BGM AudioSource 未分配");
            return;
        }

        if (bgmSource.clip == clip && bgmSource.isPlaying)
        {
            ApplyBGMVolume();
            return;
        }

        if (bgmFadeCoroutine != null)
        {
            StopCoroutine(bgmFadeCoroutine);
        }

        float fadeTime = duration > 0f ? duration : bgmFadeDuration;
        bgmFadeCoroutine = StartCoroutine(FadeInBGM(clip, fadeTime));
    }

    // 停止背景音乐，可选淡出时间
    public void StopBGM(float duration = -1f)
    {
        if (bgmSource == null)
            return;

        if (!bgmSource.isPlaying && bgmSource.clip == null)
            return;

        if (bgmFadeCoroutine != null)
        {
            StopCoroutine(bgmFadeCoroutine);
        }

        float fadeTime = duration > 0f ? duration : bgmFadeDuration;
        bgmFadeCoroutine = StartCoroutine(FadeOutBGM(fadeTime));
    }

    // 播放音效
    public void PlaySFX(AudioClip clip)
    {
        PlaySFX(clip, 1f);
    }

    public void PlaySFX(AudioClip clip, float volumeScale)
    {
        if (clip == null) return;
        if (sfxSource == null)
        {
            Debug.LogWarning("SFX AudioSource 未分配");
            return;
        }

        sfxSource.PlayOneShot(clip, volumeScale);
    }

    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        ApplyBGMVolume();
    }

    public void SetSoundVolume(float volume)
    {
        soundVolume = Mathf.Clamp01(volume);
        if (sfxSource != null)
            sfxSource.volume = soundVolume;
    }

    private void ApplyBGMVolume()
    {
        if (bgmSource != null)
            bgmSource.volume = bgmFadeLevel * musicVolume;
    }

    private IEnumerator FadeInBGM(AudioClip clip, float duration)
    {
        if (bgmSource.isPlaying && bgmSource.clip != null && bgmSource.clip != clip)
        {
            yield return StartCoroutine(FadeBGMLevel(
                bgmFadeLevel, 0f, duration * 0.5f));
            bgmSource.Stop();
        }

        bgmSource.clip = clip;
        bgmSource.loop = true;
        bgmFadeLevel = 0f;
        ApplyBGMVolume();
        bgmSource.Play();

        yield return StartCoroutine(FadeBGMLevel(0f, 1f, duration));
        bgmFadeCoroutine = null;
    }

    private IEnumerator FadeOutBGM(float duration)
    {
        if (!bgmSource.isPlaying && bgmSource.clip == null)
        {
            bgmFadeCoroutine = null;
            yield break;
        }

        yield return StartCoroutine(FadeBGMLevel(
            bgmFadeLevel, 0f, duration));

        bgmSource.Stop();
        bgmSource.clip = null;
        bgmFadeLevel = 0f;
        ApplyBGMVolume();
        bgmFadeCoroutine = null;
    }

    private IEnumerator FadeBGMLevel(float from, float to, float duration)
    {
        if (bgmSource == null)
            yield break;

        if (duration <= 0f)
        {
            bgmFadeLevel = to;
            ApplyBGMVolume();
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            bgmFadeLevel = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
            ApplyBGMVolume();
            yield return null;
        }

        bgmFadeLevel = to;
        ApplyBGMVolume();
    }
}
