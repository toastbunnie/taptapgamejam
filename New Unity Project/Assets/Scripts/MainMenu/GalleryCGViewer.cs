using UnityEngine;
using UnityEngine.UI;

public class GalleryCGViewer : MonoBehaviour
{
    public GameObject cgPanel;
    public Image cgImage;

    public bool ShowEnding(EndingData ending)
    {
        if (ending == null ||
            !EndingUnlockStore.IsUnlocked(ending.endingFlag))
            return false;

        cgImage.sprite = ending.endingCG;
        cgPanel.SetActive(true);
        return true;
    }

    public void CloseCG()
    {
        cgPanel.SetActive(false);
    }
}
