using DG.Tweening;
using UnityEngine;

public class RotateTween : MonoBehaviour
{
    public enum Mode { BackAndForth, Spin }

    public Mode mode = Mode.BackAndForth;
    public Vector3 axis = Vector3.up;

    [Header("Back And Forth")]
    public float angle = 20f;
    public float swingDuration = 1f;
    public Ease swingEase = Ease.InOutSine;

    [Header("Spin")]
    public float spinDuration = 2f;
    public bool clockwise = true;

    [Header("Float")]
    public bool floating = true;
    public float floatHeight = 0.2f;
    public float floatDuration = 1.5f;

    public bool playOnEnable = true;

    Quaternion baseRotation;
    Vector3 basePosition;
    Tween rotateTween;
    Tween floatTween;

    void Awake()
    {
        baseRotation = transform.localRotation;
        basePosition = transform.localPosition;
    }

    void OnEnable()
    {
        if (playOnEnable) Play();
    }

    void OnDisable()
    {
        Stop();
    }

    public void Play()
    {
        Stop();
        PlayRotate();
        if (floating) PlayFloat();
    }

    void PlayRotate()
    {
        Vector3 dir = axis.normalized;

        if (mode == Mode.BackAndForth)
        {
            Quaternion left = baseRotation * Quaternion.AngleAxis(-angle, dir);
            Quaternion right = baseRotation * Quaternion.AngleAxis(angle, dir);

            rotateTween = transform.DOLocalRotateQuaternion(left, swingDuration * 0.5f)
                .SetEase(Ease.OutSine)
                .SetLink(gameObject)
                .OnComplete(() =>
                {
                    rotateTween = transform.DOLocalRotateQuaternion(right, swingDuration)
                        .SetEase(swingEase)
                        .SetLoops(-1, LoopType.Yoyo)
                        .SetLink(gameObject);
                });
        }
        else
        {
            float sign = clockwise ? -1f : 1f;

            rotateTween = transform.DOLocalRotate(dir * 360f * sign, spinDuration, RotateMode.LocalAxisAdd)
                .SetEase(Ease.Linear)
                .SetLoops(-1, LoopType.Incremental)
                .SetLink(gameObject);
        }
    }

    void PlayFloat()
    {
        Vector3 low = basePosition - Vector3.up * floatHeight * 0.5f;
        Vector3 high = basePosition + Vector3.up * floatHeight * 0.5f;

        floatTween = transform.DOLocalMove(high, floatDuration * 0.5f)
            .SetEase(Ease.OutSine)
            .SetLink(gameObject)
            .OnComplete(() =>
            {
                floatTween = transform.DOLocalMove(low, floatDuration)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetLink(gameObject);
            });
    }

    public void Stop()
    {
        rotateTween?.Kill();
        floatTween?.Kill();
        rotateTween = null;
        floatTween = null;
        transform.localRotation = baseRotation;
        transform.localPosition = basePosition;
    }
}