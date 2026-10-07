using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
}