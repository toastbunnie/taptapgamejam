using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace Game.UI
{
    /// <summary>
    /// 全局设置管理器（单例，跨场景）。
    /// 负责音量设置的存储；音乐和音效的实时值会直接应用到 AudioManager。
    /// </summary>
    public class SettingsManager : MonoBehaviour
    {
        public static SettingsManager Instance { get; private set; }

        [Header("Volume (0-1)")]
        [Range(0f, 1f)] public float SoundVolume = 1f;
        [Range(0f, 1f)] public float MusicVolume = 1f;
        [Range(0f, 1f)] public float VoiceVolume = 1f;

        // 保留音量事件供其他播放系统订阅。
        [System.Serializable] public class VolumeEvent : UnityEvent<float> { }
        public VolumeEvent OnSoundVolumeChanged = new VolumeEvent();
        public VolumeEvent OnMusicVolumeChanged = new VolumeEvent();
        public VolumeEvent OnVoiceVolumeChanged = new VolumeEvent();

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadFromPrefs();
        }

        public void SetSoundVolume(float v) 
        { 
            SoundVolume = Mathf.Clamp01(v); 
            if (AudioManager.Instance != null)
                AudioManager.Instance.SetSoundVolume(SoundVolume);
            OnSoundVolumeChanged.Invoke(SoundVolume); 
        }

        public void SetMusicVolume(float v) 
        { 
            MusicVolume = Mathf.Clamp01(v); 
            if (AudioManager.Instance != null)
                AudioManager.Instance.SetMusicVolume(MusicVolume);
            OnMusicVolumeChanged.Invoke(MusicVolume); 
        }

        public void SetVoiceVolume(float v) 
        { 
            VoiceVolume = Mathf.Clamp01(v); 
            OnVoiceVolumeChanged.Invoke(VoiceVolume); 
        }

        /// <summary>把当前音量写到磁盘（拖 slider 完才调）</summary>
        public void Save()
        {
            PlayerPrefs.SetFloat("SoundVolume", SoundVolume);
            PlayerPrefs.SetFloat("MusicVolume", MusicVolume);
            PlayerPrefs.SetFloat("VoiceVolume", VoiceVolume);
            PlayerPrefs.Save();
        }

        void LoadFromPrefs()
        {
            SoundVolume = PlayerPrefs.GetFloat("SoundVolume", 1f);
            MusicVolume = PlayerPrefs.GetFloat("MusicVolume", 1f);
            VoiceVolume = PlayerPrefs.GetFloat("VoiceVolume", 1f);
        }

        /// <summary>重载当前场景（防卡死用，不加二次确认）</summary>
        public void ReloadCurrentEnvironment()
        {
            var scene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(scene.buildIndex);
        }
    }
}
