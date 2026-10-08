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

    public UIPanel[] panels;
    [SerializeField] private TMPro.TMP_InputField passwordInput;

    private void Awake()
    {
        Instance = this;
    }

    public void OpenPanel(string panelName)
    {
        foreach (UIPanel panel in panels)
        {
            if (panel.panelName == panelName)
            {
                panel.panelObject.SetActive(true);
                return;
            }
        }

        Debug.LogWarning("找不到 UI 面板：" + panelName);
    }

    public void ClosePanel(string panelName)
    {
        foreach (UIPanel panel in panels)
        {
            if (panel.panelName == panelName)
            {
                panel.panelObject.SetActive(false);
                return;
            }
        }

        Debug.LogWarning("找不到 UI 面板：" + panelName);
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