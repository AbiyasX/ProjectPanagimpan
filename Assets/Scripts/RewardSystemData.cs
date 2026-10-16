using UnityEngine;

[CreateAssetMenu(menuName = "Events/Collect Reward")]
public class RewardSystemData : ScriptableObject
{
    // For UnityEvents in the Inspector
    public void CollectReward(int index)
    {
        TryCollectReward(index);
    }

    public bool TryCollectReward(int index)
    {
        if (RewardSystem.Instance == null)
        {
            Debug.LogWarning("No RewardSystem in the scene");
            return false;
        }
        return RewardSystem.Instance.Collect(index);
    }
}
