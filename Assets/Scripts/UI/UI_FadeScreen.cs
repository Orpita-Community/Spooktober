using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Uses unscaled time, so fades still run while the game is paused (e.g. loading from the pause menu).
public class UI_FadeScreen : MonoBehaviour
{
    public Coroutine fadeEffectCo { get; private set; }
    private Image fadeImage;

    private void Awake()
    {
        fadeImage = GetComponent<Image>();
        SetAlpha(1); // Start black, GameManager fades in
    }

    public void FadeIn(float duration = 1) // black to transparent
    {
        SetAlpha(1);
        FadeEffect(0f, duration);
    }

    public void FadeOut(float duration = 1) // transparent to black
    {
        SetAlpha(fadeImage.color.a); // Continue from the current alpha if a fade in was interrupted
        FadeEffect(1f, duration);
    }

    public IEnumerator FadeInCo(float duration = 1)
    {
        FadeIn(duration);
        yield return fadeEffectCo;
    }

    public IEnumerator FadeOutCo(float duration = 1)
    {
        FadeOut(duration);
        yield return fadeEffectCo;
    }

    private void FadeEffect(float targetAlpha, float duration)
    {
        if (fadeEffectCo != null)
        {
            StopCoroutine(fadeEffectCo);
        }

        fadeEffectCo = StartCoroutine(FadeEffectCo(targetAlpha, duration));
    }

    private IEnumerator FadeEffectCo(float targetAlpha, float duration)
    {
        float startAlpha = fadeImage.color.a;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            SetAlpha(Mathf.Lerp(startAlpha, targetAlpha, elapsedTime / duration));

            yield return null;
        }

        SetAlpha(targetAlpha);
        fadeEffectCo = null;
    }

    private void SetAlpha(float alpha)
    {
        Color color = fadeImage.color;
        color.a = alpha;
        fadeImage.color = color;
        fadeImage.raycastTarget = alpha > 0f; // Block clicks while the screen is covered
    }
}
