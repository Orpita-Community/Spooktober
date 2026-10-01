using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Volume settings like RPG2D's options: a slider for each mixer group (Master, Music, Sound Effects) plus a toggle
// that switches it on or off. The AudioManager applies and keeps them, so they're in effect from the moment the game starts.
public class UI_Settings : MonoBehaviour
{
    [Header("Master")]
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Toggle masterToggle;

    [Header("Music")]
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Toggle musicToggle;

    [Header("Sound Effects")]
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private Toggle sfxToggle;

    [Space]
    [SerializeField] private Button closeButton;
    [Tooltip("Shown on a toggle's label (its first TextMeshPro child).")]
    [SerializeField] private string onLabel = "ON";
    [SerializeField] private string offLabel = "OFF";

    private void Awake()
    {
        Bind(AudioChannel.Master, masterSlider, masterToggle);
        Bind(AudioChannel.Music, musicSlider, musicToggle);
        Bind(AudioChannel.SFX, sfxSlider, sfxToggle);

        if (closeButton != null)
            closeButton.onClick.AddListener(Close);
    }

    // Esc closes menus through UI.Back(), which never calls Close(), so save here
    private void OnDisable() => PlayerPrefs.Save();

    // The saved settings go in first, so the control the menu selects on opening is one that can be used
    public void Open()
    {
        Show(AudioChannel.Master, masterSlider, masterToggle);
        Show(AudioChannel.Music, musicSlider, musicToggle);
        Show(AudioChannel.SFX, sfxSlider, sfxToggle);

        UI.Instance.OpenMenu(gameObject);
    }

    public void Close() => UI.Instance.CloseMenu(gameObject);

    private void Bind(AudioChannel channel, Slider slider, Toggle toggle)
    {
        if (slider != null)
            slider.onValueChanged.AddListener(value => AudioManager.Instance.SetVolume(channel, value));

        if (toggle != null)
        {
            toggle.onValueChanged.AddListener(on =>
            {
                AudioManager.Instance.SetOn(channel, on);
                ShowToggleState(toggle, slider, on);
            });
        }
    }

    // Without notify, so opening the menu doesn't count as a change (or play a click)
    private void Show(AudioChannel channel, Slider slider, Toggle toggle)
    {
        AudioManager audio = AudioManager.Instance;

        if (slider != null)
            slider.SetValueWithoutNotify(audio.GetVolume(channel));

        if (toggle != null)
        {
            toggle.SetIsOnWithoutNotify(audio.IsOn(channel));
            ShowToggleState(toggle, slider, toggle.isOn);
        }
    }

    private void ShowToggleState(Toggle toggle, Slider slider, bool on)
    {
        TextMeshProUGUI label = toggle.GetComponentInChildren<TextMeshProUGUI>(true);
        if (label != null)
            label.text = on ? onLabel : offLabel;

        // The value is kept while switched off; the slider just can't be moved, and fades if it has a CanvasGroup
        if (slider != null)
        {
            slider.interactable = on;

            if (slider.TryGetComponent(out CanvasGroup group))
                group.alpha = on ? 1f : .4f;
        }
    }
}
