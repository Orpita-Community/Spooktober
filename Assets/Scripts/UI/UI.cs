using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// UI hub. Menus open on a stack: Esc/Back closes the top one, and the game is paused while any menu is open.
[DefaultExecutionOrder(-100)]
public class UI : MonoBehaviour
{
    public static UI Instance { get; private set; }

    [Header("Panels")]
    public UI_Dialogue dialogueUI;
    public UI_Backlog backlogUI;
    public UI_PauseMenu pauseMenu;
    public UI_SaveLoadMenu saveLoadMenu;
    public UI_ConfirmDialog confirmDialog;
    public UI_FadeScreen fadeScreen;

    [Header("Notification")]
    [SerializeField] private TextMeshProUGUI notificationText;
    [SerializeField] private CanvasGroup notificationGroup;
    [SerializeField] private float notificationDuration = 1.5f;

    private readonly List<GameObject> menuStack = new List<GameObject>();
    private float timeScaleBeforeMenus = 1f;
    private Coroutine notificationCo;

    public bool IsModalOpen => menuStack.Count > 0;
    public GameObject TopMenu => IsModalOpen ? menuStack[menuStack.Count - 1] : null;

    public event Action OnMenusChanged;

    private void Awake()
    {
        Instance = this;

        if (notificationGroup != null)
            notificationGroup.alpha = 0f;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void OpenMenu(GameObject menu)
    {
        if (menu == null)
            return;

        if (menuStack.Contains(menu))
        {
            menuStack.Remove(menu); // Bring it back to the top
        }
        else if (menuStack.Count == 0)
        {
            timeScaleBeforeMenus = Time.timeScale;
            Time.timeScale = 0f;
        }

        menuStack.Add(menu);
        menu.SetActive(true);
        menu.transform.SetAsLastSibling(); // Draw above everything opened before it
        SelectFirstIn(menu);

        OnMenusChanged?.Invoke();
    }

    public void CloseMenu(GameObject menu)
    {
        if (menu == null || !menuStack.Remove(menu))
            return;

        menu.SetActive(false);
        AfterMenuClosed();
    }

    public void Back()
    {
        if (IsModalOpen)
            CloseMenu(TopMenu);
    }

    public void CloseAllMenus()
    {
        if (!IsModalOpen)
            return;

        for (int i = menuStack.Count - 1; i >= 0; i--)
        {
            if (menuStack[i] != null)
                menuStack[i].SetActive(false);
        }

        menuStack.Clear();
        AfterMenuClosed();
    }

    public bool IsMenuOpen(GameObject menu) => menuStack.Contains(menu);

    public void Notify(string message)
    {
        if (notificationText == null || notificationGroup == null)
        {
            Debug.Log(message);
            return;
        }

        if (notificationCo != null)
            StopCoroutine(notificationCo);

        notificationText.text = message;
        notificationGroup.transform.SetAsLastSibling();
        notificationCo = StartCoroutine(NotificationCo());
    }

    private void AfterMenuClosed()
    {
        if (IsModalOpen)
            SelectFirstIn(TopMenu);
        else
        {
            Time.timeScale = timeScaleBeforeMenus;

            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(null);
        }

        OnMenusChanged?.Invoke();
    }

    // So keyboard and gamepad can navigate a menu right after it opens
    private static void SelectFirstIn(GameObject menu)
    {
        if (EventSystem.current == null || menu == null)
            return;

        foreach (Selectable selectable in menu.GetComponentsInChildren<Selectable>())
        {
            if (selectable.IsInteractable() && selectable.navigation.mode != Navigation.Mode.None)
            {
                EventSystem.current.SetSelectedGameObject(selectable.gameObject);
                return;
            }
        }
    }

    private IEnumerator NotificationCo()
    {
        notificationGroup.alpha = 1f;
        yield return new WaitForSecondsRealtime(notificationDuration);

        float fadeTime = .3f;
        for (float t = 0; t < fadeTime; t += Time.unscaledDeltaTime)
        {
            notificationGroup.alpha = 1f - t / fadeTime;
            yield return null;
        }

        notificationGroup.alpha = 0f;
        notificationCo = null;
    }
}
