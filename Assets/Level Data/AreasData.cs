using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(fileName = "AreasData", menuName = "Scriptable Objects/AreasData")]
public class AreasData : ScriptableObject
{
    public string AreaTitle;
    public string WelcomeText;
    [TextArea(10, 10)]
    public string areaSypnosis;
    public Sprite background;

    public Sprite Player1Sprite;
    public Sprite Player2Sprite;

    [Header("Character Positions")]
    [Tooltip("If on, the players move to these positions when this area starts.")]
    public bool overridePositions;
    public Vector2 player1Position;
    public Vector2 player2Position;

    public AudioClip BackgroundMusic;
    public DialogueData areaDialogue;
    public ScenarioData[] possbileScenarios;

    [Header("Reward")]
    public string rewardName;
    public UnityEvent onRewardEarned;
}