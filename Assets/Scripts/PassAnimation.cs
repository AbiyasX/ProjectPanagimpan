using DG.Tweening;
using UnityEngine;

public class PassAnimation : MonoBehaviour
{
    public GameObject GameObjectToAnimate;

    [Header("Pass Animation")]
    public float scaleDuration = 0.5f;
    public float scaleAmount = 1f;

    public void PlayPassAnimation()
    {
        Transform t = GameObjectToAnimate.transform;
        t.DOKill();
        t.localScale = Vector3.zero;
        t.DOScale(scaleAmount, scaleDuration).SetEase(Ease.OutBack);
    }

    public void ResetScale()
    {
        GameObjectToAnimate.transform.DOKill();
        GameObjectToAnimate.transform.localScale = Vector3.zero;
    }
}
