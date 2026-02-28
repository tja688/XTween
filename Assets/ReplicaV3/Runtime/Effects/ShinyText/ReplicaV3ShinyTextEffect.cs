using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class ReplicaV3ShinyTextEffect : ReplicaV3EffectBase, IPointerEnterHandler, IPointerExitHandler
{
    [Header("组件绑定（ShinyText）")]
    [Tooltip("主要可交互判定组件。为空时回退到 RaycastSurface/ContentRect。")]
    public RectTransform InteractionHitSource;

    [Tooltip("交互范围跟随依赖组件。为空时回退到 InteractionHitSource。")]
    public RectTransform InteractionRangeDependency;

    [Tooltip("编辑器中显示可交互范围。默认开启，可手动关闭。")]
    public bool ShowInteractionRange = true;

    [Tooltip("交互范围扩展像素。")]
    public float InteractionRangePadding = 0f;

    [Tooltip("文本可见区域容器。用于计算扫光移动范围。")]
    public RectTransform ContentRect;

    [Tooltip("实际展示文本内容的 Text。")]
    public Text DisplayTextLabel;

    [Tooltip("扫光条 RectTransform。会在 X 轴往返移动。")]
    public RectTransform ShineBandRect;

    [Tooltip("扫光条 Image。运行时会写入动态渐变贴图。")]
    public Image ShineBandImage;

    [Tooltip("接收 Pointer 事件的表面（通常是透明 Image）。")]
    public Graphic RaycastSurface;

    [Header("参数（可在参数面板实时调）")]
    [Tooltip("展示文本内容。")]
    public string DisplayText = "Shiny Text";

    [Tooltip("扫光一次耗时（秒）。越小越快。")]
    public float SweepDuration = 2f;

    [Tooltip("每次扫光结束后的停顿时长（秒）。")]
    public float SweepDelay = 0.1f;

    [Tooltip("扫光条宽度倍数（相对文本容器宽度）。")]
    public float SweepTiling = 1.8f;

    [Tooltip("基础颜色（非高光区域颜色）。")]
    public Color BaseColor = new Color(0.72f, 0.72f, 0.72f, 1f);

    [Tooltip("高光颜色。")]
    public Color ShineColor = Color.white;

    [Tooltip("是否启用来回扫光。关闭时只做单向循环。")]
    public bool Yoyo = false;

    [Tooltip("鼠标悬停时是否暂停扫光。")]
    public bool PauseOnHover = false;

    [Tooltip("是否自动持续播放。")]
    public bool AutoPlay = true;

    [Header("进入/退出过渡")]
    [Tooltip("PlayIn 的位移偏移量。")]
    public float EnterOffsetX = 80f;

    [Tooltip("PlayIn / PlayOut 的过渡时长。")]
    public float TransitionDuration = 0.35f;

    private readonly List<ReplicaV3ParameterDefinition> mParameters = new List<ReplicaV3ParameterDefinition>
    {
        new ReplicaV3ParameterDefinition
        {
            Id = "sweep_duration",
            DisplayName = "扫光时长",
            Description = "每次扫光所需时间，越小速度越快。",
            Kind = ReplicaV3ParameterKind.Float,
            Min = 0.2f,
            Max = 8f,
            Step = 0.1f
        },
        new ReplicaV3ParameterDefinition
        {
            Id = "sweep_delay",
            DisplayName = "扫光停顿",
            Description = "扫光结束后停顿时间。",
            Kind = ReplicaV3ParameterKind.Float,
            Min = 0f,
            Max = 2f,
            Step = 0.05f
        },
        new ReplicaV3ParameterDefinition
        {
            Id = "sweep_tiling",
            DisplayName = "扫光宽度倍数",
            Description = "控制扫光条宽度，值越大扫光条越宽。",
            Kind = ReplicaV3ParameterKind.Float,
            Min = 1f,
            Max = 3.5f,
            Step = 0.1f
        },
        new ReplicaV3ParameterDefinition
        {
            Id = "pause_on_hover",
            DisplayName = "悬停暂停",
            Description = "开启后，鼠标停在文本上会暂停扫光。",
            Kind = ReplicaV3ParameterKind.Bool
        },
        new ReplicaV3ParameterDefinition
        {
            Id = "yoyo",
            DisplayName = "往返扫光",
            Description = "开启后扫光方向会来回反转。",
            Kind = ReplicaV3ParameterKind.Bool
        }
    };

    private Texture2D mGradientTexture;
    private Sprite mGradientSprite;
    private bool mHovering;
    private float mElapsed;
    private Color mCachedBaseColor;
    private Color mCachedShineColor;

    protected override void OnEffectInitialize()
    {
        EnsureInteractionBindings();
        if (DisplayTextLabel != null)
        {
            DisplayTextLabel.text = DisplayText;
        }

        BuildOrRefreshGradient();
        ApplyShineVisual();
        SetCanvasAlpha(1f);
        SetLifecycleLooping();
    }

    protected override void OnEffectTick(float deltaTime, float unscaledDeltaTime)
    {
        if (!AutoPlay)
        {
            return;
        }

        if (PauseOnHover && mHovering)
        {
            ApplyShineVisual();
            return;
        }

        var step = Mathf.Max(0f, unscaledDeltaTime);
        mElapsed += step;
        BuildOrRefreshGradient();
        ApplyShineVisual();
    }

    protected override void OnPlayIn()
    {
        KillTrackedTweens(false);

        if (ContentRect != null)
        {
            var basePos = ContentRect.anchoredPosition;
            ContentRect.anchoredPosition = basePos + new Vector2(-Mathf.Abs(EnterOffsetX), 0f);
            TrackTween(ContentRect
                .DOAnchorPos(basePos, Mathf.Max(0.05f, TransitionDuration))
                .SetEase(Ease.OutCubic)
                .OnComplete(SetLifecycleLooping));
        }
        else
        {
            SetLifecycleLooping();
        }

        if (EffectCanvasGroup != null)
        {
            EffectCanvasGroup.alpha = 0f;
            TrackTween(EffectCanvasGroup
                .DOFade(1f, Mathf.Max(0.05f, TransitionDuration))
                .SetEase(Ease.OutCubic));
        }
    }

    protected override void OnPlayOut(Action onComplete)
    {
        KillTrackedTweens(false);

        var doneCount = 0;
        var required = 0;

        void TryFinish()
        {
            doneCount++;
            if (doneCount >= required)
            {
                if (onComplete != null)
                {
                    onComplete();
                }
            }
        }

        if (ContentRect != null)
        {
            required++;
            var endPos = ContentRect.anchoredPosition + new Vector2(Mathf.Abs(EnterOffsetX), 0f);
            TrackTween(ContentRect
                .DOAnchorPos(endPos, Mathf.Max(0.05f, TransitionDuration))
                .SetEase(Ease.InCubic)
                .OnComplete(TryFinish));
        }

        if (EffectCanvasGroup != null)
        {
            required++;
            TrackTween(EffectCanvasGroup
                .DOFade(0f, Mathf.Max(0.05f, TransitionDuration))
                .SetEase(Ease.InCubic)
                .OnComplete(TryFinish));
        }

        if (required == 0)
        {
            onComplete?.Invoke();
        }
    }

    protected override void OnEffectReset()
    {
        EnsureInteractionBindings();
        mElapsed = 0f;
        mHovering = false;
        if (DisplayTextLabel != null)
        {
            DisplayTextLabel.text = DisplayText;
        }

        SetCanvasAlpha(1f);
        BuildOrRefreshGradient(true);
        ApplyShineVisual();
        SetLifecycleLooping();
    }

    protected override void OnEffectDispose()
    {
        ReleaseGradientResources();
    }

    public override IReadOnlyList<ReplicaV3ParameterDefinition> GetParameterDefinitions()
    {
        return mParameters;
    }

    public override bool TryGetFloatParameter(string parameterId, out float value)
    {
        switch (parameterId)
        {
            case "sweep_duration":
                value = SweepDuration;
                return true;
            case "sweep_delay":
                value = SweepDelay;
                return true;
            case "sweep_tiling":
                value = SweepTiling;
                return true;
            default:
                value = 0f;
                return false;
        }
    }

    public override bool TrySetFloatParameter(string parameterId, float value)
    {
        switch (parameterId)
        {
            case "sweep_duration":
                SweepDuration = Mathf.Clamp(value, 0.2f, 8f);
                return true;
            case "sweep_delay":
                SweepDelay = Mathf.Clamp(value, 0f, 2f);
                return true;
            case "sweep_tiling":
                SweepTiling = Mathf.Clamp(value, 1f, 3.5f);
                ApplyShineVisual();
                return true;
            default:
                return false;
        }
    }

    public override bool TryGetBoolParameter(string parameterId, out bool value)
    {
        switch (parameterId)
        {
            case "pause_on_hover":
                value = PauseOnHover;
                return true;
            case "yoyo":
                value = Yoyo;
                return true;
            default:
                value = false;
                return false;
        }
    }

    public override bool TrySetBoolParameter(string parameterId, bool value)
    {
        switch (parameterId)
        {
            case "pause_on_hover":
                PauseOnHover = value;
                return true;
            case "yoyo":
                Yoyo = value;
                return true;
            default:
                return false;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        mHovering = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        mHovering = false;
    }

    private void BuildOrRefreshGradient(bool force = false)
    {
        if (ShineBandImage == null)
        {
            return;
        }

        if (!force && mGradientTexture != null && mGradientSprite != null &&
            mCachedBaseColor == BaseColor && mCachedShineColor == ShineColor)
        {
            return;
        }

        mCachedBaseColor = BaseColor;
        mCachedShineColor = ShineColor;

        ReleaseGradientResources();

        const int width = 384;
        const int height = 32;
        mGradientTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        mGradientTexture.wrapMode = TextureWrapMode.Clamp;
        mGradientTexture.filterMode = FilterMode.Bilinear;

        var left = 0.30f;
        var center = 0.50f;
        var right = 0.70f;
        for (var x = 0; x < width; x++)
        {
            var t = x / (width - 1f);
            Color color;
            if (t < left)
            {
                color = BaseColor;
            }
            else if (t < center)
            {
                color = Color.Lerp(BaseColor, ShineColor, Mathf.InverseLerp(left, center, t));
            }
            else if (t < right)
            {
                color = Color.Lerp(ShineColor, BaseColor, Mathf.InverseLerp(center, right, t));
            }
            else
            {
                color = BaseColor;
            }

            for (var y = 0; y < height; y++)
            {
                mGradientTexture.SetPixel(x, y, color);
            }
        }

        mGradientTexture.Apply(false, false);
        mGradientSprite = Sprite.Create(
            mGradientTexture,
            new Rect(0f, 0f, width, height),
            new Vector2(0.5f, 0.5f),
            100f);

        ShineBandImage.sprite = mGradientSprite;
        ShineBandImage.color = Color.white;
    }

    private void ApplyShineVisual()
    {
        if (ContentRect == null || ShineBandRect == null)
        {
            return;
        }

        var rect = ContentRect.rect;
        if (rect.width < 0.01f || rect.height < 0.01f)
        {
            return;
        }

        var safeTiling = Mathf.Clamp(SweepTiling, 1f, 3.5f);
        ShineBandRect.anchorMin = new Vector2(0.5f, 0.5f);
        ShineBandRect.anchorMax = new Vector2(0.5f, 0.5f);
        ShineBandRect.pivot = new Vector2(0.5f, 0.5f);
        ShineBandRect.sizeDelta = new Vector2(rect.width * safeTiling, rect.height);

        var progress = ComputeCycleProgress(mElapsed, Mathf.Max(0.2f, SweepDuration), Mathf.Max(0f, SweepDelay), Yoyo);
        var range = Mathf.Max(0f, (ShineBandRect.sizeDelta.x - rect.width) * 0.5f);
        ShineBandRect.anchoredPosition = new Vector2(Mathf.Lerp(range, -range, progress), 0f);

        if (DisplayTextLabel != null)
        {
            DisplayTextLabel.text = string.IsNullOrWhiteSpace(DisplayText) ? "Shiny Text" : DisplayText;
        }
    }

    private static float ComputeCycleProgress(float elapsed, float duration, float delay, bool yoyo)
    {
        if (duration <= 0.0001f)
        {
            return 0f;
        }

        if (!yoyo)
        {
            var cycle = duration + delay;
            var t = Mathf.Repeat(elapsed, cycle);
            return t <= duration ? t / duration : 1f;
        }

        var phase = duration + delay;
        var full = phase * 2f;
        var t2 = Mathf.Repeat(elapsed, full);
        if (t2 <= duration)
        {
            return t2 / duration;
        }

        if (t2 <= phase)
        {
            return 1f;
        }

        var reverseTime = t2 - phase;
        if (reverseTime <= duration)
        {
            return 1f - reverseTime / duration;
        }

        return 0f;
    }

    private void ReleaseGradientResources()
    {
        if (mGradientSprite != null)
        {
            Destroy(mGradientSprite);
            mGradientSprite = null;
        }

        if (mGradientTexture != null)
        {
            Destroy(mGradientTexture);
            mGradientTexture = null;
        }
    }

    private void EnsureInteractionBindings()
    {
        if (InteractionHitSource == null)
        {
            if (RaycastSurface != null)
            {
                InteractionHitSource = RaycastSurface.rectTransform;
            }
            else if (ContentRect != null)
            {
                InteractionHitSource = ContentRect;
            }
            else
            {
                InteractionHitSource = EffectRoot != null ? EffectRoot : transform as RectTransform;
            }
        }
    }

    private void OnDrawGizmos()
    {
        var hitSource = InteractionHitSource != null
            ? InteractionHitSource
            : (RaycastSurface != null
                ? RaycastSurface.rectTransform
                : (ContentRect != null ? ContentRect : (EffectRoot != null ? EffectRoot : transform as RectTransform)));
        DrawInteractionRangeGizmo(ShowInteractionRange, hitSource, InteractionRangeDependency, InteractionRangePadding);
    }
}
