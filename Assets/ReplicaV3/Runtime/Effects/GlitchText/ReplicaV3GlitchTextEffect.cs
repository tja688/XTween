using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public sealed class ReplicaV3GlitchTextEffect : ReplicaV3EffectBase, IPointerEnterHandler, IPointerExitHandler
{
    [Header("组件绑定（GlitchText）")]
    [Tooltip("主要可交互判定组件。为空时回退到 GlitchContainer/ContentRoot。")]
    public RectTransform InteractionHitSource;

    [Tooltip("交互范围跟随依赖组件。为空时回退到 InteractionHitSource。")]
    public RectTransform InteractionRangeDependency;

    [Tooltip("编辑器中显示可交互范围。默认开启，可手动关闭。")]
    public bool ShowInteractionRange = true;

    [Tooltip("交互范围扩展像素。")]
    public float InteractionRangePadding = 0f;

    [Tooltip("外层内容容器（对应 V2 Backdrop）。")]
    public RectTransform ContentRoot;

    [Tooltip("故障文字容器（用于悬停判定）。")]
    public RectTransform GlitchContainer;

    [Tooltip("主文字 RectTransform。")]
    public RectTransform MainRect;

    [Tooltip("After 遮罩条。")]
    public RectTransform AfterMask;

    [Tooltip("Before 遮罩条。")]
    public RectTransform BeforeMask;

    [Tooltip("After 文本节点。")]
    public RectTransform AfterTextRect;

    [Tooltip("Before 文本节点。")]
    public RectTransform BeforeTextRect;

    [Tooltip("主文字。")]
    public Text MainText;

    [Tooltip("After 文本。")]
    public Text AfterText;

    [Tooltip("Before 文本。")]
    public Text BeforeText;

    [Tooltip("提示文字。")]
    public Text HintText;

    [Header("参数（可在参数面板实时调）")]
    [Tooltip("主文字内容。")]
    public string DisplayText = "GLITCH";

    [Tooltip("故障速度倍率。")]
    public float Speed = 1f;

    [Tooltip("是否启用文字阴影通道。")]
    public bool EnableShadows = true;

    [Tooltip("是否仅在悬停时触发故障。")]
    public bool EnableOnHover = true;

    [Tooltip("步进基础间隔。")]
    public float StepBase = 0.08f;

    [Tooltip("步进最小间隔。")]
    public float StepMin = 0.03f;

    [Tooltip("After 基准 X。")]
    public float AfterBaseX = 10f;

    [Tooltip("Before 基准 X。")]
    public float BeforeBaseX = -10f;

    [Tooltip("切片 X 偏移范围。")]
    public Vector2 SliceOffsetXRange = new Vector2(-6f, 6f);

    [Tooltip("切片 Y 偏移范围。")]
    public Vector2 SliceOffsetYRange = new Vector2(-4f, 4f);

    [Tooltip("主文字抖动范围。")]
    public Vector2 MainJitterRange = new Vector2(-2f, 2f);

    [Tooltip("切片高度范围。")]
    public Vector2 BandHeightRange = new Vector2(24f, 92f);

    [Tooltip("切片边界安全内缩。")]
    public float BandEdgePadding = 16f;

    [Tooltip("切片透明度范围。")]
    public Vector2 SliceAlphaRange = new Vector2(0.70f, 1f);

    [Tooltip("文字 punch 缩放向量。")]
    public Vector3 PunchScale = new Vector3(0.015f, 0.015f, 0f);

    [Tooltip("文字 punch 时长。")]
    public float PunchDuration = 0.08f;

    [Tooltip("文字 punch 震动次数。")]
    public int PunchVibrato = 2;

    [Tooltip("文字 punch 弹性。")]
    public float PunchElasticity = 0.4f;

    [Tooltip("After 阴影视觉颜色。")]
    public Color AfterShadowColor = Color.red;

    [Tooltip("After 阴影偏移。")]
    public Vector2 AfterShadowOffset = new Vector2(-5f, 0f);

    [Tooltip("Before 阴影视觉颜色。")]
    public Color BeforeShadowColor = Color.cyan;

    [Tooltip("Before 阴影偏移。")]
    public Vector2 BeforeShadowOffset = new Vector2(5f, 0f);

    [Tooltip("PlayIn 位移偏移。")]
    public float EnterOffset = 160f;

    [Tooltip("PlayOut 位移偏移。")]
    public float ExitOffset = 160f;

    private readonly List<ReplicaV3ParameterDefinition> mParameters = new List<ReplicaV3ParameterDefinition>
    {
        new ReplicaV3ParameterDefinition
        {
            Id = "speed",
            DisplayName = "故障速度",
            Description = "故障步进速度倍率。",
            Kind = ReplicaV3ParameterKind.Float,
            Min = 0.1f,
            Max = 5f,
            Step = 0.1f
        },
        new ReplicaV3ParameterDefinition
        {
            Id = "enable_on_hover",
            DisplayName = "仅悬停触发",
            Description = "开启后仅在悬停容器时触发故障。",
            Kind = ReplicaV3ParameterKind.Bool
        },
        new ReplicaV3ParameterDefinition
        {
            Id = "enable_shadows",
            DisplayName = "启用阴影通道",
            Description = "开启后启用 RGB 偏移阴影视觉。",
            Kind = ReplicaV3ParameterKind.Bool
        }
    };

    private float mTick;
    private bool mHoverInside;
    private bool mGlitchActive;
    private Vector2 mContentBasePosition;

    protected override void OnEffectInitialize()
    {
        EnsureBindings();
        CacheBasePose();
        ApplyModelVisual();
        ResetSlices();

        SetCanvasAlpha(1f);
        SetLifecycleLooping();
    }

    protected override void OnEffectTick(float deltaTime, float unscaledDeltaTime)
    {
        var dt = Mathf.Max(0f, unscaledDeltaTime);
        mGlitchActive = !EnableOnHover || mHoverInside;
        if (!mGlitchActive)
        {
            return;
        }

        mTick += dt;
        var speed = Mathf.Max(0.01f, Speed);
        var step = Mathf.Max(Mathf.Max(0.005f, StepMin), StepBase * speed);
        if (mTick < step)
        {
            return;
        }

        mTick = 0f;
        GlitchStep();
    }

    protected override void OnPlayIn()
    {
        KillTrackedTweens(false);
        EnsureBindings();

        var duration = Mathf.Max(0.08f, 0.35f);
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

        var duration = Mathf.Max(0.08f, 0.28f);
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
        ApplyModelVisual();

        mTick = 0f;
        mHoverInside = false;
        mGlitchActive = !EnableOnHover;

        if (ContentRoot != null)
        {
            ContentRoot.anchoredPosition = mContentBasePosition;
        }

        ResetSlices();
        SetCanvasAlpha(1f);
        SetLifecycleLooping();
    }

    public override IReadOnlyList<ReplicaV3ParameterDefinition> GetParameterDefinitions()
    {
        return mParameters;
    }

    public override bool TryGetFloatParameter(string parameterId, out float value)
    {
        switch (parameterId)
        {
            case "speed":
                value = Speed;
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
            case "speed":
                Speed = Mathf.Clamp(value, 0.1f, 5f);
                return true;
            default:
                return false;
        }
    }

    public override bool TryGetBoolParameter(string parameterId, out bool value)
    {
        switch (parameterId)
        {
            case "enable_on_hover":
                value = EnableOnHover;
                return true;
            case "enable_shadows":
                value = EnableShadows;
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
            case "enable_on_hover":
                EnableOnHover = value;
                if (!EnableOnHover)
                {
                    mHoverInside = false;
                    mGlitchActive = true;
                }

                return true;
            case "enable_shadows":
                EnableShadows = value;
                ApplyShadowState();
                return true;
            default:
                return false;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        mHoverInside = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        mHoverInside = false;
        mTick = 0f;
        if (EnableOnHover)
        {
            ResetSlices();
        }
    }

    private void EnsureBindings()
    {
        if (ContentRoot == null)
        {
            ContentRoot = EffectRoot != null ? EffectRoot.Find("Backdrop") as RectTransform : null;
            if (ContentRoot == null)
            {
                ContentRoot = EffectRoot != null ? EffectRoot : transform as RectTransform;
            }
        }

        if (GlitchContainer == null && ContentRoot != null)
        {
            var container = ContentRoot.Find("GlitchContainer");
            if (container != null)
            {
                GlitchContainer = container as RectTransform;
            }
        }

        if (MainText == null && GlitchContainer != null)
        {
            MainText = GlitchContainer.Find("MainText")?.GetComponent<Text>();
        }

        if (MainRect == null)
        {
            MainRect = MainText != null ? MainText.rectTransform : null;
        }

        if (AfterMask == null && GlitchContainer != null)
        {
            AfterMask = GlitchContainer.Find("AfterMask") as RectTransform;
        }

        if (BeforeMask == null && GlitchContainer != null)
        {
            BeforeMask = GlitchContainer.Find("BeforeMask") as RectTransform;
        }

        if (AfterText == null && AfterMask != null)
        {
            AfterText = AfterMask.Find("AfterText")?.GetComponent<Text>();
        }

        if (BeforeText == null && BeforeMask != null)
        {
            BeforeText = BeforeMask.Find("BeforeText")?.GetComponent<Text>();
        }

        if (AfterTextRect == null)
        {
            AfterTextRect = AfterText != null ? AfterText.rectTransform : null;
        }

        if (BeforeTextRect == null)
        {
            BeforeTextRect = BeforeText != null ? BeforeText.rectTransform : null;
        }

        if (HintText == null && EffectRoot != null)
        {
            HintText = EffectRoot.Find("Hint")?.GetComponent<Text>();
        }

        if (InteractionHitSource == null)
        {
            InteractionHitSource = GlitchContainer != null ? GlitchContainer : (ContentRoot != null ? ContentRoot : (EffectRoot != null ? EffectRoot : transform as RectTransform));
        }

        if (string.IsNullOrWhiteSpace(EffectKey))
        {
            EffectKey = "glitch-text-v3";
        }

        if (string.IsNullOrWhiteSpace(EffectDisplayName))
        {
            EffectDisplayName = "GlitchText V3";
        }

        if (string.IsNullOrWhiteSpace(UsageDescription))
        {
            UsageDescription = "故障文字支持常开或悬停触发，可调速度与阴影通道。";
        }
    }

    private void CacheBasePose()
    {
        if (ContentRoot != null)
        {
            mContentBasePosition = ContentRoot.anchoredPosition;
        }
    }

    private void ApplyModelVisual()
    {
        var safeText = string.IsNullOrWhiteSpace(DisplayText) ? "GLITCH" : DisplayText;

        if (HintText != null)
        {
            HintText.text = EnableOnHover
                ? "GlitchText  |  hover to trigger RGB slices"
                : "GlitchText  |  always on";
        }

        if (MainText != null)
        {
            MainText.text = safeText;
        }

        if (AfterText != null)
        {
            AfterText.text = safeText;
        }

        if (BeforeText != null)
        {
            BeforeText.text = safeText;
        }

        ApplyShadowState();
    }

    private void ApplyShadowState()
    {
        ApplyTextShadow(AfterText, EnableShadows, AfterShadowColor, AfterShadowOffset);
        ApplyTextShadow(BeforeText, EnableShadows, BeforeShadowColor, BeforeShadowOffset);
    }

    private static void ApplyTextShadow(Text text, bool enable, Color color, Vector2 offset)
    {
        if (text == null)
        {
            return;
        }

        var shadow = text.GetComponent<Shadow>();
        if (!enable)
        {
            if (shadow != null)
            {
                Destroy(shadow);
            }

            return;
        }

        if (shadow == null)
        {
            shadow = text.gameObject.AddComponent<Shadow>();
        }

        shadow.effectColor = color;
        shadow.effectDistance = offset;
    }

    private void GlitchStep()
    {
        if (MainRect == null)
        {
            return;
        }

        var height = MainRect.rect.height;
        var half = height * 0.5f;

        var afterBand = Random.Range(BandHeightRange.x, BandHeightRange.y);
        var afterY = Random.Range(-half + BandEdgePadding, half - BandEdgePadding);
        SetBand(AfterMask, afterBand, afterY);
        if (AfterTextRect != null)
        {
            AfterTextRect.anchoredPosition = new Vector2(
                AfterBaseX + Random.Range(SliceOffsetXRange.x, SliceOffsetXRange.y),
                Random.Range(SliceOffsetYRange.x, SliceOffsetYRange.y));
        }

        if (AfterText != null)
        {
            var alpha = Random.Range(SliceAlphaRange.x, SliceAlphaRange.y);
            var c = AfterText.color;
            AfterText.color = new Color(c.r, c.g, c.b, alpha);
        }

        var beforeBand = Random.Range(BandHeightRange.x, BandHeightRange.y);
        var beforeY = Random.Range(-half + BandEdgePadding, half - BandEdgePadding);
        SetBand(BeforeMask, beforeBand, beforeY);
        if (BeforeTextRect != null)
        {
            BeforeTextRect.anchoredPosition = new Vector2(
                BeforeBaseX + Random.Range(SliceOffsetXRange.x, SliceOffsetXRange.y),
                Random.Range(SliceOffsetYRange.x, SliceOffsetYRange.y));
        }

        if (BeforeText != null)
        {
            var alpha = Random.Range(SliceAlphaRange.x, SliceAlphaRange.y);
            var c = BeforeText.color;
            BeforeText.color = new Color(c.r, c.g, c.b, alpha);
        }

        MainRect.anchoredPosition = new Vector2(
            Random.Range(MainJitterRange.x, MainJitterRange.y),
            Random.Range(MainJitterRange.x, MainJitterRange.y));

        DOTween.Kill(MainRect);
        TrackTween(MainRect.DOPunchScale(
            PunchScale,
            Mathf.Max(0.01f, PunchDuration),
            Mathf.Max(1, PunchVibrato),
            Mathf.Clamp01(PunchElasticity)));
    }

    private void ResetSlices()
    {
        if (MainRect != null)
        {
            MainRect.anchoredPosition = Vector2.zero;
            MainRect.localScale = Vector3.one;
        }

        if (AfterTextRect != null)
        {
            AfterTextRect.anchoredPosition = new Vector2(AfterBaseX, 0f);
        }

        if (BeforeTextRect != null)
        {
            BeforeTextRect.anchoredPosition = new Vector2(BeforeBaseX, 0f);
        }

        if (AfterText != null)
        {
            var c = AfterText.color;
            AfterText.color = new Color(c.r, c.g, c.b, 1f);
        }

        if (BeforeText != null)
        {
            var c = BeforeText.color;
            BeforeText.color = new Color(c.r, c.g, c.b, 1f);
        }

        if (EnableOnHover)
        {
            SetBand(AfterMask, 0f, 0f);
            SetBand(BeforeMask, 0f, 0f);
        }
        else
        {
            SetBand(AfterMask, 68f, 30f);
            SetBand(BeforeMask, 58f, -22f);
        }
    }

    private static void SetBand(RectTransform maskRect, float height, float centerY)
    {
        if (maskRect == null)
        {
            return;
        }

        maskRect.anchorMin = new Vector2(0f, 0.5f);
        maskRect.anchorMax = new Vector2(1f, 0.5f);
        maskRect.pivot = new Vector2(0.5f, 0.5f);
        maskRect.sizeDelta = new Vector2(0f, Mathf.Max(0f, height));
        maskRect.anchoredPosition = new Vector2(0f, centerY);
    }

    private void OnDrawGizmos()
    {
        var hitSource = InteractionHitSource != null
            ? InteractionHitSource
            : (GlitchContainer != null ? GlitchContainer : (ContentRoot != null ? ContentRoot : (EffectRoot != null ? EffectRoot : transform as RectTransform)));
        DrawInteractionRangeGizmo(ShowInteractionRange, hitSource, InteractionRangeDependency, InteractionRangePadding);
    }
}
