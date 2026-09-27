using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Scrollable history of every line read and every choice picked.
public class UI_Backlog : MonoBehaviour
{
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private RectTransform content;
    [SerializeField] private UI_BacklogEntry entryPrefab;
    [SerializeField] private Button closeButton;
    [Tooltip("Optional. Shown when nothing has been read yet.")]
    [SerializeField] private GameObject emptyLabel;

    private readonly List<UI_BacklogEntry> entryPool = new List<UI_BacklogEntry>();

    private void Awake()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(Close);
    }

    public void Open(List<DialogueScreen.BacklogItem> items)
    {
        UI.Instance.OpenMenu(gameObject);
        Populate(items);
        StartCoroutine(ScrollToBottomCo());
    }

    public void Close() => UI.Instance.CloseMenu(gameObject);

    private void Populate(List<DialogueScreen.BacklogItem> items)
    {
        while (entryPool.Count < items.Count)
            entryPool.Add(Instantiate(entryPrefab, content));

        for (int i = 0; i < entryPool.Count; i++)
        {
            bool used = i < items.Count;
            entryPool[i].gameObject.SetActive(used);

            if (used)
                entryPool[i].Setup(items[i]);
        }

        if (emptyLabel != null)
            emptyLabel.SetActive(items.Count == 0);
    }

    // Newest lines are at the bottom. Wait a frame so the layout groups have sized everything.
    private IEnumerator ScrollToBottomCo()
    {
        yield return null;

        LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        scrollRect.verticalNormalizedPosition = 0f;
    }
}
