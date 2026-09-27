using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Yes/No popup. Esc (Back) counts as No.
public class UI_ConfirmDialog : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private Button yesButton;
    [SerializeField] private Button noButton;

    private Action onYes;

    private void Awake()
    {
        yesButton.onClick.AddListener(Confirm);
        noButton.onClick.AddListener(Close);
    }

    private void OnDisable() => onYes = null;

    public void Ask(string message, Action onYes)
    {
        messageText.text = message;
        this.onYes = onYes;

        UI.Instance.OpenMenu(gameObject);

        // Default to the safe answer
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(noButton.gameObject);
    }

    private void Confirm()
    {
        Action action = onYes;
        Close();
        action?.Invoke();
    }

    private void Close() => UI.Instance.CloseMenu(gameObject);
}
