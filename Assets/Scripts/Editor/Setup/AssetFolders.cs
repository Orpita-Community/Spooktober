using System.IO;
using UnityEditor;

public static class AssetFolders
{
    // Creates "Assets/A/B/C" one folder at a time through the AssetDatabase, so assets can be created inside it
    public static void Ensure(string folder)
    {
        folder = folder.Replace('\\', '/').TrimEnd('/');

        if (AssetDatabase.IsValidFolder(folder))
            return;

        string parent = Path.GetDirectoryName(folder).Replace('\\', '/');
        Ensure(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
    }

    public static void EnsureParent(string assetPath) => Ensure(Path.GetDirectoryName(assetPath));
}
