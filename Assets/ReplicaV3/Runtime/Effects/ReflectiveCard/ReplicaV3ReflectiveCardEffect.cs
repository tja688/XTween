using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class ReplicaV3ReflectiveCardEffect : ReplicaV3EffectBase, IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler
{
    [Header("交互范围")]
    [Tooltip("主要可交互判定组件。为空时回退到 Card。")]
    public RectTransform InteractionHitSource;

    [Tooltip("交互范围跟随依赖组件。为空时回退到 InteractionHitSource。")]
    public RectTransform InteractionRangeDependency;

    [Tooltip("编辑器中显示可交互范围。默认开启，可手动关闭。")]
    public bool ShowInteractionRange = true;

    [Tooltip("交互范围扩展像素。")]
    public float InteractionRangePadding = 0f;

    [Header("组件绑定（ReflectiveCard）")]
    [Tooltip("内容根节点（用于进出场位移）。")]
    public RectTransform ContentRoot;

    [Tooltip("主卡片节点（进行倾斜与偏移）。")]
    public RectTransform Card;

    [Tooltip("卡片图像。")]
    public Image CardImage;

    [Tooltip("提示文本。")]
    public Text HintText;

    [Tooltip("用户名文本。")]
    public Text UserNameText;

    [Tooltip("职位文本。")]
    public Text RoleText;

    [Tooltip("ID 文本。")]
    public Text IdNumberText;

    [Tooltip("徽标文本。")]
    public Text BadgeText;

    [Tooltip("高光条节点。")]
    public RectTransform Sheen;

    [Tooltip("高光条图像。")]
    public Image SheenImage;

    [Tooltip("高光条 CanvasGroup。")]
    public CanvasGroup SheenGroup;

    [Tooltip("聚光节点。")]
    public RectTransform Spotlight;

    [Tooltip("聚光图像。")]
    public Image SpotlightImage;

    [Tooltip("聚光 CanvasGroup。")]
    public CanvasGroup SpotlightGroup;

    [Tooltip("噪声条节点集合。")]
    public List<RectTransform> NoiseStrips = new List<RectTransform>();

    [Tooltip("噪声条图像集合。")]
    public List<Image> NoiseImages = new List<Image>();

    [Header("参数（可在参数面板实时调）")]
    [Tooltip("倾斜强度。")]
    public float TiltStrength = 12f;

    [Tooltip("平移视差强度。")]
    public float ParallaxStrength = 18f;

    [Tooltip("倾斜平滑速度。")]
    public float TiltSmooth = 8f;

    [Tooltip("悬停混合速度。")]
    public float HoverBlendSpeed = 4f;

    [Tooltip("动量能量平滑速度。")]
    public float MotionEnergySmooth = 10f;

    [Header("高光")]
    [Tooltip("自动扫光速度。")]
    public float SheenAutoSweepSpeed = 0.6f;

    [Tooltip("自动扫光幅度。")]
    public float SheenAutoSweepAmplitude = 40f;

    [Tooltip("鼠标对高光的影响。")]
    public float SheenMouseInfluence = 180f;

    [Tooltip("高光淡入淡出速度。")]
    public float SheenFadeSpeed = 6f;

    [Header("聚光")]
    [Tooltip("聚光 X 范围。")]
    public float SpotlightX = 120f;

    [Tooltip("聚光 Y 范围。")]
    public float SpotlightY = 160f;

    [Tooltip("聚光移动平滑速度。")]
    public float SpotlightMoveSpeed = 8f;

    [Tooltip("聚光淡入淡出速度。")]
    public float SpotlightFadeSpeed = 6f;

    [Header("噪声")]
    [Tooltip("噪声波动速度。")]
    public float NoiseWaveSpeed = 7f;

    [Tooltip("噪声透明度波动速度。")]
    public float NoiseAlphaSpeed = 9f;

    [Header("颜色")]
    [Tooltip("静止卡片颜色。")]
    public Color CardRestColor = new Color(0.12f, 0.15f, 0.20f, 1f);

    [Tooltip("激活卡片颜色。")]
    public Color CardActiveColor = new Color(0.22f, 0.26f, 0.34f, 1f);

    [Header("文案")]
    [Tooltip("提示语。")]
    public string Hint = "ReflectiveCard  |  Hover card for metallic sheen";

    [Tooltip("用户名。")]
    public string UserName = "ALEXANDER DOE";

    [Tooltip("职位。")]
    public string Role = "SENIOR DEVELOPER";

    [Tooltip("ID 编号。")]
    public string IdNumber = "8901-2345-6789";

    [Tooltip("徽标文本。")]
    public string Badge = "\u25CF  SECURE ACCESS";

    [Header("进入/退出")]
    [Tooltip("进入偏移。")]
    public Vector2 EnterOffset = new Vector2(0f, -220f);

    [Tooltip("退出偏移。")]
    public Vector2 ExitOffset = new Vector2(0f, 220f);

    [Tooltip("进入/退出过渡时长。")]
    public float TransitionDuration = 0.45f;

    private readonly List<ReplicaV3ParameterDefinition> mParameters = new List<ReplicaV3ParameterDefinition>
    {
        new ReplicaV3ParameterDefinition { Id = "tilt_strength", DisplayName = "倾斜强度", Description = "卡片的旋转倾斜幅度。", Kind = ReplicaV3ParameterKind.Float, Min = 0f, Max = 25f, Step = 0.1f },
        new ReplicaV3ParameterDefinition { Id = "parallax_strength", DisplayName = "视差强度", Description = "卡片的平移跟随幅度。", Kind = ReplicaV3ParameterKind.Float, Min = 0f, Max = 60f, Step = 0.1f },
        new ReplicaV3ParameterDefinition { Id = "sheen_influence", DisplayName = "高光跟随", Description = "指针对高光偏移的影响。", Kind = ReplicaV3ParameterKind.Float, Min = 0f, Max = 300f, Step = 1f },
        new ReplicaV3ParameterDefinition { Id = "spotlight_x", DisplayName = "聚光X", Description = "聚光横向移动范围。", Kind = ReplicaV3ParameterKind.Float, Min = 0f, Max = 300f, Step = 1f },
        new ReplicaV3ParameterDefinition { Id = "spotlight_y", DisplayName = "聚光Y", Description = "聚光纵向移动范围。", Kind = ReplicaV3ParameterKind.Float, Min = 0f, Max = 300f, Step = 1f }
    };

    private readonly List<float> mNoiseBaseY = new List<float>();
    private Vector2 mCurrentTilt;
    private Vector2 mTargetTilt;
    private Vector2 mCurrentOffset;
    private Vector2 mTargetOffset;
    private Vector2 mPointerNormalized;
    private Vector2 mLastPointerScreenPosition;
    private float mHoverBlend;
    private float mHoverTarget;
    private float mMotionEnergy;
    private float mClock;

    protected override void OnEffectInitialize()
    {
        EnsureBindings();
        CacheNoiseBaseY();
        ApplyText();
        ResetVisualState();
        SetCanvasAlpha(1f);
        SetLifecycleLooping();
    }

    protected override void OnEffectTick(float deltaTime, float unscaledDeltaTime)
    {
        var dt = Mathf.Max(0f, unscaledDeltaTime);
        mClock += dt;

        UpdateHoverBlend(dt);
        UpdateCardTransform(dt);
        UpdateSheenAndSpotlight(dt);
        UpdateNoise(dt);
    }

    protected override void OnPlayIn()
    {
        EnsureBindings();
        KillTrackedTweens(false);

        var duration = Mathf.Max(0.05f, TransitionDuration);
        if (ContentRoot != null)
        {
            ContentRoot.anchoredPosition = EnterOffset;
            TrackTween(ContentRoot.DOAnchorPos(Vector2.zero, duration).SetEase(Ease.OutCubic));
        }

        if (EffectCanvasGroup != null)
        {
            SetCanvasAlpha(0f);
            TrackTween(EffectCanvasGroup.DOFade(1f, duration).SetEase(Ease.OutCubic));
        }

        SetLifecycleLooping();
    }

    protected override void OnPlayOut(Action onComplete)
    {
        KillTrackedTweens(false);
        var duration = Mathf.Max(0.05f, TransitionDuration * 0.85f);
        var pending = 0;
        var finished = 0;

        void TryComplete()
        {
            finished++;
            if (finished >= pending)
            {
                onComplete?.Invoke();
            }
        }

        if (ContentRoot != null)
        {
            pending++;
            TrackTween(ContentRoot.DOAnchorPos(ExitOffset, duration).SetEase(Ease.InCubic).OnComplete(TryComplete));
        }

        if (EffectCanvasGroup != null)
        {
            pending++;
            TrackTween(EffectCanvasGroup.DOFade(0f, duration).SetEase(Ease.InCubic).OnComplete(TryComplete));
        }

        if (pending == 0)
        {
            onComplete?.Invoke();
        }
    }

    protected override void OnEffectReset()
    {
        KillTrackedTweens(false);
        EnsureBindings();
        ApplyText();
        ResetVisualState();
        if (ContentRoot != null)
        {
            ContentRoot.anchoredPosition = Vector2.zero;
        }

        SetCanvasAlpha(1f);
        SetLifecycleLooping();
    }

    public override IReadOnlyList<ReplicaV3ParameterDefinition> GetParameterDefinitions() => mParameters;

    public override bool TryGetFloatParameter(string parameterId, out float value)
    {
        switch (parameterId)
        {
            case "tilt_strength": value = TiltStrength; return true;
            case "parallax_strength": value = ParallaxStrength; return true;
            case "sheen_influence": value = SheenMouseInfluence; return true;
            case "spotlight_x": value = SpotlightX; return true;
            case "spotlight_y": value = SpotlightY; return true;
            default: value = 0f; return false;
        }
    }

    public override bool TrySetFloatParameter(string parameterId, float value)
    {
        switch (parameterId)
        {
            case "tilt_strength": TiltStrength = Mathf.Clamp(value, 0f, 25f); return true;
            case "parallax_strength": ParallaxStrength = Mathf.Clamp(value, 0f, 60f); return true;
            case "sheen_influence": SheenMouseInfluence = Mathf.Clamp(value, 0f, 300f); return true;
            case "spotlight_x": SpotlightX = Mathf.Clamp(value, 0f, 300f); return true;
            case "spotlight_y": SpotlightY = Mathf.Clamp(value, 0f, 300f); return true;
            default: return false;
        }
    }

    public override bool TryGetBoolParameter(string parameterId, out bool value)
    {
        value = false;
        return false;
    }

    public override bool TrySetBoolParameter(string parameterId, bool value)
    {
        return false;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        mHoverTarget = 1f;
        UpdatePointerFromEvent(eventData);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        mHoverTarget = 0f;
    }

    public void OnPointerMove(PointerEventData eventData)
    {
        UpdatePointerFromEvent(eventData);
    }

    private Camera ResolveInteractionCamera(PointerEventData eventData = null)
    {
        var target = InteractionHitSource != null
            ? InteractionHitSource
            : (Card != null ? Card : (EffectRoot != null ? EffectRoot : transform as RectTransform));

        if (eventData != null)
        {
            return ResolveReliableEventCamera(eventData, target);
        }

        return ResolveReliableEventCamera(target);
    }

    private void EnsureBindings()
    {
        if (ContentRoot == null)
        {
            ContentRoot = EffectRoot != null ? EffectRoot : transform as RectTransform;
        }

        if (Card == null)
        {
            Card = ContentRoot;
        }

        if (InteractionHitSource == null)
        {
            InteractionHitSource = Card;
        }
    }

    private void ApplyText()
    {
        if (HintText != null)
        {
            HintText.text = string.IsNullOrWhiteSpace(Hint) ? "ReflectiveCard  |  Hover card for metallic sheen" : Hint;
        }

        if (UserNameText != null)
        {
            UserNameText.text = string.IsNullOrWhiteSpace(UserName) ? "ALEXANDER DOE" : UserName;
        }

        if (RoleText != null)
        {
            RoleText.text = string.IsNullOrWhiteSpace(Role) ? "SENIOR DEVELOPER" : Role;
        }

        if (IdNumberText != null)
        {
            IdNumberText.text = string.IsNullOrWhiteSpace(IdNumber) ? "8901-2345-6789" : IdNumber;
        }

        if (BadgeText != null)
        {
            BadgeText.text = string.IsNullOrWhiteSpace(Badge) ? "\u25CF  SECURE ACCESS" : Badge;
        }
    }

    private void ResetVisualState()
    {
        mCurrentTilt = Vector2.zero;
        mTargetTilt = Vector2.zero;
        mCurrentOffset = Vector2.zero;
        mTargetOffset = Vector2.zero;
        mPointerNormalized = Vector2.zero;
        mHoverBlend = 0f;
        mHoverTarget = 0f;
        mMotionEnergy = 0f;
        mClock = 0f;

        if (Card != null)
        {
            Card.localRotation = Quaternion.identity;
            Card.anchoredPosition = Vector2.zero;
        }

        if (Sheen != null)
        {
            Sheen.anchoredPosition = Vector2.zero;
        }

        if (SheenGroup != null)
        {
            SheenGroup.alpha = 0f;
        }

        if (SheenImage != null)
        {
            var c = SheenImage.color;
            c.a = 0.2f;
            SheenImage.color = c;
        }

        if (Spotlight != null)
        {
            Spotlight.anchoredPosition = Vector2.zero;
        }

        if (SpotlightGroup != null)
        {
            SpotlightGroup.alpha = 0f;
        }

        if (CardImage != null)
        {
            CardImage.color = CardRestColor;
        }
    }

    private void CacheNoiseBaseY()
    {
        mNoiseBaseY.Clear();
        for (var i = 0; i < NoiseStrips.Count; i++)
        {
            var strip = NoiseStrips[i];
            mNoiseBaseY.Add(strip != null ? strip.anchoredPosition.y : 0f);
        }
    }

    private void UpdatePointerFromEvent(PointerEventData eventData)
    {
        if (Card == null || eventData == null)
        {
            return;
        }

        var camera = ResolveInteractionCamera(eventData);
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(Card, eventData.position, camera, out var localPoint))
        {
            return;
        }

        var halfW = Mathf.Max(1f, Card.rect.width * 0.5f);
        var halfH = Mathf.Max(1f, Card.rect.height * 0.5f);
        mPointerNormalized = new Vector2(
            Mathf.Clamp(localPoint.x / halfW, -1f, 1f),
            Mathf.Clamp(localPoint.y / halfH, -1f, 1f));

        var speed = Vector2.Distance(eventData.position, mLastPointerScreenPosition) / 60f;
        mMotionEnergy = Mathf.Lerp(mMotionEnergy, Mathf.Clamp01(speed), Time.unscaledDeltaTime * Mathf.Max(0.1f, MotionEnergySmooth));
        mLastPointerScreenPosition = eventData.position;
    }

    private void UpdateHoverBlend(float dt)
    {
        mHoverBlend = Mathf.Lerp(mHoverBlend, mHoverTarget, dt * Mathf.Max(0.1f, HoverBlendSpeed));
        if (mHoverTarget <= 0f)
        {
            mPointerNormalized = Vector2.Lerp(mPointerNormalized, Vector2.zero, dt * Mathf.Max(0.1f, HoverBlendSpeed));
        }
    }

    private void UpdateCardTransform(float dt)
    {
        if (Card == null)
        {
            return;
        }

        mTargetTilt = new Vector2(
            -mPointerNormalized.y * TiltStrength,
            mPointerNormalized.x * TiltStrength) * mHoverBlend;

        mTargetOffset = new Vector2(
            mPointerNormalized.x * ParallaxStrength,
            mPointerNormalized.y * (ParallaxStrength * 0.7f)) * mHoverBlend;

        mCurrentTilt = Vector2.Lerp(mCurrentTilt, mTargetTilt, dt * Mathf.Max(0.1f, TiltSmooth));
        mCurrentOffset = Vector2.Lerp(mCurrentOffset, mTargetOffset, dt * Mathf.Max(0.1f, TiltSmooth * 0.8f));

        Card.localRotation = Quaternion.Euler(mCurrentTilt.x, mCurrentTilt.y, -mPointerNormalized.x * 1.5f * mHoverBlend);
        Card.anchoredPosition = mCurrentOffset;
    }

    private void UpdateSheenAndSpotlight(float dt)
    {
        if (Sheen != null)
        {
            var autoSweep = Mathf.Sin(mClock * SheenAutoSweepSpeed) * SheenAutoSweepAmplitude;
            var mouseInfluence = mPointerNormalized.x * SheenMouseInfluence;
            var sheenX = (autoSweep + mouseInfluence) * mHoverBlend;
            Sheen.anchoredPosition = new Vector2(sheenX, Sheen.anchoredPosition.y);
        }

        if (SheenGroup != null)
        {
            var alphaTarget = mHoverBlend * Mathf.Lerp(0.4f, 1f, mMotionEnergy);
            SheenGroup.alpha = Mathf.Lerp(SheenGroup.alpha, alphaTarget, dt * Mathf.Max(0.1f, SheenFadeSpeed));
        }

        if (SheenImage != null)
        {
            var c = SheenImage.color;
            c.a = Mathf.Lerp(0.15f, 0.35f, mMotionEnergy);
            SheenImage.color = c;
        }

        if (Spotlight != null)
        {
            var target = new Vector2(mPointerNormalized.x * SpotlightX, mPointerNormalized.y * SpotlightY) * mHoverBlend;
            Spotlight.anchoredPosition = Vector2.Lerp(Spotlight.anchoredPosition, target, dt * Mathf.Max(0.1f, SpotlightMoveSpeed));
        }

        if (SpotlightGroup != null)
        {
            var alphaTarget = mHoverBlend * Mathf.Lerp(0.2f, 0.6f, mMotionEnergy);
            SpotlightGroup.alpha = Mathf.Lerp(SpotlightGroup.alpha, alphaTarget, dt * Mathf.Max(0.1f, SpotlightFadeSpeed));
        }

        if (CardImage != null)
        {
            var cardBlend = mHoverBlend * Mathf.Lerp(0.3f, 1f, mMotionEnergy);
            CardImage.color = Color.Lerp(CardRestColor, CardActiveColor, cardBlend);
        }
    }

    private void UpdateNoise(float dt)
    {
        var strength = mHoverBlend;
        mMotionEnergy = Mathf.Lerp(mMotionEnergy, 0f, dt * Mathf.Max(0.1f, MotionEnergySmooth * 0.4f));

        var count = Mathf.Min(NoiseStrips.Count, NoiseImages.Count);
        for (var i = 0; i < count; i++)
        {
            var strip = NoiseStrips[i];
            var image = NoiseImages[i];
            if (strip == null || image == null)
            {
                continue;
            }

            var baseY = i < mNoiseBaseY.Count ? mNoiseBaseY[i] : strip.anchoredPosition.y;
            var wave = Mathf.Sin((mClock * NoiseWaveSpeed) + (i * 0.52f));
            var direction = (i % 2 == 0) ? 1f : -1f;
            var offset = direction * wave * (2f + (16f * mMotionEnergy)) * strength;
            strip.anchoredPosition = new Vector2(offset, baseY);

            var alphaWave = 0.55f + (0.45f * Mathf.Sin((mClock * NoiseAlphaSpeed) + (i * 0.73f)));
            var color = image.color;
            color.a = Mathf.Lerp(0.005f, 0.12f, mMotionEnergy * strength) * alphaWave;
            image.color = color;
        }
    }

    private void OnDrawGizmos()
    {
        var hitSource = InteractionHitSource != null
            ? InteractionHitSource
            : (Card != null ? Card : (EffectRoot != null ? EffectRoot : transform as RectTransform));
        DrawInteractionRangeGizmo(ShowInteractionRange, hitSource, InteractionRangeDependency, InteractionRangePadding);
    }
}

