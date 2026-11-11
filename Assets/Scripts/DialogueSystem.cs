using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using Sirenix.OdinInspector;

[Serializable]
public class DialogueCharacterSlot
{
    public SpeakerType speakerType;
    public string speakerName;
    public CharacterScript character;

    [Tooltip("Optional. If empty, the first Image / SpriteRenderer found on the character (or its children) is used.")]
    public Image targetImage;
    [Tooltip("Optional. If empty, the first Image / SpriteRenderer found on the character (or its children) is used.")]
    public SpriteRenderer targetRenderer;
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

    // Uses the Player 1 / Player 2 sprites (and positions, if overridden) from the area's AreasData.
    [Button("Test Set Area")]
    public void SetArea(AreasData area)
    {
        if (area == null) return;
        SetPlayerSprites(area.Player1Sprite, area.Player2Sprite);

        if (area.overridePositions)
        {
            SetCharacterPosition(SpeakerType.Player1, area.player1Position);
            SetCharacterPosition(SpeakerType.Player2, area.player2Position);
        }
    }

    // Move the players where you want them in the Scene, then press this
    // to save their current positions into that area's AreasData.
    [Button("Save Current Positions To Area")]
    void SaveCurrentPositionsToArea(AreasData area)
    {
        if (area == null)
        {
            Debug.LogWarning("Drag an AreasData asset in first");
            return;
        }

        Transform p1 = GetCharacterTransform(SpeakerType.Player1);
        Transform p2 = GetCharacterTransform(SpeakerType.Player2);

        if (p1 != null) area.player1Position = GetPosition(p1);
        if (p2 != null) area.player2Position = GetPosition(p2);
        area.overridePositions = true;

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(area);
        UnityEditor.AssetDatabase.SaveAssets();
#endif
        Debug.Log($"Saved player positions to '{area.name}'", area);
    }

    Transform GetCharacterTransform(SpeakerType type)
    {
        if (characters == null) return null;
        foreach (var slot in characters)
            if (slot != null && slot.character != null && slot.speakerType == type)
                return slot.character.transform;
        return null;
    }

    void SetCharacterPosition(SpeakerType type, Vector2 position)
    {
        Transform t = GetCharacterTransform(type);
        if (t == null) return;

        if (t is RectTransform rt)
            rt.anchoredPosition = position;
        else
            t.localPosition = new Vector3(position.x, position.y, t.localPosition.z);
    }

    static Vector2 GetPosition(Transform t)
    {
        return t is RectTransform rt ? rt.anchoredPosition : (Vector2)t.localPosition;
    }

    // Swaps the Player1 and Player2 character sprites. A null sprite keeps the current one.
    public void SetPlayerSprites(Sprite player1Sprite, Sprite player2Sprite)
    {
        if (characters == null) return;

        foreach (var slot in characters)
        {
            if (slot == null) continue;

            Sprite sprite = null;
            if (slot.speakerType == SpeakerType.Player1) sprite = player1Sprite;
            else if (slot.speakerType == SpeakerType.Player2) sprite = player2Sprite;

            if (sprite == null) continue;

            Image image = slot.targetImage;
            SpriteRenderer renderer = slot.targetRenderer;

            if (image == null && renderer == null && slot.character != null)
            {
                image = slot.character.GetComponentInChildren<Image>(true);
                if (image == null)
                    renderer = slot.character.GetComponentInChildren<SpriteRenderer>(true);
            }

            if (image != null) image.sprite = sprite;
            if (renderer != null) renderer.sprite = sprite;

            if (image == null && renderer == null)
                Debug.LogWarning($"DialogueSystem: no Image or SpriteRenderer found for character '{slot.speakerName}'", this);
        }
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