using TMPro;
using UnityEngine;
using DG.Tweening;
using Sirenix.OdinInspector;

public class NotificationSystem : MonoBehaviour
{

    [SerializeField] GameObject textMessageUI;

    [Header("Correct / Wrong")]
    public Color correctColor = Color.green;
    public Color wrongColor = Color.red;

    public static NotificationSystem Instance;

    private TextMeshProUGUI textMessage;
    private Transform originalParent;
    private Vector3 startPos;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Cached in Awake so messages sent before Start still work
        if (textMessageUI == null)
        {
            Debug.LogWarning("NotificationSystem: Text Message UI is not assigned");
            return;
        }

        // true = also finds the text if it starts inactive
        textMessage = textMessageUI.GetComponentInChildren<TextMeshProUGUI>(true);
        if (textMessage == null)
            Debug.LogWarning("NotificationSystem: no TextMeshProUGUI under Text Message UI");

        originalParent = textMessageUI.transform.parent;
        startPos = textMessageUI.transform.localPosition;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void ShowCorrect(string message)
    {
        showMessage(message, correctColor);
    }

    public void ShowWrong(string message)
    {
        showMessage(message, wrongColor);
    }

    [Button("Show Message")]
    public void showMessage(string message, Color color = default(Color), GameObject displayObject = null)
    {
        if (textMessageUI == null || textMessage == null)
        {
            Debug.LogWarning($"NotificationSystem can't show message (missing UI): {message}");
            return;
        }

        textMessageUI.SetActive(true);
        textMessage.gameObject.SetActive(true);

        textMessage.DOKill();
        textMessageUI.transform.DOKill();

        textMessage.text = message;
        textMessage.color = new Color(color.r, color.g, color.b, 1f);
        textMessage.alpha = 1f;

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

        textMessage.DOFade(0f, 2f).SetDelay(1f);
        textMessageUI.transform.DOLocalMoveY(50f, 2f).SetRelative(true);
    }
}
