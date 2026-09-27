using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

// Save slots: manual slots "01".."N", plus "auto" and "quick". Each slot is a .sav file with a .png thumbnail.
// Nothing is loaded automatically on scene start and nothing is saved on quit; the player decides.
[DefaultExecutionOrder(-100)]
public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    public const string AutoSlot = "auto";
    public const string QuickSlot = "quick";

    [Header("Save Settings")]
    [SerializeField] private int manualSlotCount = 12;
    [SerializeField] private bool encryptData = true;
    [SerializeField] private string saveFolder = "Saves";
    [SerializeField] private string fileExtension = ".sav";

    [Header("Thumbnails")]
    [SerializeField] private int thumbnailWidth = 320;
    [SerializeField] private int thumbnailHeight = 180;

    [Header("References")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private StoryManager storyManager;
    [SerializeField] private DialogueManager dialogueManager;
    [SerializeField] private UI ui;

    private FileDataHandler dataHandler;
    private GameData gameData;
    private byte[] pendingThumbnail; // Captured just before a menu opened, reused until all menus close
    private bool isCapturing;

    public bool IsRestoring { get; private set; }
    public bool IsBusy { get; private set; }
    public int ManualSlotCount => manualSlotCount;
    public string SaveDirectory => Path.Combine(Application.persistentDataPath, saveFolder);

    public bool CanSave => !gameManager.InMainMenu && !gameManager.IsTransitioning && !IsRestoring && !IsBusy;
    public bool HasAnySave => GetLatestSlot() != null;

    // Headless runs (batch mode, automated tests) have no screen to capture
    private static bool CanCaptureScreen => !Application.isBatchMode;

    public event Action OnSlotsChanged;

    private void Awake()
    {
        Instance = this;
        dataHandler = new FileDataHandler(encryptData);
        gameData = new GameData();
        ui.OnMenusChanged += HandleMenusChanged;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        if (ui != null)
            ui.OnMenusChanged -= HandleMenusChanged;
    }

    #region Slots

    public static string ManualSlotKey(int index) => (index + 1).ToString("00");

    public IEnumerable<string> AllSlotKeys()
    {
        for (int i = 0; i < manualSlotCount; i++)
            yield return ManualSlotKey(i);

        yield return AutoSlot;
        yield return QuickSlot;
    }

    public string GetSavePath(string key) => Path.Combine(SaveDirectory, SlotFileName(key) + fileExtension);
    public string GetThumbnailPath(string key) => Path.Combine(SaveDirectory, SlotFileName(key) + ".png");

    private static string SlotFileName(string key) => key == AutoSlot || key == QuickSlot ? key : "slot_" + key;

    public SaveSlotInfo GetSlotInfo(string key)
    {
        string path = GetSavePath(key);
        SaveSlotInfo info = new SaveSlotInfo { key = key, thumbnailPath = GetThumbnailPath(key) };

        if (!dataHandler.Exists(path) && !dataHandler.Exists(path + FileDataHandler.BackupExtension))
            return info;

        info.exists = true;
        GameData data = dataHandler.LoadData(path);

        if (data == null)
        {
            info.corrupt = true;
            return info;
        }

        info.incompatible = data.saveVersion > GameData.CurrentVersion;
        info.metadata = data.metadata ?? new SaveMetadata();
        return info;
    }

    public SaveSlotInfo GetLatestSlot()
    {
        return AllSlotKeys()
            .Select(GetSlotInfo)
            .Where(info => info.IsLoadable)
            .OrderByDescending(info => info.Timestamp)
            .FirstOrDefault();
    }

    public void DeleteSlot(string key)
    {
        dataHandler.DeleteData(GetSavePath(key));

        try
        {
            if (File.Exists(GetThumbnailPath(key)))
                File.Delete(GetThumbnailPath(key));
        }
        catch (Exception e)
        {
            Debug.LogWarning("Could not delete save thumbnail.\n" + e.Message);
        }

        OnSlotsChanged?.Invoke();
    }

    #endregion

    #region Saving

    public void SaveToSlot(string key) => TrySave(key, "Game saved");
    public void QuickSave() => TrySave(QuickSlot, "Quick saved");
    public void Autosave() => TrySave(AutoSlot, "Autosaved");

    private void TrySave(string key, string message)
    {
        if (!CanSave)
            return;

        StartCoroutine(SaveCo(key, message));
    }

    // Opens a menu after grabbing a thumbnail of the game without the menu on it
    public void CaptureThenOpen(Action open)
    {
        if (isCapturing)
            return;

        if (ui.IsModalOpen || !CanSave || !CanCaptureScreen)
        {
            open?.Invoke();
            return;
        }

        StartCoroutine(CaptureThenOpenCo(open));
    }

    private IEnumerator CaptureThenOpenCo(Action open)
    {
        isCapturing = true;
        dialogueManager.PrepareForSave();

        yield return new WaitForEndOfFrame();

        pendingThumbnail = ThumbnailUtility.CaptureScreenPNG(thumbnailWidth, thumbnailHeight);
        isCapturing = false;
        open?.Invoke();
    }

    private IEnumerator SaveCo(string key, string message)
    {
        IsBusy = true;

        // Gather the data right away so it matches the moment the player asked to save
        dialogueManager.PrepareForSave();
        GameData data = CollectGameData();

        byte[] thumbnail = pendingThumbnail;
        if (thumbnail == null && CanCaptureScreen)
        {
            yield return new WaitForEndOfFrame();
            thumbnail = ThumbnailUtility.CaptureScreenPNG(thumbnailWidth, thumbnailHeight);
        }

        bool saved = dataHandler.SaveData(GetSavePath(key), data);

        if (saved && thumbnail != null)
            WriteThumbnail(key, thumbnail);

        IsBusy = false;
        ui.Notify(saved ? message : "Save failed");
        OnSlotsChanged?.Invoke();
    }

    private GameData CollectGameData()
    {
        gameData ??= new GameData();
        gameData.metadata ??= new SaveMetadata();

        // Saveables write into the running data (like the reference project), so state they don't touch is kept
        foreach (ISaveable saveable in GetPersistentSaveables())
            saveable.SaveData(ref gameData);

        foreach (ISaveable saveable in GetSceneSaveables(SceneManager.GetActiveScene()))
            saveable.SaveData(ref gameData);

        dialogueManager.GetPreview(out string previewSpeaker, out string previewText);

        gameData.saveVersion = GameData.CurrentVersion;
        gameData.metadata.timestampUtc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
        gameData.metadata.chapterText = storyManager.CurrentChapterLabel;
        gameData.metadata.previewSpeaker = previewSpeaker;
        gameData.metadata.previewText = DialogueText.Truncate(previewText, 90);

        return gameData;
    }

    private void WriteThumbnail(string key, byte[] png)
    {
        try
        {
            File.WriteAllBytes(GetThumbnailPath(key), png);
        }
        catch (Exception e)
        {
            Debug.LogWarning("Could not write save thumbnail.\n" + e.Message);
        }
    }

    #endregion

    #region Loading

    public void LoadFromSlot(string key)
    {
        if (IsBusy || IsRestoring || gameManager.IsTransitioning)
            return;

        GameData data = dataHandler.LoadData(GetSavePath(key));

        if (data == null)
        {
            ui.Notify("This save can't be loaded");
            return;
        }

        if (data.saveVersion > GameData.CurrentVersion)
        {
            ui.Notify("This save is from a newer version of the game");
            return;
        }

        if (string.IsNullOrEmpty(data.sceneName) || !Application.CanStreamedLevelBeLoaded(data.sceneName))
        {
            Debug.LogError($"Save '{key}' is in scene '{data.sceneName}', which isn't in the Build Profile's scene list.");
            ui.Notify("This save can't be loaded");
            return;
        }

        StartCoroutine(LoadCo(data));
    }

    public void QuickLoad()
    {
        SaveSlotInfo quick = GetSlotInfo(QuickSlot);

        if (quick.IsLoadable)
            LoadFromSlot(QuickSlot);
        else
            ui.Notify("No quick save yet");
    }

    public void LoadLatest()
    {
        SaveSlotInfo latest = GetLatestSlot();

        if (latest != null)
            LoadFromSlot(latest.key);
    }

    private IEnumerator LoadCo(GameData data)
    {
        IsRestoring = true;
        pendingThumbnail = null;

        yield return gameManager.TransitionCo(data.sceneName,
            beforeLoad: () =>
            {
                gameData = data;

                // Systems that live across scenes: story values, dialogue history, play time
                foreach (ISaveable saveable in GetPersistentSaveables())
                    saveable.LoadData(data);
            },
            onSceneLoaded: scene =>
            {
                // Runs after the scene's Awake/OnEnable but before any Start,
                // so triggers already see the restored state and the resumed dialogue
                foreach (ISaveable saveable in GetSceneSaveables(scene))
                    saveable.LoadData(data);

                dialogueManager.ResumePending();
            });

        IsRestoring = false;
    }

    // New Game starts from empty data
    public void ResetGameData()
    {
        gameData = new GameData();
        pendingThumbnail = null;
    }

    #endregion

    private IEnumerable<ISaveable> GetPersistentSaveables() => transform.root.GetComponentsInChildren<ISaveable>(true);

    private IEnumerable<ISaveable> GetSceneSaveables(Scene scene)
    {
        return FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include)
            .Where(behaviour => behaviour.gameObject.scene == scene && !behaviour.transform.IsChildOf(transform.root))
            .OfType<ISaveable>();
    }

    private void HandleMenusChanged()
    {
        if (!ui.IsModalOpen)
            pendingThumbnail = null;
    }

#if UNITY_EDITOR
    private void Reset()
    {
        gameManager = transform.root.GetComponentInChildren<GameManager>(true);
        storyManager = transform.root.GetComponentInChildren<StoryManager>(true);
        dialogueManager = transform.root.GetComponentInChildren<DialogueManager>(true);
        ui = transform.root.GetComponentInChildren<UI>(true);
    }

    [ContextMenu("Open Save Folder")]
    private void OpenSaveFolder()
    {
        Directory.CreateDirectory(SaveDirectory);
        UnityEditor.EditorUtility.RevealInFinder(SaveDirectory);
    }
#endif
}
