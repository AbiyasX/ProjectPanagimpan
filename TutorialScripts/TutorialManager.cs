using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

/// <summary>
/// Runs the whole HOME (START) tutorial scene from top to bottom.
/// Each line of the scene script is one "yield return" below, so it's easy to edit.
/// </summary>
public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance { get; private set; }

    [Header("References")]
    public TutorialUI ui;
    public GameObject parentCharacter;     // parent sprite, hidden until "the parent arrives on the scene"

    [Header("Names (can be overwritten from your save data in Start)")]
    public string familyName = "Santos";
    public string kid1Name = "Kid 1";
    public string kid2Name = "Kid 2";
    public string parentName = "Nanay";

    [Header("Practice Prompts")]
    public List<string> round1Memories = new List<string> { "Your first day of school", "Playing outside in the rain" };
    public List<string> round2Memories = new List<string> { "Your last birthday party", "Losing your favorite toy" };

    [Tooltip("Must match the emotion names your camera / face detection returns.")]
    public List<string> emotions = new List<string> { "Happy", "Sad", "Angry", "Scared", "Surprised" };

    [Header("Camera Guess")]
    [Tooltip("ON = pick the guess with buttons (for testing in the Editor). OFF = use your camera script.")]
    public bool useButtonFallbackForGuess = true;
    public UnityEvent onOpenCamera;        // hook your camera script's "open" method here
    public UnityEvent onCloseCamera;       // hook your camera script's "close" method here

    [Header("Rewards")]
    public UnityEvent onKiteCharged;       // hook your kite charge / pasalubong reward here

    [Header("After Tutorial")]
    public string mapSceneName = "Map";

    string receivedGuess;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        // Example: if your game stores names somewhere else, load them here.
        // kid1Name = GameData.Kid1Name;
        // kid2Name = GameData.Kid2Name;

        if (parentCharacter != null) parentCharacter.SetActive(false);
        StartCoroutine(RunTutorial());
    }

    // Shortcut so the scene below reads like the script
    IEnumerator Say(string speaker, string line) => ui.Say(speaker, line);

    // =====================================================================
    //  THE SCENE
    // =====================================================================
    IEnumerator RunTutorial()
    {
        string K1 = kid1Name, K2 = kid2Name, P = parentName;

        yield return Say("", $"At the {familyName} home, the two friends are getting ready to go to school.");
        yield return Say(K1, "Do you have everything you need?");
        yield return Say(K2, "I do! How about you?");
        yield return Say(K1, "I'm ready!");
        yield return Say(P,  $"{K1}! Don't forget to bring the spaghetti I cooked for you and your friends!");
        yield return Say(K1, "Oh that's right!");
        yield return Say(K1, "We need that so we have something to bring to the festival!");
        yield return Say(K2, "Maybe on the way, we can get some more things!");
        yield return Say(K1, "Yeah! Our friends would love that.");

        // NOTE: The parent arrives on the scene here.
        if (parentCharacter != null) parentCharacter.SetActive(true);

        yield return Say(P, "Wait! Before you guys go.");
        yield return Say(P, "I just want to make sure you guys remember how to get to school!");

        // NOTE: Prompt choice to ask if a tutorial is wanted.
        bool wantsTutorial = false;
        yield return ui.AskYesNo("Do you want to learn how to play?", answer => wantsTutorial = answer);

        if (!wantsTutorial)
        {
            yield return Say(P,  "Alright, be safe my kids!");
            yield return Say(K1, "Thanks Nanay!");
            yield return Say(K2, "Thank you Tita!");
            yield return FinishTutorial();
            yield break;
        }

        // ---------- YES: the tutorial ----------
        yield return Say(P, "Alright, let me show you the way.");
        yield return Say(P, "On the way, you'll pass places full of memories. At each one, you'll take turns sharing how you felt.");
        yield return Say(P, "And every time you share your feelings with each other, it charges up your kite!");

        // Practice round 1 of 2: Kid 1 shares, Kid 2 guesses
        yield return PracticeRound(K1, K2, round1Memories, isFirstRound: true);

        yield return Say(P, $"Now let's switch! This time, {K2} picks the memory and the feeling, and {K1} guesses.");

        // Practice round 2 of 2: Kid 2 shares, Kid 1 guesses
        yield return PracticeRound(K2, K1, round2Memories, isFirstRound: false);

        yield return Say(P,  "Great job, both of you! On the real journey, you'll keep switching turns at every stop. The more you share and understand each other, the more your kite charges up, and the more gifts you can bring!");
        yield return Say(K2, "Let's try to get a lot!");
        yield return Say(K1, "Wow Nanay, you know a lot! Thank you!");
        yield return Say(K2, "Now we won't get lost!");
        yield return Say(P,  "Of course my kids, now you go out and be safe!");
        yield return Say(K1, "Thanks Nanay!");
        yield return Say(K2, "Thank you Tita!");

        yield return FinishTutorial();
    }

    // =====================================================================
    //  ONE PRACTICE ROUND (used twice, with the names swapped)
    // =====================================================================
    IEnumerator PracticeRound(string sharer, string guesser, List<string> memories, bool isFirstRound)
    {
        string P = parentName;

        if (isFirstRound)
            yield return Say(P, $"Let's practice! {sharer}, pick a memory, then pick how it made you feel. Shh, keep it a secret!");
        else
            yield return Say(P, $"{sharer}, your turn to pick a memory and a feeling. {guesser}, get ready to guess!");

        // Remind the guesser to look away
        yield return ui.ShowPopup($"{guesser}, look away! No peeking!");

        // Sharer picks a memory, then a feeling (secret)
        int memoryIndex = -1;
        yield return ui.PickOption($"{sharer}, pick a memory", memories, i => memoryIndex = i);
        string memory = memories[memoryIndex];

        int feelingIndex = -1;
        yield return ui.PickOption($"How did \"{memory}\" make you feel?", emotions, i => feelingIndex = i);
        string secretFeeling = emotions[feelingIndex];

        // Pass the phone
        yield return Say(P, $"Now pass the phone to {guesser}. Talk about the memory together, but don't say the feeling!");
        yield return ui.ShowPopup($"Memory:\n{memory}\n\nTalk about it together!");
        yield return Say(P, $"{guesser}, what do you think {sharer} felt? Show it with your face!");

        // Camera tips, then the guess
        yield return ui.ShowPopup("Camera tips:\n- Look at the camera\n- Stay in the light\n- Hold still");

        string guess = null;
        yield return GetFaceGuess(g => guess = g);

        bool correct = string.Equals(guess, secretFeeling, StringComparison.OrdinalIgnoreCase);
        if (correct)
        {
            onKiteCharged.Invoke();
            yield return Say(P, "You got it! Your kite is charging up, and you found pasalubong to bring to your friends!");
        }
        else
        {
            yield return Say(P, $"That's okay! {sharer}, tell {guesser} how you really felt, and why.");
            yield return ui.ShowPopup($"{sharer} really felt: {secretFeeling}");
            yield return Say(P, "Sometimes we feel differently about the same thing, and that's okay.");
        }
    }

    // =====================================================================
    //  CAMERA GUESS
    // =====================================================================
    IEnumerator GetFaceGuess(Action<string> onGuess)
    {
        if (useButtonFallbackForGuess)
        {
            int i = -1;
            yield return ui.PickOption("(Test mode) Which face did you make?", emotions, x => i = x);
            onGuess(emotions[i]);
            yield break;
        }

        receivedGuess = null;
        ui.HideAll();
        onOpenCamera.Invoke();
        yield return new WaitUntil(() => receivedGuess != null);
        onCloseCamera.Invoke();
        onGuess(receivedGuess);
    }

    /// <summary>
    /// Your camera / face detection script calls this when it detects an emotion:
    /// TutorialManager.Instance.SubmitFaceGuess("Happy");
    /// </summary>
    public void SubmitFaceGuess(string emotion)
    {
        receivedGuess = emotion;
    }

    // =====================================================================
    //  END
    // =====================================================================
    IEnumerator FinishTutorial()
    {
        // NOTE: Pop-up reminding kids they'll be guided throughout the game.
        yield return ui.ShowPopup("Don't worry! You'll be guided throughout the whole game.");

        // NOTE: Game Start - show map screen to next location.
        SceneManager.LoadScene(mapSceneName);
    }
}
