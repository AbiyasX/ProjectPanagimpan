using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class NameSetter : MonoBehaviour
{
    [Header("Name Setter Screen")]
    [SerializeField] private GameObject nameSetUI;
    [SerializeField] private TextMeshProUGUI inputText;
    [SerializeField] private TMP_InputField nameInput;
    [Space]
    [SerializeField] private GameObject nameErrorUI;
    [SerializeField] private TextMeshProUGUI nameError;
    [Space]
    [SerializeField] private Image polaroidImage;
    [SerializeField] private Sprite player1Image;
    [SerializeField] private Sprite player2Image;
    
    [Header("Name Confirmation Screen")]
    [SerializeField] private GameObject nameConfirmationUI;
    [Space]
    [SerializeField] private TextMeshProUGUI nameTextPlayer1;
    [SerializeField] private TextMeshProUGUI nameTextPlayer2;
    
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

                    polaroidImage.sprite = player2Image;
                    inputText.text = "Player 2's name is...";
                    nameInput.text = "";
                    nameErrorUI.SetActive(false);
                }
                else if (currentPlayer == 2)
                {
                    p2Name = nameInput.text.Trim();

                    if (nameSetUI != null && nameConfirmationUI != null)
                    {
                        nameTextPlayer1.text = p1Name;
                        nameTextPlayer2.text = p2Name;

                        nameSetUI.SetActive(false);
                        nameConfirmationUI.SetActive(true);
                    }
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

        polaroidImage.sprite = player1Image;
        inputText.text = "Player 1's name is...";
        nameInput.text = "";
        nameErrorUI.SetActive(false);
    }
}
