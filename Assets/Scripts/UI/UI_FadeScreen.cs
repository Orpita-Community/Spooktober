using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Full-screen black fade, plus title cards ("ACT 1" / "The Man in the Rain") shown while the screen is black.
// Uses unscaled time, so fades still run while the game is paused (e.g. loading from the pause menu).
public class UI_FadeScreen : MonoBehaviour, IPointerClickHandler
{
    [Header("Title Card (optional)")]
    [SerializeField] private CanvasGroup titleCard;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI subtitleText;
    [SerializeField] private float titleFadeTime = .7f;
    [Tooltip("How long the card stays up. Clicking or pressing Advance cuts it short.")]
    [SerializeField] private float titleHoldTime = 2.2f;

    public Coroutine fadeEffectCo { get; private set; }
    private Image fadeImage;
    private Color screenColor = Color.black;
    private int lastClickFrame = -1;

    public event Action<TitleCard> OnTitleCardShown;

    private void Awake()
    {
        fadeImage = GetComponent<Image>();
        SetAlpha(1); // Start black, GameManager fades in

        if (titleCard != null)
            titleCard.alpha = 0f;
    }

    public void FadeIn(float duration = 1) // black to transparent
    {
        SetScreenColor(Color.black);
        SetAlpha(1);
        FadeEffect(0f, duration);
    }

    public void FadeOut(float duration = 1) // transparent to black
    {
        SetScreenColor(Color.black);
        SetAlpha(fadeImage.color.a); // Continue from the current alpha if a fade in was interrupted
        FadeEffect(1f, duration);
    }

    public IEnumerator FadeInCo(float duration = 1)
    {
        FadeIn(duration);
        yield return WaitForFadeCo();
    }

    public IEnumerator FadeOutCo(float duration = 1)
    {
        FadeOut(duration);
        yield return WaitForFadeCo();
    }

    // Jumps to a colour (e.g. white) and fades it away, showing whatever is underneath
    public void Flash(Color color, float duration = .6f)
    {
        SetScreenColor(color);
        SetAlpha(color.a);
        FadeEffect(0f, duration);
    }

    // Shows a title card on the black screen: text fades in, holds, fades out. Call it once the screen is black.
    public IEnumerator TitleCardCo(TitleCard card, bool quick, Func<bool> skipRequested = null)
    {
        if (card == null || card.IsEmpty || titleCard == null)
            yield break;

        titleText.text = card.title;
        subtitleText.text = card.subtitle;
        subtitleText.gameObject.SetActive(!string.IsNullOrWhiteSpace(card.subtitle));
        OnTitleCardShown?.Invoke(card);

        float fadeTime = quick ? titleFadeTime * .35f : titleFadeTime;
        float holdTime = quick ? .4f : titleHoldTime;

        yield return FadeTitleCo(1f, fadeTime);

        int holdStartFrame = Time.frameCount;

        for (float t = 0; t < holdTime; t += Time.unscaledDeltaTime)
        {
            if (lastClickFrame >= holdStartFrame || (skipRequested != null && skipRequested()))
                break;

            yield return null;
        }

        yield return FadeTitleCo(0f, fadeTime);
    }

    // The black screen catches clicks while it's up, which is how a click cuts a title card short
    public void OnPointerClick(PointerEventData eventData) => lastClickFrame = Time.frameCount;

    private IEnumerator FadeTitleCo(float target, float duration)
    {
        float start = titleCard.alpha;

        for (float t = 0; t < duration; t += Time.unscaledDeltaTime)
        {
            titleCard.alpha = Mathf.Lerp(start, target, t / duration);
            yield return null;
        }

        titleCard.alpha = target;
    }

    // Stops waiting if another fade takes over, instead of waiting on a coroutine that was stopped
    private IEnumerator WaitForFadeCo()
    {
        Coroutine mine = fadeEffectCo;

        while (mine != null && fadeEffectCo == mine)
            yield return null;
    }

    private void FadeEffect(float targetAlpha, float duration)
    {
        if (fadeEffectCo != null)
        {
            StopCoroutine(fadeEffectCo);
            fadeEffectCo = null;
        }

        // An instant fade would finish inside StartCoroutine and leave fadeEffectCo pointing at a finished fade
        if (duration <= 0f)
        {
            SetAlpha(targetAlpha);
            return;
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

    private void SetScreenColor(Color color)
    {
        screenColor = color;
        SetAlpha(fadeImage.color.a);
    }

    private void SetAlpha(float alpha)
    {
        fadeImage.color = new Color(screenColor.r, screenColor.g, screenColor.b, alpha);
        fadeImage.raycastTarget = alpha > 0f; // Block clicks while the screen is covered
    }
}
