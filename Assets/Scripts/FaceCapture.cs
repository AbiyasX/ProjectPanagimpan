using TMPro;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class EmotionDisplay
{
    public Emotions emotion;
    public Sprite image;
    public string label;
}

public class FaceCapture : MonoBehaviour
{
    [Header("Face Recognition")]
    public FERController ferController;
    public OpenCVFaceFeeder faceFeeder;

    [Header("Detected Emotion")]
    public EmotionDisplay[] emotionDisplays;
    public Image detectedEmotionImage;
    public TextMeshProUGUI detectedEmotionText;
    public string detectedTextFormat = "{0}";
    public bool upperCaseDetectedText = true;
    public Sprite noFaceImage;
    public string noFaceText = "...";

    public bool IsActive { get; private set; }

    public Emotions CurrentEmotion => ferController != null ? ferController.CurrentEmotion : Emotions.None;

    private Emotions shownEmotion;
    private bool hasShown;

    private void Awake()
    {
        if (ferController == null)
            ferController = FindAnyObjectByType<FERController>(FindObjectsInactive.Include);
        if (faceFeeder == null)
            faceFeeder = FindAnyObjectByType<OpenCVFaceFeeder>(FindObjectsInactive.Include);
    }

    private void Update()
    {
        if (!IsActive) return;

        Emotions emotion = CurrentEmotion;
        if (hasShown && emotion == shownEmotion) return;

        ShowEmotion(emotion);
    }

    public void SetActive(bool active)
    {
        if (ferController != null)
        {
            if (active) ferController.CurrentEmotion = Emotions.None;
            ferController.gameObject.SetActive(active);
        }
        if (faceFeeder != null)
            faceFeeder.gameObject.SetActive(active);

        IsActive = active;

        if (active)
            ShowEmotion(Emotions.None);
    }

    public void SetActiveIfChanged(bool active)
    {
        if (active == IsActive) return;
        SetActive(active);
    }

    public bool IsShowing(Emotions emotion)
    {
        return CurrentEmotion == emotion;
    }

    void ShowEmotion(Emotions emotion)
    {
        shownEmotion = emotion;
        hasShown = true;

        EmotionDisplay display = FindDisplay(emotion);
        bool noFace = emotion == Emotions.None;

        Sprite sprite = noFace ? noFaceImage : display?.image;
        if (detectedEmotionImage != null)
        {
            detectedEmotionImage.sprite = sprite;
            detectedEmotionImage.preserveAspect = true;
            detectedEmotionImage.gameObject.SetActive(sprite != null);
        }

        if (detectedEmotionText != null)
        {
            if (noFace)
            {
                detectedEmotionText.text = noFaceText;
            }
            else
            {
                string name = display != null && !string.IsNullOrWhiteSpace(display.label) ? display.label : emotion.ToString();
                string text = string.Format(detectedTextFormat, name);
                detectedEmotionText.text = upperCaseDetectedText ? text.ToUpper() : text;
            }
        }
    }

    EmotionDisplay FindDisplay(Emotions emotion)
    {
        if (emotionDisplays == null) return null;

        foreach (EmotionDisplay display in emotionDisplays)
            if (display != null && display.emotion == emotion)
                return display;

        return null;
    }
}