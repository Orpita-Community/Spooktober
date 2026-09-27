using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// Scene flow (New Game, Continue, main menu), fades, play time and the System input map (Pause, Quick Save/Load).
[DefaultExecutionOrder(-100)]
public class GameManager : MonoBehaviour, ISaveable
{
    public static GameManager Instance { get; private set; }

    [Header("Scenes")]
    [SerializeField] private string mainMenuScene = "MainMenu";
    [SerializeField] private string firstGameplayScene = "Shop";

    [Header("Transitions")]
    [SerializeField] private float fadeDuration = .5f;

    [Header("References")]
    [SerializeField] private UI ui;
    [SerializeField] private SaveManager saveManager;
    [SerializeField] private StoryManager storyManager;
    [SerializeField] private DialogueManager dialogueManager;

    private Action<Scene> pendingSceneLoaded;
    private InputAction pauseAction;
    private InputAction quickSaveAction;
    private InputAction quickLoadAction;

    public bool IsTransitioning { get; private set; }
    public bool InMainMenu => SceneManager.GetActiveScene().name == mainMenuScene;
    public float PlayTime { get; private set; }

    private void Awake()
    {
        Instance = this;
        SceneManager.sceneLoaded += HandleSceneLoaded;

        pauseAction = GameInput.Find("System/Pause");
        quickSaveAction = GameInput.Find("System/QuickSave");
        quickLoadAction = GameInput.Find("System/QuickLoad");
    }

    private void Start()
    {
        ui.fadeScreen.FadeIn(fadeDuration);
    }

    private void OnDestroy()
    {
        if (Instance != this)
            return;

        Instance = null;
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void Update()
    {
        if (!InMainMenu && !IsTransitioning)
            PlayTime += Time.unscaledDeltaTime;

        HandleSystemInput();
    }

    private void HandleSystemInput()
    {
        if (IsTransitioning || saveManager.IsRestoring)
            return;

        if (GameInput.WasPressed(pauseAction))
        {
            // Esc goes back one menu, and only opens the pause menu when nothing is open
            if (ui.IsModalOpen)
                ui.Back();
            else
                OpenPauseMenu();
        }
        else if (GameInput.WasPressed(quickSaveAction) && !ui.IsModalOpen)
        {
            saveManager.QuickSave();
        }
        else if (GameInput.WasPressed(quickLoadAction) && !ui.IsModalOpen)
        {
            saveManager.QuickLoad();
        }
    }

    public void OpenPauseMenu()
    {
        if (InMainMenu || IsTransitioning || ui.IsModalOpen)
            return;

        saveManager.CaptureThenOpen(() => ui.pauseMenu.Open());
    }

    public void NewGame()
    {
        if (IsTransitioning)
            return;

        StartCoroutine(TransitionCo(firstGameplayScene, () =>
        {
            dialogueManager.ResetAll();
            storyManager.ResetToDefaults();
            saveManager.ResetGameData();
            PlayTime = 0f;
        }, null));
    }

    public void ContinueGame() => saveManager.LoadLatest();

    public void ReturnToMainMenu()
    {
        if (IsTransitioning)
            return;

        StartCoroutine(TransitionCo(mainMenuScene, dialogueManager.EndImmediate, null));
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // Fade out, run beforeLoad, load the scene, run onSceneLoaded (after Awake, before any Start), fade in.
    public IEnumerator TransitionCo(string sceneName, Action beforeLoad, Action<Scene> onSceneLoaded)
    {
        IsTransitioning = true;
        ui.CloseAllMenus();
        Time.timeScale = 1f;

        yield return ui.fadeScreen.FadeOutCo(fadeDuration);

        beforeLoad?.Invoke();
        pendingSceneLoaded = onSceneLoaded;

        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(sceneName);
        while (!loadOperation.isDone)
            yield return null;

        yield return null; // Let the new scene's Start() methods run before the player sees it

        yield return ui.fadeScreen.FadeInCo(fadeDuration);
        IsTransitioning = false;
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Action<Scene> callback = pendingSceneLoaded;
        pendingSceneLoaded = null;
        callback?.Invoke(scene);
    }

    public void LoadData(GameData data)
    {
        PlayTime = data.metadata != null ? data.metadata.playTimeSeconds : 0f;
    }

    public void SaveData(ref GameData data)
    {
        data.sceneName = SceneManager.GetActiveScene().name;
        data.metadata.playTimeSeconds = PlayTime;
    }

#if UNITY_EDITOR
    private void Reset()
    {
        ui = transform.root.GetComponentInChildren<UI>(true);
        saveManager = transform.root.GetComponentInChildren<SaveManager>(true);
        storyManager = transform.root.GetComponentInChildren<StoryManager>(true);
        dialogueManager = transform.root.GetComponentInChildren<DialogueManager>(true);
    }
#endif
}
