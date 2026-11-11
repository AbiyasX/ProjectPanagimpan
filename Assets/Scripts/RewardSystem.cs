using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Reward
{
    public string rewardName;
    public Texture rewardImage;
    public GameObject rewardObject;
    public bool isCollected;
}

public class RewardSystem : MonoBehaviour
{
    public static RewardSystem Instance;

    public RewardPanelScript rewardPanel;
    public List<Reward> rewards = new List<Reward>();

    public int CollectedCount
    {
        get
        {
            int count = 0;
            foreach (var reward in rewards)
                if (reward.isCollected) count++;
            return count;
        }
    }

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

        Reward reward = rewards[index];

        if (reward.isCollected)
            return false;

        reward.isCollected = true;

        if (rewardPanel != null)
            rewardPanel.ShowReward(reward.rewardImage, reward.rewardName);
        else
            Debug.LogWarning("RewardPanelScript is not assigned in RewardSystem");

        return true;
    }

    [Button("Test Collect")]
    void TestCollect(int index)
    {
        Collect(index);
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