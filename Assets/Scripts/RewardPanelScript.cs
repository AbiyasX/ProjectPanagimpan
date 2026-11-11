using DG.Tweening;
using Sirenix.OdinInspector;
using TMPro;
using UIImage = UnityEngine.UI.RawImage;
using UIButton = UnityEngine.UI.Button;
using UnityEngine;

public class RewardPanelScript : MonoBehaviour
{
    [Header("References")]
    public GameObject rewardPanel;
    public UIImage rewardImage;
    public UIButton tapButton;
    public TextMeshProUGUI rewardText;
    public TextMeshProUGUI TapText;
    public GameObject particle;

    [Header("Settings")]
    public float imageSize = 1f;
    public int tapsToReveal = 3;
    public float shakeDuration = 0.4f;
    public float shakeAngle = 15f;
    public float revealPopScale = 1.25f;
    public float autoHideDelay = 3f;

    [Header("Tap Text Pulse")]
    public float pulseMinAlpha = 0.2f;
    public float pulseDuration = 0.6f;

    public AudioClip tapSFX;

    public AudioClip rewardSFX;

    public bool IsShowing { get; private set; }
    public event System.Action Closed;

    Sequence ShowRowardTween;
    Tween tapPulseTween;
    Transform imageTf;
    int tapCount;
    bool isAnimating;
    bool revealed;

    void Awake()
    {
        imageTf = rewardImage.transform;
        tapButton.onClick.AddListener(OnTap);
    }

    void OnDestroy()
    {
        tapButton.onClick.RemoveListener(OnTap);
        ShowRowardTween?.Kill();
        tapPulseTween?.Kill();
    }

    [Button]
    public void ShowReward(Texture rewardSprite, string rewardDescription)
    {
        ShowRowardTween?.Kill();
        tapPulseTween?.Kill();

        tapCount = 0;
        revealed = false;
        isAnimating = true;
        IsShowing = true;

        rewardImage.texture = rewardSprite;
        rewardImage.color = Color.black;
        imageTf.localScale = Vector3.zero;
        imageTf.localRotation = Quaternion.identity;

        rewardText.text = rewardDescription;
        rewardText.alpha = 0f;

        TapText.alpha = 0f;

        particle.SetActive(false);
        rewardPanel.SetActive(true);

        ShowRowardTween = DOTween.Sequence()
            .Append(imageTf.DOScale(Vector3.one * imageSize, 0.5f).SetEase(Ease.OutBack))
            .Append(TapText.DOFade(1f, 0.3f))
            .OnComplete(() =>
            {
                isAnimating = false;
                StartTapPulse();
            });
    }

    void StartTapPulse()
    {
        tapPulseTween?.Kill();
        TapText.alpha = 1f;
        tapPulseTween = TapText.DOFade(pulseMinAlpha, pulseDuration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);
    }

    void OnTap()
    {
        if (isAnimating) return;

        if (revealed)
        {
            if (autoHideDelay < 0f) Hide();
            return;
        }

        tapCount++;
        AudioManager.Instance.PlaySFX(tapSFX);
        isAnimating = true;

        if (tapCount < tapsToReveal) Shake();
        else Reveal();
    }

    void Shake()
    {
        float a = shakeAngle * (1f + 0.3f * (tapCount - 1));
        float step = shakeDuration / 4f;

        ShowRowardTween = DOTween.Sequence()
            .Append(Tilt(a, step))
            .Append(Tilt(-a, step))
            .Append(Tilt(a * 0.5f, step))
            .Append(Tilt(0f, step))
            .Insert(0f, imageTf.DOPunchScale(Vector3.one * imageSize * 0.1f, shakeDuration, 6, 0.5f))
            .OnComplete(() => isAnimating = false);
    }

    void Reveal()
    {
        float a = shakeAngle * 1.6f;
        float step = shakeDuration / 6f;

        tapPulseTween?.Kill();
        ShowRowardTween = DOTween.Sequence()
            .Append(Tilt(a, step))
            .Append(Tilt(-a, step))
            .Append(Tilt(a, step))
            .Append(Tilt(-a, step))
            .Append(Tilt(0f, step))
            .Insert(0f, TapText.DOFade(0f, 0.3f))
            .Append(imageTf.DOScale(Vector3.one * imageSize * 0.85f, 0.12f).SetEase(Ease.InQuad))
            .AppendCallback(() => particle.SetActive(true))
            .Append(imageTf.DOScale(Vector3.one * imageSize * revealPopScale, 0.3f).SetEase(Ease.OutBack))
            .Join(rewardImage.DOColor(Color.white, 0.2f))
            .JoinCallback(() => AudioManager.Instance.PlaySFX(rewardSFX))
            .Append(imageTf.DOScale(Vector3.one * imageSize, 0.2f).SetEase(Ease.OutQuad))
            .Append(rewardText.DOFade(1f, 0.3f))
            .OnComplete(() =>
            {
                revealed = true;
                isAnimating = false;
                if (autoHideDelay >= 0f) Hide(autoHideDelay);
            });
    }

    void Hide(float delay = 0f)
    {
        isAnimating = true;

        ShowRowardTween = DOTween.Sequence()
            .AppendInterval(delay)
            .Append(imageTf.DOScale(Vector3.zero, 0.5f).SetEase(Ease.InBack))
            .Join(rewardText.DOFade(0f, 0.5f))
            .AppendCallback(() => particle.SetActive(false))
            .OnComplete(() =>
            {
                isAnimating = false;
                rewardPanel.SetActive(false);
                IsShowing = false;
                Closed?.Invoke();
            });
    }

    Tween Tilt(float z, float duration) =>
        imageTf.DOLocalRotate(new Vector3(0f, 0f, z), duration).SetEase(Ease.InOutSine);
}