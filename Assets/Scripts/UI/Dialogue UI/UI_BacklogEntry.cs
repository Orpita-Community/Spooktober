using TMPro;
using UnityEngine;

public class UI_BacklogEntry : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI speakerNameText;
    [SerializeField] private TextMeshProUGUI bodyText;

    [Header("Picked Choice Style")]
    [SerializeField] private string choicePrefix = "> ";
    [SerializeField] private Color choiceColor = new Color(1f, .78f, .35f);

    private Color defaultBodyColor;
    private bool hasDefaultColor;

    public void Setup(DialogueScreen.BacklogItem item)
    {
        if (!hasDefaultColor)
        {
            defaultBodyColor = bodyText.color;
            hasDefaultColor = true;
        }

        bool showName = !item.isChoice && !string.IsNullOrEmpty(item.speakerName);
        speakerNameText.gameObject.SetActive(showName);
        speakerNameText.text = item.speakerName;
        speakerNameText.color = item.nameColor;

        bodyText.text = item.isChoice ? choicePrefix + item.text : item.text;
        bodyText.color = item.isChoice ? choiceColor : defaultBodyColor;
    }
}
