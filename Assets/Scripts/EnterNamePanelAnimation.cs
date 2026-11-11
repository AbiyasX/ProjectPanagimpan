using DG.Tweening;
using UnityEngine;

public class EnterNamePanelAnimation : MonoBehaviour
{

    public NameSetter NSetter;

    public GameObject transitionObject;

    public float dropDuration = 0.8f;
    public float okBounceScale = 0.15f;
    public float okBounceDuration = 0.25f;
    public float exitDuration = 0.5f;
    public float offscreenPadding = 50f;

    RectTransform rect;
    Vector2 centerPos;
    Vector3 baseScale;
    bool initialized;
    bool isExiting;
    Sequence seq;

    void Awake()
    {
        Init();
        rect.anchoredPosition = AbovePos();
    }

    void Start()
    {
        PlayEnter();
    }

    void OnDestroy()
    {
        seq?.Kill();
    }

    void Init()
    {
        if (initialized) return;
        initialized = true;

        rect = transitionObject.GetComponent<RectTransform>();
        centerPos = rect.anchoredPosition;
        baseScale = rect.localScale;
    }

    public void PlayEnter()
    {
        Init();
        seq?.Kill();
        isExiting = false;

        rect.localScale = baseScale;
        rect.anchoredPosition = AbovePos();
        transitionObject.SetActive(true);

        seq = DOTween.Sequence()
            .Append(rect.DOAnchorPos(centerPos, dropDuration).SetEase(Ease.OutBounce));
    }

    public void PlayExit()
    {
        if(!NSetter.IsNameSet()) return;
        Init();
        if (isExiting) return;
        isExiting = true;

        seq?.Kill();
        rect.anchoredPosition = centerPos;

        seq = DOTween.Sequence()
            .Append(rect.DOPunchScale(baseScale * okBounceScale, okBounceDuration, 8, 0.6f))
            .Append(rect.DOAnchorPos(LeftPos(), exitDuration).SetEase(Ease.InBack))
            .OnComplete(() =>
            {
                rect.localScale = baseScale;
                isExiting = false;
            });
    }

    Vector2 AbovePos()
    {
        return centerPos + Vector2.up * (GetParentSize().y * 0.5f + rect.rect.height * 0.5f + offscreenPadding);
    }

    Vector2 LeftPos()
    {
        return centerPos + Vector2.left * (GetParentSize().x * 0.5f + rect.rect.width * 0.5f + offscreenPadding);
    }

    Vector2 GetParentSize()
    {
        RectTransform parent = rect.parent as RectTransform;
        return parent != null ? parent.rect.size : new Vector2(Screen.width, Screen.height);
    }
}