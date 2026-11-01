using DG.Tweening;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class ScenarioCard
{
    public TextMeshProUGUI titleText;
    public Image image;
    public TextMeshProUGUI sypnosisText;
}

[System.Serializable]
public class SelectedScenarioDisplay
{
    public TextMeshProUGUI scenarioText;
    public Image image;
    public TextMeshProUGUI sypnosisText;
}

public class ScenarioPicker : MonoBehaviour
{
    [Header("Choice Cards")]
    [Min(1)]
    public int choiceCount = 2;
    public ScenarioCard[] cards;

    [Header("Selected Scenario")]
    public SelectedScenarioDisplay[] selectedDisplays;
    [Min(0)]
    public float typeDuration = 2f;
    public bool hideImageWhenEmpty = true;

    public event System.Action<ScenarioData> ScenarioSelected;

    public ScenarioData CurrentScene { get; private set; }

    private ScenarioData[] choices;

    public void ShowRandomChoices(IEnumerable<ScenarioData> pool)
    {
        choices = pool != null
            ? pool.OrderBy(x => Random.value).Take(choiceCount).ToArray()
            : null;

        ShowChoices();
    }

    public void SelectScene(int index)
    {
        if (choices == null || index < 0 || index >= choices.Length || choices[index] == null) return;

        CurrentScene = choices[index];
        ShowSelectedScenario(CurrentScene);
        ScenarioSelected?.Invoke(CurrentScene);
    }

    void ShowChoices()
    {
        if (cards == null) return;

        for (int i = 0; i < cards.Length; i++)
        {
            ScenarioCard card = cards[i];
            if (card == null) continue;

            bool hasScenario = choices != null && i < choices.Length && choices[i] != null;
            ScenarioData scenario = hasScenario ? choices[i] : null;

            if (card.titleText != null)
                card.titleText.text = hasScenario ? scenario.scenarioTitle : "";

            SetScenarioImage(card.image, hasScenario ? scenario.Image : null);
            SetScenarioSypnosis(card.sypnosisText, hasScenario ? scenario.scenaroSypnosis : "");
        }
    }

    void ShowSelectedScenario(ScenarioData scenario)
    {
        if (selectedDisplays == null) return;

        foreach (SelectedScenarioDisplay display in selectedDisplays)
        {
            if (display == null) continue;

            if (display.scenarioText != null)
            {
                display.scenarioText.DOKill();
                display.scenarioText.text = "";
                if (scenario != null)
                    display.scenarioText.DOText(scenario.textScenario, typeDuration);
            }

            SetScenarioImage(display.image, scenario != null ? scenario.Image : null);
            SetScenarioSypnosis(display.sypnosisText, scenario != null ? scenario.scenaroSypnosis : "");
        }
    }

    void SetScenarioImage(Image image, Sprite sprite)
    {
        if (image == null) return;

        image.sprite = sprite;
        image.preserveAspect = true;

        if (hideImageWhenEmpty)
            image.gameObject.SetActive(sprite != null);
        else
            image.enabled = sprite != null;
    }

    void SetScenarioSypnosis(TextMeshProUGUI text, string sypnosis)
    {
        if (text == null) return;
        text.text = sypnosis ?? "";
    }
}