using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class EmotionButton
{
    public Button button;
    public Emotions emotion;
    public Sprite image;
    public string label;
}

public class EmotionPicker : MonoBehaviour
{
    [Header("Emotion Buttons")]
    public EmotionButton[] emotionButtons;

    [Header("Chosen Emotion")]
    public Image chosenEmotionImage;
    public TextMeshProUGUI chosenEmotionText;
    public string chosenTextFormat = "You Chose: {0}";
    public string noChoiceText = "You Chose: ";

    [Header("Confirm Panel")]
    public GameObject confirmPanel;
    public Image confirmEmotionImage;
    public TextMeshProUGUI confirmEmotionText;
    public string confirmTextFormat = "{0}";
    public bool upperCaseConfirmText = true;

    public event System.Action<Emotions> EmotionPicked;

    [ShowInInspector, ReadOnly]
    public Emotions PickedEmotion { get; private set; }

    private EmotionButton chosen;

    private void Awake()
    {
        if (emotionButtons != null)
        {
            foreach (EmotionButton entry in emotionButtons)
            {
                if (entry == null || entry.button == null) continue;

                EmotionButton thisEntry = entry;
                entry.button.onClick.AddListener(() => Choose(thisEntry));
            }
        }

        ClearChoice();
    }

    void Choose(EmotionButton entry)
    {
        chosen = entry;

        SetImage(chosenEmotionImage, GetSprite(entry));

        if (chosenEmotionText != null)
            chosenEmotionText.text = string.Format(chosenTextFormat, GetLabel(entry));
    }

    public void OpenConfirmPanel()
    {
        if (chosen == null)
        {
            Debug.LogWarning($"EmotionPicker ({name}): OpenConfirmPanel was called but no emotion is chosen on this EmotionPicker.", this);
            return;
        }

        if (confirmPanel == null)
        {
            Confirm();
            return;
        }

        confirmPanel.SetActive(true);

        SetImage(confirmEmotionImage, GetSprite(chosen));

        if (confirmEmotionText != null)
        {
            string label = string.Format(confirmTextFormat, GetLabel(chosen));
            confirmEmotionText.text = upperCaseConfirmText ? label.ToUpper() : label;
            confirmEmotionText.ForceMeshUpdate();
            Debug.Log($"EmotionPicker: set \"{GetPath(confirmEmotionText.transform)}\" to \"{confirmEmotionText.text}\" (in scene: {confirmEmotionText.gameObject.scene.IsValid()}, active: {confirmEmotionText.gameObject.activeInHierarchy})", confirmEmotionText);
            StartCoroutine(CheckTextNextFrame(confirmEmotionText.text));
        }
        else
        {
            Debug.LogWarning($"EmotionPicker ({name}): Confirm Emotion Text is not assigned.", this);
        }

        if (confirmEmotionImage == null)
            Debug.LogWarning($"EmotionPicker ({name}): Confirm Emotion Image is not assigned.", this);
    }

    System.Collections.IEnumerator CheckTextNextFrame(string expected)
    {
        yield return null;
        yield return null;

        if (confirmEmotionText != null && confirmEmotionText.text != expected)
            Debug.LogWarning($"EmotionPicker: something changed \"{GetPath(confirmEmotionText.transform)}\" back to \"{confirmEmotionText.text}\" after it was set to \"{expected}\". Check the other components on that object.", confirmEmotionText);
    }

    static string GetPath(Transform t)
    {
        string path = t.name;
        while (t.parent != null)
        {
            t = t.parent;
            path = t.name + "/" + path;
        }
        return path;
    }

    public void CloseConfirmPanel()
    {
        if (confirmPanel != null)
            confirmPanel.SetActive(false);
    }

    public void Confirm()
    {
        if (chosen == null) return;

        Emotions emotion = chosen.emotion;
        PickedEmotion = emotion;
        ClearChoice();
        EmotionPicked?.Invoke(emotion);
    }

    public void ClearChoice()
    {
        chosen = null;

        SetImage(chosenEmotionImage, null);

        if (chosenEmotionText != null)
            chosenEmotionText.text = noChoiceText;

        CloseConfirmPanel();
    }

    void SetImage(Image image, Sprite sprite)
    {
        if (image == null) return;

        image.sprite = sprite;
        image.preserveAspect = true;
        image.gameObject.SetActive(sprite != null);
    }

    Sprite GetSprite(EmotionButton entry)
    {
        if (entry.image != null) return entry.image;

        if (entry.button == null) return null;

        foreach (Image child in entry.button.GetComponentsInChildren<Image>(true))
            if (child != entry.button.image && child.sprite != null)
                return child.sprite;

        return entry.button.image != null ? entry.button.image.sprite : null;
    }

    string GetLabel(EmotionButton entry)
    {
        return string.IsNullOrWhiteSpace(entry.label) ? entry.emotion.ToString() : entry.label;
    }
}