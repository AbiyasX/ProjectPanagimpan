using DG.Tweening;
using Sirenix.OdinInspector;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


[RequireComponent(typeof(NameSetter))]
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

    [Header("Dialogue")]
    public DialogueSystem dialogueSystem;
    public DialogueData endingDialogue;

    /*[Header("Player Names")]
    public GameObject playerNameUI;
    public TMP_InputField player1Input;
    public TMP_InputField player2Input;*/

    /*[Header("AreaUI")]
    public GameObject AreaUI;
    public TextMeshProUGUI areaTitle;
    public TextMeshProUGUI welcomeText;
    public TextMeshProUGUI areaSypnosis;*/

    [Header("Area Background")]
    [Tooltip("UI Image that shows the area's background sprite (put it behind your UI in the Canvas).")]
    public Image backgroundImage;
    [Tooltip("Optional. Use this instead of (or with) the Image if your background is a world-space SpriteRenderer.")]
    public SpriteRenderer backgroundRenderer;
    [Tooltip("Fade time when switching backgrounds. 0 = instant.")]
    [MinValue(0)]
    public float backgroundFadeDuration = 0.5f;
    [Tooltip("Keep the last area's background visible during the ending dialogue / results.")]
    public bool keepBackgroundOnEnd = true;

    [Header("Game Settings")]
    [MinValue(1)]
    public int roundsPerArea = 3;
    [Tooltip("Correct guesses needed in an area to earn the reward.")]
    [MinValue(1)]
    public int correctGuessesForReward = 2;

    [Header("Datas")]
    public AreasData[] SceneArea;
    public TextMeshProUGUI[] scenarioTitleText;

    [Header("Scenario Choice Cards")]
    [Tooltip("One Image per choice card, same order as Scenario Title Text.")]
    public Image[] scenarioImage;
    [Tooltip("One synopsis text per choice card, same order as Scenario Title Text.")]
    public TextMeshProUGUI[] scenarioSypnosisText;

    [Header("Selected Scenario")]
    [Tooltip("Images that show the picked scenario's image (e.g. on the emotion-pick and camera screens).")]
    public Image[] selectedScenarioImage;
    [Tooltip("Texts that show the picked scenario's synopsis.")]
    public TextMeshProUGUI[] selectedScenarioSypnosis;
    [Tooltip("Hide the Image object when a scenario has no image.")]
    public bool hideImageWhenEmpty = true;

    private ScenarioData[] selectedScenes;
    private ScenarioData currentScene;

    [ReadOnly]
    Emotions pickedEmotions;

    //private string player1 = "Player 1";
    //private string player2 = "Player 2";

    private string currentPlayer;

    private bool playerSwitcher = true;
    private int startingPlayer;
    private bool showAreaTitle = true;
    private bool showUI = false;
    public bool gameStarted = false;

    bool isEmotionCorrect = false;
    bool isPLayerEmotionCorrect = false;

    private int Ui_Index;
    private int Area_Index;

    private int rounds;
    private int correctGuesses;
    private string areaResultMessage;

    private NameSetter nameSetterScript;

    private void Start()
    {
        nameSetterScript = GetComponent<NameSetter>();

        getEmotions = FindAnyObjectByType<FERController>(FindObjectsInactive.Include);
        faceFeeder = FindAnyObjectByType<OpenCVFaceFeeder>(FindObjectsInactive.Include);
        if (dialogueSystem == null)
            dialogueSystem = FindAnyObjectByType<DialogueSystem>(FindObjectsInactive.Include);
        showAreaTitle = false;
        showUI = false;
        SetFERActive(false);

        /*if (playerNameUI != null)
            playerNameUI.SetActive(true);*/
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
        //AreaUI.SetActive(showAreaTitle);
        UI_Scenes[Ui_Index].SetActive(showUI);
        foreach (TextMeshProUGUI text in currentplayerText)
        {
            text.text = nextPlayer();
        }
    }

    /*public void ConfirmPlayerNames()
    {
        if (player1Input != null && !string.IsNullOrWhiteSpace(player1Input.text))
            player1 = player1Input.text.Trim();
        if (player2Input != null && !string.IsNullOrWhiteSpace(player2Input.text))
            player2 = player2Input.text.Trim();

        if (playerNameUI != null)
            playerNameUI.SetActive(false);

        gameStarted = true;
        CurrentArea();
    }*/

    void PlayDialogue(DialogueData dialogue, System.Action onFinished)
    {
        if (dialogueSystem == null || dialogue == null)
        {
            onFinished?.Invoke();
            return;
        }

        dialogueSystem.SetTextReplacement("{player1}", nameSetterScript.p1Name);
        dialogueSystem.SetTextReplacement("{player2}", nameSetterScript.p2Name);
        dialogueSystem.PlayDialogue(dialogue, onFinished);
    }

    // ---------- Background ----------

    // Puts the area's background sprite on the Image / SpriteRenderer.
    void ShowAreaBackground(AreasData area)
    {
        Sprite sprite = area != null ? area.background : null;

        if (sprite == null)
        {
            ClearBackground();
            return;
        }

        if (backgroundImage != null)
            SetImageSprite(backgroundImage, sprite);

        if (backgroundRenderer != null)
            SetRendererSprite(backgroundRenderer, sprite);

        if (backgroundImage == null && backgroundRenderer == null)
            Debug.LogWarning("ScenarioSystem: assign a Background Image or Background Renderer to show area backgrounds.");
    }

    void SetImageSprite(Image image, Sprite sprite)
    {
        if (image.sprite == sprite && image.gameObject.activeSelf) return; // same background, no flicker

        image.DOKill();
        image.gameObject.SetActive(true);

        if (backgroundFadeDuration <= 0f || image.sprite == null)
        {
            image.sprite = sprite;
            image.color = new Color(image.color.r, image.color.g, image.color.b, 1f);
            return;
        }

        float half = backgroundFadeDuration * 0.5f;
        DOTween.Sequence()
            .Append(image.DOFade(0f, half))
            .AppendCallback(() => image.sprite = sprite)
            .Append(image.DOFade(1f, half))
            .SetTarget(image);
    }

    void SetRendererSprite(SpriteRenderer renderer, Sprite sprite)
    {
        if (renderer.sprite == sprite && renderer.gameObject.activeSelf) return;

        renderer.DOKill();
        renderer.gameObject.SetActive(true);

        if (backgroundFadeDuration <= 0f || renderer.sprite == null)
        {
            renderer.sprite = sprite;
            renderer.color = new Color(renderer.color.r, renderer.color.g, renderer.color.b, 1f);
            return;
        }

        float half = backgroundFadeDuration * 0.5f;
        DOTween.Sequence()
            .Append(renderer.DOFade(0f, half))
            .AppendCallback(() => renderer.sprite = sprite)
            .Append(renderer.DOFade(1f, half))
            .SetTarget(renderer);
    }

    void ClearBackground()
    {
        if (backgroundImage != null)
        {
            backgroundImage.DOKill();
            backgroundImage.sprite = null;
            backgroundImage.gameObject.SetActive(false);
        }

        if (backgroundRenderer != null)
        {
            backgroundRenderer.DOKill();
            backgroundRenderer.sprite = null;
            backgroundRenderer.gameObject.SetActive(false);
        }
    }

    // ---------- Scenario Image & Synopsis ----------

    // Sets a UI Image to a scenario's sprite (hides it if there's no sprite and hideImageWhenEmpty is on).
    void SetScenarioImage(Image image, Sprite sprite)
    {
        if (image == null) return;

        image.sprite = sprite;
        image.preserveAspect = true;

        if (hideImageWhenEmpty)
            image.gameObject.SetActive(sprite != null);
        else
            image.enabled = sprite != null;
    }

    void SetScenarioSypnosis(TextMeshProUGUI text, string sypnosis)
    {
        if (text == null) return;
        text.text = sypnosis ?? "";
    }

    // Fills the choice cards (title / image / synopsis) for the two random scenarios.
    void ShowScenarioChoices()
    {
        int cardCount = scenarioTitleText.Length;

        for (int i = 0; i < cardCount; i++)
        {
            bool hasScenario = selectedScenes != null && i < selectedScenes.Length && selectedScenes[i] != null;
            ScenarioData scenario = hasScenario ? selectedScenes[i] : null;

            if (scenarioTitleText[i] != null)
                scenarioTitleText[i].text = hasScenario ? scenario.scenarioTitle : "";

            if (scenarioImage != null && i < scenarioImage.Length)
                SetScenarioImage(scenarioImage[i], hasScenario ? scenario.Image : null);

            if (scenarioSypnosisText != null && i < scenarioSypnosisText.Length)
                SetScenarioSypnosis(scenarioSypnosisText[i], hasScenario ? scenario.scenaroSypnosis : "");
        }
    }

    // Shows the picked scenario's image / synopsis on the later screens.
    void ShowSelectedScenario(ScenarioData scenario)
    {
        if (selectedScenarioImage != null)
            foreach (Image img in selectedScenarioImage)
                SetScenarioImage(img, scenario != null ? scenario.Image : null);

        if (selectedScenarioSypnosis != null)
            foreach (TextMeshProUGUI txt in selectedScenarioSypnosis)
                SetScenarioSypnosis(txt, scenario != null ? scenario.scenaroSypnosis : "");
    }

    // --------------------------------

    void EndGame()
    {
        gameStarted = false;
        showUI = false;
        showAreaTitle = false;
        SetFERActive(false);
        //AreaUI.SetActive(false);
        foreach (GameObject scene in UI_Scenes)
        {
            scene.SetActive(false);
        }

        if (!keepBackgroundOnEnd)
            ClearBackground();

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
        return currentPlayer = activePlayer == 0 ? nameSetterScript.p1Name : nameSetterScript.p2Name;
    }

    string GetLastPlayer()
    {
        int lastPlayerIndex = playerSwitcher ? 1 - startingPlayer : startingPlayer;
        return lastPlayerIndex == 0 ? nameSetterScript.p1Name : nameSetterScript.p2Name;
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
        correctGuesses = 0;
        CurrentArea();
    }

    public void CurrentArea()
    {
        showUI = false;
        showAreaTitle = false;
        if (Area_Index >= SceneArea.Length) return;

        AreasData area = SceneArea[Area_Index];
        /*areaTitle.text = area.AreaTitle;
        welcomeText.text = area.WelcomeText;
        areaSypnosis.text = area.areaSypnosis;*/

        // Swap in this area's background before its dialogue plays
        ShowAreaBackground(area);

        PlayDialogue(area.areaDialogue, () =>
        {
            continueButton();
        });
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
                areaResultMessage = CheckAreaReward();
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
        if (selectedScenes == null || index < 0 || index >= selectedScenes.Length) return;

        currentScene = selectedScenes[index];
        playerSwitcher = true;
        foreach (TextMeshProUGUI allScenearioText in textScenario)
        {
            allScenearioText.DOKill();
            allScenearioText.text = "";
            allScenearioText.DOText(currentScene.textScenario, 2f);
        }

        ShowSelectedScenario(currentScene);

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

        if (isPLayerEmotionCorrect)
            correctGuesses++;

        string rewardMessage = $"\nCorrect guesses: {correctGuesses}/{roundsPerArea}";

        ShowNextUI();
        GetRandomScenario();

        if (!string.IsNullOrEmpty(areaResultMessage))
        {
            rewardMessage = areaResultMessage;
            areaResultMessage = null;
        }

        if (NotificationSystem.Instance == null)
        {
            Debug.LogWarning("No NotificationSystem in the scene");
            return;
        }

        if (isPLayerEmotionCorrect)
            NotificationSystem.Instance.ShowCorrect($"You guessed {lastPlayer}'s emotion correctly!" + rewardMessage);
        else
            NotificationSystem.Instance.ShowWrong($"You did not guess {lastPlayer}'s emotion correctly!" + rewardMessage);
    }

    // End of the area: give the area's reward if the players got enough correct guesses
    string CheckAreaReward()
    {
        string result = $"\nArea finished! Correct guesses: {correctGuesses}/{roundsPerArea}";

        if (correctGuesses < correctGuessesForReward)
            return result + $"\nYou needed {correctGuessesForReward} to get the reward.";

        AreasData area = SceneArea[Area_Index];
        area.onRewardEarned?.Invoke();

        return string.IsNullOrEmpty(area.rewardName) ? result : result + $"\nYou earned: {area.rewardName}!";
    }

    private void GetRandomScenario()
    {
        playerSwitcher = true;
        if (Area_Index >= SceneArea.Length) return;
        selectedScenes = SceneArea[Area_Index].possbileScenarios.OrderBy(x => Random.value).Take(2).ToArray();
        if (selectedScenes == null) return;

        ShowScenarioChoices();
    }
}