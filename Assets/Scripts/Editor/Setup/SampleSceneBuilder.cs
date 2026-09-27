using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Builds the MainMenu scene and the Shop scene (the template SampleScene, renamed), then sets the build scene list.
public static class SampleSceneBuilder
{
    public const string MainMenuScene = "MainMenu";
    public const string ShopScene = "Shop";
    public const string MainMenuPath = "Assets/Scenes/" + MainMenuScene + ".unity";
    public const string ShopPath = "Assets/Scenes/" + ShopScene + ".unity";
    private const string TemplateScenePath = "Assets/Scenes/SampleScene.unity";

    private const string BackgroundName = "Shop Background";
    private const string IntroTriggerName = "Intro Trigger";

    public static void Build(SampleStoryBuilder.Result story)
    {
        AssetFolders.Ensure("Assets/Scenes");

        BuildMainMenu();
        BuildShop(story);

        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(MainMenuPath, true),
            new EditorBuildSettingsScene(ShopPath, true)
        };
    }

    private static void BuildMainMenu()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        GameObject cameraGO = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        cameraGO.tag = "MainCamera";
        cameraGO.transform.position = new Vector3(0, 0, -10);
        Camera camera = cameraGO.GetComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 5;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(.03f, .02f, .05f);

        // No EventSystem here: the GameSystems prefab brings its own
        GameObject canvasGO = new GameObject("MainMenuCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGO.layer = LayerMask.NameToLayer("UI");
        UIKit.ConfigureCanvas(canvasGO, 0);

        Image vance = UIKit.Create("Vance", canvasGO.transform, typeof(Image)).GetComponent<Image>();
        vance.sprite = PlaceholderArt.Portrait(PlaceholderArt.Character.Vance, PortraitExpression.Smirk);
        vance.preserveAspect = true;
        vance.color = new Color(1, 1, 1, .85f);
        vance.raycastTarget = false;
        UIKit.Place(vance.gameObject, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-120, 0), new Vector2(760, 760));

        TextMeshProUGUI title = UIKit.Text("Title", canvasGO.transform, "SPOOKTOBER", 110, TextAlignmentOptions.Left,
            new Color(.9f, .62f, 1f), FontStyles.Bold);
        UIKit.Place(title.gameObject, new Vector2(0, 1), new Vector2(0, 1), new Vector2(160, -140), new Vector2(1000, 140));

        TextMeshProUGUI subtitle = UIKit.Text("Subtitle", canvasGO.transform, "One night. One costume shop. One contract.", 34,
            TextAlignmentOptions.Left, UIKit.MutedTextColor, FontStyles.Italic);
        UIKit.Place(subtitle.gameObject, new Vector2(0, 1), new Vector2(0, 1), new Vector2(166, -280), new Vector2(1000, 50));

        GameObject buttons = UIKit.Create("Buttons", canvasGO.transform);
        UIKit.Place(buttons, new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(160, -110), new Vector2(420, 380));
        UIKit.Vertical(buttons, 20, TextAnchor.UpperLeft);

        Vector2 size = new Vector2(380, 72);
        Button newGame = UIKit.Button("NewGameButton", buttons.transform, "New Game", 32, size);
        Button continueGame = UIKit.Button("ContinueButton", buttons.transform, "Continue", 32, size);
        Button load = UIKit.Button("LoadButton", buttons.transform, "Load", 32, size);
        Button quit = UIKit.Button("QuitButton", buttons.transform, "Quit", 32, size);

        UI_MainMenu menu = canvasGO.AddComponent<UI_MainMenu>();
        UIKit.Wire(menu, "newGameButton", newGame);
        UIKit.Wire(menu, "continueButton", continueGame);
        UIKit.Wire(menu, "loadButton", load);
        UIKit.Wire(menu, "quitButton", quit);

        EditorSceneManager.SaveScene(scene, MainMenuPath);
    }

    private static void BuildShop(SampleStoryBuilder.Result story)
    {
        // The template scene already has a camera and a Global Light 2D, so it becomes the Shop
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ShopPath) == null && AssetDatabase.LoadAssetAtPath<SceneAsset>(TemplateScenePath) != null)
        {
            string error = AssetDatabase.MoveAsset(TemplateScenePath, ShopPath);
            if (!string.IsNullOrEmpty(error))
                throw new System.Exception("Could not rename SampleScene to Shop: " + error);
        }

        Scene scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(ShopPath) != null
            ? EditorSceneManager.OpenScene(ShopPath, OpenSceneMode.Single)
            : EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        // Replace what a previous run created, keep anything else in the scene
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == BackgroundName || root.name == IntroTriggerName)
                Object.DestroyImmediate(root);
        }

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.TryGetComponent(out Camera camera))
            {
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(.03f, .02f, .05f);
            }
        }

        GameObject background = new GameObject(BackgroundName, typeof(SpriteRenderer));
        SpriteRenderer renderer = background.GetComponent<SpriteRenderer>();
        renderer.sprite = PlaceholderArt.ShopBackground();
        renderer.sortingOrder = -10;

        // Load after opening the scene (see SampleStoryBuilder.Result)
        Dialogue_ConversationSO intro = story.Load<Dialogue_ConversationSO>(story.introPath);
        Story_VariableSO introPlayedFlag = story.Load<Story_VariableSO>(story.introPlayedFlagPath);

        if (intro == null || introPlayedFlag == null)
            throw new System.Exception("Sample story assets are missing, build the story first.");

        // Plays the opening once per playthrough: requires "Intro Played == 0", then sets it to 1
        GameObject trigger = new GameObject(IntroTriggerName, typeof(DialogueTrigger));
        SerializedObject serialized = new SerializedObject(trigger.GetComponent<DialogueTrigger>());
        UIKit.Property(serialized, "conversation").objectReferenceValue = intro;
        UIKit.Property(serialized, "playOnStart").boolValue = true;

        SerializedProperty requirements = UIKit.Property(serialized, "requirements");
        requirements.arraySize = 1;
        SerializedProperty requirement = requirements.GetArrayElementAtIndex(0);
        requirement.FindPropertyRelative("variable").objectReferenceValue = introPlayedFlag;
        requirement.FindPropertyRelative("comparison").intValue = (int)ComparisonType.Equal;
        requirement.FindPropertyRelative("compareTo").intValue = (int)CompareTarget.Constant;
        requirement.FindPropertyRelative("constant").intValue = 0;

        SerializedProperty effects = UIKit.Property(serialized, "effectsOnPlay");
        effects.arraySize = 1;
        SerializedProperty effect = effects.GetArrayElementAtIndex(0);
        effect.FindPropertyRelative("variable").objectReferenceValue = introPlayedFlag;
        effect.FindPropertyRelative("operation").intValue = (int)EffectOperation.Set;
        effect.FindPropertyRelative("value").intValue = 1;

        serialized.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.SaveScene(scene, ShopPath);
    }
}
