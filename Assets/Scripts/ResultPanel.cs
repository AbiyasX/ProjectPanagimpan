using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class EmotionImage
{
    public Emotions emotion;
    public Texture image;
}

public class ResultPanel : MonoBehaviour
{
    public GameObject resultpanel;

    public string playersChooseText;

    public RawImage p1_Emotion;
    public RawImage p2_Emotion;
    public TextMeshProUGUI P1_EmotionText;
    public TextMeshProUGUI P2_EmotionText;
    public TextMeshProUGUI p1_Name;
    public TextMeshProUGUI p2_Name;

    public TextMeshProUGUI resultText;
    public string CorrectText;
    public string icorrectText;

    public Button continueButton;

    public List<EmotionImage> emotionImages = new List<EmotionImage>();

    [Header("Animation")]
    public float firstRevealDelay = 0.3f;
    public float lastPlayerWait = 1f;
    public float suspenseDuration = 1.5f;
    [Range(0f, 1f)] public float silhouetteAlpha = 0.6f;
    [Min(1)] public int suspensePulses = 3;
    public float suspensePulseScale = 1.1f;
    public float resultTextDelay = 0.4f;
    public float slamStartScale = 2.5f;
    public float slamDuration = 0.25f;
    public float slamTiltAngle = 12f;
    public float slamTiltDuration = 0.4f;
    public float slamShakeStrength = 10f;
    public RectTransform shakeTarget;
    public float shakeStrength = 15f;

    public bool IsShowing { get; private set; }

    System.Action onContinue;
    Sequence revealSeq;
    Vector3 p1BaseScale;
    Vector3 p2BaseScale;
    Vector2 p1BasePos;
    Vector2 p2BasePos;
    bool initialized;

    void Awake()
    {
        Init();
    }

    void Init()
    {
        if (initialized) return;
        initialized = true;

        p1BaseScale = p1_Emotion.transform.localScale;
        p2BaseScale = p2_Emotion.transform.localScale;

        p1BasePos = p1_Emotion.rectTransform.anchoredPosition;
        p2BasePos = p2_Emotion.rectTransform.anchoredPosition;

        if (p1BaseScale == Vector3.zero) p1BaseScale = Vector3.one;
        if (p2BaseScale == Vector3.zero) p2BaseScale = Vector3.one;

        if (continueButton != null)
            continueButton.onClick.AddListener(Continue);
        else
            Debug.LogWarning("ResultPanel: assign the Continue Button");
    }

    void OnDestroy()
    {
        if (continueButton != null)
            continueButton.onClick.RemoveListener(Continue);
        revealSeq?.Kill();
    }

    public void ShowResult(string p1Name, Emotions p1Emotion, string p2Name, Emotions p2Emotion, bool isCorrect, System.Action onContinue)
    {
        Init();
        revealSeq?.Kill();
        this.onContinue = onContinue;

        p1_Name.text = p1Name + " " + playersChooseText;
        p2_Name.text = p2Name + " " + playersChooseText;

        SetEmotionImage(p1_Emotion, p1Emotion);
        SetEmotionImage(p2_Emotion, p2Emotion);

        SetEmotionText(P1_EmotionText, p1Emotion);
        SetEmotionText(P2_EmotionText, p2Emotion);

        resultText.text = isCorrect ? CorrectText : icorrectText;

        HideImage(p1_Emotion, p1BaseScale, p1BasePos);
        HideImage(p2_Emotion, p2BaseScale, p2BasePos);
        resultText.alpha = 0f;
        if (continueButton != null) continueButton.gameObject.SetActive(false);

        IsShowing = true;
        resultpanel.SetActive(true);

        revealSeq = DOTween.Sequence()
            .AppendInterval(firstRevealDelay)
            .Append(Slam(p1_Emotion, p1BaseScale))
            .Join(FadeInText(P1_EmotionText))
            .AppendInterval(lastPlayerWait)
            .Append(Suspense(p2_Emotion, p2BaseScale))
            .Append(Slam(p2_Emotion, p2BaseScale))
            .Join(FadeInText(P2_EmotionText))
            .AppendInterval(resultTextDelay)
            .Append(resultText.DOFade(1f, 0.4f))
            .AppendCallback(() =>
            {
                if (continueButton != null) continueButton.gameObject.SetActive(true);
            });
    }

    Sequence Slam(RawImage image, Vector3 baseScale)
    {
        Transform t = image.transform;

        Sequence s = DOTween.Sequence()
            .AppendCallback(() =>
            {
                t.localScale = baseScale * slamStartScale;
                t.localRotation = Quaternion.identity;
                image.color = new Color(1f, 1f, 1f, 0f);
            })
            .Append(t.DOScale(baseScale, slamDuration).SetEase(Ease.InQuad))
            .Join(image.DOFade(1f, slamDuration * 0.6f))
            .Append(t.DOPunchScale(baseScale * 0.15f, 0.3f, 8, 0.6f));

        float step = slamTiltDuration / 4f;
        Sequence tilt = DOTween.Sequence()
            .Append(t.DOLocalRotate(new Vector3(0f, 0f, slamTiltAngle), step).SetEase(Ease.OutQuad))
            .Append(t.DOLocalRotate(new Vector3(0f, 0f, -slamTiltAngle), step * 1.5f).SetEase(Ease.InOutSine))
            .Append(t.DOLocalRotate(new Vector3(0f, 0f, slamTiltAngle * 0.4f), step).SetEase(Ease.InOutSine))
            .Append(t.DOLocalRotate(Vector3.zero, step * 0.5f).SetEase(Ease.OutQuad));

        s.Insert(slamDuration, tilt);

        RectTransform rt = image.rectTransform;
        s.Insert(slamDuration, rt.DOShakeAnchorPos(slamTiltDuration, slamShakeStrength, 25, 90f, false, true));

        if (shakeTarget != null)
            s.Insert(slamDuration, shakeTarget.DOShakeAnchorPos(0.3f, shakeStrength, 20, 90f, false, true));

        return s;
    }

    Sequence Suspense(RawImage image, Vector3 baseScale)
    {
        Transform t = image.transform;
        int pulses = Mathf.Max(1, suspensePulses);
        float half = suspenseDuration / (pulses * 2f);

        return DOTween.Sequence()
            .AppendCallback(() =>
            {
                t.localScale = baseScale;
                t.localRotation = Quaternion.identity;
                image.color = new Color(0f, 0f, 0f, silhouetteAlpha * 0.4f);
            })
            .Append(t.DOScale(baseScale * suspensePulseScale, half)
                .SetEase(Ease.InOutSine)
                .SetLoops(pulses * 2, LoopType.Yoyo))
            .Join(image.DOFade(silhouetteAlpha, half)
                .SetEase(Ease.InOutSine)
                .SetLoops(pulses * 2, LoopType.Yoyo));
    }

    void HideImage(RawImage image, Vector3 baseScale, Vector2 basePos)
    {
        image.rectTransform.anchoredPosition = basePos;
        image.transform.localScale = baseScale;
        image.transform.localRotation = Quaternion.identity;
        image.color = new Color(1f, 1f, 1f, 0f);
    }

    public void Continue()
    {
        if (!IsShowing) return;

        HideResult();

        System.Action callback = onContinue;
        onContinue = null;
        callback?.Invoke();
    }

    public void HideResult()
    {
        revealSeq?.Kill();
        IsShowing = false;
        resultpanel.SetActive(false);
    }

    void SetEmotionText(TextMeshProUGUI target, Emotions emotion)
    {
        if (target == null) return;
        target.text = emotion.ToString();
        target.alpha = 0f;
    }

    Tween FadeInText(TextMeshProUGUI target)
    {
        if (target == null) return DOTween.Sequence();
        return target.DOFade(1f, slamDuration);
    }

    void SetEmotionImage(RawImage target, Emotions emotion)
    {
        Texture image = GetEmotionImage(emotion);
        target.texture = image;
        target.enabled = image != null;
    }

    Texture GetEmotionImage(Emotions emotion)
    {
        foreach (var e in emotionImages)
            if (e.emotion == emotion)
                return e.image;

        if (emotion != Emotions.None)
            Debug.LogWarning($"ResultPanel: no image assigned for emotion {emotion}");

        return null;
    }
}