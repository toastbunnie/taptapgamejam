using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class JournalClueClickHandler : MonoBehaviour, IPointerClickHandler
{
    public JournalClueView clueView;
    public JournalUI journalUI;
    public Button button;

    private int lastHandledClickFrame = -1;

    private void Awake()
    {
        if (button == null)
            button = GetComponent<Button>();
    }

    private void OnEnable()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(HandleClick);
            button.onClick.AddListener(HandleClick);
        }
    }

    private void OnDisable()
    {
        if (button != null)
            button.onClick.RemoveListener(HandleClick);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        HandleClick();
    }

    private void HandleClick()
    {
        // The Button callback and pointer callback can run for the same tap.
        if (lastHandledClickFrame == Time.frameCount)
            return;

        lastHandledClickFrame = Time.frameCount;

        if (journalUI == null || clueView == null)
            return;

        if (clueView.visualRoot != null &&
            !clueView.visualRoot.activeInHierarchy)
            return;

        journalUI.ToggleClueSelection(clueView);
    }
}
