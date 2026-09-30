using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// The dialogue box. View only: DialogueManager decides what to show, this just draws a DialogueScreen.
// Clicking anywhere on the box that isn't a button (e.g. a full-screen transparent Image) advances.
public class UI_Dialogue : MonoBehaviour, IPointerClickHandler
{
    [Header("Speaker")]
    [Tooltip("Only used for speakers who aren't standing on the stage.")]
    [SerializeField] private GameObject portraitRoot;
    [SerializeField] private Image portrait;
    [SerializeField] private GameObject namePlate;
    [SerializeField] private TextMeshProUGUI nameText;

    [Header("Text")]
    [Tooltip("Optional. The box behind the name and text, hidden for centered lines.")]
    [SerializeField] private GameObject textPanel;
    [SerializeField] private TextMeshProUGUI bodyText;
    [SerializeField] private GameObject continueIndicator;
    [SerializeField] private float charactersPerSecond = 40f;

    [Header("Centered Lines (optional)")]
    [Tooltip("Shown instead of the text box for Centered lines, usually a full-screen dim.")]
    [SerializeField] private GameObject centerRoot;
    [SerializeField] private TextMeshProUGUI centerText;

    [Header("Choices")]
    [SerializeField] private Transform choiceContainer;
    [SerializeField] private UI_DialogueChoice choicePrefab;

    [Header("Rollback")]
    [Tooltip("Shown while the player is reading back through old lines.")]
    [SerializeField] private GameObject rollbackIndicator;

    [Header("Quick Menu (all optional)")]
    [SerializeField] private Button backButton;
    [SerializeField] private Button historyButton;
    [SerializeField] private Button autoButton;
    [SerializeField] private Button skipButton;
    [SerializeField] private Button saveButton;
    [SerializeField] private Button loadButton;
    [SerializeField] private Button quickSaveButton;
    [SerializeField] private Button quickLoadButton;
    [SerializeField] private Button menuButton;
    [SerializeField] private GameObject autoOnIndicator;
    [SerializeField] private GameObject skipOnIndicator;

    private readonly List<UI_DialogueChoice> choicePool = new List<UI_DialogueChoice>();
    private DialogueManager dialogueManager;
    private DialogueScreen currentScreen;
    private Action<string> onChoiceSelected;
    private TextMeshProUGUI activeText;
    private float typingProgress;

    public bool IsTyping { get; private set; }
    public int CharacterCount { get; private set; }

    public void Initialize(DialogueManager manager)
    {
        dialogueManager = manager;

        BindQuickButton(backButton, manager.RollBack);
        BindQuickButton(historyButton, manager.OpenBacklog);
        BindQuickButton(autoButton, manager.ToggleAuto);
        BindQuickButton(skipButton, manager.ToggleSkip);
        BindQuickButton(saveButton, manager.OpenSaveMenu);
        BindQuickButton(loadButton, manager.OpenLoadMenu);
        BindQuickButton(quickSaveButton, manager.QuickSave);
        BindQuickButton(quickLoadButton, manager.QuickLoad);
        BindQuickButton(menuButton, manager.OpenPauseMenu);
    }

    public void Show() => gameObject.SetActive(true);

    public void Hide()
    {
        IsTyping = false;
        HideChoices();
        gameObject.SetActive(false);
    }

    public void Render(DialogueScreen screen, bool typeIn, Action<string> onChoice)
    {
        currentScreen = screen;
        onChoiceSelected = onChoice;

        // Centered lines replace the whole text box
        bool centered = screen.style == DialogueLineStyle.Centered && centerText != null;
        SetActive(textPanel, !centered);
        SetActive(centerRoot, centered);
        activeText = centered ? centerText : bodyText;

        // Speaker (no speaker = narrator: no name plate, no portrait)
        bool hasName = !string.IsNullOrEmpty(screen.speakerName);
        SetActive(namePlate, hasName && !centered);
        if (nameText != null)
        {
            nameText.text = screen.speakerName;
            nameText.color = screen.nameColor;
        }

        // A speaker standing on the stage is already visible
        bool showPortrait = screen.portrait != null && !screen.speakerOnStage && !centered;
        SetActive(portraitRoot, showPortrait);
        if (portrait != null)
            portrait.sprite = showPortrait ? screen.portrait : null;

        // Text. maxVisibleCharacters keeps rich text tags working while typing
        activeText.text = screen.text;
        activeText.maxVisibleCharacters = typeIn ? 0 : int.MaxValue;
        activeText.ForceMeshUpdate();
        CharacterCount = activeText.textInfo.characterCount;

        typingProgress = 0f;
        IsTyping = typeIn && CharacterCount > 0;

        SetActive(rollbackIndicator, !screen.isLive);

        HideChoices();
        if (!IsTyping)
            OnTypingFinished();
        else
            SetActive(continueIndicator, false);
    }

    public void CompleteTyping()
    {
        if (IsTyping)
            OnTypingFinished();
    }

    public void SetModeIndicators(bool auto, bool skip)
    {
        SetActive(autoOnIndicator, auto);
        SetActive(skipOnIndicator, skip);
    }

    // After a menu closes, give keyboard/gamepad focus back to the choices
    public void RestoreSelection()
    {
        if (!gameObject.activeInHierarchy || IsTyping)
            return;

        SelectFirstChoice();
    }

    private void Update()
    {
        if (!IsTyping)
            return;

        typingProgress += Time.deltaTime * charactersPerSecond; // Scaled time, so typing pauses with the game
        int visible = Mathf.FloorToInt(typingProgress);
        activeText.maxVisibleCharacters = visible;

        if (visible >= CharacterCount)
            OnTypingFinished();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left && dialogueManager != null)
            dialogueManager.Advance();
    }

    private void OnTypingFinished()
    {
        IsTyping = false;
        activeText.maxVisibleCharacters = int.MaxValue;

        ShowChoices();
        SetActive(continueIndicator, currentScreen != null && !currentScreen.HasChoices);
    }

    private void ShowChoices()
    {
        HideChoices();

        if (currentScreen == null || !currentScreen.HasChoices)
            return;

        for (int i = 0; i < currentScreen.choices.Count; i++)
        {
            DialogueScreen.ChoiceView view = currentScreen.choices[i];
            UI_DialogueChoice choice = GetPooledChoice(i);
            string choiceID = view.id;

            choice.gameObject.SetActive(true);
            choice.Setup(view.text, view.interactable, view.picked, view.locked, () => onChoiceSelected?.Invoke(choiceID));
        }

        SelectFirstChoice();
    }

    private void HideChoices()
    {
        foreach (UI_DialogueChoice choice in choicePool)
            choice.gameObject.SetActive(false);
    }

    private void SelectFirstChoice()
    {
        if (EventSystem.current == null)
            return;

        foreach (UI_DialogueChoice choice in choicePool)
        {
            if (choice.gameObject.activeSelf && choice.IsInteractable)
            {
                EventSystem.current.SetSelectedGameObject(choice.gameObject);
                return;
            }
        }

        EventSystem.current.SetSelectedGameObject(null);
    }

    private UI_DialogueChoice GetPooledChoice(int index)
    {
        while (choicePool.Count <= index)
            choicePool.Add(Instantiate(choicePrefab, choiceContainer));

        return choicePool[index];
    }

    private static void BindQuickButton(Button button, Action action)
    {
        if (button == null)
            return;

        button.onClick.AddListener(() =>
        {
            // Quick menu buttons shouldn't keep focus, or the next Enter would press them again
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(null);

            action();
        });
    }

    private static void SetActive(GameObject target, bool active)
    {
        if (target != null && target.activeSelf != active)
            target.SetActive(active);
    }
}
