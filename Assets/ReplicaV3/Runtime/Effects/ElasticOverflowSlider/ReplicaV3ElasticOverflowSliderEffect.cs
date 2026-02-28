using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class ReplicaV3ElasticOverflowSliderEffect : ReplicaV3EffectBase,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerDownHandler,
    IPointerUpHandler,
    IDragHandler
{
    private enum OverflowRegion
    {
        Left,
        Middle,
        Right
    }

    [Header("组件绑定（ElasticOverflowSlider）")]
    [Tooltip("交互轨道根节点。")]
    public RectTransform TrackRect;

    [Tooltip("轨道拉伸包装节点。")]
    public RectTransform TrackWrapper;

    [Tooltip("填充条。")]
    public RectTransform FillRect;

    [Tooltip("滑块节点。")]
    public RectTransform KnobRect;

    [Tooltip("左图标节点。")]
    public RectTransform LeftIconRect;

    [Tooltip("右图标节点。")]
    public RectTransform RightIconRect;

    [Tooltip("内容根节点。用于进入退出动画。")]
    public RectTransform ContentRoot;

    [Tooltip("轨道背景图。")]
    public Image TrackBackground;

    [Tooltip("填充图。")]
    public Image FillImage;

    [Tooltip("滑块图。")]
    public Image KnobImage;

    [Tooltip("数值文本。")]
    public Text ValueText;

    [Tooltip("主要可交互判定组件。为空时回退到 TrackRect。")]
    public RectTransform InteractionHitSource;

    [Tooltip("交互范围跟随依赖组件。为空时回退到 InteractionHitSource。")]
    public RectTransform InteractionRangeDependency;

    [Tooltip("编辑器中显示可交互范围。默认开启，可手动关闭。")]
    public bool ShowInteractionRange = true;

    [Tooltip("交互范围扩展像素。")]
    public float InteractionRangePadding = 0f;

    [Header("参数（可在参数面板实时调）")]
    [Tooltip("最小值。")]
    public float MinValue = 0f;

    [Tooltip("最大值。")]
    public float MaxValue = 100f;

    [Tooltip("当前值。")]
    public float Value = 50f;

    [Tooltip("是否启用步进。")]
    public bool Stepped = false;

    [Tooltip("步进大小。")]
    public float StepSize = 1f;

    [Tooltip("最大弹性溢出。")]
    public float MaxOverflow = 50f;

    [Tooltip("溢出回弹弹簧强度。")]
    public float OverflowSpring = 180f;

    [Tooltip("溢出回弹阻尼。")]
    public float OverflowDamping = 12f;

    [Tooltip("悬停时轨道高度。")]
    public float TrackHeightHover = 14f;

    [Tooltip("静止时轨道高度。")]
    public float TrackHeightIdle = 6f;

    [Tooltip("轨道高度变化速度。")]
    public float TrackHeightSpeed = 10f;

    [Tooltip("图标推开速度。")]
    public float IconPushSpeed = 15f;

    [Tooltip("图标基础偏移。")]
    public float IconBaseOffset = 28f;

    [Header("进入/退出")]
    [Tooltip("进入偏移。")]
    public Vector2 EnterOffset = new Vector2(0f, -180f);

    [Tooltip("退出偏移。")]
    public Vector2 ExitOffset = new Vector2(0f, 180f);

    [Tooltip("过渡时长。")]
    public float TransitionDuration = 0.34f;

    private readonly List<ReplicaV3ParameterDefinition> mParameters = new List<ReplicaV3ParameterDefinition>
    {
        new ReplicaV3ParameterDefinition { Id = "value", DisplayName = "当前值", Description = "滑块值。", Kind = ReplicaV3ParameterKind.Float, Min = 0f, Max = 100f, Step = 1f },
        new ReplicaV3ParameterDefinition { Id = "max_overflow", DisplayName = "最大溢出", Description = "拖拽越界弹性上限。", Kind = ReplicaV3ParameterKind.Float, Min = 5f, Max = 180f, Step = 1f },
        new ReplicaV3ParameterDefinition { Id = "overflow_spring", DisplayName = "回弹弹簧", Description = "越大回弹越硬。", Kind = ReplicaV3ParameterKind.Float, Min = 20f, Max = 360f, Step = 1f },
        new ReplicaV3ParameterDefinition { Id = "overflow_damping", DisplayName = "回弹阻尼", Description = "越大越快稳定。", Kind = ReplicaV3ParameterKind.Float, Min = 1f, Max = 60f, Step = 0.5f },
        new ReplicaV3ParameterDefinition { Id = "stepped", DisplayName = "步进模式", Description = "开启后值按步长跳变。", Kind = ReplicaV3ParameterKind.Bool }
    };

    private readonly Vector3[] mTrackCorners = new Vector3[4];
    private bool mIsHovering;
    private bool mIsDragging;
    private float mOverflow;
    private float mOverflowVelocity;
    private float mTrackHeight;
    private float mLeftIconTargetX;
    private float mRightIconTargetX;
    private OverflowRegion mRegion = OverflowRegion.Middle;
    private Vector2 mBaseContentPos;

    protected override void OnEffectInitialize()
    {
        EnsureBindings();
        if (ContentRoot != null)
        {
            mBaseContentPos = ContentRoot.anchoredPosition;
        }

        mTrackHeight = TrackHeightIdle;
        RefreshValueVisual();
        ApplyElasticVisual();
        SetCanvasAlpha(1f);
        SetLifecycleLooping();
    }

    protected override void OnEffectTick(float deltaTime, float unscaledDeltaTime)
    {
        var dt = Mathf.Max(0f, unscaledDeltaTime);

        if (!mIsDragging)
        {
            var force = (-OverflowSpring * mOverflow) - (OverflowDamping * mOverflowVelocity);
            mOverflowVelocity += force * dt;
            mOverflow += mOverflowVelocity * dt;

            if (Mathf.Abs(mOverflow) < 0.1f && Mathf.Abs(mOverflowVelocity) < 0.5f)
            {
                mOverflow = 0f;
                mOverflowVelocity = 0f;
            }
        }

        var targetHeight = (mIsHovering || mIsDragging) ? TrackHeightHover : TrackHeightIdle;
        mTrackHeight = SmoothTo(mTrackHeight, targetHeight, TrackHeightSpeed, dt);

        UpdateIconTargets(dt);
        ApplyElasticVisual();
    }

    protected override void OnPlayIn()
    {
        KillTrackedTweens(false);
        SetCanvasAlpha(0f);

        if (ContentRoot != null)
        {
            ContentRoot.anchoredPosition = mBaseContentPos + EnterOffset;
            TrackTween(ContentRoot
                .DOAnchorPos(mBaseContentPos, Mathf.Max(0.05f, TransitionDuration))
                .SetEase(Ease.OutCubic));
        }

        if (EffectCanvasGroup != null)
        {
            TrackTween(EffectCanvasGroup.DOFade(1f, Mathf.Max(0.05f, TransitionDuration)).SetEase(Ease.OutCubic));
        }

        SetLifecycleLooping();
    }

    protected override void OnPlayOut(Action onComplete)
    {
        KillTrackedTweens(false);
        var done = 0;
        var required = 0;

        void TryDone()
        {
            done++;
            if (done >= required)
            {
                onComplete?.Invoke();
            }
        }

        if (ContentRoot != null)
        {
            required++;
            TrackTween(ContentRoot
                .DOAnchorPos(mBaseContentPos + ExitOffset, Mathf.Max(0.05f, TransitionDuration))
                .SetEase(Ease.InCubic)
                .OnComplete(TryDone));
        }

        if (EffectCanvasGroup != null)
        {
            required++;
            TrackTween(EffectCanvasGroup.DOFade(0f, Mathf.Max(0.05f, TransitionDuration)).SetEase(Ease.InCubic).OnComplete(TryDone));
        }

        if (required == 0)
        {
            onComplete?.Invoke();
        }
    }

    protected override void OnEffectReset()
    {
        EnsureBindings();
        KillTrackedTweens(false);

        mIsHovering = false;
        mIsDragging = false;
        mOverflow = 0f;
        mOverflowVelocity = 0f;
        mTrackHeight = TrackHeightIdle;
        mLeftIconTargetX = 0f;
        mRightIconTargetX = 0f;
        mRegion = OverflowRegion.Middle;
        Value = Mathf.Clamp(Value, MinValue, MaxValue);

        if (ContentRoot != null)
        {
            ContentRoot.anchoredPosition = mBaseContentPos;
        }

        RefreshValueVisual();
        ApplyElasticVisual();
        SetCanvasAlpha(1f);
        SetLifecycleLooping();
    }

    public override IReadOnlyList<ReplicaV3ParameterDefinition> GetParameterDefinitions() => mParameters;

    public override bool TryGetFloatParameter(string parameterId, out float value)
    {
        switch (parameterId)
        {
            case "value": value = Value; return true;
            case "max_overflow": value = MaxOverflow; return true;
            case "overflow_spring": value = OverflowSpring; return true;
            case "overflow_damping": value = OverflowDamping; return true;
            default: value = 0f; return false;
        }
    }

    public override bool TrySetFloatParameter(string parameterId, float value)
    {
        switch (parameterId)
        {
            case "value":
                Value = Mathf.Clamp(value, MinValue, MaxValue);
                RefreshValueVisual();
                return true;
            case "max_overflow": MaxOverflow = Mathf.Clamp(value, 5f, 180f); return true;
            case "overflow_spring": OverflowSpring = Mathf.Clamp(value, 20f, 360f); return true;
            case "overflow_damping": OverflowDamping = Mathf.Clamp(value, 1f, 60f); return true;
            default: return false;
        }
    }

    public override bool TryGetBoolParameter(string parameterId, out bool value)
    {
        if (parameterId == "stepped")
        {
            value = Stepped;
            return true;
        }

        value = false;
        return false;
    }

    public override bool TrySetBoolParameter(string parameterId, bool value)
    {
        if (parameterId == "stepped")
        {
            Stepped = value;
            return true;
        }

        return false;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        mIsHovering = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        mIsHovering = false;
        if (!mIsDragging)
        {
            mRegion = OverflowRegion.Middle;
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        mIsDragging = true;
        mIsHovering = true;
        UpdateFromPointer(eventData);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        mIsDragging = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        UpdateFromPointer(eventData);
    }

    private RectTransform EnsureBindings()
    {
        if (InteractionHitSource == null)
        {
            InteractionHitSource = TrackRect != null ? TrackRect : (EffectRoot != null ? EffectRoot : transform as RectTransform);
        }

        if (ContentRoot == null)
        {
            ContentRoot = EffectRoot != null ? EffectRoot : transform as RectTransform;
        }

        return InteractionHitSource;
    }

    private void UpdateFromPointer(PointerEventData eventData)
    {
        if (!mIsDragging || eventData == null || TrackRect == null)
        {
            return;
        }

        var cam = ResolveReliableEventCamera(eventData, TrackRect);
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(TrackRect, eventData.position, cam, out var localPoint))
        {
            var width = Mathf.Max(1f, TrackRect.rect.width);
            var normalized = Mathf.Clamp01((localPoint.x + (width * 0.5f)) / width);
            var newValue = Mathf.Lerp(MinValue, MaxValue, normalized);
            if (Stepped)
            {
                var step = Mathf.Max(0.0001f, StepSize);
                newValue = Mathf.Round(newValue / step) * step;
            }

            Value = Mathf.Clamp(newValue, MinValue, MaxValue);
            RefreshValueVisual();
        }

        TrackRect.GetWorldCorners(mTrackCorners);
        var left = mTrackCorners[0].x;
        var right = mTrackCorners[3].x;
        var screenX = eventData.position.x;

        OverflowRegion nextRegion;
        float rawOverflow;
        if (screenX < left)
        {
            nextRegion = OverflowRegion.Left;
            rawOverflow = left - screenX;
        }
        else if (screenX > right)
        {
            nextRegion = OverflowRegion.Right;
            rawOverflow = screenX - right;
        }
        else
        {
            nextRegion = OverflowRegion.Middle;
            rawOverflow = 0f;
        }

        if (nextRegion != mRegion)
        {
            mRegion = nextRegion;
            PlayRegionPulse(nextRegion);
        }

        mOverflow = Decay(rawOverflow, MaxOverflow);
        mOverflowVelocity = 0f;
    }

    private void UpdateIconTargets(float dt)
    {
        var overflowAbs = Mathf.Abs(mOverflow);
        float leftTarget;
        float rightTarget;

        if (mRegion == OverflowRegion.Left)
        {
            leftTarget = -overflowAbs;
            rightTarget = 0f;
        }
        else if (mRegion == OverflowRegion.Right)
        {
            leftTarget = 0f;
            rightTarget = overflowAbs;
        }
        else
        {
            leftTarget = 0f;
            rightTarget = 0f;
        }

        mLeftIconTargetX = SmoothTo(mLeftIconTargetX, leftTarget, IconPushSpeed, dt);
        mRightIconTargetX = SmoothTo(mRightIconTargetX, rightTarget, IconPushSpeed, dt);
    }

    private void RefreshValueVisual()
    {
        var normalized = GetValueNormalized();

        if (FillRect != null)
        {
            FillRect.anchorMax = new Vector2(normalized, 1f);
        }

        if (ValueText != null)
        {
            ValueText.text = Mathf.RoundToInt(Value).ToString();
        }

        if (KnobRect != null)
        {
            KnobRect.anchorMin = new Vector2(normalized, 0.5f);
            KnobRect.anchorMax = new Vector2(normalized, 0.5f);
            KnobRect.anchoredPosition = Vector2.zero;
        }
    }

    private void ApplyElasticVisual()
    {
        if (TrackRect == null)
        {
            return;
        }

        var width = Mathf.Max(1f, TrackRect.rect.width);
        var overflowAbs = Mathf.Abs(mOverflow);
        var overflow01 = Mathf.Clamp01(overflowAbs / Mathf.Max(1f, MaxOverflow));

        TrackRect.sizeDelta = new Vector2(TrackRect.sizeDelta.x, mTrackHeight);

        if (TrackWrapper != null)
        {
            var scaleX = 1f + (overflowAbs / width);
            var scaleY = Mathf.Lerp(1f, 0.82f, overflow01);
            TrackWrapper.localScale = new Vector3(scaleX, scaleY, 1f);
            TrackWrapper.pivot = mRegion == OverflowRegion.Left
                ? new Vector2(1f, 0.5f)
                : (mRegion == OverflowRegion.Right ? new Vector2(0f, 0.5f) : new Vector2(0.5f, 0.5f));
        }

        if (LeftIconRect != null)
        {
            LeftIconRect.anchoredPosition = new Vector2(IconBaseOffset + mLeftIconTargetX, LeftIconRect.anchoredPosition.y);
        }

        if (RightIconRect != null)
        {
            RightIconRect.anchoredPosition = new Vector2(-IconBaseOffset + mRightIconTargetX, RightIconRect.anchoredPosition.y);
        }

        if (TrackBackground != null)
        {
            var c = TrackBackground.color;
            c.a = Mathf.Lerp(0.28f, 0.46f, overflow01);
            TrackBackground.color = c;
        }

        if (FillImage != null)
        {
            FillImage.color = Color.Lerp(new Color(0.74f, 0.78f, 0.88f, 0.86f), Color.white, overflow01 * 0.5f);
        }

        if (KnobImage != null)
        {
            KnobImage.color = Color.Lerp(new Color(0.93f, 0.95f, 1f, 1f), Color.white, overflow01);
        }

        if (KnobRect != null)
        {
            var knobSize = Mathf.Lerp(14f, 20f, Mathf.InverseLerp(TrackHeightIdle, TrackHeightHover, mTrackHeight));
            KnobRect.sizeDelta = new Vector2(knobSize, knobSize);
        }
    }

    private void PlayRegionPulse(OverflowRegion region)
    {
        if (region == OverflowRegion.Left && LeftIconRect != null)
        {
            TrackTween(LeftIconRect.DOPunchScale(Vector3.one * 0.3f, 0.25f, 1, 0.5f));
        }
        else if (region == OverflowRegion.Right && RightIconRect != null)
        {
            TrackTween(RightIconRect.DOPunchScale(Vector3.one * 0.3f, 0.25f, 1, 0.5f));
        }
    }

    private float GetValueNormalized()
    {
        var total = MaxValue - MinValue;
        if (Mathf.Approximately(total, 0f))
        {
            return 0f;
        }

        return Mathf.Clamp01((Value - MinValue) / total);
    }

    private static float Decay(float value, float max)
    {
        if (max <= 0f)
        {
            return 0f;
        }

        var entry = value / max;
        var sigmoid = 2f * ((1f / (1f + Mathf.Exp(-entry))) - 0.5f);
        return sigmoid * max;
    }

    private static float SmoothTo(float current, float target, float speed, float dt)
    {
        return Mathf.Lerp(current, target, 1f - Mathf.Exp(-Mathf.Max(0f, speed) * dt));
    }

    private void OnDrawGizmos()
    {
        var hitSource = InteractionHitSource != null
            ? InteractionHitSource
            : (TrackRect != null ? TrackRect : (EffectRoot != null ? EffectRoot : transform as RectTransform));
        DrawInteractionRangeGizmo(ShowInteractionRange, hitSource, InteractionRangeDependency, InteractionRangePadding);
    }
}
