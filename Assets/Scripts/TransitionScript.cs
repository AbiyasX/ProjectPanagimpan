using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

public class TransitionScript : MonoBehaviour
{

    public static TransitionScript Instance { get; private set; }

    public GameObject leftCloud;
    public GameObject rightCloud;
    public float holdTime = 0.3f;

    RectTransform leftRect;
    RectTransform rightRect;
    Vector2 leftClosedPos;
    Vector2 rightClosedPos;
    bool initialized;
    Sequence seq;

    void Awake()
    {
        Init();
        leftRect.anchoredPosition = LeftOpenPos();
        rightRect.anchoredPosition = RightOpenPos();
        leftCloud.SetActive(false);
        rightCloud.SetActive(false);
    }

    void OnDestroy()
    {
        seq?.Kill();
    }

    void Init()
    {
        if (initialized) return;
        initialized = true;

        leftRect = leftCloud.GetComponent<RectTransform>();
        rightRect = rightCloud.GetComponent<RectTransform>();

        leftClosedPos = leftRect.anchoredPosition;
        rightClosedPos = rightRect.anchoredPosition;
    }
    [Button]
    public void PlayTransitionAnimation(float duration, float delay = 0f)
    {
        PlayTransitionAnimation(duration, delay, null, null);
    }

    public void PlayTransitionAnimation(float duration, float delay, System.Action onCovered, System.Action onFinished = null)
    {
        Init();
        seq?.Kill();

        leftCloud.SetActive(true);
        rightCloud.SetActive(true);

        leftRect.anchoredPosition = LeftOpenPos();
        rightRect.anchoredPosition = RightOpenPos();

        seq = DOTween.Sequence()
            .AppendInterval(delay)
            .Append(leftRect.DOAnchorPos(leftClosedPos, duration).SetEase(Ease.OutCubic))
            .Join(rightRect.DOAnchorPos(rightClosedPos, duration).SetEase(Ease.OutCubic))
            .AppendCallback(() => onCovered?.Invoke())
            .AppendInterval(holdTime)
            .Append(leftRect.DOAnchorPos(LeftOpenPos(), duration).SetEase(Ease.InCubic))
            .Join(rightRect.DOAnchorPos(RightOpenPos(), duration).SetEase(Ease.InCubic))
            .OnComplete(() =>
            {
                leftCloud.SetActive(false);
                rightCloud.SetActive(false);
                onFinished?.Invoke();
            });
    }

    Vector2 LeftOpenPos()
    {
        return leftClosedPos + Vector2.left * (GetParentWidth(leftRect) * 0.5f + leftRect.rect.width);
    }

    Vector2 RightOpenPos()
    {
        return rightClosedPos + Vector2.right * (GetParentWidth(rightRect) * 0.5f + rightRect.rect.width);
    }

    float GetParentWidth(RectTransform rect)
    {
        RectTransform parent = rect.parent as RectTransform;
        return parent != null ? parent.rect.width : Screen.width;
    }
}