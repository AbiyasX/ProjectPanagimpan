using DG.Tweening;
using Sirenix.OdinInspector;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;

public class CharacterScript : MonoBehaviour
{
    [SerializeField] GameObject textBubble;

    [SerializeField] bool isTalking;

    private Tween scaleTween;
    private Vector3 baseScale;

    private void Awake()
    {
        baseScale = transform.localScale;
    }

    [Button("Invoke Talk")]
    public void StartTalking(bool talking)
    {
        isTalking = talking;
        textBubble.SetActive(isTalking);

        scaleTween?.Kill(); 
        scaleTween = transform.DOScale(baseScale * (isTalking ? 1.2f : 1f), 0.5f).SetEase(Ease.InOutElastic);
    }

}
