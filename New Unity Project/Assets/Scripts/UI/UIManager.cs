using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Game.Sample;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [System.Serializable]
    public class UIPanel
    {
        public string panelName;
        public GameObject panelObject;
    }

    [Header("主面板")]
    [SerializeField] private GameObject backpackPanel;
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private GameObject journalPanel;

    public UIPanel[] panels;
    [SerializeField] private TMPro.TMP_InputField passwordInput;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void SubmitPassword(PasswordBox puzzle)
    {
        if (passwordInput == null || puzzle == null)
        {
            Debug.LogWarning("UIManager：密码输入框或密码谜题没有绑定！");
            return;
        }

        puzzle.OnCodeSubmit(passwordInput.text);
    }
}

/// <summary>
/// Keeps the exploration panels mutually exclusive without ending or advancing dialogue.
/// </summary>
public static class UIPanelExclusivity
{
    public enum Panel
    {
        Backpack,
        Journal,
        Dialogue,
        Settings
    }

    public static bool TryOpen(Panel panel)
    {
        Game.Dialogue.DialogueManager dialogueManager =
            Object.FindObjectOfType<Game.Dialogue.DialogueManager>();

        if (panel != Panel.Dialogue &&
            dialogueManager != null &&
            dialogueManager.IsDialogueActive)
        {
            return false;
        }

        if (panel != Panel.Backpack)
        {
            Game.UI.BackpackPanelToggle backpack =
                Object.FindObjectOfType<Game.UI.BackpackPanelToggle>(true);
            if (backpack != null)
                backpack.Close();
        }

        if (panel != Panel.Journal)
        {
            JournalUI journal = Object.FindObjectOfType<JournalUI>(true);
            if (journal != null)
                journal.CloseJournal();
        }

        if (panel != Panel.Settings)
        {
            Game.UI.SettingsPanel settings =
                Object.FindObjectOfType<Game.UI.SettingsPanel>(true);
            if (settings != null)
                settings.Hide(immediate: true);
        }

        return true;
    }
}
