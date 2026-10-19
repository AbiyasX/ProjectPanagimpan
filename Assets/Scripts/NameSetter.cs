using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class NameSetter : MonoBehaviour
{
    [Header("Player Names")]
    [SerializeField] private GameObject pNameUI;
    [SerializeField] private TextMeshProUGUI pNameText;
    [SerializeField] private TMP_InputField pNameInput;
    
    [HideInInspector] public string p1Name = "Player 1";
    [HideInInspector] public string p2Name = "Player 2";
    private int currentPlayer = 1;

    ///
    private ScenarioSystem sceneSystemScript;

    void Start()
    {
        sceneSystemScript = GetComponent<ScenarioSystem>();
    }
    ///

    public void ConfirmPlayerName()
    {
        if (pNameInput != null && !string.IsNullOrWhiteSpace(pNameInput.text))
            if (currentPlayer == 1)
            {
                p1Name = pNameInput.text.Trim();
                currentPlayer++;
                pNameInput.text = "";
            }
            else if (currentPlayer == 2)
            {
                p2Name = pNameInput.text.Trim();

                if (pNameUI != null)
                    pNameUI.SetActive(false);

                sceneSystemScript.gameStarted = true;
                sceneSystemScript.CurrentArea();
            }
    }
}
