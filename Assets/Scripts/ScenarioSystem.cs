using DG.Tweening;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(NameSetter))]
public class ScenarioSystem : MonoBehaviour
{
    [Header("Parts")]
    public ScreenChanger screenChanger;
    public ScenarioPicker scenarioPicker;
    public EmotionPicker emotionPicker;
    public FaceCapture faceCapture;
    public ProgressTracker progressTracker;

    [Header("Map")]
    public MapSystem mapSystem;
    public GameObject[] hideDuringMap;

    [Header("GameUI")]
    public TextMeshProUGUI[] currentplayerText;
    public GameObject endResult;
    [MinValue(0)]
    public int cameraScreenIndex = 2;

    [Header("Dialogue")]
    public DialogueSystem dialogueSystem;
    public DialogueData endingDialogue;

    [Header("Area Background")]
    public Image backgroundImage;
    public SpriteRenderer backgroundRenderer;
    [MinValue(0)]
    public float backgroundFadeDuration = 0.5f;
    public bool keepBackgroundOnEnd = true;

    [Header("Game Settings")]
    [MinValue(1)]
    public int roundsPerArea = 3;
    [MinValue(1)]
    public int correctGuessesForReward = 2;

    [Header("Datas")]
    public AreasData[] SceneArea;

    private string currentPlayer;
    private bool playerSwitcher = true;
    private int startingPlayer;
    public bool gameStarted = false;

    bool isEmotionCorrect = false;
    bool isPLayerEmotionCorrect = false;

    private int Area_Index;
    private int rounds;
    private int correctGuesses;
    private string areaResultMessage;

    private NameSetter nameSetterScript;

    private void Awake()
    {
        if (screenChanger == null)
            screenChanger = FindAnyObjectByType<ScreenChanger>(FindObjectsInactive.Include);
        if (scenarioPicker == null)
            scenarioPicker = FindAnyObjectByType<ScenarioPicker>(FindObjectsInactive.Include);
        if (emotionPicker == null)
            emotionPicker = FindAnyObjectByType<EmotionPicker>(FindObjectsInactive.Include);
        if (faceCapture == null)
            faceCapture = FindAnyObjectByType<FaceCapture>(FindObjectsInactive.Include);
        if (mapSystem == null)
            mapSystem = FindAnyObjectByType<MapSystem>(FindObjectsInactive.Include);
        if (progressTracker == null)
            progressTracker = FindAnyObjectByType<ProgressTracker>(FindObjectsInactive.Include);
    }

    private void OnEnable()
    {
        if (scenarioPicker != null) scenarioPicker.ScenarioSelected += OnScenarioSelected;
        if (emotionPicker != null) emotionPicker.EmotionPicked += OnEmotionPicked;
    }

    private void OnDisable()
    {
        if (scenarioPicker != null) scenarioPicker.ScenarioSelected -= OnScenarioSelected;
        if (emotionPicker != null) emotionPicker.EmotionPicked -= OnEmotionPicked;
    }

    private void Start()
    {
        nameSetterScript = GetComponent<NameSetter>();

        if (dialogueSystem == null)
            dialogueSystem = FindAnyObjectByType<DialogueSystem>(FindObjectsInactive.Include);

        screenChanger.SetVisible(false);
        if (faceCapture != null)
            faceCapture.SetActive(false);
        else
            Debug.LogWarning("ScenarioSystem: no FaceCapture in the scene. Add the FaceCapture component so the camera can read the player's face.", this);
        UpdateProgress(true);
    }

    void Update()
    {
        if (!gameStarted) return;

        if (faceCapture != null)
        {
            faceCapture.SetActiveIfChanged(RequiresCamera());
            if (scenarioPicker.CurrentScene != null && faceCapture.IsActive) emotionChecker();
        }

        foreach (TextMeshProUGUI text in currentplayerText)
            text.text = nextPlayer();
    }

    bool RequiresCamera()
    {
        return gameStarted && screenChanger.IsVisible && screenChanger.CurrentIndex == cameraScreenIndex;
    }

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
        if (image.sprite == sprite && image.gameObject.activeSelf) return;

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

    public void CurrentArea()
    {
        screenChanger.SetVisible(false);
        if (Area_Index >= SceneArea.Length) return;

        AreasData area = SceneArea[Area_Index];

        ShowAreaBackground(area);

        PlayDialogue(area.areaDialogue, continueButton);
    }

    public void continueButton()
    {
        screenChanger.SetVisible(true);
        GetRandomScenario();
    }

    void nextArea()
    {
        Area_Index++;
        bool finished = Area_Index >= SceneArea.Length;

        if (!finished)
        {
            startingPlayer = Area_Index % 2;
            playerSwitcher = true;
            correctGuesses = 0;
        }

        ShowMap(() =>
        {
            if (finished)
            {
                Debug.LogWarning("You Finished The AREAS!!!");
                EndGame();
            }
            else
            {
                CurrentArea();
            }
        });
    }

    void ShowMap(System.Action onFinished)
    {
        if (mapSystem == null || Area_Index >= mapSystem.NodeCount)
        {
            onFinished?.Invoke();
            return;
        }

        screenChanger.SetVisible(false);
        SetGameUIVisible(false);

        mapSystem.ShowTravel(Area_Index, () =>
        {
            SetGameUIVisible(true);
            onFinished?.Invoke();
        });
    }

    void SetGameUIVisible(bool visible)
    {
        if (hideDuringMap == null) return;
        foreach (GameObject ui in hideDuringMap)
            if (ui != null)
                ui.SetActive(visible);
    }

    void ShowNextUI()
    {
        bool roundFinished = screenChanger.NextScreen();
        Debug.Log($"ScenarioSystem: next screen -> {screenChanger.CurrentIndex} of {screenChanger.ScreenCount} screens. Round finished: {roundFinished}. Rounds done: {(roundFinished ? rounds + 1 : rounds)}/{roundsPerArea}", this);
        if (!roundFinished) return;

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

        UpdateProgress(false);
    }

    void UpdateProgress(bool instant)
    {
        if (progressTracker != null)
            progressTracker.SetProgress(Area_Index, rounds, SceneArea.Length, roundsPerArea, instant);
    }

    private void GetRandomScenario()
    {
        playerSwitcher = true;
        if (Area_Index >= SceneArea.Length) return;
        scenarioPicker.ShowRandomChoices(SceneArea[Area_Index].possbileScenarios);
    }

    public void SelectScene(int index)
    {
        scenarioPicker.SelectScene(index);
    }

    void OnScenarioSelected(ScenarioData scenario)
    {
        playerSwitcher = true;
        ShowNextUI();
    }

    void OnEmotionPicked(Emotions emotion)
    {
        playerSwitcher = false;
        ShowNextUI();
    }

    public bool emotionChecker()
    {
        Emotions faceEmotion = faceCapture != null ? faceCapture.CurrentEmotion : Emotions.None;
        Emotions picked = emotionPicker.PickedEmotion;
        ScenarioData scene = scenarioPicker.CurrentScene;

        if (faceEmotion != Emotions.None && picked != Emotions.None &&
            scene.emotionsRequirment.HasFlag(faceEmotion) && scene.emotionsRequirment.HasFlag(picked))
        {
            return isEmotionCorrect = true;
        }

        return isEmotionCorrect = false;
    }

    public bool playerEmotionChecker()
    {
        return isPLayerEmotionCorrect = faceCapture != null && faceCapture.IsShowing(emotionPicker.PickedEmotion);
    }

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

    string CheckAreaReward()
    {
        string result = $"\nArea finished! Correct guesses: {correctGuesses}/{roundsPerArea}";

        if (correctGuesses < correctGuessesForReward)
            return result + $"\nYou needed {correctGuessesForReward} to get the reward.";

        AreasData area = SceneArea[Area_Index];
        area.onRewardEarned?.Invoke();

        return string.IsNullOrEmpty(area.rewardName) ? result : result + $"\nYou earned: {area.rewardName}!";
    }

    void EndGame()
    {
        gameStarted = false;
        screenChanger.HideAll();
        if (faceCapture != null)
            faceCapture.SetActive(false);

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
}