using System;
using UnityEngine;
using UnityEngine.Events;
using Sirenix.OdinInspector;

[Serializable]
public class RewardOutcome
{
    public string label = "Outcome";

    [Tooltip("Lowest reward result that triggers this outcome (inclusive).")]
    public int minReward;

    [Tooltip("Highest reward result that triggers this outcome (inclusive).")]
    public int maxReward;

    [Tooltip("Optional: dialogue to play first. The event fires after it finishes.")]
    public DialogueData dialogue;

    public UnityEvent onOutcome;

    public bool Matches(int reward) => reward >= minReward && reward <= maxReward;
}

public class RewardOutcomeSystem : MonoBehaviour
{
    [Tooltip("Optional: needed only if an outcome has a dialogue.")]
    public DialogueSystem dialogueSystem;

    public RewardOutcome[] outcomes;

    [Tooltip("Fires if the reward result doesn't match any outcome.")]
    public UnityEvent onNoMatch;

    // Fills in the 1 / 2-3 / 4 setup when you add the component (or right-click > Reset).
    void Reset()
    {
        outcomes = new[]
        {
            new RewardOutcome { label = "Outcome 1",   minReward = 0, maxReward = 1 },
            new RewardOutcome { label = "Outcome 2-3", minReward = 2, maxReward = 3 },
            new RewardOutcome { label = "Outcome 4",   minReward = 4, maxReward = 4 },
        };
    }

    [Button("Test Outcome")]
    public void TriggerOutcome(int rewardResult)
    {
        RewardOutcome outcome = GetOutcome(rewardResult);

        if (outcome == null)
        {
            Debug.LogWarning($"No outcome set up for reward result {rewardResult}");
            onNoMatch?.Invoke();
            return;
        }

        if (outcome.dialogue != null && dialogueSystem != null)
        {
            // Play the outcome's dialogue, then fire its event when the dialogue ends.
            dialogueSystem.PlayDialogue(outcome.dialogue, () => outcome.onOutcome?.Invoke());
        }
        else
        {
            outcome.onOutcome?.Invoke();
        }
    }

    public RewardOutcome GetOutcome(int rewardResult)
    {
        if (outcomes == null) return null;

        foreach (var outcome in outcomes)
        {
            if (outcome != null && outcome.Matches(rewardResult))
                return outcome;
        }
        return null;
    }
}