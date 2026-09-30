using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// The stage behind the dialogue box: a background, up to three characters and a close-up picture.
// View only: DialogueManager builds a StageScreen and hands it over. Live changes fade, rollback and loading snap.
public class UI_Stage : MonoBehaviour
{
    private const string SaturationProperty = "_Saturation";

    [Header("Background")]
    [Tooltip("Shows the current background.")]
    [SerializeField] private Image background;
    [Tooltip("Sits on top of the background and fades the next one in.")]
    [SerializeField] private Image incomingBackground;
    [Tooltip("Spooktober/UI Saturation. Only needed for images with a saturation below 1.")]
    [SerializeField] private Shader saturationShader;

    [Header("Characters (Left, Center, Right)")]
    [SerializeField] private Image[] characterSlots = new Image[3];
    [Tooltip("Characters who aren't speaking are tinted with this, so the speaker stands out. " +
        "The project renders in linear colour space, where tints look softer than their numbers.")]
    [SerializeField] private Color idleTint = new Color(.42f, .4f, .48f, 1f);

    [Header("Close-up")]
    [SerializeField] private CanvasGroup closeUpGroup;
    [SerializeField] private Image closeUpPicture;

    [Header("Timing")]
    [SerializeField] private float backgroundFadeTime = .45f;
    [SerializeField] private float characterFadeTime = .25f;
    [SerializeField] private float closeUpFadeTime = .3f;

    private Material backgroundMaterial;
    private Material incomingMaterial;
    private Stage_ImageSO shownBackground;
    private Stage_ImageSO shownCloseUp;
    private readonly Dialogue_SpeakerSO[] slotSpeakers = new Dialogue_SpeakerSO[3];
    private readonly Coroutine[] slotFades = new Coroutine[3];
    private Coroutine backgroundFade;
    private Coroutine closeUpFade;

    public StageScreen Current { get; private set; }

    private void Awake()
    {
        if (saturationShader != null)
        {
            backgroundMaterial = new Material(saturationShader);
            incomingMaterial = new Material(saturationShader);
        }

        ClearAll();
    }

    private void OnDestroy()
    {
        if (backgroundMaterial != null)
            Destroy(backgroundMaterial);

        if (incomingMaterial != null)
            Destroy(incomingMaterial);
    }

    public void Show() => gameObject.SetActive(true);

    public void Hide()
    {
        StopAllCoroutines();
        ClearAll();
        gameObject.SetActive(false);
    }

    public void Render(StageScreen screen, bool animate)
    {
        Current = screen;
        animate &= gameObject.activeInHierarchy; // Coroutines can't run while hidden

        RenderBackground(screen.background, animate);

        for (int i = 0; i < characterSlots.Length; i++)
            RenderSlot(i, screen.GetActor((StageSlot)i), screen.SomeoneSpeaking, animate);

        RenderCloseUp(screen.closeUp, animate);
    }

    #region Background

    private void RenderBackground(Stage_ImageSO image, bool animate)
    {
        if (image == shownBackground)
            return;

        shownBackground = image;
        StopRunning(ref backgroundFade);

        if (!animate || image == null || background.sprite == null)
        {
            ApplyImage(background, backgroundMaterial, image, 1f);
            SetAlpha(incomingBackground, 0f);
            return;
        }

        ApplyImage(incomingBackground, incomingMaterial, image, 0f);
        backgroundFade = StartCoroutine(BackgroundFadeCo(image));
    }

    private IEnumerator BackgroundFadeCo(Stage_ImageSO image)
    {
        for (float t = 0; t < backgroundFadeTime; t += Time.unscaledDeltaTime)
        {
            SetAlpha(incomingBackground, t / backgroundFadeTime);
            yield return null;
        }

        ApplyImage(background, backgroundMaterial, image, 1f);
        SetAlpha(incomingBackground, 0f);
        backgroundFade = null;
    }

    #endregion

    #region Characters

    private void RenderSlot(int index, StageScreen.Actor actor, bool someoneSpeaking, bool animate)
    {
        Image slot = characterSlots[index];
        if (slot == null)
            return;

        Dialogue_SpeakerSO previous = slotSpeakers[index];
        slotSpeakers[index] = actor != null ? actor.speaker : null;

        if (actor == null || actor.sprite == null)
        {
            if (previous != null && animate)
                StartSlotFade(index, 0f, disableWhenDone: true);
            else
                HideSlot(index);

            return;
        }

        bool entering = previous != actor.speaker || !slot.gameObject.activeSelf;

        slot.gameObject.SetActive(true);
        slot.sprite = actor.sprite;
        LayOut(slot, actor);

        // Everyone is lit during narration. While someone speaks, the others step back.
        Color tint = !someoneSpeaking || actor.speaking ? Color.white : idleTint;

        if (entering && animate)
        {
            StopRunning(ref slotFades[index]);
            slot.color = new Color(tint.r, tint.g, tint.b, 0f);
            StartSlotFade(index, 1f, disableWhenDone: false);
        }
        else
        {
            StopRunning(ref slotFades[index]);
            slot.color = tint;
        }
    }

    // Full-body art: the portrait's height comes from the speaker, its width from the sprite, and it stands on the slot's anchor
    private static void LayOut(Image slot, StageScreen.Actor actor)
    {
        Rect rect = actor.sprite.rect;
        float height = actor.speaker.stageHeight;
        float width = height * rect.width / Mathf.Max(1f, rect.height);

        RectTransform rectTransform = slot.rectTransform;
        rectTransform.sizeDelta = new Vector2(width, height);
        rectTransform.anchoredPosition = new Vector2(0f, actor.speaker.stageOffsetY);
        rectTransform.localScale = new Vector3(actor.flip ? -1f : 1f, 1f, 1f);
    }

    private void StartSlotFade(int index, float targetAlpha, bool disableWhenDone)
    {
        StopRunning(ref slotFades[index]);
        slotFades[index] = StartCoroutine(SlotFadeCo(index, targetAlpha, disableWhenDone));
    }

    private IEnumerator SlotFadeCo(int index, float targetAlpha, bool disableWhenDone)
    {
        Image slot = characterSlots[index];
        float startAlpha = slot.color.a;

        for (float t = 0; t < characterFadeTime; t += Time.unscaledDeltaTime)
        {
            SetAlpha(slot, Mathf.Lerp(startAlpha, targetAlpha, t / characterFadeTime));
            yield return null;
        }

        SetAlpha(slot, targetAlpha);
        slotFades[index] = null;

        if (disableWhenDone)
            slot.gameObject.SetActive(false);
    }

    private void HideSlot(int index)
    {
        StopRunning(ref slotFades[index]);

        if (characterSlots[index] != null)
            characterSlots[index].gameObject.SetActive(false);
    }

    #endregion

    #region Close-up

    private void RenderCloseUp(Stage_ImageSO image, bool animate)
    {
        if (image == shownCloseUp || closeUpGroup == null)
            return;

        bool wasShown = shownCloseUp != null;
        shownCloseUp = image;
        StopRunning(ref closeUpFade);

        if (image != null)
        {
            closeUpPicture.sprite = image.sprite;
            closeUpPicture.color = image.tint;
        }

        float target = image != null ? 1f : 0f;

        // Swapping one close-up for another just changes the picture
        if (!animate || (wasShown && image != null))
            closeUpGroup.alpha = target;
        else
            closeUpFade = StartCoroutine(CloseUpFadeCo(target));
    }

    private IEnumerator CloseUpFadeCo(float target)
    {
        float start = closeUpGroup.alpha;

        for (float t = 0; t < closeUpFadeTime; t += Time.unscaledDeltaTime)
        {
            closeUpGroup.alpha = Mathf.Lerp(start, target, t / closeUpFadeTime);
            yield return null;
        }

        closeUpGroup.alpha = target;
        closeUpFade = null;
    }

    #endregion

    private void ClearAll()
    {
        shownBackground = null;
        shownCloseUp = null;
        Current = null;

        ApplyImage(background, backgroundMaterial, null, 1f);
        SetAlpha(incomingBackground, 0f);

        for (int i = 0; i < characterSlots.Length; i++)
        {
            slotSpeakers[i] = null;
            slotFades[i] = null;

            if (characterSlots[i] != null)
                characterSlots[i].gameObject.SetActive(false);
        }

        if (closeUpGroup != null)
            closeUpGroup.alpha = 0f;

        backgroundFade = null;
        closeUpFade = null;
    }

    private static void ApplyImage(Image target, Material material, Stage_ImageSO image, float alpha)
    {
        if (target == null)
            return;

        target.sprite = image != null ? image.sprite : null;
        target.enabled = target.sprite != null;

        Color tint = image != null ? image.tint : Color.white;
        target.color = new Color(tint.r, tint.g, tint.b, alpha);

        // Only images that need it pay for the saturation shader
        bool desaturated = image != null && image.saturation < 1f && material != null;
        target.material = desaturated ? material : null;

        if (desaturated)
        {
            material.SetFloat(SaturationProperty, image.saturation);
            target.SetMaterialDirty(); // The same material may already be assigned, so make sure the new value is used
        }

        if (target.sprite != null && target.TryGetComponent(out AspectRatioFitter fitter))
            fitter.aspectRatio = target.sprite.rect.width / Mathf.Max(1f, target.sprite.rect.height);
    }

    private static void SetAlpha(Graphic graphic, float alpha)
    {
        if (graphic == null)
            return;

        Color color = graphic.color;
        color.a = alpha;
        graphic.color = color;
    }

    private void StopRunning(ref Coroutine coroutine)
    {
        if (coroutine != null)
            StopCoroutine(coroutine);

        coroutine = null;
    }
}
