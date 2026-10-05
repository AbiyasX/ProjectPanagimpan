using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Events/Collect Reward")]
public class RewardSystemData : ScriptableObject
{
    public void CollectReward(int index)
    {
        if(RewardSystem.Instance == null)
        {
            Debug.LogWarning("No RewardSystem in the scene");
            return;
        }
        RewardSystem.Instance.Collect(index);
    }
}


[System.Serializable]
public class Reward
{
    public GameObject rewardObject;
    public bool isCollected;
}
public class RewardSystem : MonoBehaviour
{

    public static RewardSystem Instance;

    public List<Reward> rewards = new List<Reward>();

    void Awake()
    {
        Instance = this;
    }

    public bool Collect(int index)
    {
        if (index < 0 || index >= rewards.Count)
        {
            Debug.LogWarning($"Reward index {index} doesn't exist in the RewardSystem list");
            return false;
        }

        rewards[index].isCollected = true;
        return true;
    }

    [Button("Show Rewards")]
    public void ShowRewards()
    {
        foreach (var reward in rewards)
        {
            if (reward.rewardObject != null)
                reward.rewardObject.SetActive(reward.isCollected);
        }
    }
}
