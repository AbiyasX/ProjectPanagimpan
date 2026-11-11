using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Simple UI helper for the tutorial: dialogue box, Yes/No choice, popup, and a list of option buttons.
/// Every method is a coroutine, so TutorialManager can just "yield return" them one after another.
/// </summary>
public class TutorialUI : MonoBehaviour
{
    [Header("Dialogue Box")]
    public GameObject dialoguePanel;
    public GameObject speakerNameBox;      // optional: the little name plate (hidden for the narrator)
    public TMP_Text speakerText;
    public TMP_Text dialogueText;
    public Button continueButton;          // a transparent button covering the dialogue box (tap to continue)
    public float typeSpeed = 0.02f;        // seconds per letter

    [Header("Yes / No Choice")]
    public GameObject choicePanel;
    public TMP_Text choiceQuestionText;
    public Button yesButton;
    public Button noButton;

    [Header("Popup")]
    public GameObject popupPanel;
    public TMP_Text popupText;
    public Button popupOkButton;

    [Header("Option List (memories / emotions)")]
    public GameObject optionsPanel;
    public TMP_Text optionsTitleText;
    public Transform optionsContainer;     // object with a Vertical or Grid Layout Group
    public Button optionButtonPrefab;      // PREFAB ASSET (not a child of the container) with a TMP_Text child

    bool clicked;

    void Awake()
    {
        continueButton.onClick.AddListener(() => clicked = true);
        HideAll();
    }

    public void HideAll()
    {
        dialoguePanel.SetActive(false);
        choicePanel.SetActive(false);
        popupPanel.SetActive(false);
        optionsPanel.SetActive(false);
    }

    // ---------- Dialogue ----------
    public IEnumerator Say(string speaker, string line)
    {
        HideAll();
        dialoguePanel.SetActive(true);

        bool hasSpeaker = !string.IsNullOrEmpty(speaker);
        if (speakerNameBox != null) speakerNameBox.SetActive(hasSpeaker);
        speakerText.gameObject.SetActive(hasSpeaker);
        speakerText.text = speaker;

        // Typewriter effect
        dialogueText.text = line;
        dialogueText.maxVisibleCharacters = 0;
        dialogueText.ForceMeshUpdate();
        int total = dialogueText.textInfo.characterCount;

        clicked = false;
        for (int i = 0; i <= total; i++)
        {
            if (clicked) break;            // tap while typing = show the full line instantly
            dialogueText.maxVisibleCharacters = i;
            yield return new WaitForSeconds(typeSpeed);
        }
        dialogueText.maxVisibleCharacters = total;

        // Wait for a fresh tap to continue
        clicked = false;
        yield return new WaitUntil(() => clicked);
        clicked = false;
    }

    // ---------- Yes / No ----------
    public IEnumerator AskYesNo(string question, Action<bool> onAnswer)
    {
        HideAll();
        choicePanel.SetActive(true);
        choiceQuestionText.text = question;

        int answer = -1;
        yesButton.onClick.RemoveAllListeners();
        noButton.onClick.RemoveAllListeners();
        yesButton.onClick.AddListener(() => answer = 1);
        noButton.onClick.AddListener(() => answer = 0);

        yield return new WaitUntil(() => answer != -1);
        choicePanel.SetActive(false);
        onAnswer(answer == 1);
    }

    // ---------- Popup ----------
    public IEnumerator ShowPopup(string message)
    {
        HideAll();
        popupPanel.SetActive(true);
        popupText.text = message;

        bool ok = false;
        popupOkButton.onClick.RemoveAllListeners();
        popupOkButton.onClick.AddListener(() => ok = true);

        yield return new WaitUntil(() => ok);
        popupPanel.SetActive(false);
    }

    // ---------- Pick one option from a list ----------
    public IEnumerator PickOption(string title, IList<string> options, Action<int> onPicked)
    {
        HideAll();
        foreach (Transform child in optionsContainer) Destroy(child.gameObject);

        optionsPanel.SetActive(true);
        optionsTitleText.text = title;

        int picked = -1;
        for (int i = 0; i < options.Count; i++)
        {
            int index = i; // copy for the lambda
            Button b = Instantiate(optionButtonPrefab, optionsContainer);
            b.gameObject.SetActive(true);
            b.GetComponentInChildren<TMP_Text>().text = options[i];
            b.onClick.AddListener(() => picked = index);
        }

        yield return new WaitUntil(() => picked != -1);
        optionsPanel.SetActive(false);
        onPicked(picked);
    }
}
