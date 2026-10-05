using UnityEngine;

[CreateAssetMenu(menuName = "Events/Notification Sender")]
public class NotificationSender : ScriptableObject
{
    public Color color = Color.white;

    public void SendNotification(string message)
    {
        if (NotificationSystem.Instance == null)
        {
            Debug.LogWarning("No NotificationSystem in the scene");
            return;
        }

        NotificationSystem.Instance.showMessage(message, color);
    }
}