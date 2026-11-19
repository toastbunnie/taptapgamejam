using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    [RequireComponent(typeof(Button))]
    public class SettingsPanelOpenButton : MonoBehaviour
    {
        private Button _button;
        private SettingsPanel _settingsPanel;

        private void Awake()
        {
            _button = GetComponent<Button>();
        }

        private void OnEnable()
        {
            if (_button == null)
                _button = GetComponent<Button>();

            if (_button != null)
                _button.onClick.AddListener(OpenSettings);
        }

        private void OnDisable()
        {
            if (_button != null)
                _button.onClick.RemoveListener(OpenSettings);
        }

        private void OpenSettings()
        {
            if (_settingsPanel == null && transform.parent != null)
                _settingsPanel = transform.parent.GetComponentInChildren<SettingsPanel>(true);

            if (_settingsPanel == null)
                _settingsPanel = FindObjectOfType<SettingsPanel>(true);

            if (_settingsPanel != null)
            {
                _settingsPanel.Show();
                return;
            }

            Debug.LogWarning("[SettingsPanelOpenButton] 找不到 SettingsPanel，请确认 SettingsUI 中包含设置面板预制体。", this);
        }
    }
}
