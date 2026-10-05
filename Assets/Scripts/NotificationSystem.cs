using TMPro;
using UnityEngine;
using DG.Tweening;
using Sirenix.OdinInspector;

public class NotificationSystem : MonoBehaviour
{

    [SerializeField] GameObject textMessageUI;

    public static NotificationSystem Instance;

    private Transform originalParent;
    private Vector3 startPos;

    void Start()
    {
        originalParent = textMessageUI.transform.parent;
        startPos = textMessageUI.transform.localPosition;
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    [Button("Show Message")]
    public void showMessage(string message, Color color = default(Color), GameObject displayObject = null)
    {
        textMessageUI.SetActive(true);

        TextMeshProUGUI textMessage = textMessageUI.GetComponentInChildren<TextMeshProUGUI>();

        textMessage.DOKill();
        textMessageUI.transform.DOKill();

        textMessage.text = message;
        textMessage.color = new Color(color.r, color.g, color.b, 1f);
        textMessage.alpha = 1f;

        textMessage.DOFade(0f, 2f).SetDelay(1f);

        textMessageUI.transform.DOLocalMoveY(50f, 2f).SetRelative(true);

        if (displayObject != null)
        {
            textMessageUI.transform.SetParent(displayObject.transform, false);
            textMessageUI.transform.localPosition = Vector3.zero;
        }
        else
        {
            textMessageUI.transform.SetParent(originalParent, false);
            textMessageUI.transform.localPosition = startPos;
        }

    }
}
