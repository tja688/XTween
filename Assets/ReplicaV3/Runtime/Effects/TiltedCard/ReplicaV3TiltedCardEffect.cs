using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public sealed class ReplicaV3TiltedCardEffect : ReplicaV3EffectBase
{
    [Header("组件绑定")]
    [Tooltip("整体移动容器")]
    public RectTransform Content;
    [Tooltip("主要翻转的卡片容器")]
    public RectTransform Card;
    [Tooltip("反光层")]
    public RectTransform Glint;
    [Tooltip("视差条束")]
    public RectTransform OverlayBand;
    [Tooltip("跟随鼠标的 Tooltip 容器")]
    public RectTransform Tooltip;
    [Tooltip("Tooltip 透明度控制")]
    public CanvasGroup TooltipGroup;
    [Tooltip("鼠标位置显示文本")]
    public Text TooltipText;
    [Tooltip("徽章文本")]
    public Text BadgeText;
    [Tooltip("提示文本")]
    public Text HintText;

    [Header("交互范围")]
    public RectTransform InteractionHitSource;
    public RectTransform InteractionRangeDependency;
    public bool ShowInteractionRange = true;
    public float InteractionRangePadding = 0f;

    [Header("Motion 参数")]
    public float RotateAmplitude = 14f;
    public float HoverScale = 1.1f;
    public float TooltipRotationMax = 18f;
    public float TooltipRotationVelocityScale = 0.6f;
    public float TooltipRotationLerp = 0.26f;
    public float OverlayParallax = 6f;
    public float OverlayLerp = 0.12f;
    public float TooltipAlphaLerp = 0.16f;
    public Vector2 TooltipOffset = new Vector2(24f, 42f);

    [Header("Spring 参数")]
    public float SpringStiffness = 100f;
    public float SpringDamping = 30f;
    public float SpringMass = 2f;

    [Header("Animation 参数")]
    public float EnterOffset = 180f;
    public float ExitOffset = 180f;

    private readonly List<ReplicaV3ParameterDefinition> mParameters = new List<ReplicaV3ParameterDefinition>
    {
        new ReplicaV3ParameterDefinition { Id = "rotate_amplitude", DisplayName = "翻转幅度", Description = "卡片随鼠标移动时的翻转极值", Kind = ReplicaV3ParameterKind.Float, Min = 0f, Max = 45f, Step = 1f },
        new ReplicaV3ParameterDefinition { Id = "hover_scale", DisplayName = "悬浮缩放", Description = "鼠标悬浮时卡片的放大倍数", Kind = ReplicaV3ParameterKind.Float, Min = 1f, Max = 2f, Step = 0.05f }
    };

    private float mTargetRotX;
    private float mTargetRotY;
    private float mCurrentRotX;
    private float mCurrentRotY;
    private float mVelRotX;
    private float mVelRotY;

    private float mTargetScale = 1f;
    private float mCurrentScale = 1f;
    private float mVelScale;

    private float mTargetTooltipRot;
    private float mCurrentTooltipRot;

    private float mOverlayTargetY;
    private float mOverlayCurrentY;

    private Vector2 mLastLocalPoint;
    private Vector2 mCurrentLocalPoint;
    private bool mPointerInside;

    private Vector2 mBaseContentPos = new Vector2(0f, -22f);

    protected override void OnEffectInitialize()
    {
        EnsureBindings();
        SetCanvasAlpha(1f);
        SetLifecycleLooping();

        if (Content != null) mBaseContentPos = Content.anchoredPosition;

        ResetState();
    }

    protected override void OnEffectReset()
    {
        KillTrackedTweens(false);
        EnsureBindings();
        ResetState();

        if (Content != null)
        {
            Content.anchoredPosition = mBaseContentPos;
        }

        if (Card != null)
        {
            Card.localRotation = Quaternion.identity;
            Card.localScale = Vector3.one;
        }

        if (OverlayBand != null)
        {
            OverlayBand.anchoredPosition = Vector2.zero;
        }

        if (Glint != null)
        {
            Glint.anchoredPosition = Vector2.zero;
            Glint.localRotation = Quaternion.identity;
        }

        SetCanvasAlpha(1f);
        SetLifecycleLooping();
    }

    private void ResetState()
    {
        mPointerInside = false;
        mTargetRotX = 0f;
        mTargetRotY = 0f;
        mCurrentRotX = 0f;
        mCurrentRotY = 0f;
        mVelRotX = 0f;
        mVelRotY = 0f;
        mTargetScale = 1f;
        mCurrentScale = 1f;
        mVelScale = 0f;
        mTargetTooltipRot = 0f;
        mCurrentTooltipRot = 0f;
        mOverlayTargetY = 0f;
        mOverlayCurrentY = 0f;
        mLastLocalPoint = Vector2.zero;
        mCurrentLocalPoint = Vector2.zero;
        if (TooltipGroup != null) TooltipGroup.alpha = 0f;
    }

    protected override void OnPlayIn()
    {
        EnsureBindings();
        KillTrackedTweens(false);

        if (Content != null)
        {
            Content.anchoredPosition = mBaseContentPos + new Vector2(0f, -EnterOffset);
            TrackTween(Content.DOAnchorPos(mBaseContentPos, 0.4f).SetEase(Ease.OutCubic));
        }

        SetCanvasAlpha(0f);
        if (EffectCanvasGroup != null)
        {
            TrackTween(EffectCanvasGroup.DOFade(1f, 0.3f).SetEase(Ease.OutCubic));
        }

        SetLifecycleLooping();
        ResetState();
    }

    protected override void OnPlayOut(Action onComplete)
    {
        KillTrackedTweens(false);

        if (Content != null)
        {
            TrackTween(Content.DOAnchorPos(mBaseContentPos + new Vector2(0f, ExitOffset), 0.3f).SetEase(Ease.InCubic));
        }

        if (EffectCanvasGroup != null)
        {
            TrackTween(EffectCanvasGroup.DOFade(0f, 0.25f).SetEase(Ease.InCubic).OnComplete(() => onComplete?.Invoke()));
            return;
        }

        onComplete?.Invoke();
    }

    protected override void OnEffectTick(float deltaTime, float unscaledDeltaTime)
    {
        EnsureBindings();
        if (Card == null) return;

        UpdatePointerTargets(deltaTime);
        UpdateSpring(deltaTime);
        ApplyVisual(deltaTime);
    }

    private void UpdatePointerTargets(float dt)
    {
        bool inside = false;
        Vector2 localPoint = Vector2.zero;


        Camera cam = ResolveInternalCamera();


        RectTransform hitRc = InteractionHitSource != null ? InteractionHitSource : Card;
        if (hitRc != null && RectTransformUtility.RectangleContainsScreenPoint(hitRc, Input.mousePosition, cam))
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(Card, Input.mousePosition, cam, out localPoint);
            float halfW = Mathf.Max(1f, Card.rect.width * 0.5f);
            float halfH = Mathf.Max(1f, Card.rect.height * 0.5f);
            inside = Mathf.Abs(localPoint.x) <= halfW && Mathf.Abs(localPoint.y) <= halfH;
        }

        mPointerInside = inside;
        mCurrentLocalPoint = localPoint;

        if (!inside)
        {
            mTargetRotX = 0f;
            mTargetRotY = 0f;
            mTargetScale = 1f;
            mTargetTooltipRot = 0f;
            mOverlayTargetY = 0f;
            return;
        }

        Vector2 half = Card.rect.size * 0.5f;
        float normalizedX = Mathf.Clamp(localPoint.x / Mathf.Max(1f, half.x), -1f, 1f);
        float normalizedY = Mathf.Clamp(localPoint.y / Mathf.Max(1f, half.y), -1f, 1f);
        mTargetRotX = -normalizedY * RotateAmplitude;
        mTargetRotY = normalizedX * RotateAmplitude;
        mTargetScale = Mathf.Max(0.6f, HoverScale);

        mOverlayTargetY = normalizedY * OverlayParallax;

        float velocityY = localPoint.y - mLastLocalPoint.y;
        mTargetTooltipRot = Mathf.Clamp(
            -velocityY * TooltipRotationVelocityScale,
            -Mathf.Abs(TooltipRotationMax),
            Mathf.Abs(TooltipRotationMax));

        mLastLocalPoint = localPoint;

        if (TooltipText != null)
        {
            TooltipText.text = $"x:{normalizedX:0.00} y:{normalizedY:0.00}";
        }
    }

    private void UpdateSpring(float dt)
    {
        SpringStep(ref mCurrentRotX, ref mVelRotX, mTargetRotX, SpringStiffness, SpringDamping, SpringMass, dt);
        SpringStep(ref mCurrentRotY, ref mVelRotY, mTargetRotY, SpringStiffness, SpringDamping, SpringMass, dt);
        SpringStep(ref mCurrentScale, ref mVelScale, mTargetScale, SpringStiffness, SpringDamping, SpringMass, dt);

        float tooltipLerp = 1f - Mathf.Pow(1f - Mathf.Clamp01(TooltipRotationLerp), Mathf.Max(1f, dt * 60f));
        mCurrentTooltipRot = Mathf.Lerp(mCurrentTooltipRot, mTargetTooltipRot, tooltipLerp);

        float overlayLerp = 1f - Mathf.Pow(1f - Mathf.Clamp01(OverlayLerp), Mathf.Max(1f, dt * 60f));
        mOverlayCurrentY = Mathf.Lerp(mOverlayCurrentY, mOverlayTargetY, overlayLerp);
    }

    private void ApplyVisual(float dt)
    {
        if (Card != null)
        {
            Card.localRotation = Quaternion.Euler(mCurrentRotX, mCurrentRotY, 0f);
            Card.localScale = Vector3.one * mCurrentScale;
        }

        if (Glint != null)
        {
            float glintX = Mathf.InverseLerp(-RotateAmplitude, RotateAmplitude, mCurrentRotY);
            float glintY = Mathf.InverseLerp(-RotateAmplitude, RotateAmplitude, mCurrentRotX);
            Glint.anchoredPosition = new Vector2((glintX - 0.5f) * 120f, (glintY - 0.5f) * -100f);
            Glint.localRotation = Quaternion.Euler(0f, 0f, (glintX - 0.5f) * 24f);
        }

        if (OverlayBand != null)
        {
            float overlayShiftX = mCurrentRotY * 0.4f;
            float overlayShiftY = mOverlayCurrentY + (mCurrentRotX * 0.3f);
            OverlayBand.anchoredPosition = new Vector2(overlayShiftX, overlayShiftY);
        }

        if (Tooltip != null)
        {
            if (mPointerInside)
            {
                Tooltip.anchoredPosition = mCurrentLocalPoint + TooltipOffset;
            }
            Tooltip.localRotation = Quaternion.Euler(0f, 0f, mCurrentTooltipRot);
        }

        if (TooltipGroup != null)
        {
            float alphaLerp = 1f - Mathf.Pow(1f - Mathf.Clamp01(TooltipAlphaLerp), Mathf.Max(1f, dt * 60f));
            float target = mPointerInside ? 1f : 0f;
            TooltipGroup.alpha = Mathf.Lerp(TooltipGroup.alpha, target, alphaLerp);
        }
    }

    private static void SpringStep(ref float pos, ref float vel, float target, float stiffness, float damping, float mass, float dt)
    {
        float displacement = pos - target;
        float springForce = -stiffness * displacement;
        float dampForce = -damping * vel;
        float accel = (springForce + dampForce) / Mathf.Max(0.001f, mass);
        vel += accel * dt;
        pos += vel * dt;

        if (Mathf.Abs(displacement) < 0.001f && Mathf.Abs(vel) < 0.01f)
        {
            pos = target;
            vel = 0f;
        }
    }

    private Camera ResolveInternalCamera()
    {
        var target = InteractionHitSource != null ? InteractionHitSource : (Card != null ? Card : (EffectRoot != null ? EffectRoot : transform as RectTransform));
        return ResolveReliableEventCamera(target);
    }

    private void EnsureBindings()
    {
        if (EffectRoot == null)
        {
            EffectRoot = transform as RectTransform;
        }

        if (Content == null)
        {
            var content = transform.Find("CardFrame");
            if (content == null && EffectRoot != null)
            {
                content = EffectRoot.Find("CardFrame");
            }

            Content = content as RectTransform;
        }

        if (Card == null)
        {
            RectTransform card = null;
            if (Content != null)
            {
                card = Content.Find("Card") as RectTransform;
            }

            if (card == null && EffectRoot != null)
            {
                card = EffectRoot.Find("CardFrame/Card") as RectTransform;
            }

            Card = card;
        }

        if (Glint == null && Card != null)
        {
            Glint = Card.Find("Glint") as RectTransform;
        }

        if (OverlayBand == null && Card != null)
        {
            OverlayBand = Card.Find("OverlayBand") as RectTransform;
        }

        if (Tooltip == null)
        {
            if (Content != null)
            {
                Tooltip = Content.Find("Tooltip") as RectTransform;
            }

            if (Tooltip == null && EffectRoot != null)
            {
                Tooltip = EffectRoot.Find("CardFrame/Tooltip") as RectTransform;
            }
        }

        if (TooltipGroup == null && Tooltip != null)
        {
            TooltipGroup = Tooltip.GetComponent<CanvasGroup>();
        }

        if (TooltipText == null && Tooltip != null)
        {
            var tooltipText = Tooltip.Find("TooltipText");
            if (tooltipText != null)
            {
                TooltipText = tooltipText.GetComponent<Text>();
            }
        }

        if (BadgeText == null && OverlayBand != null)
        {
            var badge = OverlayBand.Find("Badge");
            if (badge != null)
            {
                BadgeText = badge.GetComponent<Text>();
            }
        }

        if (HintText == null && EffectRoot != null)
        {
            var hint = EffectRoot.Find("Hint");
            if (hint != null)
            {
                HintText = hint.GetComponent<Text>();
            }
        }

        if (InteractionHitSource == null)
        {
            InteractionHitSource = Card != null ? Card : (EffectRoot != null ? EffectRoot : transform as RectTransform);
        }
    }

    public override IReadOnlyList<ReplicaV3ParameterDefinition> GetParameterDefinitions() => mParameters;

    public override bool TryGetFloatParameter(string parameterId, out float value)
    {
        switch (parameterId)
        {
            case "rotate_amplitude": value = RotateAmplitude; return true;
            case "hover_scale": value = HoverScale; return true;
            default: value = 0f; return false;
        }
    }

    public override bool TrySetFloatParameter(string parameterId, float value)
    {
        switch (parameterId)
        {
            case "rotate_amplitude": RotateAmplitude = value; return true;
            case "hover_scale": HoverScale = value; return true;
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

    private void OnDrawGizmos()
    {
        var hitSource = InteractionHitSource != null ? InteractionHitSource : (Card != null ? Card : (EffectRoot != null ? EffectRoot : transform as RectTransform));
        DrawInteractionRangeGizmo(ShowInteractionRange, hitSource, InteractionRangeDependency, InteractionRangePadding);
    }
}
