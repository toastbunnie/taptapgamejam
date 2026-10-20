using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Game.Dialogue;

/// <summary>
/// Changes a scene background when a configured dialogue line begins.
/// Place this component beside DialogueManager and assign either a UI Image
/// or a SpriteRenderer as the background target.
/// </summary>
[RequireComponent(typeof(DialogueManager))]
public sealed class DialogueBackgroundSwitcher : MonoBehaviour
{
    [Serializable]
    private class BackgroundChange
    {
        [Tooltip("开始显示这句时切换；填写 Dialogue CSV 中的 ID")]
        public int dialogueId;

        public Sprite background;

        [Min(0f)]
        [Tooltip("淡出再淡入时长；设为 0 则立即切换")]
        public float fadeDuration = 0.4f;

        [Tooltip("背景切换完成后显示的场景物件")]
        public GameObject[] activateObjectsAfterChange;
    }

    [Header("背景目标（二选一）")]
    [SerializeField] private Image uiBackground;
    [SerializeField] private SpriteRenderer spriteBackground;

    [Header("台词触发的背景变化")]
    [SerializeField] private List<BackgroundChange> changes =
        new List<BackgroundChange>();

    private DialogueManager dialogueManager;
    private Coroutine changeCoroutine;
    private float originalAlpha = 1f;

    private void Awake()
    {
        dialogueManager = GetComponent<DialogueManager>();

        if (uiBackground != null)
            originalAlpha = uiBackground.color.a;
        else if (spriteBackground != null)
            originalAlpha = spriteBackground.color.a;
    }

    private void OnEnable()
    {
        if (dialogueManager == null)
            dialogueManager = GetComponent<DialogueManager>();

        if (dialogueManager != null)
            dialogueManager.DialogueRecordStarted += OnDialogueRecordStarted;
    }

    private void OnDisable()
    {
        if (dialogueManager != null)
            dialogueManager.DialogueRecordStarted -= OnDialogueRecordStarted;

        if (changeCoroutine != null)
        {
            StopCoroutine(changeCoroutine);
            changeCoroutine = null;
            SetAlpha(originalAlpha);
        }
    }

    private void OnDialogueRecordStarted(int dialogueId)
    {
        foreach (BackgroundChange change in changes)
        {
            if (change == null || change.dialogueId != dialogueId ||
                change.background == null)
                continue;

            ChangeBackground(change);
            return;
        }
    }

    private void ChangeBackground(BackgroundChange change)
    {
        if (uiBackground == null && spriteBackground == null)
        {
            Debug.LogWarning(
                $"[{nameof(DialogueBackgroundSwitcher)}] 尚未指定 UI Image 或 SpriteRenderer 背景。",
                this);
            return;
        }

        if (changeCoroutine != null)
            StopCoroutine(changeCoroutine);

        if (change.fadeDuration <= 0f)
        {
            SetSprite(change.background);
            SetAlpha(originalAlpha);
            ActivateObjects(change);
            changeCoroutine = null;
            return;
        }

        changeCoroutine = StartCoroutine(
            FadeAndChange(change));
    }

    private IEnumerator FadeAndChange(BackgroundChange change)
    {
        float duration = change.fadeDuration;
        float halfDuration = duration * 0.5f;
        float startAlpha = GetAlpha();
        yield return FadeAlpha(startAlpha, 0f, halfDuration);

        SetSprite(change.background);
        yield return FadeAlpha(0f, originalAlpha, halfDuration);

        ActivateObjects(change);
        changeCoroutine = null;
    }

    private static void ActivateObjects(BackgroundChange change)
    {
        if (change.activateObjectsAfterChange == null)
            return;

        foreach (GameObject target in change.activateObjectsAfterChange)
        {
            if (target != null)
                target.SetActive(true);
        }
    }

    private IEnumerator FadeAlpha(float from, float to, float duration)
    {
        if (duration <= 0f)
        {
            SetAlpha(to);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            SetAlpha(Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration)));
            yield return null;
        }

        SetAlpha(to);
    }

    private void SetSprite(Sprite sprite)
    {
        if (uiBackground != null)
            uiBackground.sprite = sprite;
        else if (spriteBackground != null)
            spriteBackground.sprite = sprite;
    }

    private float GetAlpha()
    {
        if (uiBackground != null)
            return uiBackground.color.a;
        if (spriteBackground != null)
            return spriteBackground.color.a;
        return 1f;
    }

    private void SetAlpha(float alpha)
    {
        if (uiBackground != null)
        {
            Color color = uiBackground.color;
            color.a = alpha;
            uiBackground.color = color;
        }

        if (spriteBackground != null)
        {
            Color color = spriteBackground.color;
            color.a = alpha;
            spriteBackground.color = color;
        }
    }
}
