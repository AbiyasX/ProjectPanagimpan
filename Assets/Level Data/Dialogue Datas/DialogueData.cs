using UnityEngine;
using UnityEngine.Events;



public enum SpeakerType
{
    Custom,
    Player1,
    Player2
}

[System.Serializable]
public class DialogueText
{
    [Tooltip("Custom uses the Dialogue Speaker text. Player1/Player2 use the names typed at the start.")]
    public SpeakerType speakerType = SpeakerType.Custom;
    public string dialogueSpeaker;

    [TextArea(10,20)]
    public string dialogueText;
    public UnityEvent OnTriggerEvent;
    
}

[CreateAssetMenu(fileName = "DialogueData", menuName = "Scriptable Objects/DialogueData")]
public class DialogueData : ScriptableObject
{
    public GameObject dialogue_Background;
    public DialogueText[] dialogue_Texts;
    public UnityEvent startTriggerEvent;
    public UnityEvent endTriggerEvent;

}
