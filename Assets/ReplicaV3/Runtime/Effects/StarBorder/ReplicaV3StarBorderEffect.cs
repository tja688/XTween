using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public sealed class ReplicaV3StarBorderEffect : ReplicaV3EffectBase
{
    private static Sprite sRadialSprite;

    [Header("组件绑定")]
    [Tooltip("特效的总容器")]
    public RectTransform Container;
    [Tooltip("上边缘的流光条RectTransform")]
    public RectTransform GlowTop;
    [Tooltip("上边缘流光条的图片组件")]
    public Image GlowTopImage;
    [Tooltip("下边缘的流光条RectTransform")]
    public RectTransform GlowBottom;
    [Tooltip("下边缘流光条的图片组件")]
    public Image GlowBottomImage;

    [Header("交互范围")]
    public RectTransform InteractionHitSource;
    public RectTransform InteractionRangeDependency;
    public bool ShowInteractionRange = true;
    public float InteractionRangePadding = 0f;

    [Header("尺寸参数")]
    public Vector2 ContainerSize = new Vector2(520f, 96f);
    public float EnterOffset = 220f;
    public float ExitOffset = 220f;

    [Header("Loop Motion")]
    public float SpeedSeconds = 6f;

    [Header("Glow 参数")]
    public Color GlowColor = Color.white;
    [Range(0f, 1f)] public float GlowOpacity = 0.7f;

    private readonly List<ReplicaV3ParameterDefinition> mParameters = new List<ReplicaV3ParameterDefinition>
    {
        new ReplicaV3ParameterDefinition { Id = "speed_seconds", DisplayName = "流光耗时(秒)", Description = "完成一次横穿的时间", Kind = ReplicaV3ParameterKind.Float, Min = 1f, Max = 15f, Step = 0.5f },
        new ReplicaV3ParameterDefinition { Id = "glow_opacity", DisplayName = "流光透明度", Description = "初始流光的亮度", Kind = ReplicaV3ParameterKind.Float, Min = 0f, Max = 1f, Step = 0.05f }
    };

    private Tween mTopPosTween;
    private Tween mTopFadeTween;
    private Tween mBottomPosTween;
    private Tween mBottomFadeTween;

    private float mTopBaseY;
    private float mBottomBaseY;

    protected override void OnEffectInitialize()
    {
        EnsureBindings();
        SetCanvasAlpha(1f);
        SetLifecycleLooping();
        ApplyGlowVisual();

        if (GlowTop != null) mTopBaseY = GlowTop.anchoredPosition.y;
        if (GlowBottom != null) mBottomBaseY = GlowBottom.anchoredPosition.y;

        StartLoops();
    }

    protected override void OnEffectTick(float deltaTime, float unscaledDeltaTime)
    {
        // Tween handles logic
    }

    protected override void OnPlayIn()
    {
        EnsureBindings();
        KillTrackedTweens(false);
        StopLoops();
        ApplyGlowVisual();

        if (Container != null)
        {
            Container.anchoredPosition = new Vector2(0f, -EnterOffset);
            TrackTween(Container.DOAnchorPos(Vector2.zero, 0.4f).SetEase(Ease.OutCubic));
        }

        SetCanvasAlpha(0f);
        if (EffectCanvasGroup != null)
        {
            TrackTween(EffectCanvasGroup.DOFade(1f, 0.3f).SetEase(Ease.OutCubic));
        }

        SetLifecycleLooping();
        StartLoops();
    }

    protected override void OnPlayOut(Action onComplete)
    {
        KillTrackedTweens(false);
        StopLoops();

        if (Container != null)
        {
            TrackTween(Container.DOAnchorPos(new Vector2(0f, ExitOffset), 0.3f).SetEase(Ease.InCubic));
        }

        if (EffectCanvasGroup != null)
        {
            TrackTween(EffectCanvasGroup.DOFade(0f, 0.25f).SetEase(Ease.InCubic).OnComplete(() => onComplete?.Invoke()));
            return;
        }

        onComplete?.Invoke();
    }

    protected override void OnEffectReset()
    {
        EnsureBindings();
        KillTrackedTweens(false);
        StopLoops();
        ApplyGlowVisual();

        if (Container != null)
        {
            Container.anchoredPosition = Vector2.zero;
        }

        if (GlowTop != null)
        {
            GlowTop.anchoredPosition = new Vector2(-Mathf.Max(50f, ContainerSize.x * 1.25f), mTopBaseY);
        }

        if (GlowBottom != null)
        {
            GlowBottom.anchoredPosition = new Vector2(Mathf.Max(50f, ContainerSize.x * 1.25f), mBottomBaseY);
        }

        SetCanvasAlpha(1f);
        SetLifecycleLooping();
        StartLoops();
    }

    protected override void OnEffectDispose()
    {
        StopLoops();
    }

    private RectTransform EnsureBindings()
    {
        if (Container == null && EffectRoot != null)
        {
            var container = EffectRoot.Find("Container");
            if (container != null)
            {
                Container = container as RectTransform;
            }
        }

        if (Container == null)
        {
            Container = EffectRoot != null ? EffectRoot : transform as RectTransform;
        }

        if (GlowTop == null && Container != null)
        {
            var top = Container.Find("GlowTop");
            if (top != null)
            {
                GlowTop = top as RectTransform;
            }
        }

        if (GlowBottom == null && Container != null)
        {
            var bottom = Container.Find("GlowBottom");
            if (bottom != null)
            {
                GlowBottom = bottom as RectTransform;
            }
        }

        if (GlowTopImage == null && GlowTop != null)
        {
            GlowTopImage = GlowTop.GetComponent<Image>();
        }

        if (GlowBottomImage == null && GlowBottom != null)
        {
            GlowBottomImage = GlowBottom.GetComponent<Image>();
        }

        if (InteractionHitSource == null)
        {
            InteractionHitSource = Container != null ? Container : (EffectRoot != null ? EffectRoot : transform as RectTransform);
        }

        return InteractionHitSource;
    }

    private void ApplyGlowVisual()
    {
        var alpha = Mathf.Clamp01(GlowOpacity);

        if (GlowTopImage != null)
        {
            if (GlowTopImage.sprite == null)
            {
                GlowTopImage.sprite = GetOrCreateRadialSprite();
            }

            GlowTopImage.color = new Color(GlowColor.r, GlowColor.g, GlowColor.b, alpha);
        }

        if (GlowBottomImage != null)
        {
            if (GlowBottomImage.sprite == null)
            {
                GlowBottomImage.sprite = GetOrCreateRadialSprite();
            }

            GlowBottomImage.color = new Color(GlowColor.r, GlowColor.g, GlowColor.b, alpha);
        }
    }

    private void StartLoops()
    {
        StartTopCycle(true);
        StartBottomCycle(true);
    }

    private void StopLoops()
    {
        mTopPosTween?.Kill();
        mTopFadeTween?.Kill();
        mBottomPosTween?.Kill();
        mBottomFadeTween?.Kill();
        mTopPosTween = null;
        mTopFadeTween = null;
        mBottomPosTween = null;
        mBottomFadeTween = null;
    }

    private void StartTopCycle(bool leftToRight)
    {
        if (GlowTop == null || GlowTopImage == null || !Application.isPlaying) return;

        mTopPosTween?.Kill();
        mTopFadeTween?.Kill();

        float span = Mathf.Max(50f, ContainerSize.x * 1.25f);
        float startX = leftToRight ? -span : span;
        float endX = leftToRight ? span : -span;

        GlowTop.anchoredPosition = new Vector2(startX, mTopBaseY);
        GlowTopImage.color = new Color(GlowColor.r, GlowColor.g, GlowColor.b, GlowOpacity);

        float duration = Mathf.Max(0.05f, SpeedSeconds);
        mTopPosTween = TrackTween(GlowTop.DOAnchorPosX(endX, duration).SetEase(Ease.Linear).OnComplete(() => StartTopCycle(!leftToRight)));
        mTopFadeTween = TrackTween(GlowTopImage.DOFade(0f, duration).SetEase(Ease.Linear));
    }

    private void StartBottomCycle(bool rightToLeft)
    {
        if (GlowBottom == null || GlowBottomImage == null || !Application.isPlaying) return;

        mBottomPosTween?.Kill();
        mBottomFadeTween?.Kill();

        float span = Mathf.Max(50f, ContainerSize.x * 1.25f);
        float startX = rightToLeft ? span : -span;
        float endX = rightToLeft ? -span : span;

        GlowBottom.anchoredPosition = new Vector2(startX, mBottomBaseY);
        GlowBottomImage.color = new Color(GlowColor.r, GlowColor.g, GlowColor.b, GlowOpacity);

        float duration = Mathf.Max(0.05f, SpeedSeconds);
        mBottomPosTween = TrackTween(GlowBottom.DOAnchorPosX(endX, duration).SetEase(Ease.Linear).OnComplete(() => StartBottomCycle(!rightToLeft)));
        mBottomFadeTween = TrackTween(GlowBottomImage.DOFade(0f, duration).SetEase(Ease.Linear));
    }

    public override IReadOnlyList<ReplicaV3ParameterDefinition> GetParameterDefinitions() => mParameters;

    public override bool TryGetFloatParameter(string parameterId, out float value)
    {
        switch (parameterId)
        {
            case "speed_seconds": value = SpeedSeconds; return true;
            case "glow_opacity": value = GlowOpacity; return true;
            default: value = 0f; return false;
        }
    }

    public override bool TrySetFloatParameter(string parameterId, float value)
    {
        switch (parameterId)
        {
            case "speed_seconds":
                SpeedSeconds = Mathf.Clamp(value, 1f, 15f);
                StopLoops();
                StartLoops();
                return true;
            case "glow_opacity":
                GlowOpacity = Mathf.Clamp01(value);
                ApplyGlowVisual();
                return true;
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
        var hitSource = InteractionHitSource != null ? InteractionHitSource : (Container != null ? Container : (EffectRoot != null ? EffectRoot : transform as RectTransform));
        DrawInteractionRangeGizmo(ShowInteractionRange, hitSource, InteractionRangeDependency, InteractionRangePadding);
    }

    private static Sprite GetOrCreateRadialSprite()
    {
        if (sRadialSprite != null)
        {
            return sRadialSprite;
        }

        const int size = 128;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
            name = "ReplicaV3_StarBorder_RadialSprite"
        };

        var center = (size - 1) * 0.5f;
        var maxDistance = center;
        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var dx = x - center;
                var dy = y - center;
                var distance = Mathf.Sqrt((dx * dx) + (dy * dy)) / maxDistance;
                var alpha = Mathf.Clamp01(1f - (distance / 0.10f));
                alpha = alpha * alpha * (3f - (2f * alpha));
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        texture.Apply(false, false);
        sRadialSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
        return sRadialSprite;
    }
}
