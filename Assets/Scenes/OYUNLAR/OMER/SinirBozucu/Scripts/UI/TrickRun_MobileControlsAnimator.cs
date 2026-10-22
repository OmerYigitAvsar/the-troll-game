using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrickRun_MobileControlsAnimator : MonoBehaviour
{
    [Header("Dönüş Animasyonu")]
    [SerializeField, Min(0.05f)] private float rotationDuration = 0.32f;

    private TrickRun_PlayerController playerController;
    private readonly List<RectTransform> arrowButtons = new List<RectTransform>();
    private readonly List<float> normalAngles = new List<float>();
    private readonly List<CanvasGroup> jumpButtonGroups = new List<CanvasGroup>();
    private Coroutine rotationAnimation;
    private Coroutine jumpAnimation;

    private void Awake()
    {
        playerController = GetComponent<TrickRun_PlayerController>();
        FindArrowButtons();
    }

    private void OnEnable()
    {
        if (playerController != null)
        {
            playerController.ControlsReversedChanged += AnimateControls;
            playerController.JumpingEnabledChanged += AnimateJumpButton;
        }
    }

    private void OnDisable()
    {
        if (playerController != null)
        {
            playerController.ControlsReversedChanged -= AnimateControls;
            playerController.JumpingEnabledChanged -= AnimateJumpButton;
        }
    }

    private void FindArrowButtons()
    {
        arrowButtons.Clear();
        normalAngles.Clear();
        jumpButtonGroups.Clear();

        TrickRun_MobileControlButton[] buttons =
            FindObjectsByType<TrickRun_MobileControlButton>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (TrickRun_MobileControlButton button in buttons)
        {
            if (!button.IsDirectionalButton)
            {
                CanvasGroup group = button.GetComponent<CanvasGroup>();
                if (group == null) group = button.gameObject.AddComponent<CanvasGroup>();
                jumpButtonGroups.Add(group);
                continue;
            }

            RectTransform rect = button.transform as RectTransform;
            if (rect == null)
                continue;

            arrowButtons.Add(rect);
            normalAngles.Add(rect.localEulerAngles.z);
        }
    }

    private void AnimateJumpButton(bool enabled)
    {
        jumpButtonGroups.RemoveAll(group => group == null);
        if (jumpButtonGroups.Count == 0) FindArrowButtons();
        if (jumpAnimation != null) StopCoroutine(jumpAnimation);
        jumpAnimation = StartCoroutine(FadeJumpButtons(enabled ? 1f : 0.35f));
    }

    private IEnumerator FadeJumpButtons(float targetAlpha)
    {
        float elapsed = 0f;
        const float duration = 0.2f;
        float[] starts = new float[jumpButtonGroups.Count];
        for (int i = 0; i < starts.Length; i++)
        {
            CanvasGroup group = jumpButtonGroups[i];
            starts[i] = group != null ? group.alpha : targetAlpha;
        }
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            for (int i = 0; i < jumpButtonGroups.Count; i++)
                if (jumpButtonGroups[i] != null) jumpButtonGroups[i].alpha = Mathf.Lerp(starts[i], targetAlpha, t);
            yield return null;
        }
        for (int i = 0; i < jumpButtonGroups.Count; i++)
            if (jumpButtonGroups[i] != null) jumpButtonGroups[i].alpha = targetAlpha;
        jumpAnimation = null;
    }

    private void AnimateControls(bool reversed)
    {
        for (int i = arrowButtons.Count - 1; i >= 0; i--)
        {
            if (arrowButtons[i] != null)
                continue;

            arrowButtons.RemoveAt(i);
            normalAngles.RemoveAt(i);
        }

        if (arrowButtons.Count == 0)
            FindArrowButtons();

        if (rotationAnimation != null)
            StopCoroutine(rotationAnimation);

        rotationAnimation = StartCoroutine(RotateButtons(reversed));
    }

    private IEnumerator RotateButtons(bool reversed)
    {
        float[] startAngles = new float[arrowButtons.Count];
        for (int i = 0; i < arrowButtons.Count; i++)
        {
            RectTransform button = arrowButtons[i];
            startAngles[i] = button != null ? button.localEulerAngles.z : 0f;
        }

        float elapsed = 0f;
        while (elapsed < rotationDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / rotationDuration);
            t = t * t * (3f - 2f * t);

            for (int i = 0; i < arrowButtons.Count; i++)
            {
                if (arrowButtons[i] == null) continue;
                float target = normalAngles[i] + (reversed ? 180f : 0f);
                float angle = Mathf.LerpAngle(startAngles[i], target, t);
                arrowButtons[i].localRotation = Quaternion.Euler(0f, 0f, angle);
            }

            yield return null;
        }

        for (int i = 0; i < arrowButtons.Count; i++)
        {
            if (arrowButtons[i] == null) continue;
            float target = normalAngles[i] + (reversed ? 180f : 0f);
            arrowButtons[i].localRotation = Quaternion.Euler(0f, 0f, target);
        }

        rotationAnimation = null;
    }
}
