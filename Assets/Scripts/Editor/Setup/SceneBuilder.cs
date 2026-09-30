using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Builds the MainMenu scene (logo, Mr. Vance, the menu buttons) and sets up the Shop scene
// (the template SampleScene, renamed): its backdrop and the trigger that starts the prologue. Then sets the build scene list.
public static class SceneBuilder
{
    public const string MainMenuScene = "MainMenu";
    public const string ShopScene = "Shop";
    public const string MainMenuPath = "Assets/Scenes/" + MainMenuScene + ".unity";
    public const string ShopPath = "Assets/Scenes/" + ShopScene + ".unity";
    private const string TemplateScenePath = "Assets/Scenes/SampleScene.unity";

    private const string BackgroundName = "Shop Background";
    private const string IntroTriggerName = "Intro Trigger";

    public static void Build(StoryBuilder.Result story)
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

        // Pure black, so the logo's black background disappears into it
        GameObject cameraGO = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        cameraGO.tag = "MainCamera";
        cameraGO.transform.position = new Vector3(0, 0, -10);
        Camera camera = cameraGO.GetComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 5;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.black;

        // No EventSystem here: the GameSystems prefab brings its own
        GameObject canvasGO = new GameObject("MainMenuCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGO.layer = LayerMask.NameToLayer("UI");
        UIKit.ConfigureCanvas(canvasGO, 0);

        Sprite vanceSprite = StoryArt.Character(StoryArt.Vance);
        Image vance = UIKit.Create("Vance", canvasGO.transform, typeof(Image)).GetComponent<Image>();
        vance.sprite = vanceSprite;
        vance.preserveAspect = true;
        vance.color = new Color(.78f, .76f, .84f, 1f);
        vance.raycastTarget = false;
        float vanceHeight = 1500f;
        UIKit.Place(vance.gameObject, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-150, -560),
            new Vector2(vanceHeight * vanceSprite.rect.width / vanceSprite.rect.height, vanceHeight));

        Sprite logoSprite = StoryArt.Background(StoryArt.Logo);
        Image logo = UIKit.Create("Logo", canvasGO.transform, typeof(Image)).GetComponent<Image>();
        logo.sprite = logoSprite;
        logo.preserveAspect = true;
        logo.raycastTarget = false;
        float logoWidth = 840f;
        UIKit.Place(logo.gameObject, new Vector2(0, 1), new Vector2(0, 1), new Vector2(110, -40),
            new Vector2(logoWidth, logoWidth * logoSprite.rect.height / logoSprite.rect.width));

        GameObject buttons = UIKit.Create("Buttons", canvasGO.transform);
        UIKit.Place(buttons, new Vector2(0, 0), new Vector2(0, 0), new Vector2(200, 110), new Vector2(420, 380));
        UIKit.Vertical(buttons, 20, TextAnchor.LowerLeft);

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

    private static void BuildShop(StoryBuilder.Result story)
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
                camera.backgroundColor = Color.black;
            }
        }

        // The story plays on the stage (in front of everything), but the scene itself still looks like the shop
        Sprite shopSprite = StoryArt.Background(StoryArt.Shop);
        GameObject background = new GameObject(BackgroundName, typeof(SpriteRenderer));
        SpriteRenderer renderer = background.GetComponent<SpriteRenderer>();
        renderer.sprite = shopSprite;
        renderer.sortingOrder = -10;
        float unitsTall = shopSprite.rect.height / shopSprite.pixelsPerUnit;
        background.transform.localScale = Vector3.one * (10f / unitsTall); // Fill an orthographic camera of size 5

        // Load after opening the scene (see StoryBuilder.Result)
        Dialogue_ConversationSO opening = story.Load<Dialogue_ConversationSO>(story.openingPath);
        Story_VariableSO introPlayedFlag = story.Load<Story_VariableSO>(story.introPlayedFlagPath);

        if (opening == null || introPlayedFlag == null)
            throw new System.Exception("Story assets are missing, build the story first.");

        // Plays the prologue once per playthrough: requires "Intro Played == 0", then sets it to 1
        GameObject trigger = new GameObject(IntroTriggerName, typeof(DialogueTrigger));
        SerializedObject serialized = new SerializedObject(trigger.GetComponent<DialogueTrigger>());
        UIKit.Property(serialized, "conversation").objectReferenceValue = opening;
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
