using UnityEngine;
using TMPro;

public class EndingButton : MonoBehaviour
{
    [Header("结局信息")]
    public EndingData endingData;

    [Header("Gallery")]
    public TMP_Text buttonText;
    public GalleryCGViewer cgViewer;

    void Start()
    {
        RefreshDisplay();
    }

    public void RefreshDisplay()
    {
        if (endingData == null)
        {
            Debug.LogError("EndingButton没有绑定EndingData：" + gameObject.name);
            return;
        }

        FlagManager flagManager = FindObjectOfType<FlagManager>();

        if (flagManager == null)
        {
            Debug.LogError("找不到 FlagManager！");
            return;
        }

        bool unlocked = EndingUnlockStore.IsUnlocked(endingData.endingFlag) ||
                        flagManager.GetFlag(endingData.endingFlag);

        Debug.Log("Gallery检查：" + endingData.endingFlag + " = " + unlocked);

        if (unlocked)
        {
            buttonText.text = endingData.endingTitle + "：" + endingData.endingName;
        }
        else
        {
            buttonText.text = endingData.endingTitle + "：？？？";
        }
    }

    public void OnClick()
    {
        if (endingData == null)
            return;

        bool unlocked = EndingUnlockStore.IsUnlocked(endingData.endingFlag);
        FlagManager flagManager = FindObjectOfType<FlagManager>();
        if (!unlocked && flagManager != null)
            unlocked = flagManager.GetFlag(endingData.endingFlag);

        if (!unlocked)
            return;

        cgViewer.ShowCG(endingData.endingCG);
    }
}
