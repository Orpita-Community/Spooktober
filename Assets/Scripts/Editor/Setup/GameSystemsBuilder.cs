using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

// Builds Resources/GameSystems.prefab (managers, EventSystem, all UI) and the small UI prefabs it uses,
// following Documentation/Dialogue-and-Save-System.md. Rebuilding overwrites these prefabs.
public static class GameSystemsBuilder
{
    public const string PrefabFolder = "Assets/Prefabs/UI";
    public const string GameSystemsPath = "Assets/Resources/GameSystems.prefab";
    public const string InputActionsPath = "Assets/InputSystem_Actions.inputactions";

    private static readonly Color TitleColor = new Color(.94f, .9f, .83f); // The bone white of the logo's letters

    public static GameObject Build(Dialogue_DatabaseSO database, string mainMenuScene, string firstGameplayScene)
    {
        AssetFolders.Ensure(PrefabFolder);
        AssetFolders.EnsureParent(GameSystemsPath);

        UI_DialogueChoice choicePrefab = SavePrefab(BuildChoicePrefab(), $"{PrefabFolder}/UI_DialogueChoice.prefab").GetComponent<UI_DialogueChoice>();
        UI_BacklogEntry backlogEntryPrefab = SavePrefab(BuildBacklogEntryPrefab(), $"{PrefabFolder}/UI_BacklogEntry.prefab").GetComponent<UI_BacklogEntry>();
        UI_SaveSlot saveSlotPrefab = SavePrefab(BuildSaveSlotPrefab(), $"{PrefabFolder}/UI_SaveSlot.prefab").GetComponent<UI_SaveSlot>();

        GameObject root = new GameObject("GameSystems", typeof(GameSystems));

        // Managers
        GameManager gameManager = new GameObject("GameManager", typeof(GameManager)).GetComponent<GameManager>();
        SaveManager saveManager = new GameObject("SaveManager", typeof(SaveManager)).GetComponent<SaveManager>();
        StoryManager storyManager = new GameObject("StoryManager", typeof(StoryManager)).GetComponent<StoryManager>();
        DialogueManager dialogueManager = new GameObject("DialogueManager", typeof(DialogueManager)).GetComponent<DialogueManager>();

        foreach (Component manager in new Component[] { gameManager, saveManager, storyManager, dialogueManager })
            manager.transform.SetParent(root.transform, false);

        EventSystem eventSystem = BuildEventSystem(root.transform);

        // Backgrounds and characters, drawn below all other UI
        UI_Stage stage = BuildStage(root.transform);

        // UI
        GameObject canvasGO = new GameObject("UICanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(UI));
        canvasGO.transform.SetParent(root.transform, false);
        canvasGO.layer = LayerMask.NameToLayer("UI");
        UIKit.ConfigureCanvas(canvasGO, 10);
        UI ui = canvasGO.GetComponent<UI>();

        ui.dialogueUI = BuildDialogueBox(canvasGO.transform, choicePrefab);
        ui.backlogUI = BuildBacklog(canvasGO.transform, backlogEntryPrefab);
        ui.pauseMenu = BuildPauseMenu(canvasGO.transform);
        ui.saveLoadMenu = BuildSaveLoadMenu(canvasGO.transform, saveSlotPrefab);
        ui.confirmDialog = BuildConfirmDialog(canvasGO.transform);
        ui.stage = stage;
        BuildNotification(canvasGO.transform, ui);

        GameObject fadeCanvas = new GameObject("FadeCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        fadeCanvas.transform.SetParent(root.transform, false);
        fadeCanvas.layer = LayerMask.NameToLayer("UI");
        UIKit.ConfigureCanvas(fadeCanvas, 100);
        ui.fadeScreen = BuildFadeScreen(fadeCanvas.transform);

        // Wiring
        UIKit.Wire(root.GetComponent<GameSystems>(), "eventSystem", eventSystem);

        UIKit.WireString(gameManager, "mainMenuScene", mainMenuScene);
        UIKit.WireString(gameManager, "firstGameplayScene", firstGameplayScene);
        UIKit.Wire(gameManager, "ui", ui);
        UIKit.Wire(gameManager, "saveManager", saveManager);
        UIKit.Wire(gameManager, "storyManager", storyManager);
        UIKit.Wire(gameManager, "dialogueManager", dialogueManager);

        UIKit.Wire(saveManager, "gameManager", gameManager);
        UIKit.Wire(saveManager, "storyManager", storyManager);
        UIKit.Wire(saveManager, "dialogueManager", dialogueManager);
        UIKit.Wire(saveManager, "ui", ui);

        UIKit.Wire(storyManager, "database", database);

        UIKit.Wire(dialogueManager, "database", database);
        UIKit.Wire(dialogueManager, "storyManager", storyManager);
        UIKit.Wire(dialogueManager, "saveManager", saveManager);
        UIKit.Wire(dialogueManager, "gameManager", gameManager);
        UIKit.Wire(dialogueManager, "ui", ui);

        EditorUtility.SetDirty(ui);

        GameObject prefab = SavePrefab(root, GameSystemsPath);
        return prefab;
    }

    private static GameObject SavePrefab(GameObject root, string path)
    {
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path, out bool success);
        Object.DestroyImmediate(root);

        if (!success)
            throw new System.Exception("Could not save prefab " + path);

        return prefab;
    }

    #region EventSystem

    private static EventSystem BuildEventSystem(Transform parent)
    {
        GameObject go = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        go.transform.SetParent(parent, false);

        // Point the UI module at the project's actions. The references must be the asset's own
        // InputActionReference sub-assets, runtime-created ones wouldn't survive being saved in a prefab.
        InputSystemUIInputModule module = go.GetComponent<InputSystemUIInputModule>();
        InputActionAsset actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
        InputActionReference[] references = AssetDatabase.LoadAllAssetsAtPath(InputActionsPath).OfType<InputActionReference>().ToArray();

        SerializedObject serialized = new SerializedObject(module);
        UIKit.Property(serialized, "m_ActionsAsset").objectReferenceValue = actions;

        (string field, string action)[] bindings =
        {
            ("m_PointAction", "Point"), ("m_MoveAction", "Navigate"), ("m_SubmitAction", "Submit"), ("m_CancelAction", "Cancel"),
            ("m_LeftClickAction", "Click"), ("m_MiddleClickAction", "MiddleClick"), ("m_RightClickAction", "RightClick"),
            ("m_ScrollWheelAction", "ScrollWheel"), ("m_TrackedDevicePositionAction", "TrackedDevicePosition"),
            ("m_TrackedDeviceOrientationAction", "TrackedDeviceOrientation")
        };

        foreach ((string field, string action) in bindings)
        {
            InputActionReference reference = references
                .Where(r => r.action != null && r.action.actionMap.name == "UI" && r.action.name == action)
                .OrderBy(r => (r.hideFlags & HideFlags.HideInHierarchy) != 0) // Prefer the visible reference
                .FirstOrDefault();

            if (reference == null)
                throw new System.Exception($"InputSystem_Actions has no UI/{action} action.");

            UIKit.Property(serialized, field).objectReferenceValue = reference;
        }

        serialized.ApplyModifiedPropertiesWithoutUndo();
        return go.GetComponent<EventSystem>();
    }

    #endregion

    #region Stage

    private static UI_Stage BuildStage(Transform root)
    {
        GameObject canvasGO = new GameObject("StageCanvas", typeof(Canvas), typeof(CanvasScaler));
        canvasGO.transform.SetParent(root, false);
        canvasGO.layer = LayerMask.NameToLayer("UI");
        UIKit.ConfigureCanvas(canvasGO, 1);

        GameObject stageGO = UIKit.Create("Stage", canvasGO.transform);
        UIKit.Stretch(stageGO);
        UI_Stage stage = stageGO.AddComponent<UI_Stage>();

        Image background = BackgroundLayer("Background", stageGO.transform);
        Image incomingBackground = BackgroundLayer("IncomingBackground", stageGO.transform);

        // Characters stand on the bottom edge of the screen, their size and offset come from each speaker
        GameObject characters = UIKit.Create("Characters", stageGO.transform);
        UIKit.Stretch(characters);
        Image left = CharacterSlot("Left", characters.transform, .25f);
        Image center = CharacterSlot("Center", characters.transform, .5f);
        Image right = CharacterSlot("Right", characters.transform, .75f);

        // A picture held up to the camera (a letter, a box), over a dimmed stage
        GameObject closeUp = UIKit.Create("CloseUp", stageGO.transform, typeof(CanvasGroup));
        UIKit.Stretch(closeUp);
        CanvasGroup closeUpGroup = closeUp.GetComponent<CanvasGroup>();
        closeUpGroup.alpha = 0f;
        closeUpGroup.blocksRaycasts = false;
        closeUpGroup.interactable = false;

        // Linear colour space blends UI more softly than the alpha suggests: .78 reads as about half brightness
        Image dim = UIKit.Panel("Dim", closeUp.transform, new Color(0, 0, 0, .78f));
        UIKit.Stretch(dim.gameObject);
        dim.raycastTarget = false;

        Image picture = UIKit.Create("Picture", closeUp.transform, typeof(Image)).GetComponent<Image>();
        picture.preserveAspect = true;
        picture.raycastTarget = false;
        UIKit.Place(picture.gameObject, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(0, 90), new Vector2(1100, 640));

        UIKit.Wire(stage, "background", background);
        UIKit.Wire(stage, "incomingBackground", incomingBackground);
        UIKit.Wire(stage, "saturationShader", AssetDatabase.LoadAssetAtPath<Shader>(StoryArt.SaturationShader));
        UIKit.WireArray(stage, "characterSlots", left, center, right);
        UIKit.Wire(stage, "closeUpGroup", closeUpGroup);
        UIKit.Wire(stage, "closeUpPicture", picture);

        stageGO.SetActive(false);
        return stage;
    }

    private static Image BackgroundLayer(string name, Transform parent)
    {
        Image image = UIKit.Create(name, parent, typeof(Image), typeof(AspectRatioFitter)).GetComponent<Image>();
        image.raycastTarget = false;

        // Covers the whole screen at any aspect ratio, cropping the edges instead of letterboxing
        AspectRatioFitter fitter = image.GetComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        fitter.aspectRatio = 16f / 9f;
        return image;
    }

    private static Image CharacterSlot(string name, Transform parent, float x)
    {
        Image image = UIKit.Create(name, parent, typeof(Image)).GetComponent<Image>();
        image.preserveAspect = true;
        image.raycastTarget = false;
        UIKit.Place(image.gameObject, new Vector2(x, 0), new Vector2(.5f, 0), Vector2.zero, new Vector2(600, 1560));
        image.gameObject.SetActive(false);
        return image;
    }

    #endregion

    #region Fade screen

    private static UI_FadeScreen BuildFadeScreen(Transform canvas)
    {
        Image fade = UIKit.Panel("FadeScreen", canvas, Color.black);
        UIKit.Stretch(fade.gameObject);
        UI_FadeScreen fadeScreen = fade.gameObject.AddComponent<UI_FadeScreen>();

        // "ACT 1" / "The Man in the Rain", shown on the black screen between acts
        GameObject card = UIKit.Create("TitleCard", fade.transform, typeof(CanvasGroup));
        UIKit.Stretch(card);
        CanvasGroup cardGroup = card.GetComponent<CanvasGroup>();
        cardGroup.alpha = 0f;
        cardGroup.blocksRaycasts = false; // Clicks go to the black screen, which cuts the card short
        cardGroup.interactable = false;

        TextMeshProUGUI title = UIKit.Text("Title", card.transform, "ACT 1", 110, TextAlignmentOptions.Center, TitleColor, FontStyles.Bold);
        title.characterSpacing = 24;
        UIKit.Place(title.gameObject, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(0, 70), new Vector2(1600, 150));

        Image divider = UIKit.Panel("Divider", card.transform, new Color(1f, .54f, .24f, .85f));
        divider.raycastTarget = false;
        UIKit.Place(divider.gameObject, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(0, -12), new Vector2(360, 3));

        TextMeshProUGUI subtitle = UIKit.Text("Subtitle", card.transform, "The Man in the Rain", 44, TextAlignmentOptions.Center,
            new Color(.76f, .64f, .92f), FontStyles.Italic);
        UIKit.Place(subtitle.gameObject, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(0, -75), new Vector2(1600, 80));

        UIKit.Wire(fadeScreen, "titleCard", cardGroup);
        UIKit.Wire(fadeScreen, "titleText", title);
        UIKit.Wire(fadeScreen, "subtitleText", subtitle);
        return fadeScreen;
    }

    #endregion

    #region Dialogue box

    private static UI_Dialogue BuildDialogueBox(Transform canvas, UI_DialogueChoice choicePrefab)
    {
        GameObject box = UIKit.Create("DialogueBox", canvas);
        UIKit.Stretch(box);
        UI_Dialogue dialogue = box.AddComponent<UI_Dialogue>();

        // Transparent full-screen image: clicks anywhere that isn't a button bubble up to UI_Dialogue and advance
        Image clickCatcher = UIKit.Panel("ClickCatcher", box.transform, new Color(0, 0, 0, 0));
        UIKit.Stretch(clickCatcher.gameObject);

        // Centered lines (captions, the inscription, the final text) replace the text box with a darkened screen
        Image centerRoot = UIKit.Panel("CenterRoot", box.transform, new Color(0, 0, 0, .72f));
        UIKit.Stretch(centerRoot.gameObject);
        centerRoot.raycastTarget = false;
        TextMeshProUGUI centerText = UIKit.Text("CenterText", centerRoot.transform, "", 46, TextAlignmentOptions.Center, TitleColor);
        UIKit.Place(centerText.gameObject, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(0, 60), new Vector2(1400, 600));
        centerText.lineSpacing = 12;
        centerRoot.gameObject.SetActive(false);

        // Portrait, standing above the left side of the text box
        GameObject portraitRoot = UIKit.Create("PortraitRoot", box.transform);
        UIKit.Place(portraitRoot, new Vector2(0, 0), new Vector2(0, 0), new Vector2(110, 290), new Vector2(460, 460));
        Image portrait = UIKit.Create("Portrait", portraitRoot.transform, typeof(Image)).GetComponent<Image>();
        UIKit.Stretch(portrait.gameObject);
        portrait.preserveAspect = true;
        portrait.raycastTarget = false;

        // Text box
        Image textPanel = UIKit.Panel("TextPanel", box.transform, UIKit.PanelColor, rounded: true);
        RectTransform panelRect = (RectTransform)textPanel.transform;
        panelRect.anchorMin = new Vector2(0, 0);
        panelRect.anchorMax = new Vector2(1, 0);
        panelRect.pivot = new Vector2(.5f, 0);
        panelRect.offsetMin = new Vector2(60, 90);
        panelRect.offsetMax = new Vector2(-60, 90 + 260);

        Image namePlate = UIKit.Panel("NamePlate", textPanel.transform, UIKit.AccentColor, rounded: true);
        UIKit.Place(namePlate.gameObject, new Vector2(0, 1), new Vector2(0, .5f), new Vector2(50, 0), new Vector2(340, 58));
        TextMeshProUGUI nameText = UIKit.Text("NameText", namePlate.transform, "Name", 32, TextAlignmentOptions.Center, style: FontStyles.Bold);
        UIKit.Stretch(nameText.gameObject, 12, 4, 12, 4);

        TextMeshProUGUI bodyText = UIKit.Text("BodyText", textPanel.transform, "", 34);
        UIKit.Stretch(bodyText.gameObject, 60, 36, 60, 48);

        TextMeshProUGUI continueIndicator = UIKit.Text("ContinueIndicator", textPanel.transform, "»", 40, TextAlignmentOptions.Center, UIKit.MutedTextColor);
        UIKit.Place(continueIndicator.gameObject, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-24, 14), new Vector2(40, 40));

        // Choices stack upward from just above the text box
        GameObject choices = UIKit.Create("Choices", box.transform);
        UIKit.Place(choices, new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(120, 380), new Vector2(980, 0));
        UIKit.Vertical(choices, 14, TextAnchor.LowerCenter);
        choices.GetComponent<VerticalLayoutGroup>().childForceExpandWidth = true;
        UIKit.FitVertically(choices);

        TextMeshProUGUI rollback = UIKit.Text("RollbackIndicator", box.transform, "« Reading back — scroll down to return", 26,
            TextAlignmentOptions.Center, UIKit.MutedTextColor, FontStyles.Italic);
        UIKit.Place(rollback.gameObject, new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(0, -30), new Vector2(900, 50));
        rollback.gameObject.SetActive(false);

        // Quick menu along the bottom
        GameObject quickMenu = UIKit.Create("QuickMenu", box.transform);
        RectTransform quickRect = (RectTransform)quickMenu.transform;
        quickRect.anchorMin = new Vector2(0, 0);
        quickRect.anchorMax = new Vector2(1, 0);
        quickRect.pivot = new Vector2(.5f, 0);
        quickRect.offsetMin = new Vector2(60, 24);
        quickRect.offsetMax = new Vector2(-60, 24 + 52);
        UIKit.Horizontal(quickMenu, 34, TextAnchor.MiddleCenter);

        Button QuickButton(string label)
        {
            Button button = UIKit.Button(label.Replace(".", ""), quickMenu.transform, label, 24, new Vector2(110, 48), flat: true);
            UIKit.NoNavigation(button);
            return button;
        }

        Button back = QuickButton("Back");
        Button history = QuickButton("History");
        Button auto = QuickButton("Auto");
        Button skip = QuickButton("Skip");
        Button save = QuickButton("Save");
        Button load = QuickButton("Load");
        Button quickSave = QuickButton("Q.Save");
        Button quickLoad = QuickButton("Q.Load");
        Button menu = QuickButton("Menu");

        GameObject autoOn = ModeUnderline(auto.transform);
        GameObject skipOn = ModeUnderline(skip.transform);

        UIKit.Wire(dialogue, "portraitRoot", portraitRoot);
        UIKit.Wire(dialogue, "portrait", portrait);
        UIKit.Wire(dialogue, "namePlate", namePlate.gameObject);
        UIKit.Wire(dialogue, "nameText", nameText);
        UIKit.Wire(dialogue, "textPanel", textPanel.gameObject);
        UIKit.Wire(dialogue, "bodyText", bodyText);
        UIKit.Wire(dialogue, "continueIndicator", continueIndicator.gameObject);
        UIKit.Wire(dialogue, "centerRoot", centerRoot.gameObject);
        UIKit.Wire(dialogue, "centerText", centerText);
        UIKit.Wire(dialogue, "choiceContainer", choices.transform);
        UIKit.Wire(dialogue, "choicePrefab", choicePrefab);
        UIKit.Wire(dialogue, "rollbackIndicator", rollback.gameObject);
        UIKit.Wire(dialogue, "backButton", back);
        UIKit.Wire(dialogue, "historyButton", history);
        UIKit.Wire(dialogue, "autoButton", auto);
        UIKit.Wire(dialogue, "skipButton", skip);
        UIKit.Wire(dialogue, "saveButton", save);
        UIKit.Wire(dialogue, "loadButton", load);
        UIKit.Wire(dialogue, "quickSaveButton", quickSave);
        UIKit.Wire(dialogue, "quickLoadButton", quickLoad);
        UIKit.Wire(dialogue, "menuButton", menu);
        UIKit.Wire(dialogue, "autoOnIndicator", autoOn);
        UIKit.Wire(dialogue, "skipOnIndicator", skipOn);

        box.SetActive(false);
        return dialogue;
    }

    private static GameObject ModeUnderline(Transform button)
    {
        Image underline = UIKit.Panel("On", button, UIKit.AccentColor);
        UIKit.Place(underline.gameObject, new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(0, 2), new Vector2(70, 4));
        underline.raycastTarget = false;
        underline.gameObject.SetActive(false);
        return underline.gameObject;
    }

    private static GameObject BuildChoicePrefab()
    {
        Button button = UIKit.Button("UI_DialogueChoice", null, "Choice", 30, new Vector2(980, 68));
        GameObject go = button.gameObject;
        TextMeshProUGUI label = go.transform.Find("Label").GetComponent<TextMeshProUGUI>();
        UIKit.Stretch(label.gameObject, 40, 6, 40, 6);
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.enableAutoSizing = true; // Long options shrink instead of overflowing
        label.fontSizeMin = 20;
        label.fontSizeMax = 30;

        Image picked = UIKit.Panel("PickedMarker", go.transform, new Color(1f, .78f, .35f, 1f));
        UIKit.Place(picked.gameObject, new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(12, 0), new Vector2(8, 44));
        picked.raycastTarget = false;
        picked.gameObject.SetActive(false);

        TextMeshProUGUI locked = UIKit.Text("LockedMarker", go.transform, "locked", 20, TextAlignmentOptions.MidlineRight, UIKit.MutedTextColor, FontStyles.Italic);
        UIKit.Place(locked.gameObject, new Vector2(1, .5f), new Vector2(1, .5f), new Vector2(-24, 0), new Vector2(120, 40));
        locked.gameObject.SetActive(false);

        UI_DialogueChoice choice = go.AddComponent<UI_DialogueChoice>();
        UIKit.Wire(choice, "button", button);
        UIKit.Wire(choice, "label", label);
        UIKit.Wire(choice, "pickedMarker", picked.gameObject);
        UIKit.Wire(choice, "lockedMarker", locked.gameObject);
        return go;
    }

    #endregion

    #region Backlog

    private static UI_Backlog BuildBacklog(Transform canvas, UI_BacklogEntry entryPrefab)
    {
        Image background = UIKit.Panel("Backlog", canvas, new Color(.02f, .01f, .04f, .94f));
        UIKit.Stretch(background.gameObject);
        GameObject go = background.gameObject;
        UI_Backlog backlog = go.AddComponent<UI_Backlog>();

        TextMeshProUGUI title = UIKit.Text("Title", go.transform, "History", 44, TextAlignmentOptions.Center, style: FontStyles.Bold);
        UIKit.Place(title.gameObject, new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(0, -40), new Vector2(600, 60));

        GameObject scrollView = UIKit.Create("ScrollView", go.transform, typeof(ScrollRect));
        UIKit.Stretch(scrollView, 260, 130, 260, 120);
        ScrollRect scrollRect = scrollView.GetComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 40;

        GameObject viewport = UIKit.Create("Viewport", scrollView.transform, typeof(RectMask2D), typeof(Image));
        UIKit.Stretch(viewport);
        viewport.GetComponent<Image>().color = new Color(0, 0, 0, 0); // Lets the mouse wheel reach the ScrollRect

        GameObject content = UIKit.Create("Content", viewport.transform);
        RectTransform contentRect = (RectTransform)content.transform;
        contentRect.anchorMin = new Vector2(0, 1);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.pivot = new Vector2(.5f, 1);
        contentRect.sizeDelta = Vector2.zero;
        UIKit.Vertical(content, 26, TextAnchor.UpperLeft, new RectOffset(20, 20, 10, 10)).childForceExpandWidth = true;
        UIKit.FitVertically(content);

        scrollRect.viewport = (RectTransform)viewport.transform;
        scrollRect.content = contentRect;

        TextMeshProUGUI empty = UIKit.Text("EmptyLabel", go.transform, "Nothing has been said yet.", 30, TextAlignmentOptions.Center, UIKit.MutedTextColor);
        UIKit.Place(empty.gameObject, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(800, 60));

        Button close = UIKit.Button("CloseButton", go.transform, "Close", 28, new Vector2(240, 60));
        UIKit.Place(close.gameObject, new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(0, 40), new Vector2(240, 60));

        UIKit.Wire(backlog, "scrollRect", scrollRect);
        UIKit.Wire(backlog, "content", contentRect);
        UIKit.Wire(backlog, "entryPrefab", entryPrefab);
        UIKit.Wire(backlog, "closeButton", close);
        UIKit.Wire(backlog, "emptyLabel", empty.gameObject);

        go.SetActive(false);
        return backlog;
    }

    private static GameObject BuildBacklogEntryPrefab()
    {
        GameObject go = UIKit.Create("UI_BacklogEntry", null);
        UIKit.Vertical(go, 4, TextAnchor.UpperLeft).childForceExpandWidth = true;

        TextMeshProUGUI speaker = UIKit.Text("SpeakerName", go.transform, "Speaker", 26, style: FontStyles.Bold);
        TextMeshProUGUI body = UIKit.Text("Body", go.transform, "Text", 30);

        UI_BacklogEntry entry = go.AddComponent<UI_BacklogEntry>();
        UIKit.Wire(entry, "speakerNameText", speaker);
        UIKit.Wire(entry, "bodyText", body);
        return go;
    }

    #endregion

    #region Menus

    private static Transform Window(GameObject menu, Vector2 size, Color? dim = null)
    {
        Image background = menu.AddComponent<Image>();
        background.color = dim ?? new Color(0, 0, 0, .7f);
        UIKit.Stretch(menu);

        Image window = UIKit.Panel("Window", menu.transform, UIKit.PanelColor, rounded: true);
        UIKit.Place(window.gameObject, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, size);
        return window.transform;
    }

    private static UI_PauseMenu BuildPauseMenu(Transform canvas)
    {
        GameObject go = UIKit.Create("PauseMenu", canvas);
        Transform window = Window(go, new Vector2(520, 700));
        UIKit.Vertical(window.gameObject, 18, TextAnchor.MiddleCenter, new RectOffset(40, 40, 40, 40));

        UIKit.Text("Title", window, "Paused", 48, TextAlignmentOptions.Center, style: FontStyles.Bold);

        Vector2 size = new Vector2(360, 66);
        Button resume = UIKit.Button("ResumeButton", window, "Resume", 30, size);
        Button save = UIKit.Button("SaveButton", window, "Save", 30, size);
        Button load = UIKit.Button("LoadButton", window, "Load", 30, size);
        Button history = UIKit.Button("HistoryButton", window, "History", 30, size);
        Button mainMenu = UIKit.Button("MainMenuButton", window, "Main Menu", 30, size);
        Button quit = UIKit.Button("QuitButton", window, "Quit", 30, size);

        UI_PauseMenu pause = go.AddComponent<UI_PauseMenu>();
        UIKit.Wire(pause, "resumeButton", resume);
        UIKit.Wire(pause, "saveButton", save);
        UIKit.Wire(pause, "loadButton", load);
        UIKit.Wire(pause, "historyButton", history);
        UIKit.Wire(pause, "mainMenuButton", mainMenu);
        UIKit.Wire(pause, "quitButton", quit);

        go.SetActive(false);
        return pause;
    }

    private static UI_SaveLoadMenu BuildSaveLoadMenu(Transform canvas, UI_SaveSlot slotPrefab)
    {
        GameObject go = UIKit.Create("SaveLoadMenu", canvas);
        Image background = go.AddComponent<Image>();
        background.color = new Color(.02f, .01f, .04f, .97f);
        UIKit.Stretch(go);

        GameObject column = UIKit.Create("Column", go.transform);
        UIKit.Stretch(column, 0, 20, 0, 20);
        UIKit.Vertical(column, 22, TextAnchor.UpperCenter, new RectOffset(0, 0, 20, 0));

        TextMeshProUGUI title = UIKit.Text("Title", column.transform, "Save Game", 44, TextAlignmentOptions.Center, style: FontStyles.Bold);

        GameObject special = UIKit.Create("SpecialSlots", column.transform);
        UIKit.Horizontal(special, 20, TextAnchor.MiddleCenter);
        UI_SaveSlot autoSlot = ((GameObject)PrefabUtility.InstantiatePrefab(slotPrefab.gameObject, special.transform)).GetComponent<UI_SaveSlot>();
        UI_SaveSlot quickSlot = ((GameObject)PrefabUtility.InstantiatePrefab(slotPrefab.gameObject, special.transform)).GetComponent<UI_SaveSlot>();
        autoSlot.name = "AutoSlot";
        quickSlot.name = "QuickSlot";

        GameObject grid = UIKit.Create("Grid", column.transform, typeof(GridLayoutGroup));
        GridLayoutGroup gridLayout = grid.GetComponent<GridLayoutGroup>();
        gridLayout.cellSize = new Vector2(560, 132);
        gridLayout.spacing = new Vector2(20, 14);
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = 3;
        gridLayout.childAlignment = TextAnchor.UpperCenter;

        Button close = UIKit.Button("CloseButton", column.transform, "Back", 28, new Vector2(240, 60));

        UI_SaveLoadMenu menu = go.AddComponent<UI_SaveLoadMenu>();
        UIKit.Wire(menu, "titleText", title);
        UIKit.Wire(menu, "specialSlotsRow", special);
        UIKit.Wire(menu, "autoSlot", autoSlot);
        UIKit.Wire(menu, "quickSlot", quickSlot);
        UIKit.Wire(menu, "slotGrid", grid.transform);
        UIKit.Wire(menu, "slotPrefab", slotPrefab);
        UIKit.Wire(menu, "closeButton", close);

        go.SetActive(false);
        return menu;
    }

    private static GameObject BuildSaveSlotPrefab()
    {
        GameObject go = UIKit.Create("UI_SaveSlot", null, typeof(Image), typeof(Button), typeof(LayoutElement));
        ((RectTransform)go.transform).sizeDelta = new Vector2(560, 132);
        LayoutElement layout = go.GetComponent<LayoutElement>();
        layout.preferredWidth = 560;
        layout.preferredHeight = 132;

        Image image = go.GetComponent<Image>();
        image.sprite = UIKit.RoundedSprite;
        image.type = Image.Type.Sliced;
        Button button = go.GetComponent<Button>();
        button.targetGraphic = image;
        button.colors = UIKit.ButtonColors();

        RawImage thumbnail = UIKit.Create("Thumbnail", go.transform, typeof(RawImage)).GetComponent<RawImage>();
        UIKit.Place(thumbnail.gameObject, new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(12, 0), new Vector2(192, 108));
        thumbnail.color = Color.white;
        thumbnail.raycastTarget = false;

        const float textX = 220;
        TextMeshProUGUI slotLabel = UIKit.Text("SlotLabel", go.transform, "Slot", 26, style: FontStyles.Bold);
        UIKit.Place(slotLabel.gameObject, new Vector2(0, 1), new Vector2(0, 1), new Vector2(textX, -10), new Vector2(160, 34));

        TextMeshProUGUI timestamp = UIKit.Text("Timestamp", go.transform, "", 19, TextAlignmentOptions.TopRight, UIKit.MutedTextColor);
        UIKit.Place(timestamp.gameObject, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-14, -14), new Vector2(200, 28));

        TextMeshProUGUI chapter = UIKit.Text("Chapter", go.transform, "", 22, color: new Color(.84f, .56f, 1f));
        UIKit.Place(chapter.gameObject, new Vector2(0, 1), new Vector2(0, 1), new Vector2(textX, -44), new Vector2(320, 30));

        TextMeshProUGUI preview = UIKit.Text("Preview", go.transform, "", 19, color: UIKit.MutedTextColor, style: FontStyles.Italic);
        UIKit.Place(preview.gameObject, new Vector2(0, 1), new Vector2(0, 1), new Vector2(textX, -76), new Vector2(230, 48));
        preview.overflowMode = TextOverflowModes.Ellipsis;

        TextMeshProUGUI empty = UIKit.Text("EmptyLabel", go.transform, "Empty", 26, TextAlignmentOptions.MidlineLeft, UIKit.MutedTextColor);
        UIKit.Place(empty.gameObject, new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(textX, -8), new Vector2(300, 40));

        TextMeshProUGUI corrupt = UIKit.Text("CorruptLabel", go.transform, "Damaged save", 24, TextAlignmentOptions.MidlineLeft, new Color(1f, .45f, .45f));
        UIKit.Place(corrupt.gameObject, new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(textX, -8), new Vector2(300, 40));
        corrupt.gameObject.SetActive(false);

        Button delete = UIKit.Button("DeleteButton", go.transform, "Delete", 18, new Vector2(90, 32));
        UIKit.Place(delete.gameObject, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-12, 12), new Vector2(90, 32));
        UIKit.NoNavigation(delete);

        UI_SaveSlot slot = go.AddComponent<UI_SaveSlot>();
        UIKit.Wire(slot, "button", button);
        UIKit.Wire(slot, "thumbnail", thumbnail);
        UIKit.Wire(slot, "slotLabel", slotLabel);
        UIKit.Wire(slot, "timestampText", timestamp);
        UIKit.Wire(slot, "chapterText", chapter);
        UIKit.Wire(slot, "previewText", preview);
        UIKit.Wire(slot, "emptyLabel", empty.gameObject);
        UIKit.Wire(slot, "corruptLabel", corrupt.gameObject);
        UIKit.Wire(slot, "deleteButton", delete);
        return go;
    }

    private static UI_ConfirmDialog BuildConfirmDialog(Transform canvas)
    {
        GameObject go = UIKit.Create("ConfirmDialog", canvas);
        Transform window = Window(go, new Vector2(720, 320), new Color(0, 0, 0, .6f));

        TextMeshProUGUI message = UIKit.Text("Message", window, "Are you sure?", 32, TextAlignmentOptions.Center);
        UIKit.Stretch(message.gameObject, 40, 110, 40, 30);

        GameObject buttons = UIKit.Create("Buttons", window);
        UIKit.Place(buttons, new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(0, 30), new Vector2(560, 66));
        UIKit.Horizontal(buttons, 40, TextAnchor.MiddleCenter);
        Button yes = UIKit.Button("YesButton", buttons.transform, "Yes", 30, new Vector2(220, 66));
        Button no = UIKit.Button("NoButton", buttons.transform, "No", 30, new Vector2(220, 66));

        UI_ConfirmDialog confirm = go.AddComponent<UI_ConfirmDialog>();
        UIKit.Wire(confirm, "messageText", message);
        UIKit.Wire(confirm, "yesButton", yes);
        UIKit.Wire(confirm, "noButton", no);

        go.SetActive(false);
        return confirm;
    }

    private static void BuildNotification(Transform canvas, UI ui)
    {
        Image background = UIKit.Panel("Notification", canvas, UIKit.PanelColor, rounded: true);
        UIKit.Place(background.gameObject, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-40, -40), new Vector2(320, 64));
        background.raycastTarget = false;

        CanvasGroup group = background.gameObject.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        group.blocksRaycasts = false;
        group.interactable = false;

        TextMeshProUGUI text = UIKit.Text("Text", background.transform, "Game saved", 26, TextAlignmentOptions.Center);
        UIKit.Stretch(text.gameObject, 12, 6, 12, 6);

        UIKit.Wire(ui, "notificationText", text);
        UIKit.Wire(ui, "notificationGroup", group);
    }

    #endregion
}
