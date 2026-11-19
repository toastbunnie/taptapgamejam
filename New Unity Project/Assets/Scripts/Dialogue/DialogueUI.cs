using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Dialogue
{
    public class DialogueUI : MonoBehaviour
    {
        [Header("左右布局")]
        [SerializeField] private GameObject leftLayout;
        [SerializeField] private GameObject rightLayout;

        [Header("左侧布局组件")]
        [SerializeField] private Image leftAvatar;
        [SerializeField] private TMP_Text leftNameText;
        [SerializeField] private TMP_Text leftContentText;
        [SerializeField] private GameObject characterBorder;

        [Header("右侧布局组件")]
        [SerializeField] private Image rightAvatar;
        [SerializeField] private TMP_Text rightNameText;
        [SerializeField] private TMP_Text rightContentText;

        [Header("打字机效果")]
        [SerializeField, Min(1f)]
        private float charactersPerSecond = 35f;

        [Header("选项")]
        [SerializeField] private Transform optionContainer;
        [SerializeField] private Button optionButtonPrefab;

        private readonly List<GameObject> spawnedOptions = new();
        private Coroutine typingCoroutine;
        private string fullContent = "";
        private TMP_Text currentContentText;

        public bool IsTyping { get; private set; }

        public void ShowDialogue(
            string character,
            string content,
            string position,
            Sprite avatar)
        {
            StopTyping();
            ClearOptions();

            bool showRight = position == "右";

            leftLayout.SetActive(!showRight);
            rightLayout.SetActive(showRight);

            TMP_Text nameText = showRight
                ? rightNameText
                : leftNameText;

            if (characterBorder != null)
                characterBorder.SetActive(avatar != null);

            currentContentText = showRight
                ? rightContentText
                : leftContentText;

            Image currentAvatar = showRight
                ? rightAvatar
                : leftAvatar;

            nameText.text = character;
            fullContent = content ?? "";
            currentContentText.text = "";

            currentAvatar.sprite = avatar;
            currentAvatar.gameObject.SetActive(avatar != null);

            gameObject.SetActive(true);

            typingCoroutine = StartCoroutine(TypeText());
        }

        private IEnumerator TypeText()
        {
            IsTyping = true;
            currentContentText.text = "";

            foreach (char character in fullContent)
            {
                currentContentText.text += character;
                yield return new WaitForSecondsRealtime(
                    1f / charactersPerSecond);
            }

            currentContentText.text = fullContent;
            IsTyping = false;
            typingCoroutine = null;
        }

        public void CompleteTyping()
        {
            if (!IsTyping) return;

            StopTyping();

            if (currentContentText != null)
                currentContentText.text = fullContent;
        }

        private void StopTyping()
        {
            if (typingCoroutine != null)
            {
                StopCoroutine(typingCoroutine);
                typingCoroutine = null;
            }

            IsTyping = false;
        }

        public void ShowOptions(
            List<DialogueRecord> options,
            Action<DialogueRecord> onSelected)
        {
            StopTyping();
            ClearOptions();
            gameObject.SetActive(true);

            foreach (DialogueRecord option in options)
            {
                Button button = Instantiate(
                    optionButtonPrefab, optionContainer);

                TMP_Text buttonText =
                    button.GetComponentInChildren<TMP_Text>();

                if (buttonText != null)
                    buttonText.text = option.content;

                button.onClick.AddListener(
                    () => onSelected?.Invoke(option));

                spawnedOptions.Add(button.gameObject);
            }
        }

        public void ClearOptions()
        {
            foreach (GameObject option in spawnedOptions)
            {
                if (option != null)
                    Destroy(option);
            }

            spawnedOptions.Clear();
        }

        public void HideDialogue()
        {
            StopTyping();
            ClearOptions();
            gameObject.SetActive(false);
        }
    }
}
