using System;
using System.Collections.Generic;
using UnityEngine;

// Stable ids for save files and (later) localization keys.
// Assets use their asset GUID. Lines and choices inside an asset get a short random id.
public static class SaveIDUtility
{
    public static string NewId() => Guid.NewGuid().ToString("N").Substring(0, 8);

    // Gives every item a unique id. Unity's list "+" button copies the last element including its id,
    // so duplicates get a fresh id while the first occurrence keeps the original one.
    public static bool EnsureUniqueIds<T>(IList<T> items, Func<T, string> getId, Action<T, string> setId, HashSet<string> usedIds = null)
    {
        if (items == null)
            return false;

        usedIds ??= new HashSet<string>();
        bool changed = false;

        foreach (T item in items)
        {
            if (item == null)
                continue;

            string id = getId(item);

            if (string.IsNullOrEmpty(id) || usedIds.Contains(id))
            {
                do { id = NewId(); } while (usedIds.Contains(id));

                setId(item, id);
                changed = true;
            }

            usedIds.Add(id);
        }

        return changed;
    }

#if UNITY_EDITOR
    // Always re-stamp instead of only filling empty ids: a duplicated asset (Ctrl+D) copies the original's id
    public static bool StampAssetGUID(UnityEngine.Object asset, ref string saveID)
    {
        string path = UnityEditor.AssetDatabase.GetAssetPath(asset);
        if (string.IsNullOrEmpty(path))
            return false; // Not saved to disk yet (e.g. just created from the menu)

        string guid = UnityEditor.AssetDatabase.AssetPathToGUID(path);
        if (string.IsNullOrEmpty(guid) || guid == saveID)
            return false;

        saveID = guid;
        UnityEditor.EditorUtility.SetDirty(asset);
        return true;
    }
#endif
}
