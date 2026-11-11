using UnityEngine;
using TMPro;
using DG.Tweening;
using Sirenix.OdinInspector;

public class RoundSystem : MonoBehaviour
{
    public static RoundSystem Instance { get; private set; }
    public AudioClip roundStartSFX;
    [SerializeField] private GameObject roundUI;
    [SerializeField] private TextMeshProUGUI roundText;

    [SerializeField] private GameObject UIPop;

    [SerializeField] private string roundFormat = "Round {0}";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private Sequence roundSeq;
    [Button]
    public void OpenRoundScreen(int round)
    {
        if (roundText != null)
            roundText.text = string.Format(roundFormat, round);

        roundUI.SetActive(true);

        Transform t = UIPop.transform;

        roundSeq?.Kill();         
        t.localScale = Vector3.zero;

        roundSeq = DOTween.Sequence()
            .JoinCallback(() => AudioManager.Instance.PlaySFX(roundStartSFX))
            .Append(t.DOScale(5f, 0.5f).SetEase(Ease.OutQuad))      
            .Append(t.DOShakeScale(0.4f, 0.1f, 10, 90))             
            .AppendInterval(1f)                                    
            .Append(t.DOScale(0f, 0.4f).SetEase(Ease.InBack))      
            .OnComplete(() => roundUI.SetActive(false));
    }

    public void CloseRoundScreen()
    {
        roundUI.SetActive(false);
    }
}