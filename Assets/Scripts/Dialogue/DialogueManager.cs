using System;
using UnityEngine;
using UnityEngine.InputSystem;

// Runs dialogue: owns the runner and history, handles input, rollback, skip and auto, and saves/restores the dialogue.
// It also draws the stage behind the box, plays chapter title cards, line transitions and sounds, keeps the music
// and ambience in step with the story, and ends the game.
[DefaultExecutionOrder(-100)]
public class DialogueManager : MonoBehaviour, ISaveable
{
    public static DialogueManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private Dialogue_DatabaseSO database;
    [SerializeField] private StoryManager storyManager;
    [SerializeField] private SaveManager saveManager;
    [SerializeField] private GameManager gameManager;
    [SerializeField] private AudioManager audioManager;
    [SerializeField] private UI ui;

    [Header("Skip & Auto")]
    [Tooltip("Seconds between lines while skipping.")]
    [SerializeField] private float skipInterval = .05f;
    [Tooltip("Auto mode waits this long after a line finishes typing...")]
    [SerializeField] private float autoBaseDelay = 1.2f;
    [Tooltip("...plus this much per character, so longer lines stay up longer.")]
    [SerializeField] private float autoDelayPerCharacter = .03f;

    [Header("Stage Transitions")]
    [SerializeField] private Color flashColor = new Color(1f, 1f, 1f, .9f);
    [SerializeField] private float flashDuration = .6f;

    private DialogueRunner runner;
    private DialogueHistory history;
    private DialogueSaveState pendingResume;
    private UI_Dialogue dialogueUI;

    private bool autoMode;
    private bool skipMode;
    private float skipTimer;
    private float autoTimer;
    private bool pendingAutosave;
    private bool inTransition; // A title card or fade is between the player and the next screen
    private int inputBlockedUntilFrame = -1;
    private int lastEndFrame = -1;

    private InputAction advanceAction;
    private InputAction rollBackAction;
    private InputAction rollForwardAction;
    private InputAction historyAction;
    private InputAction skipAction;
    private InputAction toggleAutoAction;

    public bool IsActive => runner != null && runner.IsActive;
    public bool IsRollingBack => history.IsRollingBack;
    public bool AutoMode => autoMode;
    public bool IsSkipping => skipMode || GameInput.IsPressed(skipAction);
    public DialogueHistory History => history;

    public event Action OnDialogueStarted;
    public event Action OnDialogueEnded;

    private void Awake()
    {
        Instance = this;

        history = new DialogueHistory();
        runner = new DialogueRunner(storyManager.State, history, FindConversation);
        runner.Ended += HandleRunnerEnded;
        runner.ConversationEntered += HandleConversationEntered;

        dialogueUI = ui.dialogueUI;
        dialogueUI.Initialize(this);
        ui.OnMenusChanged += HandleMenusChanged;

        advanceAction = GameInput.Find("Dialogue/Advance");
        rollBackAction = GameInput.Find("Dialogue/RollBack");
        rollForwardAction = GameInput.Find("Dialogue/RollForward");
        historyAction = GameInput.Find("Dialogue/History");
        skipAction = GameInput.Find("Dialogue/Skip");
        toggleAutoAction = GameInput.Find("Dialogue/ToggleAuto");
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        if (ui != null)
            ui.OnMenusChanged -= HandleMenusChanged;
    }

    public Dialogue_ConversationSO FindConversation(string saveID) => database != null ? database.GetConversation(saveID) : null;
    public Dialogue_SpeakerSO FindSpeaker(string saveID) => database != null ? database.GetSpeaker(saveID) : null;
    public Stage_ImageSO FindImage(string saveID) => database != null ? database.GetImage(saveID) : null;
    public Audio_SoundSO FindSound(string saveID) => database != null ? database.GetSound(saveID) : null;

    public bool StartDialogue(Dialogue_ConversationSO conversation)
    {
        if (conversation == null || IsActive)
            return false;

        // The key press that closed the last dialogue must not open the next one
        if (Time.frameCount == lastEndFrame)
            return false;

        string chapterBefore = storyManager.State.ChapterID;

        if (!runner.Start(conversation))
            return false;

        BeginPresentation(true, chapterBefore);
        return true;
    }

    #region Player actions (input, click, quick menu)

    public void Advance()
    {
        if (!CanTakeInput())
            return;

        if (history.IsRollingBack)
        {
            RollForward();
            return;
        }

        if (dialogueUI.IsTyping)
        {
            dialogueUI.CompleteTyping();
            return;
        }

        if (runner.Phase == DialoguePhase.Choices)
            return; // Only a choice moves on from here

        StepRunner();
    }

    public void RollBack()
    {
        if (!CanTakeInput() || !history.StepBack())
            return;

        skipMode = false;
        autoMode = false;
        RenderHistoryEntry(history.ViewIndex);
    }

    public void RollForward()
    {
        if (!CanTakeInput() || !history.StepForward())
            return;

        if (history.IsRollingBack)
            RenderHistoryEntry(history.ViewIndex);
        else
            RenderPresent(false, false);
    }

    public void ToggleAuto()
    {
        if (!CanTakeInput())
            return;

        ReturnToPresent();
        autoMode = !autoMode;
        autoTimer = 0f;

        if (autoMode)
            skipMode = false;
    }

    public void ToggleSkip()
    {
        if (!CanTakeInput())
            return;

        ReturnToPresent();
        skipMode = !skipMode;
        skipTimer = 0f;

        if (skipMode)
            autoMode = false;
    }

    public void OpenBacklog()
    {
        if (ui.IsMenuOpen(ui.backlogUI.gameObject))
            return;

        ui.backlogUI.Open(DialogueScreen.BuildBacklog(history.Entries, FindConversation));
    }

    public void OpenSaveMenu()
    {
        if (!ui.IsModalOpen && saveManager.CanSave)
            saveManager.CaptureThenOpen(() => ui.saveLoadMenu.Open(SaveLoadMode.Save));
    }

    public void OpenLoadMenu()
    {
        if (!ui.IsModalOpen)
            saveManager.CaptureThenOpen(() => ui.saveLoadMenu.Open(SaveLoadMode.Load));
    }

    public void QuickSave()
    {
        if (!ui.IsModalOpen)
            saveManager.QuickSave();
    }

    public void QuickLoad()
    {
        if (!ui.IsModalOpen)
            saveManager.QuickLoad();
    }

    public void OpenPauseMenu() => gameManager.OpenPauseMenu();

    #endregion

    private void Update()
    {
        if (!IsActive || !CanTakeInput())
            return;

        // Autosave once the conversation's first line is fully on screen, so the thumbnail shows it
        if (pendingAutosave && !dialogueUI.IsTyping && !history.IsRollingBack && saveManager.CanSave)
        {
            pendingAutosave = false;
            saveManager.Autosave();
            return;
        }

        if (GameInput.WasPressed(historyAction))
        {
            OpenBacklog();
            return;
        }

        if (GameInput.WasPressed(rollBackAction))
        {
            RollBack();
            return;
        }

        if (GameInput.WasPressed(rollForwardAction))
        {
            RollForward();
            return;
        }

        if (GameInput.WasPressed(toggleAutoAction))
            ToggleAuto();

        bool skipping = IsSkipping;
        dialogueUI.SetModeIndicators(autoMode, skipping);

        if (history.IsRollingBack)
        {
            if (skipping)
                ReturnToPresent(); // Skip means "take me back to the story"
            else
            {
                if (GameInput.WasPressed(advanceAction))
                    RollForward();

                return;
            }
        }

        if (GameInput.WasPressed(advanceAction))
        {
            Advance();
            return;
        }

        if (skipping)
            UpdateSkip();
        else if (autoMode)
            UpdateAuto();
    }

    private void UpdateSkip()
    {
        if (runner.Phase == DialoguePhase.Choices)
        {
            dialogueUI.CompleteTyping();
            skipMode = false; // Skipping always stops at choices
            return;
        }

        skipTimer += Time.deltaTime;
        if (skipTimer < skipInterval)
            return;

        dialogueUI.CompleteTyping();
        StepRunner();
    }

    private void UpdateAuto()
    {
        if (dialogueUI.IsTyping || runner.Phase == DialoguePhase.Choices)
        {
            autoTimer = 0f;
            return;
        }

        autoTimer += Time.deltaTime;

        if (autoTimer >= autoBaseDelay + autoDelayPerCharacter * dialogueUI.CharacterCount)
            StepRunner();
    }

    private void StepRunner()
    {
        skipTimer = 0f;
        autoTimer = 0f;

        HistoryEntry before = history.Present;
        string chapterBefore = storyManager.State.ChapterID;
        runner.Advance();

        if (runner.IsActive)
            PresentNew(before, chapterBefore);
    }

    // Choices that attach to the text already on screen (e.g. under a reaction) shouldn't type that text out again
    private bool HasNewText(HistoryEntry before)
    {
        HistoryEntry present = history.Present;
        return present != null && present != before && present.type != HistoryEntryType.ChoiceOnly;
    }

    private void OnChoiceSelected(string choiceID)
    {
        if (!CanTakeInput() || history.IsRollingBack || runner.Phase != DialoguePhase.Choices)
            return;

        HistoryEntry before = history.Present;
        string chapterBefore = storyManager.State.ChapterID;

        if (!runner.Choose(choiceID))
            return;

        // The Enter/South press that picked the choice must not also advance
        BlockInputThisFrame();
        autoTimer = 0f;

        if (runner.IsActive)
            PresentNew(before, chapterBefore);
    }

    // live = the dialogue just started (title cards and transitions play). Otherwise it was restored from a save.
    private void BeginPresentation(bool live, string chapterBefore)
    {
        skipMode = false;
        skipTimer = 0f;
        autoTimer = 0f;
        BlockInputThisFrame();

        ui.stage.Show();
        dialogueUI.SetModeIndicators(autoMode, false);
        OnDialogueStarted?.Invoke();

        if (live)
            PresentNew(null, chapterBefore);
        else
            RenderPresent(false, false);
    }

    // Shows the screen the runner just moved to. Entering a new chapter plays its title card first,
    // and a line can ask to fade through black or flash, and play a sound.
    private void PresentNew(HistoryEntry before, string chapterBefore)
    {
        bool typeIn = HasNewText(before);
        bool skipping = IsSkipping;
        TitleCard card = NewChapterCard(chapterBefore);
        StageDirection direction = NewLineDirection(before);
        StageTransition transition = direction != null ? direction.transition : StageTransition.None;
        Audio_SoundSO sound = direction != null && !skipping ? direction.sound : null;

        // Skipping jumps straight through fades, but act cards still show (briefly) so the player knows where they are
        if (card != null || (transition == StageTransition.Fade && !skipping))
        {
            PresentThroughBlack(card, typeIn, skipping, hideBoxNow: before == null, sound);
            return;
        }

        RenderPresent(typeIn, true);
        PlaySound(sound);

        if (transition == StageTransition.Flash && !skipping)
            ui.fadeScreen.Flash(flashColor, flashDuration);
    }

    private void PresentThroughBlack(TitleCard card, bool typeIn, bool quick, bool hideBoxNow, Audio_SoundSO sound)
    {
        inTransition = true;

        // A dialogue that just started has nothing to show yet, so no empty box during the fade out.
        // Otherwise the previous screen stays up until the screen is black.
        if (hideBoxNow)
            dialogueUI.Hide();

        gameManager.FadeThrough(
            whileBlack: () =>
            {
                if (!runner.IsActive)
                    return;

                dialogueUI.Hide();
                RenderStage(PresentScreen(), false, true);
            },
            card, quick,
            onRevealed: () =>
            {
                inTransition = false;

                if (!runner.IsActive)
                    return;

                RenderPresent(typeIn, false);
                PlaySound(sound); // With the new scene, not over the title card
                BlockInputThisFrame();
            });
    }

    private TitleCard NewChapterCard(string chapterBefore)
    {
        string chapterID = storyManager.State.ChapterID;

        if (chapterID == chapterBefore || database == null)
            return null;

        Story_ChapterSO chapter = database.GetChapter(chapterID);
        return chapter != null && chapter.titleCard != null && !chapter.titleCard.IsEmpty ? chapter.titleCard : null;
    }

    // The stage direction of the line that just appeared (choices and reactions have none)
    private StageDirection NewLineDirection(HistoryEntry before)
    {
        HistoryEntry present = history.Present;

        if (present == null || present == before || present.type != HistoryEntryType.Line)
            return null;

        Dialogue_ConversationSO conversation = FindConversation(present.conversationID);
        DialogueLine line = conversation != null ? conversation.GetLine(present.itemID) : null;
        return line != null ? line.stage : null;
    }

    private DialogueScreen PresentScreen() =>
        DialogueScreen.Build(history.Entries, history.Entries.Count - 1, true, FindConversation, storyManager.State);

    private void RenderPresent(bool typeIn, bool animateStage)
    {
        DialogueScreen screen = PresentScreen();
        RenderStage(screen, animateStage, true);

        dialogueUI.Show();
        dialogueUI.Render(screen, typeIn && !IsSkipping, OnChoiceSelected);
    }

    // Rollback shows the stage exactly as it was, without animating, and leaves the music alone
    private void RenderHistoryEntry(int index)
    {
        DialogueScreen screen = DialogueScreen.Build(history.Entries, index, false, FindConversation, storyManager.State);
        RenderStage(screen, false, false);
        dialogueUI.Render(screen, false, null);
    }

    private void RenderStage(DialogueScreen screen, bool animate, bool present)
    {
        StageScreen stage = StageScreen.Build(screen.stage, screen.speaker, FindSpeaker, FindImage);
        ui.stage.Render(stage, animate && !IsSkipping);

        // The music and ambience follow the present screen, which is also how a loaded save gets its music back
        if (present && audioManager != null)
        {
            audioManager.PlayMusic(FindSound(screen.stage.musicID));
            audioManager.PlayAmbience(FindSound(screen.stage.ambienceID));
        }
    }

    private void PlaySound(Audio_SoundSO sound)
    {
        if (sound != null && audioManager != null)
            audioManager.PlaySFX(sound);
    }

    private void ReturnToPresent()
    {
        if (!history.IsRollingBack)
            return;

        history.ExitRollback();
        RenderPresent(false, false);
    }

    private bool CanTakeInput()
    {
        return IsActive
            && !inTransition
            && !ui.IsModalOpen
            && !gameManager.IsTransitioning
            && !saveManager.IsRestoring
            && Time.frameCount > inputBlockedUntilFrame;
    }

    private void BlockInputThisFrame() => inputBlockedUntilFrame = Time.frameCount;

    private void HandleConversationEntered(Dialogue_ConversationSO conversation)
    {
        if (conversation.autosaveOnStart)
            pendingAutosave = true;
    }

    private void HandleRunnerEnded()
    {
        lastEndFrame = Time.frameCount;
        skipMode = false;
        autoMode = false;
        pendingAutosave = false;
        inTransition = false;

        dialogueUI.Hide();

        // When the story is over, the last picture stays up and fades out under the end card
        Dialogue_ConversationSO gameEnding = runner.GameEnding;
        if (gameEnding == null)
            ui.stage.Hide();

        OnDialogueEnded?.Invoke();

        if (gameEnding != null)
            gameManager.EndGame(gameEnding.endCard);
    }

    private void HandleMenusChanged()
    {
        if (ui.IsModalOpen || !IsActive)
            return;

        // The press that closed the menu must not also advance the dialogue
        BlockInputThisFrame();
        dialogueUI.RestoreSelection();
    }

    #region Saving

    // Called before saving or capturing a thumbnail, so the save shows exactly what the player will resume to
    public void PrepareForSave()
    {
        if (!IsActive)
            return;

        ReturnToPresent();
        dialogueUI.CompleteTyping();
        skipMode = false;
    }

    public void GetPreview(out string speakerName, out string text)
    {
        speakerName = "";
        text = "";

        if (!IsActive || history.Entries.Count == 0)
            return;

        DialogueScreen screen = DialogueScreen.Build(history.Entries, history.Entries.Count - 1, true, FindConversation, null);
        speakerName = screen.speakerName;
        text = DialogueText.StripTags(screen.text);
    }

    // Closes any dialogue without saving anything (used before loading or leaving to the main menu)
    public void EndImmediate()
    {
        pendingResume = null;
        pendingAutosave = false;
        inTransition = false;
        history.ExitRollback();

        if (runner.IsActive)
            runner.Stop();
    }

    // New Game: forget everything, including the backlog
    public void ResetAll()
    {
        EndImmediate();
        history.Clear();
    }

    public void SaveData(ref GameData data)
    {
        data.dialogue = runner.GetSaveState();
        data.history = history.Snapshot();
    }

    public void LoadData(GameData data)
    {
        EndImmediate();
        history.Load(data.history);
        pendingResume = data.dialogue;
    }

    // Called by SaveManager once the saved scene has loaded (before any Start runs)
    public void ResumePending()
    {
        DialogueSaveState state = pendingResume;
        pendingResume = null;

        if (state == null || !state.isActive)
            return;

        if (runner.Resume(state))
            BeginPresentation(false, null);
    }

    #endregion

#if UNITY_EDITOR
    private void Reset()
    {
        storyManager = transform.root.GetComponentInChildren<StoryManager>(true);
        saveManager = transform.root.GetComponentInChildren<SaveManager>(true);
        gameManager = transform.root.GetComponentInChildren<GameManager>(true);
        audioManager = transform.root.GetComponentInChildren<AudioManager>(true);
        ui = transform.root.GetComponentInChildren<UI>(true);
    }
#endif
}
