using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Hover and click sounds for a button or toggle, through the AudioManager (so they follow the SFX volume).
// Add it to any button that should make a sound; the setup tools add it to every menu button.
[RequireComponent(typeof(Selectable))]
public class UI_ButtonSound : MonoBehaviour, IPointerEnterHandler
{
    private Selectable selectable;

    private void Awake()
    {
        selectable = GetComponent<Selectable>();

        if (selectable is Button button)
            button.onClick.AddListener(PlayClick);
        else if (selectable is Toggle toggle)
            toggle.onValueChanged.AddListener(_ => PlayClick()); // Code that syncs a toggle uses SetIsOnWithoutNotify, so no sound
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (selectable.IsInteractable() && AudioManager.Instance != null)
            AudioManager.Instance.PlayButtonHover();
    }

    private static void PlayClick()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayButtonClick();
    }
}
