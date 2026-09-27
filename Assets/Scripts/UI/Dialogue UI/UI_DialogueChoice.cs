using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// One choice button. Hovering selects it, so mouse and keyboard/gamepad share the same highlight.
public class UI_DialogueChoice : MonoBehaviour, IPointerEnterHandler
{
    [SerializeField] private Button button;
    [SerializeField] private TextMeshProUGUI label;
    [Tooltip("Shown on the option the player picked (visible during rollback).")]
    [SerializeField] private GameObject pickedMarker;
    [Tooltip("Shown when the option is visible but its requirements aren't met.")]
    [SerializeField] private GameObject lockedMarker;

    private Action onClick;

    public Button Button => button;
    public bool IsInteractable => button != null && button.interactable;

    private void Awake()
    {
        if (button == null)
            button = GetComponent<Button>();

        button.onClick.AddListener(() => onClick?.Invoke());
    }

    public void Setup(string text, bool interactable, bool picked, bool locked, Action onClick)
    {
        if (button == null)
            button = GetComponent<Button>();

        this.onClick = onClick;
        label.text = text;
        button.interactable = interactable;

        if (pickedMarker != null)
            pickedMarker.SetActive(picked);

        if (lockedMarker != null)
            lockedMarker.SetActive(locked);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (IsInteractable)
            button.Select();
    }
}
