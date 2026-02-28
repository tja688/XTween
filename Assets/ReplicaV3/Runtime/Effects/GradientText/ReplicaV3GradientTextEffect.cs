using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public enum ReplicaV3GradientTextDirection
{
    Horizontal,
    Vertical,
    Diagonal
}

public sealed class ReplicaV3GradientTextEffect : ReplicaV3EffectBase, IPointerEnterHandler, IPointerExitHandler
{
    [Header("组件绑定（GradientText）")]
    [Tooltip("主要可交互判定组件。为空时回退到 Frame/ContentRoot。")]
    public RectTransform InteractionHitSource;

    [Tooltip("交互范围跟随依赖组件。为空时回退到 InteractionHitSource。")]
    public RectTransform InteractionRangeDependency;

    [Tooltip("编辑器中显示可交互范围。默认开启，可手动关闭。")]
    public bool ShowInteractionRange = true;

    [Tooltip("交互范围扩展像素。")]
    public float InteractionRangePadding = 0f;

    [Tooltip("过渡容器。")]
    public RectTransform ContentRoot;

    [Tooltip("外框容器。")]
    public RectTransform Frame;

    [Tooltip("边框渐变裁切容器。")]
    public RectTransform BorderGradientClip;

    [Tooltip("边框渐变位移容器。")]
    public RectTransform BorderGradientRect;

    [Tooltip("边框渐变图像。")]
    public Image BorderGradientImage;

    [Tooltip("文字区域底板。")]
    public RectTransform InnerBackground;

    [Tooltip("文字渐变位移容器。")]
    public RectTransform TextGradientRect;

    [Tooltip("文字渐变图像。")]
    public Image TextGradientImage;

    [Tooltip("文本遮罩文字。")]
    public Text MaskText;

    [Header("参数（可在参数面板实时调）")]
    [Tooltip("展示文本。")]
    public string DisplayText = "GradientText";

    [Tooltip("渐变颜色停靠点。")]
    public Color[] Colors =
    {
        new Color(0.321f, 0.153f, 1f, 1f),
        new Color(1f, 0.624f, 0.988f, 1f),
        new Color(0.694f, 0.620f, 0.937f, 1f)
    };

    [Tooltip("渐变动画速度。")]
    public float AnimationSpeed = 8f;

    [Tooltip("渐变平铺倍数。")]
    public float GradientTiling = 3f;

    [Tooltip("是否显示边框渐变。")]
    public bool ShowBorder = false;

    [Tooltip("渐变运动方向。")]
    public ReplicaV3GradientTextDirection Direction = ReplicaV3GradientTextDirection.Horizontal;

    [Tooltip("悬停时暂停。")]
    public bool PauseOnHover = false;

    [Tooltip("是否往返播放。")]
    public bool Yoyo = true;

    [Tooltip("PlayIn 位移偏移。")]
    public float EnterOffset = 160f;

    [Tooltip("PlayOut 位移偏移。")]
    public float ExitOffset = 160f;

    [Tooltip("边框厚度（显示边框时生效）。")]
    public float BorderThickness = 2f;

    [Tooltip("渐变纹理宽度。")]
    public int GradientTextureWidth = 512;

    [Tooltip("渐变纹理高度。")]
    public int GradientTextureHeight = 64;

    private readonly List<ReplicaV3ParameterDefinition> mParameters = new List<ReplicaV3ParameterDefinition>
    {
        new ReplicaV3ParameterDefinition
        {
            Id = "animation_speed",
            DisplayName = "动画速度",
            Description = "渐变移动速度。",
            Kind = ReplicaV3ParameterKind.Float,
            Min = 0.1f,
            Max = 20f,
            Step = 0.1f
        },
        new ReplicaV3ParameterDefinition
        {
            Id = "gradient_tiling",
            DisplayName = "渐变平铺",
            Description = "渐变条纹平铺倍数。",
            Kind = ReplicaV3ParameterKind.Float,
            Min = 1f,
            Max = 6f,
            Step = 0.1f
        },
        new ReplicaV3ParameterDefinition
        {
            Id = "show_border",
            DisplayName = "显示边框",
            Description = "是否启用外框渐变边。",
            Kind = ReplicaV3ParameterKind.Bool
        },
        new ReplicaV3ParameterDefinition
        {
            Id = "pause_on_hover",
            DisplayName = "悬停暂停",
            Description = "悬停时暂停渐变位移。",
            Kind = ReplicaV3ParameterKind.Bool
        },
        new ReplicaV3ParameterDefinition
        {
            Id = "yoyo",
            DisplayName = "往返模式",
            Description = "开启后渐变来回运动。",
            Kind = ReplicaV3ParameterKind.Bool
        }
    };

    private float mElapsed;
    private bool mHovering;
    private Vector2 mContentBasePosition;
    private Texture2D mGradientTexture;
    private Sprite mGradientSprite;

    protected override void OnEffectInitialize()
    {
        EnsureBindings();
        CacheBasePose();
        ApplyStaticVisual();
        BuildGradientSprite(force: true);
        ApplyGradientMotion();

        SetCanvasAlpha(1f);
        SetLifecycleLooping();
    }

    protected override void OnEffectTick(float deltaTime, float unscaledDeltaTime)
    {
        if (PauseOnHover && mHovering)
        {
            ApplyGradientMotion();
            return;
        }

        if (AnimationSpeed > 0.01f)
        {
            mElapsed += Mathf.Max(0f, unscaledDeltaTime);
        }

        BuildGradientSprite();
        ApplyGradientMotion();
    }

    protected override void OnPlayIn()
    {
        KillTrackedTweens(false);
        EnsureBindings();

        var duration = 0.35f;
        if (ContentRoot != null)
        {
            ContentRoot.anchoredPosition = mContentBasePosition + new Vector2(0f, EnterOffset);
            TrackTween(ContentRoot
                .DOAnchorPos(mContentBasePosition, duration)
                .SetEase(Ease.OutCubic));
        }

        SetCanvasAlpha(0f);
        if (EffectCanvasGroup != null)
        {
            TrackTween(EffectCanvasGroup
                .DOFade(1f, duration)
                .SetEase(Ease.OutCubic));
        }

        SetLifecycleLooping();
    }

    protected override void OnPlayOut(Action onComplete)
    {
        KillTrackedTweens(false);

        var duration = 0.25f;
        var doneCount = 0;
        var required = 0;

        void TryFinish()
        {
            doneCount++;
            if (doneCount >= required)
            {
                onComplete?.Invoke();
            }
        }

        if (ContentRoot != null)
        {
            required++;
            TrackTween(ContentRoot
                .DOAnchorPos(mContentBasePosition + new Vector2(0f, ExitOffset), duration)
                .SetEase(Ease.InCubic)
                .OnComplete(TryFinish));
        }

        if (EffectCanvasGroup != null)
        {
            required++;
            TrackTween(EffectCanvasGroup
                .DOFade(0f, duration)
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
        KillTrackedTweens(false);
        EnsureBindings();
        CacheBasePose();

        mElapsed = 0f;
        mHovering = false;

        ApplyStaticVisual();
        BuildGradientSprite(force: true);
        ApplyGradientMotion();

        if (ContentRoot != null)
        {
            ContentRoot.anchoredPosition = mContentBasePosition;
        }

        SetCanvasAlpha(1f);
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
            case "animation_speed":
                value = AnimationSpeed;
                return true;
            case "gradient_tiling":
                value = GradientTiling;
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
            case "animation_speed":
                AnimationSpeed = Mathf.Clamp(value, 0.1f, 20f);
                return true;
            case "gradient_tiling":
                GradientTiling = Mathf.Clamp(value, 1f, 6f);
                ApplyGradientMotion();
                return true;
            default:
                return false;
        }
    }

    public override bool TryGetBoolParameter(string parameterId, out bool value)
    {
        switch (parameterId)
        {
            case "show_border":
                value = ShowBorder;
                return true;
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
            case "show_border":
                ShowBorder = value;
                ApplyStaticVisual();
                return true;
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

    private void EnsureBindings()
    {
        if (ContentRoot == null)
        {
            ContentRoot = EffectRoot != null ? EffectRoot.Find("Content") as RectTransform : null;
            if (ContentRoot == null)
            {
                ContentRoot = EffectRoot != null ? EffectRoot : transform as RectTransform;
            }
        }

        if (Frame == null && ContentRoot != null)
        {
            Frame = ContentRoot.Find("Frame") as RectTransform;
        }

        if (BorderGradientClip == null && Frame != null)
        {
            BorderGradientClip = Frame.Find("BorderGradientClip") as RectTransform;
        }

        if (BorderGradientRect == null && BorderGradientClip != null)
        {
            BorderGradientRect = BorderGradientClip.Find("BorderGradient") as RectTransform;
        }

        if (BorderGradientImage == null && BorderGradientRect != null)
        {
            BorderGradientImage = BorderGradientRect.GetComponent<Image>();
        }

        if (InnerBackground == null && Frame != null)
        {
            InnerBackground = Frame.Find("InnerBackground") as RectTransform;
        }

        if (MaskText == null && InnerBackground != null)
        {
            MaskText = InnerBackground.Find("TextMask/MaskText")?.GetComponent<Text>();
        }

        if (TextGradientRect == null && MaskText != null)
        {
            TextGradientRect = MaskText.transform.Find("TextGradient") as RectTransform;
        }

        if (TextGradientImage == null && TextGradientRect != null)
        {
            TextGradientImage = TextGradientRect.GetComponent<Image>();
        }

        if (InteractionHitSource == null)
        {
            InteractionHitSource = Frame != null ? Frame : (ContentRoot != null ? ContentRoot : (EffectRoot != null ? EffectRoot : transform as RectTransform));
        }

        if (string.IsNullOrWhiteSpace(EffectKey))
        {
            EffectKey = "gradient-text-v3";
        }

        if (string.IsNullOrWhiteSpace(EffectDisplayName))
        {
            EffectDisplayName = "GradientText V3";
        }

        if (string.IsNullOrWhiteSpace(UsageDescription))
        {
            UsageDescription = "动态渐变文字支持方向、速度、边框和悬停暂停。";
        }
    }

    private void CacheBasePose()
    {
        if (ContentRoot != null)
        {
            mContentBasePosition = ContentRoot.anchoredPosition;
        }
    }

    private void ApplyStaticVisual()
    {
        if (MaskText != null)
        {
            MaskText.text = string.IsNullOrWhiteSpace(DisplayText) ? "GradientText" : DisplayText;
        }

        if (BorderGradientClip != null)
        {
            BorderGradientClip.gameObject.SetActive(ShowBorder);
        }

        if (InnerBackground != null)
        {
            if (ShowBorder)
            {
                var t = Mathf.Max(0f, BorderThickness);
                InnerBackground.offsetMin = new Vector2(t, t);
                InnerBackground.offsetMax = new Vector2(-t, -t);
            }
            else
            {
                InnerBackground.offsetMin = Vector2.zero;
                InnerBackground.offsetMax = Vector2.zero;
            }
        }
    }

    private void BuildGradientSprite(bool force = false)
    {
        if (TextGradientImage == null && BorderGradientImage == null)
        {
            return;
        }

        var safeColors = Colors != null && Colors.Length >= 2
            ? Colors
            : new[]
            {
                new Color(0.321f, 0.153f, 1f, 1f),
                new Color(1f, 0.624f, 0.988f, 1f),
                new Color(0.694f, 0.620f, 0.937f, 1f)
            };

        if (!force && mGradientTexture != null && mGradientSprite != null)
        {
            return;
        }

        ReleaseGradientResources();

        var width = Mathf.Max(32, GradientTextureWidth);
        var height = Mathf.Max(8, GradientTextureHeight);
        mGradientTexture = new Texture2D(width, height, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };

        for (var x = 0; x < width; x++)
        {
            var t = width <= 1 ? 0f : x / (float)(width - 1);
            var c = SampleMultiStopGradient(safeColors, t);
            for (var y = 0; y < height; y++)
            {
                mGradientTexture.SetPixel(x, y, c);
            }
        }

        mGradientTexture.Apply(false, false);
        mGradientSprite = Sprite.Create(mGradientTexture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 100f);

        if (TextGradientImage != null)
        {
            TextGradientImage.sprite = mGradientSprite;
            TextGradientImage.color = Color.white;
            TextGradientImage.preserveAspect = false;
        }

        if (BorderGradientImage != null)
        {
            BorderGradientImage.sprite = mGradientSprite;
            BorderGradientImage.color = Color.white;
            BorderGradientImage.preserveAspect = false;
        }
    }

    private void ApplyGradientMotion()
    {
        var progress = NormalizedProgress(mElapsed, Mathf.Max(0.1f, AnimationSpeed), Yoyo);
        var tiling = Mathf.Max(1f, GradientTiling);

        if (TextGradientRect != null && InnerBackground != null)
        {
            ApplyGradientToRect(TextGradientRect, InnerBackground, tiling, Direction, progress);
        }

        if (ShowBorder && BorderGradientRect != null && Frame != null)
        {
            ApplyGradientToRect(BorderGradientRect, Frame, tiling, Direction, progress);
        }
    }

    private static void ApplyGradientToRect(
        RectTransform gradientRect,
        RectTransform targetRect,
        float tiling,
        ReplicaV3GradientTextDirection direction,
        float progress)
    {
        if (gradientRect == null || targetRect == null)
        {
            return;
        }

        var baseSize = targetRect.rect.size;
        if (baseSize.x <= 0.01f || baseSize.y <= 0.01f)
        {
            return;
        }

        gradientRect.anchorMin = new Vector2(0.5f, 0.5f);
        gradientRect.anchorMax = new Vector2(0.5f, 0.5f);
        gradientRect.pivot = new Vector2(0.5f, 0.5f);

        switch (direction)
        {
            case ReplicaV3GradientTextDirection.Vertical:
            {
                gradientRect.sizeDelta = new Vector2(baseSize.x, baseSize.y * tiling);
                var range = Mathf.Max(0f, (gradientRect.sizeDelta.y - baseSize.y) * 0.5f);
                gradientRect.anchoredPosition = new Vector2(0f, Mathf.Lerp(-range, range, progress));
                break;
            }
            case ReplicaV3GradientTextDirection.Diagonal:
            {
                gradientRect.sizeDelta = new Vector2(baseSize.x * tiling, baseSize.y * tiling);
                var rangeX = Mathf.Max(0f, (gradientRect.sizeDelta.x - baseSize.x) * 0.5f);
                var rangeY = Mathf.Max(0f, (gradientRect.sizeDelta.y - baseSize.y) * 0.5f);
                gradientRect.anchoredPosition = new Vector2(
                    Mathf.Lerp(rangeX, -rangeX, progress),
                    Mathf.Lerp(-rangeY, rangeY, progress));
                break;
            }
            default:
            {
                gradientRect.sizeDelta = new Vector2(baseSize.x * tiling, baseSize.y);
                var range = Mathf.Max(0f, (gradientRect.sizeDelta.x - baseSize.x) * 0.5f);
                gradientRect.anchoredPosition = new Vector2(Mathf.Lerp(range, -range, progress), 0f);
                break;
            }
        }
    }

    private static Color SampleMultiStopGradient(Color[] colors, float t)
    {
        if (colors == null || colors.Length == 0)
        {
            return Color.white;
        }

        if (colors.Length == 1)
        {
            return colors[0];
        }

        t = Mathf.Clamp01(t);
        var count = colors.Length;
        var scaled = t * count;
        var index = Mathf.Clamp(Mathf.FloorToInt(scaled), 0, count - 1);
        var next = (index + 1) % count;
        var localT = scaled - index;
        return Color.Lerp(colors[index], colors[next], localT);
    }

    private static float NormalizedProgress(float elapsed, float duration, bool yoyo)
    {
        if (duration <= 0.0001f)
        {
            return 0f;
        }

        if (!yoyo)
        {
            return Mathf.Repeat(elapsed / duration, 1f);
        }

        var fullCycle = duration * 2f;
        var t = Mathf.Repeat(elapsed, fullCycle);
        if (t <= duration)
        {
            return t / duration;
        }

        return 1f - ((t - duration) / duration);
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

    private void OnDrawGizmos()
    {
        var hitSource = InteractionHitSource != null
            ? InteractionHitSource
            : (Frame != null ? Frame : (ContentRoot != null ? ContentRoot : (EffectRoot != null ? EffectRoot : transform as RectTransform)));
        DrawInteractionRangeGizmo(ShowInteractionRange, hitSource, InteractionRangeDependency, InteractionRangePadding);
    }
}
