using UnityEngine;
using TMPro;
using UnityEngine.UI;

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

        // Gallery access is based only on the persistent unlock written after
        // the ending presentation completes.
        bool unlocked = EndingUnlockStore.IsUnlocked(endingData.endingFlag);
        Button button = GetComponent<Button>();
        if (button != null)
            button.interactable = unlocked;

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

        if (!EndingUnlockStore.IsUnlocked(endingData.endingFlag))
        {
            if (cgViewer != null)
                cgViewer.CloseCG();
            return;
        }

        if (cgViewer != null)
            cgViewer.ShowEnding(endingData);
    }
}
