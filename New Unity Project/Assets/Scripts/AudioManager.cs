using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("音频播放器")]
    public AudioSource bgmSource;
    public AudioSource sfxSource;

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
    }

    // 播放背景音乐
    public void PlayBGM(AudioClip clip)
    {
        if (clip == null) return;

        if (bgmSource.clip == clip && bgmSource.isPlaying)
            return;

        bgmSource.clip = clip;
        bgmSource.loop = true;
        bgmSource.Play();
    }

    // 停止背景音乐
    public void StopBGM()
    {
        bgmSource.Stop();
    }

    // 播放音效
    public void PlaySFX(AudioClip clip)
    {
        if (clip == null) return;

        sfxSource.PlayOneShot(clip);
    }
}