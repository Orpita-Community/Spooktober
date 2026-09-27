using System;
using System.IO;
using UnityEngine;

public static class ThumbnailUtility
{
    // Must be called at the end of a frame (after WaitForEndOfFrame), when the screen is fully drawn
    public static byte[] CaptureScreenPNG(int width, int height)
    {
        Texture2D fullScreen = null;
        Texture2D thumbnail = null;
        RenderTexture renderTexture = null;
        RenderTexture previousActive = RenderTexture.active;

        try
        {
            fullScreen = ScreenCapture.CaptureScreenshotAsTexture();
            renderTexture = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            Graphics.Blit(fullScreen, renderTexture);

            RenderTexture.active = renderTexture;
            thumbnail = new Texture2D(width, height, TextureFormat.RGB24, false);
            thumbnail.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            thumbnail.Apply();

            return thumbnail.EncodeToPNG();
        }
        catch (Exception e)
        {
            Debug.LogWarning("Could not capture a save thumbnail.\n" + e);
            return null;
        }
        finally
        {
            RenderTexture.active = previousActive;

            if (renderTexture != null)
                RenderTexture.ReleaseTemporary(renderTexture);

            if (fullScreen != null)
                UnityEngine.Object.Destroy(fullScreen);

            if (thumbnail != null)
                UnityEngine.Object.Destroy(thumbnail);
        }
    }

    // The caller owns the returned texture and must Destroy it
    public static Texture2D LoadPNG(string path)
    {
        if (string.IsNullOrEmpty(path) || !File.Exists(path))
            return null;

        try
        {
            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGB24, false);

            if (texture.LoadImage(File.ReadAllBytes(path)))
                return texture;

            UnityEngine.Object.Destroy(texture);
        }
        catch (Exception e)
        {
            Debug.LogWarning("Could not load save thumbnail: " + path + "\n" + e.Message);
        }

        return null;
    }
}
