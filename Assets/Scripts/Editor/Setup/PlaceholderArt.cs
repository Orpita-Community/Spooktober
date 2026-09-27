using System.IO;
using UnityEditor;
using UnityEngine;

// Draws simple placeholder portraits and a shop backdrop so the sample runs before real art exists.
// Replace the PNGs (same file names) with real art and every speaker picks them up.
public static class PlaceholderArt
{
    public const string PortraitFolder = "Assets/Art/Placeholder/Portraits";
    public const string BackgroundFolder = "Assets/Art/Placeholder/Backgrounds";

    private const int PortraitSize = 512;

    private static readonly Color32 Clear = new Color32(0, 0, 0, 0);
    private static readonly Color32 Ink = new Color32(25, 20, 30, 255);

    public enum Character { Adam, Vance }

    public static Sprite Portrait(Character character, PortraitExpression expression)
    {
        string path = $"{PortraitFolder}/{character}_{expression}.png";

        if (!File.Exists(path))
        {
            Painter painter = new Painter(PortraitSize, PortraitSize);
            DrawCharacter(painter, character);
            DrawFace(painter, character, expression);
            WritePng(path, painter);
        }

        return ImportSprite(path, 100f);
    }

    public static Sprite ShopBackground()
    {
        const int width = 480, height = 270;
        string path = $"{BackgroundFolder}/Shop_Night.png";

        if (!File.Exists(path))
        {
            Painter p = new Painter(width, height);

            // Night gradient
            for (int y = 0; y < height; y++)
            {
                Color32 row = Color32.Lerp(new Color32(10, 8, 16, 255), new Color32(34, 24, 48, 255), y / (float)height);
                p.FillRect(0, y, width, y + 1, row);
            }

            // Rainy window
            p.FillRect(318, 118, 452, 242, new Color32(20, 16, 26, 255));
            p.FillRect(324, 124, 446, 236, new Color32(34, 52, 78, 255));
            Random.InitState(31);
            for (int i = 0; i < 70; i++)
            {
                int x = Random.Range(326, 444);
                int y = Random.Range(128, 232);
                p.Line(x, y, x - 2, y - 7, 0.6f, new Color32(92, 118, 150, 255));
            }
            p.FillRect(383, 124, 387, 236, new Color32(20, 16, 26, 255));
            p.FillRect(324, 178, 446, 182, new Color32(20, 16, 26, 255));

            // Shelves with costume boxes
            Color32 wood = new Color32(52, 36, 30, 255);
            Color32[] boxes =
            {
                new Color32(92, 40, 48, 255), new Color32(58, 70, 44, 255), new Color32(88, 66, 30, 255),
                new Color32(60, 44, 86, 255), new Color32(40, 60, 76, 255)
            };
            for (int shelf = 0; shelf < 3; shelf++)
            {
                int y = 96 + shelf * 58;
                p.FillRect(28, y, 272, y + 6, wood);

                for (int b = 0; b < 5; b++)
                {
                    int x = 38 + b * 46 + (shelf * 13 % 17);
                    int h = 22 + (b * 7 + shelf * 5) % 18;
                    p.FillRect(x, y + 6, x + 34, y + 6 + h, boxes[(b + shelf) % boxes.Length]);
                }
            }

            // Counter and a mask lying on it
            p.FillRect(0, 0, width, 62, new Color32(42, 28, 24, 255));
            p.FillRect(0, 58, width, 64, new Color32(66, 46, 38, 255));
            p.FillEllipse(300, 72, 20, 12, new Color32(214, 206, 190, 255));
            p.FillCircle(292, 74, 3, Ink);
            p.FillCircle(308, 74, 3, Ink);

            WritePng(path, p);
        }

        return ImportSprite(path, 27f); // 270px tall = 10 units = an orthographic camera of size 5
    }

    private static void DrawCharacter(Painter p, Character character)
    {
        bool vance = character == Character.Vance;

        Color32 skin = vance ? new Color32(214, 208, 220, 255) : new Color32(232, 198, 172, 255);
        Color32 clothes = vance ? new Color32(36, 22, 46, 255) : new Color32(64, 86, 118, 255);
        Color32 hair = new Color32(58, 42, 36, 255);

        p.FillEllipse(256, 0, 210, 175, clothes);        // Shoulders
        p.FillRect(226, 150, 286, 200, skin);             // Neck
        p.FillCircle(256, 290, 118, skin);                // Head

        if (vance)
        {
            p.FillRect(200, 150, 312, 172, new Color32(122, 30, 52, 255)); // Cravat
            p.FillRect(132, 382, 380, 402, new Color32(24, 14, 30, 255));  // Hat brim
            p.FillRect(176, 400, 336, 500, new Color32(24, 14, 30, 255));  // Hat crown
            p.FillRect(176, 404, 336, 420, new Color32(122, 30, 52, 255)); // Hat band
        }
        else
        {
            p.FillEllipse(256, 360, 128, 70, hair, minY: 330); // Hair
        }
    }

    private static void DrawFace(Painter p, Character character, PortraitExpression expression)
    {
        const int leftEye = 212, rightEye = 300, eyeY = 300, mouthY = 236;
        Color32 skin = character == Character.Vance ? new Color32(214, 208, 220, 255) : new Color32(232, 198, 172, 255);

        int eyeSize = expression switch
        {
            PortraitExpression.Scared => 15,
            PortraitExpression.Shocked => 19,
            PortraitExpression.Happy => 9,
            _ => 11
        };

        p.FillCircle(leftEye, eyeY, eyeSize, Ink);
        p.FillCircle(rightEye, eyeY, eyeSize, Ink);

        switch (expression)
        {
            case PortraitExpression.Happy:
                p.Arc(256, mouthY + 30, 42, 200, 340, 5, Ink);
                break;

            case PortraitExpression.Sad:
                p.Line(186, 336, 232, 344, 4, Ink);
                p.Line(326, 336, 280, 344, 4, Ink);
                p.Arc(256, mouthY - 30, 38, 25, 155, 5, Ink);
                break;

            case PortraitExpression.Angry:
                p.Line(186, 348, 236, 328, 5, Ink);
                p.Line(326, 348, 276, 328, 5, Ink);
                p.Arc(256, mouthY - 34, 44, 40, 140, 5, Ink);
                break;

            case PortraitExpression.Scared:
                p.Line(186, 340, 232, 348, 4, Ink);
                p.Line(326, 340, 280, 348, 4, Ink);
                p.FillCircle(256, mouthY, 14, Ink);
                p.FillCircle(256, mouthY, 8, skin);
                break;

            case PortraitExpression.Shocked:
                p.FillEllipse(256, mouthY - 4, 20, 26, Ink);
                break;

            case PortraitExpression.Smirk:
                p.Line(222, mouthY, 272, mouthY, 3.5f, Ink);
                p.Line(272, mouthY, 296, mouthY + 16, 3.5f, Ink);
                break;

            case PortraitExpression.Worried:
                p.Line(190, 344, 232, 336, 4, Ink);
                p.Line(322, 344, 280, 336, 4, Ink);
                p.Line(226, mouthY, 286, mouthY - 6, 3.5f, Ink);
                break;

            default: // Normal
                p.Line(226, mouthY, 286, mouthY, 3.5f, Ink);
                break;
        }
    }

    private static void WritePng(string path, Painter painter)
    {
        AssetFolders.EnsureParent(path);

        Texture2D texture = new Texture2D(painter.Width, painter.Height, TextureFormat.RGBA32, false);
        texture.SetPixels32(painter.Pixels);
        texture.Apply();
        File.WriteAllBytes(path, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);

        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
    }

    private static Sprite ImportSprite(string path, float pixelsPerUnit)
    {
        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);

        if (importer.textureType != TextureImporterType.Sprite || !Mathf.Approximately(importer.spritePixelsPerUnit, pixelsPerUnit))
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = pixelsPerUnit;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    // Tiny software rasterizer, just enough for flat placeholder shapes
    private class Painter
    {
        public readonly int Width;
        public readonly int Height;
        public readonly Color32[] Pixels;

        public Painter(int width, int height)
        {
            Width = width;
            Height = height;
            Pixels = new Color32[width * height];

            for (int i = 0; i < Pixels.Length; i++)
                Pixels[i] = Clear;
        }

        private void Set(int x, int y, Color32 color)
        {
            if (x >= 0 && x < Width && y >= 0 && y < Height)
                Pixels[y * Width + x] = color;
        }

        public void FillRect(int x0, int y0, int x1, int y1, Color32 color)
        {
            for (int y = y0; y < y1; y++)
                for (int x = x0; x < x1; x++)
                    Set(x, y, color);
        }

        public void FillCircle(float cx, float cy, float r, Color32 color) => FillEllipse(cx, cy, r, r, color);

        public void FillEllipse(float cx, float cy, float rx, float ry, Color32 color, float minY = float.MinValue)
        {
            for (int y = Mathf.FloorToInt(cy - ry); y <= Mathf.CeilToInt(cy + ry); y++)
            {
                if (y < minY)
                    continue;

                for (int x = Mathf.FloorToInt(cx - rx); x <= Mathf.CeilToInt(cx + rx); x++)
                {
                    float dx = (x - cx) / rx;
                    float dy = (y - cy) / ry;

                    if (dx * dx + dy * dy <= 1f)
                        Set(x, y, color);
                }
            }
        }

        public void Line(float x0, float y0, float x1, float y1, float thickness, Color32 color)
        {
            int steps = Mathf.CeilToInt(Vector2.Distance(new Vector2(x0, y0), new Vector2(x1, y1))) + 1;

            for (int i = 0; i <= steps; i++)
            {
                float t = i / (float)steps;
                FillCircle(Mathf.Lerp(x0, x1, t), Mathf.Lerp(y0, y1, t), thickness, color);
            }
        }

        public void Arc(float cx, float cy, float radius, float fromDegrees, float toDegrees, float thickness, Color32 color)
        {
            for (float angle = fromDegrees; angle <= toDegrees; angle += 1.5f)
            {
                float rad = angle * Mathf.Deg2Rad;
                FillCircle(cx + Mathf.Cos(rad) * radius, cy + Mathf.Sin(rad) * radius, thickness, color);
            }
        }
    }
}
