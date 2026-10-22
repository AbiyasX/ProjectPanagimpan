using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class NameSetter : MonoBehaviour
{
    [Header("Player Names")]
    [SerializeField] private GameObject nameSetUI;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TMP_InputField nameInput;
    [Space]
    [SerializeField] private GameObject nameErrorUI;
    [SerializeField] private TextMeshProUGUI nameError;
    [Space]
    [SerializeField] private GameObject nameConfirmationUI;
    
    [HideInInspector] public string p1Name = "Player 1";
    [HideInInspector] public string p2Name = "Player 2";
    private int currentPlayer = 1;

    private ScenarioSystem sceneSystemScript;

    void Start()
    {
        sceneSystemScript = GetComponent<ScenarioSystem>();
    }

    public void ConfirmPlayerName()
    {
        if (nameInput != null)
            if (string.IsNullOrWhiteSpace(nameInput.text))
            {
                nameErrorUI.SetActive(true);
                nameError.text = "Your name can't be blank!";
            }
            else
            {
                if (currentPlayer == 1)
                {
                    p1Name = nameInput.text.Trim();
                    currentPlayer++;

                    PassSystem.Instance.OpenPassScreen();

                    nameText.text = "Player 2's name is...";
                    nameInput.text = "";
                    nameErrorUI.SetActive(false);
                }
                else if (currentPlayer == 2)
                {
                    p2Name = nameInput.text.Trim();

                    if (nameSetUI != null && nameConfirmationUI != null)
                        nameSetUI.SetActive(false);
                        nameConfirmationUI.SetActive(true);
                }
            }
    }

    public void ConfirmBothNames()
    {
        nameConfirmationUI.SetActive(false);

        sceneSystemScript.gameStarted = true;
        sceneSystemScript.CurrentArea();
    }

    public void ResetNames()
    {
        nameSetUI.SetActive(true);
        nameConfirmationUI.SetActive(false);

        currentPlayer = 1;

        nameText.text = "Player 1's name is...";
        nameInput.text = "";
        nameErrorUI.SetActive(false);
    }
}
