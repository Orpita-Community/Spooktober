using UnityEditor;
using UnityEngine;

// Headless-only setup of the sample content (no editor menu, so it can't overwrite work by accident):
//   Unity.exe -batchmode -projectPath <project> -executeMethod SpooktoberSetup.BuildAll -quit
// Running it overwrites the sample story data, the GameSystems and UI prefabs, and the MainMenu and Shop scenes.
public static class SpooktoberSetup
{
    public static void BuildAll()
    {
        SampleStoryBuilder.Result story = SampleStoryBuilder.Build();
        AssetDatabase.SaveAssets();

        GameSystemsBuilder.Build(story.Load<Dialogue_DatabaseSO>(story.databasePath), SampleSceneBuilder.MainMenuScene, SampleSceneBuilder.ShopScene);
        SampleSceneBuilder.Build(story);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[Spooktober] Sample content built: data, GameSystems prefab, MainMenu and Shop scenes.");
    }
}
