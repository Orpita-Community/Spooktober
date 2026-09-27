using System.Collections.Generic;
using UnityEngine;

// Starts a conversation when interacted with (IInteractable), or on scene start.
// Play once: add a requirement "Flag == 0" and an effect "Flag = 1".
public class DialogueTrigger : MonoBehaviour, IInteractable
{
    [SerializeField] private Dialogue_ConversationSO conversation;

    [Header("Conditions")]
    [SerializeField] private List<StoryCondition> requirements = new List<StoryCondition>();
    [Tooltip("Applied when the conversation starts.")]
    [SerializeField] private List<StoryEffect> effectsOnPlay = new List<StoryEffect>();

    [Header("Auto Play")]
    [Tooltip("Start the conversation when the scene starts (not when loading a save).")]
    [SerializeField] private bool playOnStart;

    public bool CanPlay =>
        conversation != null
        && DialogueManager.Instance != null
        && !DialogueManager.Instance.IsActive
        && StoryManager.Instance != null
        && StoryManager.Instance.Evaluate(requirements);

    private void Start()
    {
        bool restoringSave = SaveManager.Instance != null && SaveManager.Instance.IsRestoring;

        if (playOnStart && !restoringSave)
            TryPlay();
    }

    public void Interact() => TryPlay();

    public bool TryPlay()
    {
        if (!CanPlay || !DialogueManager.Instance.StartDialogue(conversation))
            return false;

        StoryManager.Instance.State.Apply(effectsOnPlay);
        return true;
    }
}
