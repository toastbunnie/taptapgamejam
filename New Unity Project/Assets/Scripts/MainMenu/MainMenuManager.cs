using UnityEngine;
using UnityEngine.SceneManagement;
using Game.Audio;

public class MainMenuManager : MonoBehaviour
{
    public GameObject mainMenuPanel;
    public GameObject galleryPanel;

    [SerializeField] private string menuBgmId = "主场景bgm";

    private void Start()
    {
        AudioDatabase database = FindObjectOfType<AudioDatabase>();
        if (database == null || !database.TryGetBGM(menuBgmId, out AudioClip clip))
        {
            Debug.LogWarning($"[MainMenuManager] 找不到主菜单 BGM：{menuBgmId}", this);
            return;
        }

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayBGM(clip);
        else
            Debug.LogWarning("[MainMenuManager] 找不到 AudioManager，无法播放主菜单 BGM。", this);
    }

    public void StartGame()
    {
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.StartNewGame("1-1");
        }
        else
        {
            Debug.LogError("找不到 SaveManager，请检查 MainMenu 场景中的绑定。");
        }
    }

    public void ContinueGame()
    {
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.ContinueGame();
        }
        else
        {
            Debug.LogError("找不到 SaveManager，请检查 MainMenu 场景中的绑定。");
        }
    }

    public void OpenGallery()
    {
        mainMenuPanel.SetActive(false);
        galleryPanel.SetActive(true);

        FindObjectOfType<GalleryManager>().RefreshEndingButtons();
    }

    public void CloseGallery()
    {
        galleryPanel.SetActive(false);
        mainMenuPanel.SetActive(true);
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    public void BackToMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }
}
