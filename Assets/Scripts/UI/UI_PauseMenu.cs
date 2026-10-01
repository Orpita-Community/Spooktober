using UnityEngine;
using UnityEngine.UI;

public class UI_PauseMenu : MonoBehaviour
{
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button saveButton;
    [SerializeField] private Button loadButton;
    [SerializeField] private Button historyButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button quitButton;

    private void Awake()
    {
        resumeButton.onClick.AddListener(Close);
        saveButton.onClick.AddListener(() => UI.Instance.saveLoadMenu.Open(SaveLoadMode.Save));
        loadButton.onClick.AddListener(() => UI.Instance.saveLoadMenu.Open(SaveLoadMode.Load));

        if (historyButton != null)
            historyButton.onClick.AddListener(() => DialogueManager.Instance.OpenBacklog());

        if (settingsButton != null)
            settingsButton.onClick.AddListener(() => UI.Instance.settingsMenu.Open());

        mainMenuButton.onClick.AddListener(() =>
            UI.Instance.confirmDialog.Ask("Return to the main menu?\nUnsaved progress will be lost.", GameManager.Instance.ReturnToMainMenu));

        quitButton.onClick.AddListener(() =>
            UI.Instance.confirmDialog.Ask("Quit the game?\nUnsaved progress will be lost.", GameManager.Instance.QuitGame));
    }

    public void Open()
    {
        UI.Instance.OpenMenu(gameObject);

        // Saving is blocked while a save is being written
        saveButton.interactable = SaveManager.Instance.CanSave;
    }

    private void Close() => UI.Instance.CloseMenu(gameObject);
}
