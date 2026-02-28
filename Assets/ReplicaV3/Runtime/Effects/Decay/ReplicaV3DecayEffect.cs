using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class ReplicaV3DecayEffect : ReplicaV3EffectBase, IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler
{
    [Header("组件绑定（Decay）")]
    [Tooltip("主要可交互判定组件。为空时回退到 CardRoot。")]
    public RectTransform InteractionHitSource;

    [Tooltip("交互范围跟随依赖组件。为空时回退到 InteractionHitSource。")]
    public RectTransform InteractionRangeDependency;

    [Tooltip("编辑器中显示可交互范围。默认开启，可手动关闭。")]
    public bool ShowInteractionRange = true;

    [Tooltip("交互范围扩展像素。")]
    public float InteractionRangePadding = 0f;

    [Tooltip("卡片根节点。用于位移和旋转。")]
    public RectTransform CardRoot;

    [Tooltip("卡片主图。用于颜色扰动。")]
    public Image CardImage;

    [Tooltip("噪声条带列表。")]
    public List<RectTransform> NoiseStrips = new List<RectTransform>();

    [Tooltip("噪声条带对应 Image。顺序与 NoiseStrips 一致。")]
    public List<Image> NoiseImages = new List<Image>();

    [Header("参数（可在参数面板实时调）")]
    [Tooltip("偏移边界。")]
    public float MoveBound = 50f;

    [Tooltip("位移插值强度。")]
    public float PositionLerp = 0.10f;

    [Tooltip("旋转插值强度。")]
    public float RotationLerp = 0.10f;

    [Tooltip("扭曲插值强度。")]
    public float DistortionLerp = 0.06f;

    [Tooltip("位移映射范围。")]
    public float DistortionDistanceMax = 200f;

    [Tooltip("条带振幅。")]
    public float StripAmplitude = 18f;

    [Tooltip("条带振幅递进。")]
    public float StripAmplitudeStep = 0.4f;

    [Tooltip("条带波动速度。")]
    public float StripWaveSpeed = 7.2f;

    [Tooltip("条带透明度波动速度。")]
    public float StripAlphaSpeed = 11.5f;

    [Tooltip("主图静止色。")]
    public Color BasePhotoColor = new Color(0.36f, 0.39f, 0.46f, 1f);

    [Tooltip("主图激活色。")]
    public Color ActivePhotoColor = new Color(0.70f, 0.72f, 0.77f, 1f);

    [Tooltip("是否持续更新扭曲。")]
    public bool EnableLoop = true;

    [Header("进入/退出")]
    [Tooltip("进入偏移。")]
    public Vector2 EnterOffset = new Vector2(0f, -220f);

    [Tooltip("退出偏移。")]
    public Vector2 ExitOffset = new Vector2(0f, 220f);

    [Tooltip("过渡时长。")]
    public float TransitionDuration = 0.36f;

    private readonly List<ReplicaV3ParameterDefinition> mParameters = new List<ReplicaV3ParameterDefinition>
    {
        new ReplicaV3ParameterDefinition { Id = "move_bound", DisplayName = "偏移边界", Description = "限制卡片偏移幅度。", Kind = ReplicaV3ParameterKind.Float, Min = 8f, Max = 180f, Step = 1f },
        new ReplicaV3ParameterDefinition { Id = "position_lerp", DisplayName = "位移平滑", Description = "卡片位移追随速度。", Kind = ReplicaV3ParameterKind.Float, Min = 0.01f, Max = 0.6f, Step = 0.01f },
        new ReplicaV3ParameterDefinition { Id = "distortion_lerp", DisplayName = "扭曲平滑", Description = "噪声扭曲变化平滑度。", Kind = ReplicaV3ParameterKind.Float, Min = 0.01f, Max = 0.6f, Step = 0.01f },
        new ReplicaV3ParameterDefinition { Id = "strip_amplitude", DisplayName = "条带振幅", Description = "噪声条带横向位移幅度。", Kind = ReplicaV3ParameterKind.Float, Min = 0f, Max = 48f, Step = 0.5f },
        new ReplicaV3ParameterDefinition { Id = "enable_loop", DisplayName = "启用循环", Description = "关闭后停止实时扭曲更新。", Kind = ReplicaV3ParameterKind.Bool }
    };

    private Vector2 mCurrentOffset;
    private float mCurrentRotationZ;
    private float mDistortionScale;
    private Vector2 mCachedCursor;
    private readonly List<float> mNoiseBaseY = new List<float>();
    private bool mPointerInside;
    private bool mHasLocalPointer;
    private Vector2 mPointerLocal;

    protected override void OnEffectInitialize()
    {
        EnsureBindings();
        CacheNoiseBaseY();
        mCachedCursor = Input.mousePosition;
        SetCanvasAlpha(1f);
        SetLifecycleLooping();
    }

    protected override void OnEffectTick(float deltaTime, float unscaledDeltaTime)
    {
        if (!EnableLoop)
        {
            return;
        }

        var dt = Mathf.Max(0f, unscaledDeltaTime);
        UpdateCardTilt(dt);
        UpdateDistortion(dt);
    }

    protected override void OnPlayIn()
    {
        EnsureBindings();
        KillTrackedTweens(false);

        var duration = Mathf.Max(0.05f, TransitionDuration);
        SetCanvasAlpha(0f);

        if (CardRoot != null)
        {
            var target = Vector2.zero;
            CardRoot.anchoredPosition = EnterOffset;
            TrackTween(CardRoot.DOAnchorPos(target, duration).SetEase(Ease.OutCubic));
        }

        if (EffectCanvasGroup != null)
        {
            TrackTween(EffectCanvasGroup.DOFade(1f, duration).SetEase(Ease.OutCubic));
        }

        SetLifecycleLooping();
    }

    protected override void OnPlayOut(Action onComplete)
    {
        KillTrackedTweens(false);
        var duration = Mathf.Max(0.05f, TransitionDuration);
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

        if (CardRoot != null)
        {
            required++;
            TrackTween(CardRoot.DOAnchorPos(ExitOffset, duration).SetEase(Ease.InCubic).OnComplete(TryDone));
        }

        if (EffectCanvasGroup != null)
        {
            required++;
            TrackTween(EffectCanvasGroup.DOFade(0f, duration).SetEase(Ease.InCubic).OnComplete(TryDone));
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

        mCurrentOffset = Vector2.zero;
        mCurrentRotationZ = 0f;
        mDistortionScale = 0f;
        mCachedCursor = Input.mousePosition;
        mPointerInside = false;
        mHasLocalPointer = false;

        if (CardRoot != null)
        {
            CardRoot.anchoredPosition = Vector2.zero;
            CardRoot.localRotation = Quaternion.identity;
        }

        ApplyStripVisual(0f);
        SetCanvasAlpha(1f);
        SetLifecycleLooping();
    }

    public override IReadOnlyList<ReplicaV3ParameterDefinition> GetParameterDefinitions() => mParameters;

    public override bool TryGetFloatParameter(string parameterId, out float value)
    {
        switch (parameterId)
        {
            case "move_bound": value = MoveBound; return true;
            case "position_lerp": value = PositionLerp; return true;
            case "distortion_lerp": value = DistortionLerp; return true;
            case "strip_amplitude": value = StripAmplitude; return true;
            default: value = 0f; return false;
        }
    }

    public override bool TrySetFloatParameter(string parameterId, float value)
    {
        switch (parameterId)
        {
            case "move_bound": MoveBound = Mathf.Clamp(value, 8f, 180f); return true;
            case "position_lerp": PositionLerp = Mathf.Clamp(value, 0.01f, 0.6f); return true;
            case "distortion_lerp": DistortionLerp = Mathf.Clamp(value, 0.01f, 0.6f); return true;
            case "strip_amplitude": StripAmplitude = Mathf.Clamp(value, 0f, 48f); return true;
            default: return false;
        }
    }

    public override bool TryGetBoolParameter(string parameterId, out bool value)
    {
        if (parameterId == "enable_loop")
        {
            value = EnableLoop;
            return true;
        }

        value = false;
        return false;
    }

    public override bool TrySetBoolParameter(string parameterId, bool value)
    {
        if (parameterId == "enable_loop")
        {
            EnableLoop = value;
            return true;
        }

        return false;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        mPointerInside = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        mPointerInside = false;
        mHasLocalPointer = false;
    }

    public void OnPointerMove(PointerEventData eventData)
    {
        if (!mPointerInside || eventData == null)
        {
            return;
        }

        var target = EnsureBindings();
        var cam = ResolveReliableEventCamera(eventData, target);
        if (target != null && RectTransformUtility.ScreenPointToLocalPointInRectangle(target, eventData.position, cam, out var local))
        {
            mPointerLocal = local;
            mHasLocalPointer = true;
        }
    }

    private RectTransform EnsureBindings()
    {
        if (CardRoot == null)
        {
            CardRoot = EffectRoot != null ? EffectRoot : transform as RectTransform;
        }

        if (InteractionHitSource == null)
        {
            InteractionHitSource = CardRoot != null ? CardRoot : (EffectRoot != null ? EffectRoot : transform as RectTransform);
        }

        return InteractionHitSource;
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

    private void UpdateCardTilt(float dt)
    {
        if (CardRoot == null)
        {
            return;
        }

        var normalized = Vector2.zero;
        if (mPointerInside && mHasLocalPointer)
        {
            var halfW = Mathf.Max(1f, CardRoot.rect.width * 0.5f);
            var halfH = Mathf.Max(1f, CardRoot.rect.height * 0.5f);
            normalized = new Vector2(
                Mathf.Clamp(mPointerLocal.x / halfW, -1f, 1f),
                Mathf.Clamp(mPointerLocal.y / halfH, -1f, 1f));
        }

        var mapX = normalized.x * 120f;
        var mapY = normalized.y * 120f;
        var mapRz = normalized.x * 10f;

        var lerpPos = 1f - Mathf.Pow(1f - Mathf.Clamp01(PositionLerp), Mathf.Max(1f, dt * 60f));
        var lerpRot = 1f - Mathf.Pow(1f - Mathf.Clamp01(RotationLerp), Mathf.Max(1f, dt * 60f));

        var targetX = Mathf.Lerp(mCurrentOffset.x, mapX, lerpPos);
        var targetY = Mathf.Lerp(mCurrentOffset.y, mapY, lerpPos);
        var targetRz = Mathf.Lerp(mCurrentRotationZ, mapRz, lerpRot);

        var bound = Mathf.Max(8f, MoveBound);
        if (targetX > bound) targetX = bound + ((targetX - bound) * 0.2f);
        if (targetX < -bound) targetX = -bound + ((targetX + bound) * 0.2f);
        if (targetY > bound) targetY = bound + ((targetY - bound) * 0.2f);
        if (targetY < -bound) targetY = -bound + ((targetY + bound) * 0.2f);

        mCurrentOffset = new Vector2(targetX, targetY);
        mCurrentRotationZ = targetRz;

        CardRoot.anchoredPosition = mCurrentOffset;
        CardRoot.localRotation = Quaternion.Euler(0f, 0f, mCurrentRotationZ);
    }

    private void UpdateDistortion(float dt)
    {
        var cursor = (Vector2)Input.mousePosition;
        var travelled = Vector2.Distance(mCachedCursor, cursor);
        var mapped = Mathf.Clamp01(travelled / Mathf.Max(1f, DistortionDistanceMax));

        var lerp = 1f - Mathf.Pow(1f - Mathf.Clamp01(DistortionLerp), Mathf.Max(1f, dt * 60f));
        mDistortionScale = Mathf.Lerp(mDistortionScale, mapped, lerp);

        ApplyStripVisual(mDistortionScale);

        if (CardImage != null)
        {
            CardImage.color = Color.Lerp(BasePhotoColor, ActivePhotoColor, mDistortionScale * 0.6f);
        }

        mCachedCursor = cursor;
    }

    private void ApplyStripVisual(float distortion)
    {
        var waveSpeed = Mathf.Max(0.1f, StripWaveSpeed);
        var alphaSpeed = Mathf.Max(0.1f, StripAlphaSpeed);

        for (var i = 0; i < NoiseStrips.Count; i++)
        {
            var strip = NoiseStrips[i];
            if (strip == null)
            {
                continue;
            }

            var wave = Mathf.Sin((Time.unscaledTime * waveSpeed) + (i * 0.65f));
            var direction = (i % 2 == 0) ? 1f : -1f;
            var x = direction * wave * (StripAmplitude + (i * StripAmplitudeStep)) * distortion;
            var y = i < mNoiseBaseY.Count ? mNoiseBaseY[i] : strip.anchoredPosition.y;
            strip.anchoredPosition = new Vector2(x, y);

            if (i >= NoiseImages.Count || NoiseImages[i] == null)
            {
                continue;
            }

            var alphaWave = 0.5f + (0.5f * Mathf.Sin((Time.unscaledTime * alphaSpeed) + (i * 0.9f)));
            var c = NoiseImages[i].color;
            c.a = Mathf.Lerp(0.02f, 0.20f, distortion) * alphaWave;
            NoiseImages[i].color = c;
        }
    }

    private void OnDrawGizmos()
    {
        var hitSource = InteractionHitSource != null
            ? InteractionHitSource
            : (CardRoot != null ? CardRoot : (EffectRoot != null ? EffectRoot : transform as RectTransform));
        DrawInteractionRangeGizmo(ShowInteractionRange, hitSource, InteractionRangeDependency, InteractionRangePadding);
    }
}
