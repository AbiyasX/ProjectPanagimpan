using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(fileName = "AreasData", menuName = "Scriptable Objects/AreasData")]
public class AreasData : ScriptableObject
{
    public string AreaTitle;
    public string WelcomeText;
    [TextArea(10,10)]
    public string areaSypnosis;
    public Sprite background;

    public AudioClip BackgroundMusic;
    public DialogueData areaDialogue;
    public ScenarioData[] possbileScenarios;

    [Header("Reward")]
    public string rewardName;
    public UnityEvent onRewardEarned;
}
