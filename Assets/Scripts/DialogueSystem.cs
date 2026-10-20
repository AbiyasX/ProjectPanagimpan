using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using DG.Tweening;
using Sirenix.OdinInspector;

[Serializable]
public class DialogueCharacterSlot
{
    public SpeakerType speakerType;
    public string speakerName;
    public CharacterScript character;
}

public class DialogueSystem : MonoBehaviour
{
    public TextMeshProUGUI speakerUI;
    public TextMeshProUGUI dialogueTextUI;
    public GameObject dialogueBackGround;
    public GameObject dialoguePanel;
    public DialogueData[] dialogueData;
    public DialogueCharacterSlot[] characters;
    public float charsPerSecond = 40f;

    private Tween typeTween;
    private int textIndex = 0;
    private DialogueData currentDialogue;
    private Action onDialogueFinished;
    private bool isPlaying;

    private readonly Dictionary<string, string> textReplacements = new Dictionary<string, string>();

    public bool IsPlaying => isPlaying;

    void Start()
    {
        if (!isPlaying)
        {
            dialoguePanel.SetActive(false);
            dialogueBackGround.SetActive(false);
        }
    }

    private void Update()
    {
        if (!isPlaying) return;

        if (Input.GetMouseButtonDown(0))
        {
            OnTap();
        }
    }

    public void SetTextReplacement(string token, string value)
    {
        textReplacements[token] = value;
    }

    [Button("Play Dialogue")]
    public void PlayDialogue(int dialogueIndex)
    {
        if (dialogueIndex < 0 || dialogueIndex >= dialogueData.Length)
        {
            Debug.LogWarning($"Dialogue index {dialogueIndex} doesn't exist");
            return;
        }

        PlayDialogue(dialogueData[dialogueIndex]);
    }

    public void PlayDialogue(DialogueData dialogue, Action onFinished = null)
    {
        if (dialogue == null || dialogue.dialogue_Texts == null || dialogue.dialogue_Texts.Length == 0)
        {
            Debug.LogWarning("Dialogue is empty, skipping");
            onFinished?.Invoke();
            return;
        }

        currentDialogue = dialogue;
        onDialogueFinished = onFinished;
        textIndex = 0;
        isPlaying = true;

        dialoguePanel.SetActive(true);
        dialogueBackGround.SetActive(true);

        currentDialogue.startTriggerEvent?.Invoke();

        StartLine();
    }

    string ApplyReplacements(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;

        foreach (var pair in textReplacements)
        {
            text = text.Replace(pair.Key, pair.Value);
        }
        return text;
    }

    string GetSpeaker(DialogueText line)
    {
        switch (line.speakerType)
        {
            case SpeakerType.Player1: return "{player1}";
            case SpeakerType.Player2: return "{player2}";
            default: return line.dialogueSpeaker;
        }
    }

    void UpdateTalkingCharacters(DialogueText line)
    {
        if (characters == null) return;

        foreach (var slot in characters)
        {
            if (slot == null || slot.character == null) continue;
            slot.character.StartTalking(IsSpeaker(slot, line));
        }
    }

    bool IsSpeaker(DialogueCharacterSlot slot, DialogueText line)
    {
        if (line == null || line.speakerType != slot.speakerType) return false;

        if (slot.speakerType == SpeakerType.Player1 || slot.speakerType == SpeakerType.Player2)
            return true;

        return string.Equals(slot.speakerName, line.dialogueSpeaker, StringComparison.OrdinalIgnoreCase);
    }

    void StartLine()
    {
        var line = currentDialogue.dialogue_Texts[textIndex];

        speakerUI.text = ApplyReplacements(GetSpeaker(line));
        UpdateTalkingCharacters(line);

        typeTween?.Kill();
        dialogueTextUI.text = "";

        string text = ApplyReplacements(line.dialogueText);
        float duration = text.Length / charsPerSecond;
        typeTween = dialogueTextUI.DOText(text, duration)
            .SetEase(Ease.Linear)
            .SetLink(gameObject);

        line.OnTriggerEvent?.Invoke();
    }

    void OnTap()
    {
        if (typeTween != null && typeTween.IsActive() && typeTween.IsPlaying())
        {
            typeTween.Complete();
        }
        else
        {
            NextLine();
        }
    }

    void NextLine()
    {
        textIndex++;

        if (textIndex < currentDialogue.dialogue_Texts.Length)
            StartLine();
        else
            EndDialogue();
    }

    void EndDialogue()
    {
        currentDialogue.endTriggerEvent?.Invoke();
        typeTween?.Kill();
        isPlaying = false;

        UpdateTalkingCharacters(null);
        speakerUI.text = "";
        dialogueTextUI.text = "";
        dialoguePanel.SetActive(false);
        dialogueBackGround.SetActive(false);

        currentDialogue = null;
        textIndex = 0;

        Action finished = onDialogueFinished;
        onDialogueFinished = null;
        finished?.Invoke();
    }
}