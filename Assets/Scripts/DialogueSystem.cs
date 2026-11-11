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

    [Header("Typing Sound")]
    public AudioSource typingAudioSource;
    public AudioClip[] typingClips;
    [Range(0f, 1f)] public float typingVolume = 0.6f;
    [Min(1)] public int playEveryNChars = 2;
    public float typingMinPitch = 0.95f;
    public float typingMaxPitch = 1.05f;

    private Tween typeTween;
    private int textIndex = 0;
    private DialogueData currentDialogue;
    private Action onDialogueFinished;
    private bool isPlaying;
    private int lastTypedLength;

    private readonly Dictionary<string, string> textReplacements = new Dictionary<string, string>();

    public bool IsPlaying => isPlaying;

    void Awake()
    {
        if (typingAudioSource == null)
        {
            typingAudioSource = gameObject.AddComponent<AudioSource>();
            typingAudioSource.playOnAwake = false;
        }
    }

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
        lastTypedLength = 0;

        // Set the FULL text once so TMP lays out the final lines/wrapping right away,
        // then reveal it by tweening maxVisibleCharacters (no reflow while typing).
        string text = ApplyReplacements(line.dialogueText);
        dialogueTextUI.text = text;
        dialogueTextUI.maxVisibleCharacters = 0;
        dialogueTextUI.ForceMeshUpdate();

        int total = dialogueTextUI.textInfo.characterCount;
        float duration = total / charsPerSecond;

        typeTween = DOTween.To(
                () => dialogueTextUI.maxVisibleCharacters,
                x => dialogueTextUI.maxVisibleCharacters = x,
                total,
                duration)
            .SetEase(Ease.Linear)
            .OnUpdate(PlayTypingSound)
            .SetLink(gameObject);

        line.OnTriggerEvent?.Invoke();
    }

    void PlayTypingSound()
    {
        int length = dialogueTextUI.maxVisibleCharacters;
        if (length <= lastTypedLength) return;

        int previous = lastTypedLength;
        lastTypedLength = length;

        var info = dialogueTextUI.textInfo;
        if (length - 1 >= info.characterCount) return;
        if (char.IsWhiteSpace(info.characterInfo[length - 1].character)) return;

        if (length / playEveryNChars == previous / playEveryNChars) return;
        if (typingClips == null || typingClips.Length == 0) return;

        AudioClip clip = typingClips[UnityEngine.Random.Range(0, typingClips.Length)];
        if (clip == null) return;

        typingAudioSource.pitch = UnityEngine.Random.Range(typingMinPitch, typingMaxPitch);
        typingAudioSource.PlayOneShot(clip, typingVolume);
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
        dialogueTextUI.maxVisibleCharacters = 99999;
        dialoguePanel.SetActive(false);
        dialogueBackGround.SetActive(false);

        currentDialogue = null;
        textIndex = 0;

        Action finished = onDialogueFinished;
        onDialogueFinished = null;
        finished?.Invoke();
    }
}