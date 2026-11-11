using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
public class ChoicesPanel : MonoBehaviour
{

    public GameObject choicePanel;

    public Button Yes;
    public Button No;

    public UnityEvent OnYesClickedEvent;
    public UnityEvent OnNoClickedEvent;

    private void Awake()
    {
        Yes.onClick.AddListener(OnYesClicked);
        No.onClick.AddListener(OnNoClicked);
    }

    private void OnYesClicked()
    {
        OnYesClickedEvent?.Invoke();
    }

    private void OnNoClicked()
    {
        OnNoClickedEvent?.Invoke();
    }

    public void ShowPanel()
    {
        choicePanel.SetActive(true);
    }
}