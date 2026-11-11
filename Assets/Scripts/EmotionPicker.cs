using System.Collections.Generic;
using DG.Tweening;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class EmotionButton
{
    public Button button;
    public Emotions emotion;
    public Sprite image;
    public string label;
}

public class EmotionPicker : MonoBehaviour
{
    [Header("Emotion Buttons")]
    public EmotionButton[] emotionButtons;

    [Header("Selected Button Animation")]
    [Tooltip("How big the selected button gets (1 = normal size).")]
    public float selectedScale = 1.15f;
    [Tooltip("Seconds to grow / shrink.")]
    public float scaleDuration = 0.25f;
    public Ease scaleEase = Ease.OutBack;
    [Tooltip("Max rotation left/right in degrees.")]
    public float wobbleAngle = 8f;
    [Tooltip("Seconds for one swing from left to right.")]
    public float wobbleDuration = 0.5f;
    public Ease wobbleEase = Ease.InOutSine;
    [Tooltip("Keep animating when Time.timeScale is 0.")]
    public bool ignoreTimeScale = true;

    [Header("Chosen Emotion")]
    public Image chosenEmotionImage;
    public TextMeshProUGUI chosenEmotionText;
    public string chosenTextFormat = "You'd Feel: {0}";
    public string noChoiceText = "You'd Feel: ";

    [Header("Confirm Panel")]
    public GameObject confirmPanel;
    public Image confirmEmotionImage;
    public TextMeshProUGUI confirmEmotionText;
    public string confirmTextFormat = "{0}";
    public bool upperCaseConfirmText = true;

    public AudioClip clickSFX;

    public event System.Action<Emotions> EmotionPicked;

    [ShowInInspector, ReadOnly]
    public Emotions PickedEmotion { get; private set; }

    private EmotionButton chosen;

    // Original scale/rotation of each button so we can return to it.
    private readonly Dictionary<Transform, Vector3> originalScales = new Dictionary<Transform, Vector3>();
    private readonly Dictionary<Transform, Vector3> originalRotations = new Dictionary<Transform, Vector3>();

    private void Awake()
    {
        if (emotionButtons != null)
        {
            foreach (EmotionButton entry in emotionButtons)
            {
                if (entry == null || entry.button == null) continue;

                Transform t = entry.button.transform;
                originalScales[t] = t.localScale;
                originalRotations[t] = t.localEulerAngles;

                EmotionButton thisEntry = entry;
                entry.button.onClick.AddListener(() => Choose(thisEntry));
            }
        }

        ClearChoice();
    }

    void Choose(EmotionButton entry)
    {
        if (chosen != entry)
        {
            Deselect(chosen);
            chosen = entry;
            Select(entry);
        }

        SetImage(chosenEmotionImage, GetSprite(entry), GetColor(entry));

        if (chosenEmotionText != null)
            chosenEmotionText.text = string.Format(chosenTextFormat, GetLabel(entry));
    }

    // ---------- DOTween animation ----------

    void Select(EmotionButton entry)
    {
        if (entry == null || entry.button == null) return;

        Transform t = entry.button.transform;
        if (!originalScales.TryGetValue(t, out Vector3 baseScale)) return;
        Vector3 baseRot = originalRotations[t];
        AudioManager.Instance.PlaySFX(clickSFX);
        // Draw on top of neighbours WITHOUT changing its place in the layout.
        SetOnTop(t, true);

        t.DOKill();

        t.DOScale(baseScale * selectedScale, scaleDuration)
            .SetEase(scaleEase)
            .SetUpdate(ignoreTimeScale)
            .SetLink(t.gameObject);

        Vector3 right = baseRot + new Vector3(0f, 0f, -wobbleAngle);
        Vector3 left = baseRot + new Vector3(0f, 0f, wobbleAngle);

        // Swing out to one side first (half a swing), then rock left <-> right forever.
        t.DOLocalRotate(right, wobbleDuration * 0.5f)
            .SetEase(Ease.OutSine)
            .SetUpdate(ignoreTimeScale)
            .SetLink(t.gameObject)
            .OnComplete(() =>
                t.DOLocalRotate(left, wobbleDuration)
                    .SetEase(wobbleEase)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetUpdate(ignoreTimeScale)
                    .SetLink(t.gameObject));
    }

    void Deselect(EmotionButton entry)
    {
        if (entry == null || entry.button == null) return;

        Transform t = entry.button.transform;
        if (!originalScales.TryGetValue(t, out Vector3 baseScale)) return;

        t.DOKill();

        t.DOScale(baseScale, scaleDuration)
            .SetEase(Ease.OutQuad)
            .SetUpdate(ignoreTimeScale)
            .SetLink(t.gameObject);

        t.DOLocalRotate(originalRotations[t], scaleDuration)
            .SetEase(Ease.OutQuad)
            .SetUpdate(ignoreTimeScale)
            .SetLink(t.gameObject)
            .OnComplete(() => SetOnTop(t, false));
    }

    // Uses a per-button Canvas with override sorting, so the button renders above
    // the others while its sibling order (and grid position) stays the same.
    void SetOnTop(Transform t, bool onTop)
    {
        if (t == null) return;

        Canvas c = t.GetComponent<Canvas>();
        if (c == null)
        {
            if (!onTop) return;
            c = t.gameObject.AddComponent<Canvas>();
            // A nested Canvas needs its own raycaster or the button stops being clickable.
            if (t.GetComponent<GraphicRaycaster>() == null)
                t.gameObject.AddComponent<GraphicRaycaster>();
        }

        if (onTop)
        {
            Canvas parent = t.parent != null ? t.parent.GetComponentInParent<Canvas>() : null;
            c.overrideSorting = true;
            c.sortingLayerID = parent != null ? parent.sortingLayerID : 0;
            c.sortingOrder = (parent != null ? parent.sortingOrder : 0) + 1;
        }
        else
        {
            c.overrideSorting = false;
        }
    }

    /// <summary>
    /// Stops every button tween and snaps all buttons back to normal size/rotation.
    /// Call this (e.g. from a UnityEvent) before showing your next panel.
    /// </summary>
    public void StopButtonAnimations()
    {
        ResetAllButtonsInstantly();
    }

    void ResetAllButtonsInstantly()
    {
        foreach (var pair in originalScales)
        {
            Transform t = pair.Key;
            if (t == null) continue;
            t.DOKill();
            t.localScale = pair.Value;
            t.localEulerAngles = originalRotations[t];
            SetOnTop(t, false);
        }
    }

    private void OnDisable()
    {
        // Snap buttons back to normal if the picker is hidden mid-animation.
        ResetAllButtonsInstantly();
        chosen = null;
    }

    private void OnDestroy()
    {
        foreach (var pair in originalScales)
            if (pair.Key != null) pair.Key.DOKill();
    }

    // ---------- Confirm panel ----------

    public void OpenConfirmPanel()
    {
        if (chosen == null)
        {
            Debug.LogWarning($"EmotionPicker ({name}): OpenConfirmPanel was called but no emotion is chosen on this EmotionPicker.", this);
            return;
        }

        if (confirmPanel == null)
        {
            Confirm();
            return;
        }

        confirmPanel.SetActive(true);

        SetImage(confirmEmotionImage, GetSprite(chosen), GetColor(chosen));

        if (confirmEmotionText != null)
        {
            string label = string.Format(confirmTextFormat, GetLabel(chosen));
            confirmEmotionText.text = upperCaseConfirmText ? label.ToUpper() : label;
            confirmEmotionText.ForceMeshUpdate();
            Debug.Log($"EmotionPicker: set \"{GetPath(confirmEmotionText.transform)}\" to \"{confirmEmotionText.text}\" (in scene: {confirmEmotionText.gameObject.scene.IsValid()}, active: {confirmEmotionText.gameObject.activeInHierarchy})", confirmEmotionText);
            StartCoroutine(CheckTextNextFrame(confirmEmotionText.text));
        }
        else
        {
            Debug.LogWarning($"EmotionPicker ({name}): Confirm Emotion Text is not assigned.", this);
        }

        if (confirmEmotionImage == null)
            Debug.LogWarning($"EmotionPicker ({name}): Confirm Emotion Image is not assigned.", this);
    }

    System.Collections.IEnumerator CheckTextNextFrame(string expected)
    {
        yield return null;
        yield return null;

        if (confirmEmotionText != null && confirmEmotionText.text != expected)
            Debug.LogWarning($"EmotionPicker: something changed \"{GetPath(confirmEmotionText.transform)}\" back to \"{confirmEmotionText.text}\" after it was set to \"{expected}\". Check the other components on that object.", confirmEmotionText);
    }

    static string GetPath(Transform t)
    {
        string path = t.name;
        while (t.parent != null)
        {
            t = t.parent;
            path = t.name + "/" + path;
        }
        return path;
    }

    public void CloseConfirmPanel()
    {
        if (confirmPanel != null)
            confirmPanel.SetActive(false);
    }

    public void Confirm()
    {
        if (chosen == null) return;

        Emotions emotion = chosen.emotion;
        PickedEmotion = emotion;

        // Moving on to the next panel: kill the tweens right away, no shrink-back animation.
        ClearChoiceInternal(true);
        EmotionPicked?.Invoke(emotion);
    }

    public void ClearChoice()
    {
        ClearChoiceInternal(false);
    }

    void ClearChoiceInternal(bool instant)
    {
        if (instant)
            ResetAllButtonsInstantly();
        else
            Deselect(chosen);

        chosen = null;

        SetImage(chosenEmotionImage, null, Color.white);

        if (chosenEmotionText != null)
            chosenEmotionText.text = noChoiceText;

        CloseConfirmPanel();
    }

    // ---------- Helpers ----------

    void SetImage(Image image, Sprite sprite, Color color)
    {
        if (image == null) return;

        image.sprite = sprite;
        image.color = color;
        image.preserveAspect = true;
        image.gameObject.SetActive(sprite != null);
    }

    Sprite GetSprite(EmotionButton entry)
    {
        if (entry.image != null) return entry.image;

        if (entry.button == null) return null;

        foreach (Image child in entry.button.GetComponentsInChildren<Image>(true))
            if (child != entry.button.image && child.sprite != null)
                return child.sprite;

        return entry.button.image != null ? entry.button.image.sprite : null;
    }

    Color GetColor(EmotionButton entry)
    {
        if (entry.button == null) return Color.white;

        //if (entry.color.HasValue) return entry.color.Value;

        foreach (Image child in entry.button.GetComponentsInChildren<Image>(true))
            if (child != entry.button.image && child.sprite != null)
                return child.color;

        return entry.button.image != null ? entry.button.image.color : Color.white;
    }

    string GetLabel(EmotionButton entry)
    {
        return string.IsNullOrWhiteSpace(entry.label) ? entry.emotion.ToString() : entry.label;
    }
}