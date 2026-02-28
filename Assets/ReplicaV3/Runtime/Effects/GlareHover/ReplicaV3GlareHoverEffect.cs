using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class ReplicaV3GlareHoverEffect : ReplicaV3EffectBase, IPointerEnterHandler, IPointerExitHandler
{
    private static Sprite sGlareSprite;

    [Header("组件绑定（GlareHover）")]
    [Tooltip("主要容器。")]
    public RectTransform Container;

    [Tooltip("高光条节点。")]
    public RectTransform Glare;

    [Tooltip("高光图。")]
    public Image GlareImage;

    [Tooltip("边框高亮组件。")]
    public Outline ContainerOutline;

    [Tooltip("标签文本。")]
    public Text LabelText;

    [Tooltip("主要可交互判定组件。为空时回退到 Container。")]
    public RectTransform InteractionHitSource;

    [Tooltip("交互范围跟随依赖组件。为空时回退到 InteractionHitSource。")]
    public RectTransform InteractionRangeDependency;

    [Tooltip("编辑器中显示可交互范围。默认开启，可手动关闭。")]
    public bool ShowInteractionRange = true;

    [Tooltip("交互范围扩展像素。")]
    public float InteractionRangePadding = 0f;

    [Header("参数（可在参数面板实时调）")]
    [Tooltip("高光扫描时长。")]
    public float TransitionDuration = 0.65f;

    [Tooltip("高光透明度。")]
    [Range(0f, 1f)]
    public float GlareOpacity = 0.5f;

    [Tooltip("高光尺寸百分比（相对容器最大边）。")]
    public float GlareSizePercent = 250f;

    [Tooltip("高光角度。")]
    public float GlareAngleDeg = -45f;

    [Tooltip("是否只播放一次。")]
    public bool PlayOnce = false;

    [Tooltip("标签文案。")]
    public string Label = "GlareHover";

    [Tooltip("边框基础色。")]
    public Color BorderColor = new Color(0.2f, 0.2f, 0.2f, 1f);

    [Header("进入/退出")]
    [Tooltip("进入偏移。")]
    public Vector2 EnterOffset = new Vector2(0f, -220f);

    [Tooltip("退出偏移。")]
    public Vector2 ExitOffset = new Vector2(0f, 220f);

    [Tooltip("进入/退出过渡时长。")]
    public float TransitionFadeDuration = 0.3f;

    private readonly List<ReplicaV3ParameterDefinition> mParameters = new List<ReplicaV3ParameterDefinition>
    {
        new ReplicaV3ParameterDefinition { Id = "sweep_duration", DisplayName = "扫描时长", Description = "高光扫过容器的耗时。", Kind = ReplicaV3ParameterKind.Float, Min = 0.05f, Max = 3f, Step = 0.05f },
        new ReplicaV3ParameterDefinition { Id = "glare_opacity", DisplayName = "高光透明度", Description = "高光层不透明度。", Kind = ReplicaV3ParameterKind.Float, Min = 0f, Max = 1f, Step = 0.01f },
        new ReplicaV3ParameterDefinition { Id = "glare_size", DisplayName = "高光尺寸%", Description = "高光层大小百分比。", Kind = ReplicaV3ParameterKind.Float, Min = 100f, Max = 500f, Step = 5f },
        new ReplicaV3ParameterDefinition { Id = "play_once", DisplayName = "只播一次", Description = "离开时是否回退高光。", Kind = ReplicaV3ParameterKind.Bool }
    };

    private bool mHovered;
    private Vector2 mStartGlarePos;
    private Vector2 mEndGlarePos;
    private Tween mGlareTween;

    protected override void OnEffectInitialize()
    {
        EnsureBindings();
        RecomputeGlarePath();
        ApplyStaticVisual();
        ResetVisual();
        SetCanvasAlpha(1f);
        SetLifecycleLooping();
    }

    protected override void OnEffectTick(float deltaTime, float unscaledDeltaTime)
    {
        UpdateHoverFromPointer();
    }

    protected override void OnPlayIn()
    {
        EnsureBindings();
        KillTrackedTweens(false);

        var duration = Mathf.Max(0.05f, TransitionFadeDuration);
        SetCanvasAlpha(0f);

        if (Container != null)
        {
            Container.anchoredPosition = EnterOffset;
            TrackTween(Container.DOAnchorPos(Vector2.zero, duration).SetEase(Ease.OutCubic));
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
        var duration = Mathf.Max(0.05f, TransitionFadeDuration);
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

        if (Container != null)
        {
            required++;
            TrackTween(Container.DOAnchorPos(ExitOffset, duration).SetEase(Ease.InCubic).OnComplete(TryDone));
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
        KillGlareTween();

        mHovered = false;
        ApplyStaticVisual();
        ResetVisual();
        SetCanvasAlpha(1f);
        SetLifecycleLooping();
    }

    public override IReadOnlyList<ReplicaV3ParameterDefinition> GetParameterDefinitions() => mParameters;

    public override bool TryGetFloatParameter(string parameterId, out float value)
    {
        switch (parameterId)
        {
            case "sweep_duration": value = TransitionDuration; return true;
            case "glare_opacity": value = GlareOpacity; return true;
            case "glare_size": value = GlareSizePercent; return true;
            default: value = 0f; return false;
        }
    }

    public override bool TrySetFloatParameter(string parameterId, float value)
    {
        switch (parameterId)
        {
            case "sweep_duration": TransitionDuration = Mathf.Clamp(value, 0.05f, 3f); return true;
            case "glare_opacity": GlareOpacity = Mathf.Clamp01(value); ApplyStaticVisual(); return true;
            case "glare_size": GlareSizePercent = Mathf.Clamp(value, 100f, 500f); RecomputeGlarePath(); return true;
            default: return false;
        }
    }

    public override bool TryGetBoolParameter(string parameterId, out bool value)
    {
        if (parameterId == "play_once")
        {
            value = PlayOnce;
            return true;
        }

        value = false;
        return false;
    }

    public override bool TrySetBoolParameter(string parameterId, bool value)
    {
        if (parameterId == "play_once")
        {
            PlayOnce = value;
            return true;
        }

        return false;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (mHovered)
        {
            return;
        }

        mHovered = true;
        PlayGlareSweep();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        mHovered = false;
        ResetGlare();
    }

    private RectTransform EnsureBindings()
    {
        if (Container == null)
        {
            if (EffectRoot != null)
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
        }

        if (Glare == null && Container != null)
        {
            var glare = Container.Find("Glare");
            if (glare != null)
            {
                Glare = glare as RectTransform;
            }
        }

        if (GlareImage == null && Glare != null)
        {
            GlareImage = Glare.GetComponent<Image>();
        }

        if (ContainerOutline == null && Container != null)
        {
            ContainerOutline = Container.GetComponent<Outline>();
        }

        if (LabelText == null && Container != null)
        {
            var label = Container.Find("Label");
            if (label != null)
            {
                LabelText = label.GetComponent<Text>();
            }
        }

        if (InteractionHitSource == null)
        {
            InteractionHitSource = Container != null ? Container : (EffectRoot != null ? EffectRoot : transform as RectTransform);
        }

        return InteractionHitSource;
    }

    private void ApplyStaticVisual()
    {
        if (LabelText != null)
        {
            LabelText.text = string.IsNullOrWhiteSpace(Label) ? "GlareHover" : Label;
        }

        if (ContainerOutline != null)
        {
            ContainerOutline.effectColor = BorderColor;
        }

        if (Glare != null)
        {
            Glare.localRotation = Quaternion.Euler(0f, 0f, GlareAngleDeg);
        }

        if (GlareImage != null)
        {
            if (GlareImage.sprite == null)
            {
                GlareImage.sprite = GetOrCreateGlareSprite();
            }

            var c = GlareImage.color;
            c.a = Mathf.Clamp01(GlareOpacity);
            GlareImage.color = c;
        }
    }

    private void UpdateHoverFromPointer()
    {
        var target = EnsureBindings();
        if (target == null)
        {
            return;
        }

        var cam = ResolveReliableEventCamera(target);
        var screenPos = (Vector2)Input.mousePosition;
        var hovered = RectTransformUtility.RectangleContainsScreenPoint(target, screenPos, cam);
        if (hovered == mHovered)
        {
            return;
        }

        mHovered = hovered;
        if (hovered)
        {
            PlayGlareSweep();
        }
        else
        {
            ResetGlare();
        }
    }

    private void ResetVisual()
    {
        RecomputeGlarePath();
        if (Glare != null)
        {
            Glare.anchoredPosition = mStartGlarePos;
        }

        if (Container != null)
        {
            Container.anchoredPosition = Vector2.zero;
        }
    }

    private void PlayGlareSweep()
    {
        if (Glare == null)
        {
            return;
        }

        RecomputeGlarePath();
        KillGlareTween();
        Glare.anchoredPosition = mStartGlarePos;
        mGlareTween = TrackTween(Glare
            .DOAnchorPos(mEndGlarePos, Mathf.Max(0.01f, TransitionDuration))
            .SetEase(Ease.OutCubic));
    }

    private void ResetGlare()
    {
        if (Glare == null)
        {
            return;
        }

        RecomputeGlarePath();
        KillGlareTween();

        if (PlayOnce)
        {
            Glare.anchoredPosition = mStartGlarePos;
            return;
        }

        mGlareTween = TrackTween(Glare
            .DOAnchorPos(mStartGlarePos, Mathf.Max(0.01f, TransitionDuration))
            .SetEase(Ease.OutCubic));
    }

    private void RecomputeGlarePath()
    {
        if (Container == null)
        {
            mStartGlarePos = Vector2.zero;
            mEndGlarePos = Vector2.zero;
            return;
        }

        var sizePercent = Mathf.Max(100f, GlareSizePercent) / 100f;
        var glareSize = Mathf.Max(Container.rect.width, Container.rect.height) * sizePercent;
        if (Glare != null)
        {
            Glare.sizeDelta = new Vector2(glareSize, glareSize);
        }

        var travel = Mathf.Max(10f, glareSize * 0.5f);
        mStartGlarePos = new Vector2(-travel, -travel);
        mEndGlarePos = new Vector2(travel, travel);
    }

    private void KillGlareTween()
    {
        if (mGlareTween == null)
        {
            return;
        }

        mGlareTween.Kill(false);
        mGlareTween = null;
    }

    private static Sprite GetOrCreateGlareSprite()
    {
        if (sGlareSprite != null)
        {
            return sGlareSprite;
        }

        const int width = 256;
        const int height = 64;

        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
            name = "ReplicaV3_GlareHover_Sprite"
        };

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var t = x / (float)(width - 1);
                var d = Mathf.Abs(t - 0.68f) / 0.16f;
                var alpha = Mathf.Clamp01(1f - d);
                alpha = alpha * alpha * (3f - (2f * alpha));
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        texture.Apply(false, false);
        sGlareSprite = Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 100f);
        return sGlareSprite;
    }

    private void OnDrawGizmos()
    {
        var hitSource = InteractionHitSource != null
            ? InteractionHitSource
            : (Container != null ? Container : (EffectRoot != null ? EffectRoot : transform as RectTransform));
        DrawInteractionRangeGizmo(ShowInteractionRange, hitSource, InteractionRangeDependency, InteractionRangePadding);
    }
}
