using Sirenix.OdinInspector;
using System;
using UnityEngine;


[Flags]
public enum Emotions 
{
    None = 0,
    Anger = 1,
    Contempt = 2,
    Disgust = 4,
    Fear = 8,
    Happiness = 16,
    Neutral = 32,
    Sadness = 64,
    Surprise = 128
}
[CreateAssetMenu(fileName = "ScenarioData", menuName = "Scriptable Objects/ScenarioData")]
public class ScenarioData : ScriptableObject
{
    public string scenarioTitle;
    public string scenaroSypnosis;

    [Header("ScenarioResult")]
    public string goodResult;
    public string badResult;

    [TextArea(10,20)]
    public string textScenario;

    
    [EnumToggleButtons]
    public Emotions emotionsRequirment;

    [Header("Reward")]
    [Tooltip("Name shown in the notification when the reward is earned.")]
    public string rewardName;
    [Tooltip("Slot in the RewardSystem rewards list (0 = first). -1 = no reward.")]
    [MinValue(-1)]
    public int rewardIndex = -1;
}
