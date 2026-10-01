using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Lives in the MainMenu scene. The Load and Settings menus, confirm dialog, fades and music come from the GameSystems prefab.
public class UI_MainMenu : MonoBehaviour
{
    [SerializeField] private Button newGameButton;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button loadButton;
    [Tooltip("Optional. Opens the volume settings.")]
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button quitButton;

    private void Start()
    {
        newGameButton.onClick.AddListener(() => GameManager.Instance.NewGame());
        continueButton.onClick.AddListener(() => GameManager.Instance.ContinueGame());
        loadButton.onClick.AddListener(() => UI.Instance.saveLoadMenu.Open(SaveLoadMode.Load));
        quitButton.onClick.AddListener(() => GameManager.Instance.QuitGame());

        if (settingsButton != null)
            settingsButton.onClick.AddListener(() => UI.Instance.settingsMenu.Open());

        SaveManager.Instance.OnSlotsChanged += RefreshButtons;
        UI.Instance.OnMenusChanged += RefreshSelection;

        RefreshButtons();
        RefreshSelection();

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayMenuMusic();
    }

    private void OnDestroy()
    {
        if (SaveManager.Instance != null)
            SaveManager.Instance.OnSlotsChanged -= RefreshButtons;

        if (UI.Instance != null)
            UI.Instance.OnMenusChanged -= RefreshSelection;
    }

    private void RefreshButtons()
    {
        bool hasSave = SaveManager.Instance.HasAnySave;
        continueButton.interactable = hasSave;
        loadButton.interactable = hasSave;
    }

    // Give keyboard/gamepad focus back to the menu when the Load or Settings menu closes
    private void RefreshSelection()
    {
        if (UI.Instance.IsModalOpen || EventSystem.current == null)
            return;

        Button first = continueButton.interactable ? continueButton : newGameButton;
        EventSystem.current.SetSelectedGameObject(first.gameObject);
    }
}
