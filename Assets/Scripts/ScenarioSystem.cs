using DG.Tweening;
using Sirenix.OdinInspector;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;



public class ScenarioSystem : MonoBehaviour
{
    [Header("GameObjects")]
    private FERController getEmotions;
    private OpenCVFaceFeeder faceFeeder;
    private bool ferActive;
    public TextMeshProUGUI[] textScenario;

    [Header("GameUI")]
    public GameObject[] UI_Scenes;
    public GameObject emotionsButton;
    public GameObject emotionsContents;
    public TextMeshProUGUI[] currentplayerText;
    public GameObject endResult;

    [Header("Notifications")]
    public NotificationSender correctNotification;
    public NotificationSender wrongNotification;

    [Header("Dialogue")]
    public DialogueSystem dialogueSystem;
    [Tooltip("Plays after the players enter their names. Use {player1} / {player2} in the text.")]
    public DialogueData introDialogue;
    [Tooltip("Plays after the last area is finished, before the end result shows.")]
    public DialogueData endingDialogue;


    [Header("Player Names")]
    public GameObject playerNameUI;
    public TMP_InputField player1Input;
    public TMP_InputField player2Input;

    [Header("AreaUI")]
    public GameObject AreaUI;
    public TextMeshProUGUI areaTitle;
    public TextMeshProUGUI welcomeText;
    public TextMeshProUGUI areaSypnosis;

    [Header("Game Settings")]
    [MinValue(1)]
    public int roundsPerArea = 3;

    [Header("Datas")]
    public AreasData[] SceneArea;
    public TextMeshProUGUI[] scenarioTitleText;

    private ScenarioData[] selectedScenes;
    private ScenarioData currentScene;

    [ReadOnly]
    Emotions pickedEmotions;

    private string player1 = "Player 1";
    private string player2 = "Player 2";

    private string currentPlayer;

    private bool playerSwitcher = true;
    private int startingPlayer;
    private bool showAreaTitle = true;
    private bool showUI = false;
    private bool gameStarted = false;

    bool isEmotionCorrect = false;
    bool isPLayerEmotionCorrect = false;

    private int Ui_Index;
    private int Area_Index;

    private int rounds;

    private void Start()
    {
        getEmotions = FindAnyObjectByType<FERController>(FindObjectsInactive.Include);
        faceFeeder = FindAnyObjectByType<OpenCVFaceFeeder>(FindObjectsInactive.Include);
        if (dialogueSystem == null)
            dialogueSystem = FindAnyObjectByType<DialogueSystem>(FindObjectsInactive.Include);
        showAreaTitle = false;
        showUI = false;
        SetFERActive(false);

        if (playerNameUI != null)
            playerNameUI.SetActive(true);
    }

    bool RequiresCamera()
    {
        return gameStarted && showUI && Ui_Index == 2;
    }

    void SetFERActive(bool active)
    {
        if (getEmotions != null)
            getEmotions.gameObject.SetActive(active);
        if (faceFeeder != null)
            faceFeeder.gameObject.SetActive(active);
        ferActive = active;
    }

    void UpdateFERState()
    {
        bool shouldBeActive = RequiresCamera();
        if (shouldBeActive == ferActive) return;
        SetFERActive(shouldBeActive);
    }

    void Update()
    {
        if (!gameStarted) return;

        UpdateFERState();
        if (currentScene != null && ferActive) emotionChecker();
        AreaUI.SetActive(showAreaTitle);
        UI_Scenes[Ui_Index].SetActive(showUI);
        foreach (TextMeshProUGUI text in currentplayerText)
        {
            text.text = nextPlayer();
        }
    }

    public void ConfirmPlayerNames()
    {
        if (player1Input != null && !string.IsNullOrWhiteSpace(player1Input.text))
            player1 = player1Input.text.Trim();
        if (player2Input != null && !string.IsNullOrWhiteSpace(player2Input.text))
            player2 = player2Input.text.Trim();

        if (playerNameUI != null)
            playerNameUI.SetActive(false);

        PlayDialogue(introDialogue, () =>
        {
            gameStarted = true;
            CurrentArea();
        });
    }

    void PlayDialogue(DialogueData dialogue, System.Action onFinished)
    {
        if (dialogueSystem == null || dialogue == null)
        {
            onFinished?.Invoke();
            return;
        }

        dialogueSystem.SetTextReplacement("{player1}", player1);
        dialogueSystem.SetTextReplacement("{player2}", player2);
        dialogueSystem.PlayDialogue(dialogue, onFinished);
    }

    void EndGame()
    {
        gameStarted = false;
        showUI = false;
        showAreaTitle = false;
        SetFERActive(false);
        AreaUI.SetActive(false);
        foreach (GameObject scene in UI_Scenes)
        {
            scene.SetActive(false);
        }

        PlayDialogue(endingDialogue, () =>
        {
            endResult.SetActive(true);

            if (RewardSystem.Instance != null)
                RewardSystem.Instance.ShowRewards();
            else
                Debug.LogWarning("No RewardSystem in the scene");
        });
    }

    string nextPlayer()
    {
        int activePlayer = playerSwitcher ? startingPlayer : 1 - startingPlayer;
        return currentPlayer = activePlayer == 0 ? player1 : player2;
    }

    string GetLastPlayer()
    {
        int lastPlayerIndex = playerSwitcher ? 1 - startingPlayer : startingPlayer;
        return lastPlayerIndex == 0 ? player1 : player2;
    }

    void nextArea()
    {
        Area_Index++;
        if (Area_Index >= SceneArea.Length)
        {
            Debug.LogWarning("You Finished The AREAS!!!");
            EndGame();
            return;
        }
        startingPlayer = Area_Index % 2;
        playerSwitcher = true;
        CurrentArea();
    }

    void CurrentArea()
    {
        showUI = false;
        showAreaTitle = true;
        if (Area_Index >= SceneArea.Length) return;
        areaTitle.text = SceneArea[Area_Index].AreaTitle;
        welcomeText.text = SceneArea[Area_Index].WelcomeText;
        areaSypnosis.text = SceneArea[Area_Index].areaSypnosis;
    }

    public void continueButton()
    {
        showAreaTitle = false;
        showUI = true;
        GetRandomScenario();
    }

    void ShowNextUI()
    {
        Ui_Index++;
        showAreaTitle = false;
        if (Ui_Index >= UI_Scenes.Length)
        {
            Ui_Index = 0;
            rounds++;

            if (rounds >= roundsPerArea)
            {
                nextArea();
                rounds = 0;
            }
            else
            {
                startingPlayer = 1 - startingPlayer;
            }
        }

        foreach (GameObject showIU in UI_Scenes) 
        { 
            showIU.SetActive(false);
        }
    }

    // First
    public void SelectScene(int index)
    {
        currentScene = selectedScenes[index];
        playerSwitcher = true;
        foreach(TextMeshProUGUI allScenearioText in textScenario)
        {
            allScenearioText.DOKill();
            allScenearioText.text = "";
            allScenearioText.DOText(currentScene.textScenario, 2f);
        }

        ShowNextUI();
        EmotionPicker();
    }

    // Second
    public void EmotionPicker() 
    {
        Emotions[] emotions = (Emotions[])System.Enum.GetValues(typeof(Emotions));
        for (int i = 0; i < emotions.Length; i++)
        {
            Emotions emotion = emotions[i];
            GameObject emotionBtn = Instantiate(emotionsButton);
            emotionBtn.transform.SetParent(emotionsContents.transform, false);
            emotionBtn.GetComponentInChildren<TextMeshProUGUI>().text = emotions[i].ToString();

            Button thisButton = emotionBtn.GetComponent<Button>();


            thisButton.onClick.AddListener(() =>
            {
                playerSwitcher = false;
                pickedEmotions = emotion;
                ShowNextUI();
            });
        }
    }

    public bool emotionChecker()
    {
        Emotions faceEmotion = getEmotions.CurrentEmotion;

        // HasFlag(None) is always true, so None must be rejected explicitly
        if (faceEmotion != Emotions.None && pickedEmotions != Emotions.None &&
            currentScene.emotionsRequirment.HasFlag(faceEmotion) && currentScene.emotionsRequirment.HasFlag(pickedEmotions))
        {
            return isEmotionCorrect = true;
        }

        return isEmotionCorrect = false;
    } 

    public bool playerEmotionChecker()
    {
        if (getEmotions.CurrentEmotion == pickedEmotions)
        {
            return isPLayerEmotionCorrect = true;
        }
        return isPLayerEmotionCorrect = false;
    }

    //Last
    public void scenarioCondition()
    {
        playerEmotionChecker();
        emotionChecker();
        string lastPlayer = GetLastPlayer();
        string rewardMessage = TryGiveReward(currentScene);

        ShowNextUI();
        GetRandomScenario();

        if (isPLayerEmotionCorrect)
        {
            SendNotification(correctNotification, $"You guessed {lastPlayer}'s emotion correctly!" + rewardMessage, Color.green);
        }
        else
        {
            SendNotification(wrongNotification, $"You did not guess {lastPlayer}'s emotion correctly!" + rewardMessage, Color.red);
        }
    }

    // Both players' emotions matched the scenario, so give its reward
    string TryGiveReward(ScenarioData scene)
    {
        if (!isEmotionCorrect || scene == null || scene.rewardIndex < 0) return "";

        if (RewardSystem.Instance == null)
        {
            Debug.LogWarning("No RewardSystem in the scene");
            return "";
        }

        if (!RewardSystem.Instance.Collect(scene.rewardIndex)) return "";

        return string.IsNullOrEmpty(scene.rewardName) ? "" : $"\nYou earned: {scene.rewardName}!";
    }

    void SendNotification(NotificationSender sender, string message, Color fallbackColor)
    {
        if (sender != null)
            sender.SendNotification(message);
        else if (NotificationSystem.Instance != null)
            NotificationSystem.Instance.showMessage(message, fallbackColor);
        else
            Debug.LogWarning("No NotificationSystem in the scene");
    }

    private void GetRandomScenario()
    {
        playerSwitcher = true;
        if (Area_Index >= SceneArea.Length) return;
        selectedScenes = SceneArea[Area_Index].possbileScenarios.OrderBy(x => Random.value).Take(2).ToArray();
        if (selectedScenes == null) return;
        for (int i = 0; i < selectedScenes.Length; i++)
        {
            scenarioTitleText[i].text = selectedScenes[i].scenarioTitle;
        }
    }
}
