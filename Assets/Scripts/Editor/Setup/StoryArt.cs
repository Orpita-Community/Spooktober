using System.Linq;
using UnityEditor;
using UnityEngine;

// The game's art as the story and scenes use it.
// The artist sliced most files in the Sprite Editor, so each file's largest sprite is the picture itself.
public static class StoryArt
{
    // Backgrounds (1920x1080)
    public const string Street = "Assets/Art/prologue/prologue 1.0.jpg";
    public const string ShopFront = "Assets/Art/prologue/transition scene.jpg";
    public const string ShopWithNotice = "Assets/Art/prologue/Basic shop 2 جواب.jpg";
    public const string Shop = "Assets/Art/act 1/Basic shop.jpg";
    public const string WorkshopCape = "Assets/Art/act 2/Box 1.jpg";
    public const string WorkshopDress = "Assets/Art/act 2/Box 2.jpg";
    public const string ShopDawn = "Assets/Art/act 3/Humanity Ending.jpg";
    public const string ShopCold = "Assets/Art/act 3/bg Cynicism Ending.jpg";

    // Close-ups
    public const string FinalNotice = "Assets/Art/prologue/جواب.png";
    public const string MaskBox = "Assets/Art/act 1/Boxes.png";
    public const string MaskInBox = "Assets/Art/act 1/Mask in Box.png";
    public const string Mask = "Assets/Art/Characters/Mask.png";
    public const string SignedPaper = "Assets/Art/Paper.png";

    // Characters (full body)
    public const string Adam = "Assets/Art/Characters/Adam.png";
    public const string AdamMasked = "Assets/Art/Characters/Adam with Mask.png";
    public const string YoungAdam = "Assets/Art/Characters/Young Adam.png";
    public const string Vance = "Assets/Art/Characters/Mr vence_.png";
    // Cut from "uncle alex.jpg" (three poses on one sheet), with the colour palette removed
    public const string AlexanderNormal = "Assets/Art/Characters/Alexander/Alexander - Normal.png";
    public const string AlexanderSmile = "Assets/Art/Characters/Alexander/Alexander - Smile.png";
    public const string AlexanderCry = "Assets/Art/Characters/Alexander/Alexander - Cry.png";

    public const string Logo = "Assets/Art/Logo/Logo.png";

    public const string SaturationShader = "Assets/Shaders/UI_Saturation.shader";

    public static Sprite Background(string path) => Load(path, 2048);

    // Vance and Alexander are about 3000px tall, so they need a bigger limit than 2048 to stay sharp
    public static Sprite Character(string path) => Load(path, 4096);

    public static Sprite CloseUp(string path) => Load(path, 2048);

    private static Sprite Load(string path, int maxSize)
    {
        EnsureImport(path, maxSize);

        Sprite sprite = AssetDatabase.LoadAllAssetsAtPath(path)
            .OfType<Sprite>()
            .OrderByDescending(s => s.rect.width * s.rect.height)
            .FirstOrDefault();

        if (sprite == null)
            throw new System.Exception($"No sprite found in {path}. Is the file there and imported as a Sprite?");

        return sprite;
    }

    // Only what the stage can't do without: a sprite, big enough not to be scaled down.
    // Everything else (compression, filtering, the artist's slices) is left as the team set it.
    private static void EnsureImport(string path, int maxSize)
    {
        if (!(AssetImporter.GetAtPath(path) is TextureImporter importer))
            throw new System.Exception($"{path} is missing or isn't a texture.");

        bool changed = false;

        if (importer.textureType != TextureImporterType.Sprite)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            changed = true;
        }

        if (importer.maxTextureSize < maxSize)
        {
            importer.maxTextureSize = maxSize;
            changed = true;
        }

        if (changed)
            importer.SaveAndReimport();
    }
}
