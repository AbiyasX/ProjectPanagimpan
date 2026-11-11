using System;
using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The tutorial's pop-up box: a message with one button (OK) or two buttons (Yes / No).
///
/// The UI lives in the Tutorial scene (the "Popup" object under "Tutorial").
/// This script only fills in the text and turns that object on and off.
/// Move, resize, recolor or re-skin the objects in the scene however you like,
/// just keep the fields below pointing at them.
/// </summary>
public class TutorialPopup : MonoBehaviour
{
    [Header("Objects in the scene")]
    [Tooltip("The whole pop-up. Turned on when a pop-up shows, off when it closes. Keep it OFF in the scene.")]
    public GameObject panel;
    [Tooltip("Optional. The box that does a little 'pop' when it opens.")]
    public RectTransform box;
    public TextMeshProUGUI messageText;

    [Header("First button (OK / Yes)")]
    public Button yesButton;
    public TextMeshProUGUI yesLabel;

    [Header("Second button (No) - hidden when there is only one choice")]
    public Button noButton;
    public TextMeshProUGUI noLabel;

    [Header("Settings")]
    [Tooltip("Seconds before the buttons can be pressed, so a fast tap on the dialogue doesn't press them by accident.")]
    public float buttonDelay = 0.35f;
    [Tooltip("When a pop-up has only one button, slide it to the middle (halfway between where the two buttons sit in the scene).")]
    public bool centerSingleButton = true;

    int answer = -1;
    bool listening;
    bool homeSaved;
    Vector2 yesHome;
    Vector2 noHome;

    /// <summary>False if something isn't assigned in the Inspector.</summary>
    public bool IsSetUp => panel != null && messageText != null && yesButton != null && yesLabel != null && noButton != null && noLabel != null;

    void Awake()
    {
        if (panel != null) panel.SetActive(false);
    }

    /// <summary>Shows a message with one button and waits until it is pressed.</summary>
    public IEnumerator Show(string message, string okLabel = "OK")
    {
        yield return Open(message, okLabel, null);
    }

    /// <summary>Shows a question with two buttons and waits for the answer (true = first button).</summary>
    public IEnumerator Ask(string message, string yesText, string noText, Action<bool> onAnswer)
    {
        yield return Open(message, yesText, noText);
        onAnswer?.Invoke(answer == 1);
    }

    IEnumerator Open(string message, string yesText, string noText)
    {
        if (!listening)
        {
            listening = true;
            yesButton.onClick.AddListener(() => Pick(1));
            noButton.onClick.AddListener(() => Pick(0));
        }

        messageText.text = message;
        yesLabel.text = yesText;
        noButton.gameObject.SetActive(noText != null);
        if (noText != null) noLabel.text = noText;
        PlaceFirstButton(noText == null);

        answer = -1;
        yesButton.interactable = false;
        noButton.interactable = false;
        panel.SetActive(true);

        if (box != null)
        {
            box.DOKill();
            box.localScale = Vector3.one * 0.8f;
            box.DOScale(1f, 0.25f).SetEase(Ease.OutBack).SetUpdate(true).SetLink(box.gameObject);
        }

        yield return new WaitForSecondsRealtime(buttonDelay);
        yesButton.interactable = true;
        noButton.interactable = true;

        yield return new WaitUntil(() => answer != -1);
        panel.SetActive(false);
    }

    // With two buttons, the first one sits where you placed it in the scene.
    // With one button, it moves halfway between the two spots so it looks centered.
    void PlaceFirstButton(bool alone)
    {
        RectTransform yesRect = yesButton.transform as RectTransform;
        RectTransform noRect = noButton.transform as RectTransform;
        if (yesRect == null || noRect == null) return;

        if (!homeSaved)
        {
            homeSaved = true;
            yesHome = yesRect.anchoredPosition;
            noHome = noRect.anchoredPosition;
        }

        // Only works when both buttons are positioned the same way (same parent and anchors).
        bool comparable = yesRect.parent == noRect.parent
            && yesRect.anchorMin == noRect.anchorMin
            && yesRect.anchorMax == noRect.anchorMax;

        if (alone && centerSingleButton && comparable)
            yesRect.anchoredPosition = (yesHome + noHome) * 0.5f;
        else
            yesRect.anchoredPosition = yesHome;
    }

    void Pick(int value)
    {
        answer = value;
        if (AudioManager.Instance != null && AudioManager.Instance.clickSFX != null)
            AudioManager.Instance.playClickSFX();
    }
}
