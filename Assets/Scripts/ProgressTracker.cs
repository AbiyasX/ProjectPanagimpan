using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ProgressTracker : MonoBehaviour
{
    [Header("Overall Progress")]
    public Slider overallSlider;
    public TextMeshProUGUI overallText;
    public string overallTextFormat = "{0}%";

    [Header("Area Progress")]
    public Slider areaSlider;
    public TextMeshProUGUI areaText;
    public string areaTextFormat = "Round {0}/{1}";

    [Header("Animation")]
    [Min(0)]
    public float fillDuration = 0.5f;

    private void Awake()
    {
        SetupSlider(overallSlider);
        SetupSlider(areaSlider);
    }

    public void SetProgress(int areaIndex, int roundsDone, int areaCount, int roundsPerArea, bool instant = false)
    {
        int totalRounds = Mathf.Max(1, areaCount * roundsPerArea);
        int doneRounds = Mathf.Clamp(areaIndex * roundsPerArea + roundsDone, 0, totalRounds);
        float overall = (float)doneRounds / totalRounds;

        SetSlider(overallSlider, overall, instant);
        if (overallText != null)
            overallText.text = string.Format(overallTextFormat, Mathf.RoundToInt(overall * 100f));

        bool finished = areaIndex >= areaCount;
        float area = finished ? 1f : (float)roundsDone / Mathf.Max(1, roundsPerArea);

        SetSlider(areaSlider, area, instant);
        if (areaText != null)
        {
            int round = finished ? roundsPerArea : Mathf.Min(roundsDone + 1, roundsPerArea);
            areaText.text = string.Format(areaTextFormat, round, roundsPerArea);
        }
    }

    void SetupSlider(Slider slider)
    {
        if (slider == null) return;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;
        slider.interactable = false;
    }

    void SetSlider(Slider slider, float value, bool instant)
    {
        if (slider == null) return;

        slider.DOKill();

        if (instant || fillDuration <= 0f || !slider.gameObject.activeInHierarchy)
            slider.value = value;
        else
            slider.DOValue(value, fillDuration).SetTarget(slider);
    }
}
