using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(fileName = "AreasData", menuName = "Scriptable Objects/AreasData")]
public class AreasData : ScriptableObject
{
    public string AreaTitle;
    public string WelcomeText;
    [TextArea(10,10)]
    public string areaSypnosis;
    public GameObject background;
    [Tooltip("Optional. Plays when this area starts, before the area title shows. Use {player1} / {player2} in the text.")]
    public DialogueData areaDialogue;
    public ScenarioData[] possbileScenarios;

    [Header("Reward")]
    [Tooltip("Name shown in the notification when the reward is earned at the end of this area.")]
    public string rewardName;
    [Tooltip("Runs at the end of this area when the players get enough correct guesses. Add the Collect Reward asset and pick CollectReward(int).")]
    public UnityEvent onRewardEarned;
}
