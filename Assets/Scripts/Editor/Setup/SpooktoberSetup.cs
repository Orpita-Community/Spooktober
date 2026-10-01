using UnityEditor;
using UnityEngine;

// Headless-only setup (no editor menu, so it can't overwrite work by accident):
//   Unity.exe -batchmode -projectPath <project> -executeMethod SpooktoberSetup.BuildAll -quit
// Running it rewrites the story data (every conversation's text, see StoryBuilder), the Sound assets, the GameSystems
// and UI prefabs, and the Shop scene. An existing MainMenu scene is kept and only gets what's missing (see SceneBuilder).
public static class SpooktoberSetup
{
    public static void BuildAll()
    {
        StoryBuilder.Result story = StoryBuilder.Build();
        AssetDatabase.SaveAssets();

        GameSystemsBuilder.Build(story.Load<Dialogue_DatabaseSO>(story.databasePath), SceneBuilder.MainMenuScene, SceneBuilder.ShopScene);
        SceneBuilder.Build(story);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[Spooktober] Story, GameSystems prefab, MainMenu and Shop scenes built.");
    }
}
