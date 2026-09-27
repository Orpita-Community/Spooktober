using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_SaveSlot : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private RawImage thumbnail;
    [Tooltip("Shown on empty slots and saves without a thumbnail. Optional.")]
    [SerializeField] private Texture2D emptyThumbnail;

    [Header("Labels")]
    [SerializeField] private TextMeshProUGUI slotLabel;
    [SerializeField] private TextMeshProUGUI timestampText;
    [SerializeField] private TextMeshProUGUI chapterText;
    [SerializeField] private TextMeshProUGUI previewText;
    [SerializeField] private GameObject emptyLabel;
    [SerializeField] private GameObject corruptLabel;

    [Header("Delete")]
    [SerializeField] private Button deleteButton;

    private Action onClick;
    private Action onDelete;
    private Texture2D loadedThumbnail;

    public Button Button => button;

    private void Awake()
    {
        button.onClick.AddListener(() => onClick?.Invoke());

        if (deleteButton != null)
            deleteButton.onClick.AddListener(() => onDelete?.Invoke());
    }

    private void OnDestroy() => ReleaseThumbnail();

    public void Setup(SaveSlotInfo info, string label, bool clickable, Action onClick, Action onDelete)
    {
        this.onClick = onClick;
        this.onDelete = onDelete;

        button.interactable = clickable;
        slotLabel.text = label;

        bool hasData = info.IsLoadable;
        SetActive(emptyLabel, !info.exists);
        SetActive(corruptLabel, info.exists && !hasData);

        SetText(timestampText, hasData ? FormatTimestamp(info) : "");
        SetText(chapterText, hasData ? info.metadata.chapterText : "");
        SetText(previewText, hasData ? FormatPreview(info.metadata) : "");

        if (deleteButton != null)
            deleteButton.gameObject.SetActive(info.exists && onDelete != null);

        ReleaseThumbnail();
        loadedThumbnail = hasData ? ThumbnailUtility.LoadPNG(info.thumbnailPath) : null;

        if (thumbnail != null)
        {
            thumbnail.texture = loadedThumbnail != null ? loadedThumbnail : emptyThumbnail;
            thumbnail.enabled = thumbnail.texture != null;
        }
    }

    // Thumbnails are loaded from disk, so they have to be destroyed or they leak
    public void ReleaseThumbnail()
    {
        if (loadedThumbnail == null)
            return;

        if (thumbnail != null && thumbnail.texture == loadedThumbnail)
            thumbnail.texture = null;

        Destroy(loadedThumbnail);
        loadedThumbnail = null;
    }

    private static string FormatTimestamp(SaveSlotInfo info)
    {
        string date = info.Timestamp == DateTime.MinValue ? "" : info.Timestamp.ToLocalTime().ToString("yyyy-MM-dd  HH:mm");
        TimeSpan playTime = TimeSpan.FromSeconds(info.metadata.playTimeSeconds);
        return $"{date}   {(int)playTime.TotalHours}:{playTime.Minutes:00}";
    }

    private static string FormatPreview(SaveMetadata metadata)
    {
        if (string.IsNullOrEmpty(metadata.previewText))
            return "";

        return string.IsNullOrEmpty(metadata.previewSpeaker)
            ? metadata.previewText
            : $"{metadata.previewSpeaker}: {metadata.previewText}";
    }

    private static void SetText(TextMeshProUGUI text, string value)
    {
        if (text != null)
            text.text = value;
    }

    private static void SetActive(GameObject target, bool active)
    {
        if (target != null)
            target.SetActive(active);
    }
}
