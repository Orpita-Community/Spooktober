using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// One menu for both saving and loading. The auto and quick slots only appear when loading.
public class UI_SaveLoadMenu : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private string saveTitle = "Save Game";
    [SerializeField] private string loadTitle = "Load Game";

    [Header("Slots")]
    [Tooltip("Parent of the auto and quick slots, hidden when saving.")]
    [SerializeField] private GameObject specialSlotsRow;
    [SerializeField] private UI_SaveSlot autoSlot;
    [SerializeField] private UI_SaveSlot quickSlot;
    [SerializeField] private Transform slotGrid;
    [SerializeField] private UI_SaveSlot slotPrefab;

    [SerializeField] private Button closeButton;

    private readonly List<UI_SaveSlot> manualSlots = new List<UI_SaveSlot>();
    private SaveLoadMode mode;

    private SaveManager Saves => SaveManager.Instance;

    private void Awake()
    {
        closeButton.onClick.AddListener(Close);
    }

    private void OnEnable() => SaveManager.Instance.OnSlotsChanged += Refresh;

    private void OnDisable()
    {
        if (SaveManager.Instance != null)
            SaveManager.Instance.OnSlotsChanged -= Refresh;

        // Free the thumbnail textures while the menu is closed
        foreach (UI_SaveSlot slot in AllSlots())
            slot.ReleaseThumbnail();
    }

    public void Open(SaveLoadMode mode)
    {
        this.mode = mode;
        UI.Instance.OpenMenu(gameObject);
        Refresh();
    }

    public void Close() => UI.Instance.CloseMenu(gameObject);

    private void Refresh()
    {
        titleText.text = mode == SaveLoadMode.Save ? saveTitle : loadTitle;

        bool loading = mode == SaveLoadMode.Load;
        if (specialSlotsRow != null)
            specialSlotsRow.SetActive(loading);

        if (loading)
        {
            SetupSlot(autoSlot, SaveManager.AutoSlot, "Auto");
            SetupSlot(quickSlot, SaveManager.QuickSlot, "Quick");
        }

        while (manualSlots.Count < Saves.ManualSlotCount)
            manualSlots.Add(Instantiate(slotPrefab, slotGrid));

        for (int i = 0; i < Saves.ManualSlotCount; i++)
            SetupSlot(manualSlots[i], SaveManager.ManualSlotKey(i), $"Slot {i + 1}");

        SelectFirstSlot();
    }

    private void SetupSlot(UI_SaveSlot slot, string key, string label)
    {
        if (slot == null)
            return;

        SaveSlotInfo info = Saves.GetSlotInfo(key);
        bool clickable = mode == SaveLoadMode.Save || info.IsLoadable;

        slot.Setup(info, label, clickable, () => OnSlotClicked(info), () => OnDeleteClicked(info));
    }

    private void OnSlotClicked(SaveSlotInfo info)
    {
        if (mode == SaveLoadMode.Save)
        {
            if (info.exists)
                UI.Instance.confirmDialog.Ask("Overwrite this save?", () => Saves.SaveToSlot(info.key));
            else
                Saves.SaveToSlot(info.key);

            return;
        }

        if (!info.IsLoadable)
            return;

        if (GameManager.Instance.InMainMenu)
            Saves.LoadFromSlot(info.key);
        else
            UI.Instance.confirmDialog.Ask("Load this save?\nUnsaved progress will be lost.", () => Saves.LoadFromSlot(info.key));
    }

    private void OnDeleteClicked(SaveSlotInfo info)
    {
        UI.Instance.confirmDialog.Ask("Delete this save?", () => Saves.DeleteSlot(info.key));
    }

    private void SelectFirstSlot()
    {
        if (EventSystem.current == null)
            return;

        foreach (UI_SaveSlot slot in AllSlots())
        {
            if (slot.gameObject.activeInHierarchy && slot.Button.interactable)
            {
                EventSystem.current.SetSelectedGameObject(slot.gameObject);
                return;
            }
        }
    }

    private IEnumerable<UI_SaveSlot> AllSlots()
    {
        if (autoSlot != null)
            yield return autoSlot;

        if (quickSlot != null)
            yield return quickSlot;

        foreach (UI_SaveSlot slot in manualSlots)
            yield return slot;
    }
}
