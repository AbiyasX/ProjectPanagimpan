using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Runs the HOME (START) tutorial in the Tutorial scene.
///
/// It does NOT replace the game systems. It borrows the ones already in the scene:
///   DialogueSystem  - all the talking
///   ScenarioPicker  - the 2 practice memories
///   EmotionPicker   - picking the secret feeling
///   PassSystem      - the "pass the phone" screen
///   FaceCapture     - the camera guess
///   ResultPanel     - showing if the guess was right
///   RewardSystem    - the pasalubong
///   ScenarioSystem  - takes over again at the end (map screen, then the next location)
///
/// How it hooks in: when the players confirm their names, ScenarioSystem is paused and this
/// script plays the tutorial. When the tutorial ends (or is skipped), ScenarioSystem is
/// switched back on and continues with the map.
///
/// Setup: this component sits on the "Tutorial" object in the Tutorial scene, next to TutorialPopup.
/// The pop-up UI is the "Popup" child of that object. Nothing is created by code.
///
/// To change what the characters say, edit the lines in RunTutorial() and PracticeRound().
/// </summary>
public class TutorialDirector : MonoBehaviour
{
    public enum GuessTestMode { UseCamera, AlwaysCorrect, AlwaysWrong }

    [Header("Pop-up UI (in the scene)")]
    [Tooltip("The TutorialPopup in the scene. It shows the Yes/No question and the reminders.")]
    public TutorialPopup popup;

    [Header("Story")]
    [Tooltip("Name shown in the dialogue box when the parent talks.")]
    public string parentName = "PARENT";
    [Tooltip("Optional. Turned on when the parent arrives on the scene.")]
    public GameObject parentCharacter;

    [Header("Practice Prompts")]
    [Tooltip("The 2 memories for practice round 1. Leave empty to use random ones from the Home area.")]
    public ScenarioData[] round1Prompts;
    [Tooltip("The 2 memories for practice round 2. Leave empty to use random ones from the Home area.")]
    public ScenarioData[] round2Prompts;

    [Header("Reward")]
    [Tooltip("Which slot in the RewardSystem's Rewards list is the spaghetti (0 = first). It is always given at the end of the Home scene, right or wrong answers, tutorial or no tutorial.")]
    [Min(0)]
    public int spaghettiRewardIndex = 0;

    [Header("Testing")]
    [Tooltip("For testing without a camera. Set back to Use Camera before building!")]
    public GuessTestMode guessTestMode = GuessTestMode.UseCamera;

    // Systems that already exist in the scene
    ScenarioSystem scenarioSystem;
    NameSetter names;
    DialogueSystem dialogueSystem;
    ScreenChanger screenChanger;
    ScenarioPicker scenarioPicker;
    EmotionPicker emotionPicker;
    FaceCapture faceCapture;
    ResultPanel resultPanel;

    // Buttons that already exist in the scene, found by the method they call
    readonly List<Button> confirmNameButtons = new List<Button>();
    readonly List<Button> checkButtons = new List<Button>();
    readonly List<Button> passButtons = new List<Button>();

    bool running;
    bool scenarioChosen;
    bool emotionChosen;
    bool passClosed;
    bool checkPressed;
    Emotions faceAtCheck;

    // =====================================================================
    //  SETUP
    // =====================================================================

    void Awake()
    {
        // Unity's own "Rendering Debugger" overlay ([Debug Canvas] / Display Stats) can pop up by accident
        // while tapping through the game (its shortcut is Ctrl+Backspace, or a three-finger double tap).
        // Nobody needs it here, so switch it off. Delete this line if you ever want that overlay back.
        UnityEngine.Rendering.DebugManager.instance.enableRuntimeUI = false;
    }

    void Start()
    {
        if (popup == null)
            popup = FindAnyObjectByType<TutorialPopup>(FindObjectsInactive.Include);
        if (popup == null || !popup.IsSetUp)
        {
            Debug.LogError("TutorialDirector: the TutorialPopup is missing from the scene or some of its fields are empty, so the tutorial can't run. The game will play normally.", this);
            return;
        }

        scenarioSystem = FindAnyObjectByType<ScenarioSystem>(FindObjectsInactive.Include);
        if (scenarioSystem == null)
        {
            Debug.LogError("TutorialDirector: no ScenarioSystem in this scene, so the tutorial can't run.", this);
            return;
        }

        foreach (Button button in FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (Calls(button, nameof(NameSetter.ConfirmBothNames))) confirmNameButtons.Add(button);
            if (Calls(button, nameof(ScenarioSystem.scenarioCondition))) checkButtons.Add(button);
            if (Calls(button, nameof(PassSystem.ClosePassScreen))) passButtons.Add(button);
        }

        if (confirmNameButtons.Count == 0)
        {
            Debug.LogError("TutorialDirector: couldn't find the button that calls NameSetter.ConfirmBothNames, so the tutorial can't start. The game will play normally.", this);
            return;
        }

        // Our listeners run AFTER the ones set in the Inspector, so the normal button actions happen first.
        foreach (Button button in confirmNameButtons) button.onClick.AddListener(OnNamesConfirmed);
        foreach (Button button in checkButtons) button.onClick.AddListener(OnCheckPressed);
        foreach (Button button in passButtons) button.onClick.AddListener(OnPassClosed);
    }

    void OnDestroy()
    {
        foreach (Button button in confirmNameButtons) if (button != null) button.onClick.RemoveListener(OnNamesConfirmed);
        foreach (Button button in checkButtons) if (button != null) button.onClick.RemoveListener(OnCheckPressed);
        foreach (Button button in passButtons) if (button != null) button.onClick.RemoveListener(OnPassClosed);
        StopListeningToPickers();
    }

    static bool Calls(Button button, string methodName)
    {
        for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
            if (button.onClick.GetPersistentMethodName(i) == methodName)
                return true;
        return false;
    }

    // =====================================================================
    //  TAKING OVER FROM / HANDING BACK TO THE SCENARIO SYSTEM
    // =====================================================================

    // The players just confirmed their names. NameSetter has already told ScenarioSystem to start
    // the Home area (music, background, Home dialogue). We pause ScenarioSystem here and run the tutorial.
    void OnNamesConfirmed()
    {
        if (running) return;

        names = scenarioSystem.GetComponent<NameSetter>();
        dialogueSystem = scenarioSystem.dialogueSystem;
        screenChanger = scenarioSystem.screenChanger;
        scenarioPicker = scenarioSystem.scenarioPicker;
        emotionPicker = scenarioSystem.emotionPicker;
        faceCapture = scenarioSystem.faceCapture;
        resultPanel = scenarioSystem.resultPanel;

        if (names == null || dialogueSystem == null || screenChanger == null || scenarioPicker == null || emotionPicker == null)
        {
            Debug.LogError("TutorialDirector: ScenarioSystem is missing one of its parts (NameSetter, DialogueSystem, ScreenChanger, ScenarioPicker or EmotionPicker). Skipping the tutorial, the game will play normally.", this);
            return;
        }

        running = true;

        // Pause the real game. Turning the component off also stops it from reacting to the pickers.
        scenarioSystem.gameStarted = false;
        scenarioSystem.enabled = false;
        screenChanger.SetVisible(false);
        SetGameCheckButton(false);

        scenarioPicker.ScenarioSelected += OnScenarioChosen;
        emotionPicker.EmotionPicked += OnEmotionChosen;

        StartCoroutine(RunTutorial());
    }

    // The tutorial is over: switch ScenarioSystem back on and let it show the map to the next location.
    void StartRealGame()
    {
        StopListeningToPickers();
        SetGameCheckButton(true);
        running = false;

        scenarioSystem.enabled = true;
        scenarioSystem.gameStarted = true;

        // Home is finished, so the kite meter starts the journey at "area 2, round 1".
        ShowProgress(1, 0);

        // ScenarioSystem has no public "go to the next area" method, so we call its private nextArea().
        // It plays the cloud transition, shows the map travelling to the next location, then starts that area.
        // (If a public method is ever added to ScenarioSystem, call that here instead.)
        MethodInfo nextArea = typeof(ScenarioSystem).GetMethod("nextArea", BindingFlags.Instance | BindingFlags.NonPublic);
        if (nextArea != null)
        {
            nextArea.Invoke(scenarioSystem, null);
        }
        else
        {
            Debug.LogError("TutorialDirector: ScenarioSystem.nextArea() was renamed or removed, so the map can't be shown. Playing the Home area normally instead.", this);
            scenarioSystem.CurrentArea();
        }
    }

    // While the tutorial runs, the camera screen's "check" button must not run the real game's scoring.
    void SetGameCheckButton(bool on)
    {
        UnityEventCallState state = on ? UnityEventCallState.RuntimeOnly : UnityEventCallState.Off;

        foreach (Button button in checkButtons)
        {
            if (button == null) continue;
            for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
                if (button.onClick.GetPersistentMethodName(i) == nameof(ScenarioSystem.scenarioCondition))
                    button.onClick.SetPersistentListenerState(i, state);
        }
    }

    // ---------------------------------------------------------------------
    //  Camera screen: always start on the "open camera" step
    // ---------------------------------------------------------------------
    GameObject cameraPanel;      // the panel with the live camera + the check button
    Button openCameraButton;     // the button that opens that panel
    bool cameraPartsSearched;
    float cameraScreenShownAt;

    // Shows the game's camera screen with the camera panel closed, so the guesser reads the
    // instructions and presses the "open camera" button themselves.
    IEnumerator ShowCameraScreen()
    {
        FindCameraParts();

        bool wasAlreadyOpen = cameraPanel != null && cameraPanel.activeSelf;
        if (cameraPanel != null) cameraPanel.SetActive(false);

        // The open button ignores taps for a moment, so a leftover tap from the pop-up can't press it.
        if (openCameraButton != null) openCameraButton.enabled = false;

        screenChanger.NextScreen();
        screenChanger.SetVisible(true);
        if (faceCapture != null) faceCapture.SetActive(true);
        cameraScreenShownAt = Time.realtimeSinceStartup;

        // TEMPORARY (for finding the round-1 bug): say what state the camera screen started in.
        Debug.Log($"[Tutorial debug] Camera screen shown. Camera panel found: {cameraPanel != null}, was already open: {wasAlreadyOpen}, open button found: {openCameraButton != null}.", this);

        yield return new WaitForSecondsRealtime(0.5f);
        if (openCameraButton != null) openCameraButton.enabled = true;
    }

    // Finds the camera panel and its open button from how the scene's buttons are wired:
    //  - the camera panel is the object the check button sits inside and switches off when pressed
    //  - the open button is the button outside that panel that switches it on
    void FindCameraParts()
    {
        if (cameraPartsSearched) return;
        cameraPartsSearched = true;

        foreach (Button check in checkButtons)
        {
            if (check == null) continue;
            for (int i = 0; i < check.onClick.GetPersistentEventCount(); i++)
            {
                GameObject target = check.onClick.GetPersistentTarget(i) as GameObject;
                if (target == null || check.onClick.GetPersistentMethodName(i) != nameof(GameObject.SetActive)) continue;
                if (!check.transform.IsChildOf(target.transform) || IsScreen(target)) continue;
                cameraPanel = target;
            }
        }
        if (cameraPanel == null) return;

        foreach (Button button in FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (button.transform.IsChildOf(cameraPanel.transform)) continue;
            for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
                if (button.onClick.GetPersistentTarget(i) == cameraPanel && button.onClick.GetPersistentMethodName(i) == nameof(GameObject.SetActive))
                    openCameraButton = button;
        }

        // TEMPORARY (for finding the round-1 bug): report whenever the camera panel gets opened, and by what.
        cameraPanel.AddComponent<CameraPanelProbe>().director = this;
    }

    bool IsScreen(GameObject candidate)
    {
        if (screenChanger.screens == null) return false;
        foreach (GameObject screen in screenChanger.screens)
            if (screen == candidate)
                return true;
        return false;
    }

    // TEMPORARY: writes a line (with a stack trace) to the Console each time the camera panel is switched on.
    class CameraPanelProbe : MonoBehaviour
    {
        public TutorialDirector director;

        void OnEnable()
        {
            if (director == null) return;
            float since = Time.realtimeSinceStartup - director.cameraScreenShownAt;
            UnityEngine.EventSystems.EventSystem events = UnityEngine.EventSystems.EventSystem.current;
            string selected = events != null && events.currentSelectedGameObject != null ? events.currentSelectedGameObject.name : "nothing";
            Debug.Log($"[Tutorial debug] Camera panel OPENED {since:F2}s after the camera screen was shown. Selected UI object: {selected}. Frame {Time.frameCount}.", this);
        }
    }

    void StopListeningToPickers()
    {
        if (scenarioPicker != null) scenarioPicker.ScenarioSelected -= OnScenarioChosen;
        if (emotionPicker != null) emotionPicker.EmotionPicked -= OnEmotionChosen;
    }

    void OnScenarioChosen(ScenarioData scenario) => scenarioChosen = true;
    void OnEmotionChosen(Emotions emotion) => emotionChosen = true;
    void OnPassClosed() => passClosed = true;

    void OnCheckPressed()
    {
        if (!running) return;
        // Read the face right when the button is pressed, before the camera is switched off.
        faceAtCheck = faceCapture != null ? faceCapture.CurrentEmotion : Emotions.None;
        checkPressed = true;
    }

    // =====================================================================
    //  THE SCENE  (edit the lines here)
    //  {player1} and {player2} are replaced with the names the kids typed.
    // =====================================================================
    IEnumerator RunTutorial()
    {
        AreasData home = HomeArea();

        // NARRATOR + the kids getting ready: this is the Home area's own dialogue (HomeDialogData).
        if (home != null && home.areaDialogue != null)
            yield return Play(home.areaDialogue);

        // NOTE: The parent arrives on the scene here.
        if (parentCharacter != null) parentCharacter.SetActive(true);

        yield return Say(
            Parent("Wait! Before you guys go."),
            Parent("I just want to make sure you guys remember how to get to school!"));

        // NOTE: Prompt choice to ask if a tutorial is wanted.
        bool wantsTutorial = false;
        yield return popup.Ask("Do you want to learn how to play?", "Yes, show us!", "No, we're ready!", answer => wantsTutorial = answer);

        if (wantsTutorial)
        {
            yield return Say(
                Parent("Alright, let me show you the way."),
                Parent("On the way, you'll pass places full of memories. At each one, you'll take turns sharing how you felt."),
                Parent("And every time you share your feelings with each other, it charges up your kite!"));

            GetPracticePrompts(home, out ScenarioData[] prompts1, out ScenarioData[] prompts2);

            // Practice round 1 of 2: kid 1 shares, kid 2 guesses.
            yield return PracticeRound(1, 0, prompts1);

            yield return Say(Parent("Now let's switch! This time, {player2} picks the memory and the feeling, and {player1} guesses."));

            // Practice round 2 of 2: kid 2 shares, kid 1 guesses.
            yield return PracticeRound(2, 1, prompts2);

            yield return Say(
                Parent("Great job, both of you! On the real journey, you'll keep switching turns at every stop. The more you share and understand each other, the more your kite charges up, and the more gifts you can bring!"),
                Kid2("Let's try to get a lot!"),
                Kid1("Wow Nanay, you know a lot! Thank you!"),
                Kid2("Now we won't get lost!"),
                Parent("Of course my kids, now you go out and be safe!"));
        }
        else
        {
            yield return Say(Parent("Alright, be safe my kids!"));
        }

        yield return Say(
            Kid1("Thanks Nanay!"),
            Kid2("Thank you Tita!"));

        // The kids always leave home with the spaghetti (shown with the game's own reward panel).
        yield return GiveSpaghetti();

        // NOTE: Pop-up to remind kids they'll be guided throughout the game.
        yield return popup.Show("Don't worry!\nYou'll be guided the whole way through the game.", "Let's go!");

        // NOTE: Game Start - show map screen to next location.
        StartRealGame();
    }

    // =====================================================================
    //  ONE PRACTICE ROUND  (used twice, with the kids swapped)
    //  sharerIndex: 0 = player 1 shares, 1 = player 2 shares
    // =====================================================================
    IEnumerator PracticeRound(int roundNumber, int sharerIndex, ScenarioData[] prompts)
    {
        string sharer = PlayerName(sharerIndex);
        string guesser = PlayerName(1 - sharerIndex);

        if (prompts == null || prompts.Length == 0)
        {
            Debug.LogWarning("TutorialDirector: no practice prompts (the Home area has no scenarios), skipping this practice round.", this);
            yield break;
        }

        if (roundNumber == 1)
            yield return Say(Parent($"Let's practice! {sharer}, pick a memory, then pick how it made you feel. Shh, keep it a secret!"));
        else
            yield return Say(Parent($"{sharer}, your turn to pick a memory and a feeling. {guesser}, get ready to guess!"));

        // NOTE: Remind the guesser to look away.
        yield return popup.Show($"Practice {roundNumber} of 2\n\n{guesser}, look away!\nNo peeking while {sharer} picks.", "OK!");

        // ---- The sharer picks a memory (the game's own scenario screen, with 2 practice prompts) ----
        ShowCurrentPlayer(sharer);
        screenChanger.SetVisible(false);
        while (screenChanger.CurrentIndex != 0) screenChanger.NextScreen();

        scenarioChosen = false;
        scenarioPicker.ShowRandomChoices(prompts);
        screenChanger.SetVisible(true);
        yield return new WaitUntil(() => scenarioChosen);

        // ---- The sharer picks the secret feeling (the game's own emotion screen) ----
        emotionChosen = false;
        screenChanger.NextScreen();
        yield return new WaitUntil(() => emotionChosen);
        Emotions pickedEmotion = emotionPicker.PickedEmotion;

        // The emotion screen's confirm button opens the pass screen by itself.
        // Close it for now so the parent can talk first.
        screenChanger.SetVisible(false);
        if (PassSystem.Instance != null) PassSystem.Instance.ClosePassScreen();

        yield return Say(Parent($"Now pass the phone to {guesser}. Talk about the memory together, but don't say the feeling!"));

        // ---- Pass the phone (the game's own pass screen) ----
        if (PassSystem.Instance != null && passButtons.Count > 0)
        {
            passClosed = false;
            PassSystem.Instance.OpenPassScreen();
            yield return new WaitUntil(() => passClosed);
        }

        ShowCurrentPlayer(guesser);
        yield return Say(Parent($"{guesser}, what do you think {sharer} felt? Show it with your face!"));

        // NOTE: Camera opens. Show tips.
        yield return popup.Show("Camera tips\n\n- Look at the camera\n- Stay in the light\n- Hold still", "Ready!");

        // ---- The guesser shows the feeling with their face (the game's own camera screen) ----
        bool correct = false;
        Emotions faceEmotion = Emotions.None;

        if (checkButtons.Count > 0)
        {
            checkPressed = false;
            yield return ShowCameraScreen();

            yield return new WaitUntil(() => checkPressed);

            faceEmotion = faceAtCheck;
            if (faceCapture != null) faceCapture.SetActive(false);
        }
        else
        {
            Debug.LogWarning("TutorialDirector: couldn't find the camera screen's check button (the one calling ScenarioSystem.scenarioCondition), skipping the camera step.", this);
        }

        correct = faceEmotion != Emotions.None && faceEmotion == pickedEmotion;

        if (guessTestMode == GuessTestMode.AlwaysCorrect)
        {
            faceEmotion = pickedEmotion;
            correct = true;
        }
        else if (guessTestMode == GuessTestMode.AlwaysWrong)
        {
            if (faceEmotion == pickedEmotion) faceEmotion = Emotions.None;
            correct = false;
        }

        // ---- Show both feelings side by side (the game's own result panel) ----
        if (resultPanel != null)
        {
            Emotions p1Emotion = sharerIndex == 0 ? pickedEmotion : faceEmotion;
            Emotions p2Emotion = sharerIndex == 1 ? pickedEmotion : faceEmotion;

            bool resultClosed = false;
            resultPanel.ShowResult(names.p1Name, p1Emotion, names.p2Name, p2Emotion, correct, () => resultClosed = true);
            yield return new WaitUntil(() => resultClosed);
        }

        // Back to the first screen (hidden), ready for the next round.
        screenChanger.SetVisible(false);
        while (screenChanger.CurrentIndex != 0) screenChanger.NextScreen();

        if (correct)
        {
            yield return Say(Parent("You got it! Your kite is charging up, and you found pasalubong to bring to your friends!"));
        }
        else
        {
            yield return Say(
                Parent($"That's okay! {sharer}, tell {guesser} how you really felt, and why."),
                Parent("Sometimes we feel differently about the same thing, and that's okay."));
        }

        // Sharing charges the kite, right or wrong.
        ShowProgress(0, roundNumber);
    }

    // =====================================================================
    //  HELPERS
    // =====================================================================

    DialogueText Parent(string text) => Line(SpeakerType.Custom, parentName, text);
    DialogueText Kid1(string text) => Line(SpeakerType.Player1, "", text);
    DialogueText Kid2(string text) => Line(SpeakerType.Player2, "", text);

    static DialogueText Line(SpeakerType type, string speaker, string text)
    {
        return new DialogueText { speakerType = type, dialogueSpeaker = speaker, dialogueText = text };
    }

    // Plays lines through the game's DialogueSystem and waits until the last one is tapped away.
    IEnumerator Say(params DialogueText[] lines)
    {
        DialogueData data = ScriptableObject.CreateInstance<DialogueData>();
        data.dialogue_Texts = lines;
        yield return Play(data);
        Destroy(data);
    }

    IEnumerator Play(DialogueData data)
    {
        dialogueSystem.SetTextReplacement("{player1}", names.p1Name);
        dialogueSystem.SetTextReplacement("{player2}", names.p2Name);

        bool finished = false;
        dialogueSystem.PlayDialogue(data, () => finished = true);
        yield return new WaitUntil(() => finished);
    }

    string PlayerName(int index) => index == 0 ? names.p1Name : names.p2Name;

    AreasData HomeArea()
    {
        AreasData[] areas = scenarioSystem.SceneArea;
        return areas != null && areas.Length > 0 ? areas[0] : null;
    }

    // The "whose turn is it" labels on the game screens (ScenarioSystem normally fills these in).
    void ShowCurrentPlayer(string playerName)
    {
        if (scenarioSystem.currentplayerText == null) return;
        foreach (TextMeshProUGUI text in scenarioSystem.currentplayerText)
            if (text != null)
                text.text = playerName;
    }

    void ShowProgress(int areaIndex, int roundsDone)
    {
        if (scenarioSystem.progressTracker == null || scenarioSystem.SceneArea == null) return;
        scenarioSystem.progressTracker.SetProgress(areaIndex, roundsDone, scenarioSystem.SceneArea.Length, scenarioSystem.roundsPerArea);
    }

    // Gives the spaghetti through the game's RewardSystem, which shows it on the RewardPanel already in the scene
    // (tap to reveal). Waits until that panel has closed. Nothing here depends on the practice answers.
    IEnumerator GiveSpaghetti()
    {
        RewardSystem rewards = RewardSystem.Instance;

        if (rewards == null)
        {
            // RewardSystem only registers itself when its object is ON. If it is switched off in the scene,
            // switch it on here, otherwise no reward can ever be given.
            rewards = FindAnyObjectByType<RewardSystem>(FindObjectsInactive.Include);
            if (rewards == null)
            {
                Debug.LogError("TutorialDirector: there is no RewardSystem in this scene, so the spaghetti can't be given.", this);
                yield break;
            }

            Debug.LogWarning($"TutorialDirector: the '{rewards.gameObject.name}' object was switched off in the scene, so it was switched on to give the spaghetti. Tick it on in the Hierarchy to make this permanent.", rewards);
            for (Transform t = rewards.transform; t != null; t = t.parent)
                if (!t.gameObject.activeSelf)
                    t.gameObject.SetActive(true);

            if (RewardSystem.Instance == null)
            {
                Debug.LogError("TutorialDirector: the RewardSystem could not be started, so the spaghetti can't be given.", rewards);
                yield break;
            }
            rewards = RewardSystem.Instance;
        }

        if (spaghettiRewardIndex >= rewards.rewards.Count)
        {
            Debug.LogError($"TutorialDirector: Spaghetti Reward Index is {spaghettiRewardIndex}, but the RewardSystem only has {rewards.rewards.Count} reward(s).", this);
            yield break;
        }

        // Collect() marks it as collected and opens the reward panel. It returns false if it was already collected.
        if (!rewards.Collect(spaghettiRewardIndex)) yield break;

        RewardPanelScript panel = rewards.rewardPanel;
        if (panel != null)
            yield return new WaitWhile(() => panel.IsShowing);
    }

    // 2 prompts for each round. Uses the Inspector lists if filled in, otherwise random Home scenarios
    // (different ones for each round when the Home area has at least 4).
    void GetPracticePrompts(AreasData home, out ScenarioData[] prompts1, out ScenarioData[] prompts2)
    {
        List<ScenarioData> pool = new List<ScenarioData>();
        if (home != null && home.possbileScenarios != null)
            foreach (ScenarioData scenario in home.possbileScenarios)
                if (scenario != null)
                    pool.Add(scenario);

        for (int i = pool.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (pool[i], pool[j]) = (pool[j], pool[i]);
        }

        prompts1 = round1Prompts != null && round1Prompts.Length > 0 ? round1Prompts : TakeTwo(pool, 0);
        prompts2 = round2Prompts != null && round2Prompts.Length > 0 ? round2Prompts : TakeTwo(pool, 2);
    }

    static ScenarioData[] TakeTwo(List<ScenarioData> pool, int start)
    {
        if (pool.Count == 0) return new ScenarioData[0];
        if (pool.Count == 1) return new[] { pool[0] };
        return new[] { pool[start % pool.Count], pool[(start + 1) % pool.Count] };
    }
}
