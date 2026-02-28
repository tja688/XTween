using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class ReplicaV3GlassIconsTiltEffect : ReplicaV3EffectBase, IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler
{
    [Serializable]
    public sealed class IconBinding
    {
        [Tooltip("图标单元根节点。")]
        public RectTransform Root;

        [Tooltip("背板层。")]
        public RectTransform Back;

        [Tooltip("前景玻璃层。")]
        public RectTransform Front;

        [Tooltip("标签根节点。")]
        public RectTransform LabelRoot;

        [Tooltip("标签透明组。")]
        public CanvasGroup LabelGroup;

        [Tooltip("图标字形文本。")]
        public Text GlyphText;

        [Tooltip("标签文本。")]
        public Text LabelText;

        [Tooltip("背板颜色图像。")]
        public Image BackImage;

        [NonSerialized] public bool Hovered;
    }

    [Header("组件绑定（GlassIconsTilt）")]
    [Tooltip("主要可交互判定组件。为空时回退到 StageRoot/EffectRoot。")]
    public RectTransform InteractionHitSource;

    [Tooltip("交互范围跟随依赖组件。为空时回退到 InteractionHitSource。")]
    public RectTransform InteractionRangeDependency;

    [Tooltip("编辑器中显示可交互范围。默认开启，可手动关闭。")]
    public bool ShowInteractionRange = true;

    [Tooltip("交互范围扩展像素。")]
    public float InteractionRangePadding = 0f;

    [Tooltip("舞台容器，PlayIn/PlayOut 位移挂点。")]
    public RectTransform StageRoot;

    [Tooltip("图标网格容器。")]
    public RectTransform GridRoot;

    [Tooltip("提示文案。")]
    public Text HintText;

    [Tooltip("图标绑定列表。可手工绑定，也可运行时自动按层级补全。")]
    public List<IconBinding> Icons = new List<IconBinding>();

    [Header("参数（可在参数面板实时调）")]
    [Tooltip("提示文案。")]
    public string Hint = "GlassIcons  |  Hover each icon tile";

    [Tooltip("悬停缩放。")]
    public float HoverScale = 1.04f;

    [Tooltip("悬停过渡时长。")]
    public float HoverDuration = 0.24f;

    [Tooltip("回到 Idle 时长。")]
    public float IdleDuration = 0.20f;

    [Tooltip("背板悬停偏移。")]
    public Vector2 BackHoverOffset = new Vector2(-12f, 10f);

    [Tooltip("背板悬停旋转。")]
    public float BackHoverRotation = 25f;

    [Tooltip("背板 Idle 旋转。")]
    public float BackIdleRotation = 15f;

    [Tooltip("前景悬停偏移。")]
    public Vector2 FrontHoverOffset = new Vector2(0f, -8f);

    [Tooltip("前景悬停缩放。")]
    public float FrontHoverScale = 1.08f;

    [Tooltip("标签悬停 Y。")]
    public float LabelHoverY = -150f;

    [Tooltip("标签 Idle Y。")]
    public float LabelIdleY = -168f;

    [Tooltip("PlayIn 位移偏移。")]
    public float EnterOffset = 120f;

    [Tooltip("PlayOut 位移偏移。")]
    public float ExitOffset = 120f;

    private readonly List<ReplicaV3ParameterDefinition> mParameters = new List<ReplicaV3ParameterDefinition>
    {
        new ReplicaV3ParameterDefinition
        {
            Id = "hover_scale",
            DisplayName = "悬停缩放",
            Description = "图标悬停时的整体缩放倍数。",
            Kind = ReplicaV3ParameterKind.Float,
            Min = 1f,
            Max = 1.3f,
            Step = 0.01f
        },
        new ReplicaV3ParameterDefinition
        {
            Id = "hover_duration",
            DisplayName = "悬停时长",
            Description = "进入悬停状态的补间时长。",
            Kind = ReplicaV3ParameterKind.Float,
            Min = 0.05f,
            Max = 1f,
            Step = 0.01f
        },
        new ReplicaV3ParameterDefinition
        {
            Id = "idle_duration",
            DisplayName = "回弹时长",
            Description = "离开悬停后回到 Idle 的时长。",
            Kind = ReplicaV3ParameterKind.Float,
            Min = 0.05f,
            Max = 1f,
            Step = 0.01f
        },
        new ReplicaV3ParameterDefinition
        {
            Id = "front_hover_scale",
            DisplayName = "前景缩放",
            Description = "前景玻璃层悬停缩放倍数。",
            Kind = ReplicaV3ParameterKind.Float,
            Min = 1f,
            Max = 1.4f,
            Step = 0.01f
        },
        new ReplicaV3ParameterDefinition
        {
            Id = "label_hover_y",
            DisplayName = "标签悬停Y",
            Description = "标签在悬停状态的 Y 位置。",
            Kind = ReplicaV3ParameterKind.Float,
            Min = -240f,
            Max = 40f,
            Step = 1f
        }
    };

    private Vector2 mStageBasePosition;
    private bool mPointerInside;
    private Vector2 mPointerScreenPosition;
    private int mHoveredIndex = -1;

    protected override void OnEffectInitialize()
    {
        EnsureBindings();
        ApplyLabels();
        CacheBasePose();
        ResetIconVisuals();
        SetCanvasAlpha(1f);
        SetLifecycleLooping();
    }

    protected override void OnEffectTick(float deltaTime, float unscaledDeltaTime)
    {
        UpdateHoveredIcon();
    }

    protected override void OnPlayIn()
    {
        KillTrackedTweens(false);
        EnsureBindings();

        var transitionDuration = Mathf.Max(0.08f, HoverDuration);
        if (StageRoot != null)
        {
            StageRoot.anchoredPosition = mStageBasePosition + new Vector2(0f, EnterOffset);
            TrackTween(StageRoot
                .DOAnchorPos(mStageBasePosition, transitionDuration)
                .SetEase(Ease.OutCubic));
        }

        SetCanvasAlpha(0f);
        if (EffectCanvasGroup != null)
        {
            TrackTween(EffectCanvasGroup
                .DOFade(1f, transitionDuration)
                .SetEase(Ease.OutCubic));
        }

        SetLifecycleLooping();
    }

    protected override void OnPlayOut(Action onComplete)
    {
        KillTrackedTweens(false);

        var duration = Mathf.Max(0.08f, IdleDuration);
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

        if (StageRoot != null)
        {
            required++;
            TrackTween(StageRoot
                .DOAnchorPos(mStageBasePosition + new Vector2(0f, ExitOffset), duration)
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
        ApplyLabels();

        mHoveredIndex = -1;
        mPointerInside = false;
        ResetIconVisuals();

        if (StageRoot != null)
        {
            StageRoot.anchoredPosition = mStageBasePosition;
        }

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
            case "hover_scale":
                value = HoverScale;
                return true;
            case "hover_duration":
                value = HoverDuration;
                return true;
            case "idle_duration":
                value = IdleDuration;
                return true;
            case "front_hover_scale":
                value = FrontHoverScale;
                return true;
            case "label_hover_y":
                value = LabelHoverY;
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
            case "hover_scale":
                HoverScale = Mathf.Clamp(value, 1f, 1.3f);
                return true;
            case "hover_duration":
                HoverDuration = Mathf.Clamp(value, 0.05f, 1f);
                return true;
            case "idle_duration":
                IdleDuration = Mathf.Clamp(value, 0.05f, 1f);
                return true;
            case "front_hover_scale":
                FrontHoverScale = Mathf.Clamp(value, 1f, 1.4f);
                return true;
            case "label_hover_y":
                LabelHoverY = Mathf.Clamp(value, -240f, 40f);
                if (mHoveredIndex >= 0)
                {
                    ApplyHoverMask(mHoveredIndex);
                }

                return true;
            default:
                return false;
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
        mPointerInside = true;
        if (eventData != null)
        {
            mPointerScreenPosition = eventData.position;
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        mPointerInside = false;
        ApplyHoverMask(-1);
    }

    public void OnPointerMove(PointerEventData eventData)
    {
        if (eventData != null)
        {
            mPointerScreenPosition = eventData.position;
            mPointerInside = true;
        }
    }

    private void EnsureBindings()
    {
        if (StageRoot == null)
        {
            StageRoot = EffectRoot != null ? EffectRoot : transform as RectTransform;
        }

        if (GridRoot == null && StageRoot != null)
        {
            var grid = StageRoot.Find("Grid");
            if (grid != null)
            {
                GridRoot = grid as RectTransform;
            }
        }

        if (HintText == null && StageRoot != null)
        {
            var hint = StageRoot.Find("Hint");
            if (hint != null)
            {
                HintText = hint.GetComponent<Text>();
            }
        }

        if (InteractionHitSource == null)
        {
            InteractionHitSource = StageRoot != null ? StageRoot : (EffectRoot != null ? EffectRoot : transform as RectTransform);
        }

        AutoCollectIconsIfNeeded();

        if (string.IsNullOrWhiteSpace(EffectKey))
        {
            EffectKey = "glass-icons-tilt-v3";
        }

        if (string.IsNullOrWhiteSpace(EffectDisplayName))
        {
            EffectDisplayName = "GlassIconsTilt V3";
        }

        if (string.IsNullOrWhiteSpace(UsageDescription))
        {
            UsageDescription = "鼠标悬停图标卡片时触发玻璃层偏移与标签揭示。";
        }
    }

    private void AutoCollectIconsIfNeeded()
    {
        if (GridRoot == null)
        {
            return;
        }

        if (Icons == null)
        {
            Icons = new List<IconBinding>();
        }

        if (Icons.Count > 0)
        {
            return;
        }

        for (var i = 0; i < GridRoot.childCount; i++)
        {
            var child = GridRoot.GetChild(i) as RectTransform;
            if (child == null)
            {
                continue;
            }

            var binding = new IconBinding
            {
                Root = child,
                Back = child.Find("IconBase/Back") as RectTransform,
                Front = child.Find("IconBase/Front") as RectTransform,
                LabelRoot = child.Find("LabelRoot") as RectTransform
            };

            if (binding.LabelRoot != null)
            {
                binding.LabelGroup = binding.LabelRoot.GetComponent<CanvasGroup>();
                binding.LabelText = binding.LabelRoot.GetComponentInChildren<Text>(true);
            }

            if (binding.Back != null)
            {
                binding.BackImage = binding.Back.GetComponent<Image>();
            }

            if (binding.Front != null)
            {
                var glyph = binding.Front.Find("Glyph");
                if (glyph != null)
                {
                    binding.GlyphText = glyph.GetComponent<Text>();
                }
            }

            Icons.Add(binding);
        }
    }

    private void CacheBasePose()
    {
        if (StageRoot != null)
        {
            mStageBasePosition = StageRoot.anchoredPosition;
        }
    }

    private void ApplyLabels()
    {
        if (HintText != null)
        {
            HintText.text = string.IsNullOrWhiteSpace(Hint) ? "GlassIcons  |  Hover each icon tile" : Hint;
        }
    }

    private void ResetIconVisuals()
    {
        if (Icons == null)
        {
            return;
        }

        for (var i = 0; i < Icons.Count; i++)
        {
            var icon = Icons[i];
            if (icon == null)
            {
                continue;
            }

            if (icon.Root != null)
            {
                icon.Root.localScale = Vector3.one;
            }

            if (icon.Back != null)
            {
                icon.Back.anchoredPosition = Vector2.zero;
                icon.Back.localRotation = Quaternion.Euler(0f, 0f, BackIdleRotation);
            }

            if (icon.Front != null)
            {
                icon.Front.anchoredPosition = Vector2.zero;
                icon.Front.localScale = Vector3.one;
            }

            if (icon.LabelRoot != null)
            {
                icon.LabelRoot.anchoredPosition = new Vector2(0f, LabelIdleY);
            }

            if (icon.LabelGroup != null)
            {
                icon.LabelGroup.alpha = 0f;
            }

            icon.Hovered = false;
        }
    }

    private void UpdateHoveredIcon()
    {
        if (!mPointerInside)
        {
            ApplyHoverMask(-1);
            return;
        }

        var hovered = -1;
        for (var i = 0; i < Icons.Count; i++)
        {
            var icon = Icons[i];
            if (icon == null || icon.Root == null)
            {
                continue;
            }

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    icon.Root,
                    mPointerScreenPosition,
                    ResolveInteractionCamera(),
                    out var local))
            {
                continue;
            }

            var rect = icon.Root.rect;
            var halfW = Mathf.Max(1f, rect.width * 0.5f);
            var halfH = Mathf.Max(1f, rect.height * 0.5f);
            if (Mathf.Abs(local.x) <= halfW && Mathf.Abs(local.y) <= halfH)
            {
                hovered = i;
                break;
            }
        }

        ApplyHoverMask(hovered);
    }

    private void ApplyHoverMask(int hoveredIndex)
    {
        if (mHoveredIndex == hoveredIndex)
        {
            return;
        }

        mHoveredIndex = hoveredIndex;
        for (var i = 0; i < Icons.Count; i++)
        {
            var icon = Icons[i];
            if (icon == null)
            {
                continue;
            }

            var shouldHover = i == hoveredIndex;
            if (icon.Hovered == shouldHover)
            {
                continue;
            }

            icon.Hovered = shouldHover;
            AnimateIcon(icon, shouldHover);
        }
    }

    private void AnimateIcon(IconBinding icon, bool hovering)
    {
        if (icon.Root != null)
        {
            DOTween.Kill(icon.Root);
        }

        if (icon.Back != null)
        {
            DOTween.Kill(icon.Back);
        }

        if (icon.Front != null)
        {
            DOTween.Kill(icon.Front);
        }

        if (icon.LabelRoot != null)
        {
            DOTween.Kill(icon.LabelRoot);
        }

        if (icon.LabelGroup != null)
        {
            DOTween.Kill(icon.LabelGroup);
        }

        if (hovering)
        {
            var duration = Mathf.Max(0.05f, HoverDuration);

            if (icon.Root != null)
            {
                TrackTween(icon.Root.DOScale(Vector3.one * HoverScale, duration).SetEase(Ease.OutCubic));
            }

            if (icon.Back != null)
            {
                TrackTween(icon.Back.DOAnchorPos(BackHoverOffset, duration).SetEase(Ease.OutCubic));
                TrackTween(icon.Back.DOLocalRotate(new Vector3(0f, 0f, BackHoverRotation), duration).SetEase(Ease.OutCubic));
            }

            if (icon.Front != null)
            {
                TrackTween(icon.Front.DOAnchorPos(FrontHoverOffset, duration).SetEase(Ease.OutCubic));
                TrackTween(icon.Front.DOScale(Vector3.one * FrontHoverScale, duration).SetEase(Ease.OutCubic));
            }

            if (icon.LabelRoot != null)
            {
                TrackTween(icon.LabelRoot.DOAnchorPosY(LabelHoverY, duration).SetEase(Ease.OutCubic));
            }

            if (icon.LabelGroup != null)
            {
                TrackTween(icon.LabelGroup.DOFade(1f, Mathf.Max(0.05f, duration - 0.04f)).SetEase(Ease.OutCubic));
            }
        }
        else
        {
            var duration = Mathf.Max(0.05f, IdleDuration);

            if (icon.Root != null)
            {
                TrackTween(icon.Root.DOScale(Vector3.one, duration).SetEase(Ease.OutCubic));
            }

            if (icon.Back != null)
            {
                TrackTween(icon.Back.DOAnchorPos(Vector2.zero, duration).SetEase(Ease.OutCubic));
                TrackTween(icon.Back.DOLocalRotate(new Vector3(0f, 0f, BackIdleRotation), duration).SetEase(Ease.OutCubic));
            }

            if (icon.Front != null)
            {
                TrackTween(icon.Front.DOAnchorPos(Vector2.zero, duration).SetEase(Ease.OutCubic));
                TrackTween(icon.Front.DOScale(Vector3.one, duration).SetEase(Ease.OutCubic));
            }

            if (icon.LabelRoot != null)
            {
                TrackTween(icon.LabelRoot.DOAnchorPosY(LabelIdleY, duration).SetEase(Ease.OutCubic));
            }

            if (icon.LabelGroup != null)
            {
                TrackTween(icon.LabelGroup.DOFade(0f, Mathf.Max(0.05f, duration - 0.04f)).SetEase(Ease.OutCubic));
            }
        }
    }

    private Camera ResolveInteractionCamera(PointerEventData eventData = null)
    {
        var target = InteractionHitSource != null
            ? InteractionHitSource
            : (StageRoot != null ? StageRoot : (EffectRoot != null ? EffectRoot : transform as RectTransform));

        if (eventData != null)
        {
            return ResolveReliableEventCamera(eventData, target);
        }

        return ResolveReliableEventCamera(target);
    }

    private void OnDrawGizmos()
    {
        var hitSource = InteractionHitSource != null
            ? InteractionHitSource
            : (StageRoot != null ? StageRoot : (EffectRoot != null ? EffectRoot : transform as RectTransform));
        DrawInteractionRangeGizmo(ShowInteractionRange, hitSource, InteractionRangeDependency, InteractionRangePadding);
    }
}
