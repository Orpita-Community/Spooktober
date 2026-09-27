using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

// Root of Resources/GameSystems.prefab (managers, UI canvas, EventSystem).
// Created once before the first scene loads and kept for the whole session, whichever scene you press Play in.
// Don't place this prefab in scenes.
[DefaultExecutionOrder(-200)]
public class GameSystems : MonoBehaviour
{
    public const string ResourcePath = "GameSystems";

    public static GameSystems Instance { get; private set; }

    [SerializeField] private EventSystem eventSystem;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance != null)
            return;

        GameSystems prefab = Resources.Load<GameSystems>(ResourcePath);

        if (prefab == null)
        {
            Debug.LogWarning($"No GameSystems prefab found at Resources/{ResourcePath}. Dialogue and saving won't work.");
            return;
        }

        GameSystems systems = Instantiate(prefab);
        systems.name = prefab.name;
        DontDestroyOnLoad(systems.gameObject);
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("A second GameSystems was found (is the prefab placed in a scene?). Destroying it.", this);
            Destroy(gameObject);
            return;
        }

        Instance = this;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void Start() => RemoveSceneEventSystems();

    private void OnDestroy()
    {
        if (Instance != this)
            return;

        Instance = null;
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode) => RemoveSceneEventSystems();

    // Unity adds an EventSystem to a scene whenever a Canvas is created there. Only ours should exist.
    private void RemoveSceneEventSystems()
    {
        foreach (EventSystem other in FindObjectsByType<EventSystem>(FindObjectsInactive.Include))
        {
            if (other == eventSystem || other.transform.IsChildOf(transform))
                continue;

            Destroy(other.gameObject);
        }
    }

#if UNITY_EDITOR
    private void Reset() => eventSystem = GetComponentInChildren<EventSystem>(true);
#endif
}
