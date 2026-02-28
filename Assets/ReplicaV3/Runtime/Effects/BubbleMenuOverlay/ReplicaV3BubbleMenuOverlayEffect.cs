using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class ReplicaV3BubbleMenuOverlayEffect : ReplicaV3EffectBase
{
    [Serializable]
    public sealed class PillBinding
    {
        public RectTransform Rect;
        public Image Image;
        public Text Label;
        public RectTransform LabelRect;
        public Color BaseBg = Color.white;
        public Color HoverBg = Color.white;
        public Color BaseText = Color.black;
        public Color HoverText = Color.white;
    }

    [Header("组件绑定（BubbleMenuOverlay）")]
    [Tooltip("主要可交互判定组件。通常绑定到 Overlay。")]
    public RectTransform InteractionHitSource;

    [Tooltip("交互范围跟随依赖组件。为空时回退到 InteractionHitSource。")]
    public RectTransform InteractionRangeDependency;

    [Tooltip("编辑器中显示可交互范围。默认开启，可手动关闭。")]
    public bool ShowInteractionRange = true;

    [Tooltip("交互范围扩展像素。")]
    public float InteractionRangePadding = 0f;

    [Tooltip("背景节点。")]
    public RectTransform Backdrop;

    [Tooltip("顶部条节点。")]
    public RectTransform TopBar;

    [Tooltip("遮罩层节点。")]
    public RectTransform OverlayRect;

    [Tooltip("遮罩层透明度控制。")]
    public CanvasGroup OverlayGroup;

    [Tooltip("菜单开关按钮。")]
    public Button ToggleButton;

    [Tooltip("图标上横线。")]
    public RectTransform LineTop;

    [Tooltip("图标下横线。")]
    public RectTransform LineBottom;

    [Tooltip("菜单项容器。")]
    public RectTransform PillLayer;

    [Tooltip("提示文案。")]
    public Text HintText;

    [Tooltip("预制绑定的菜单项（可为空，运行时会从 PillLayer 自动收集）。")]
    public List<PillBinding> Pills = new List<PillBinding>();

    [Header("文案")]
    [Tooltip("提示文案。")]
    public string Hint = "BubbleMenu  |  Click top-right bubble to toggle";

    [Header("参数（可在参数面板实时调）")]
    [Tooltip("展开时长。")]
    public float OpenDuration = 0.5f;

    [Tooltip("展开错峰间隔。")]
    public float StaggerDelay = 0.12f;

    [Tooltip("遮罩淡入时长。")]
    public float OverlayFadeInDuration = 0.28f;

    [Tooltip("遮罩淡出时长。")]
    public float OverlayFadeOutDuration = 0.18f;

    [Tooltip("图标形态切换时长。")]
    public float IconMorphDuration = 0.22f;

    [Tooltip("悬停缩放。")]
    public float HoverScale = 1.06f;

    [Tooltip("悬停过渡时长。")]
    public float HoverDuration = 0.18f;

    [Tooltip("按下缩放。")]
    public float DownScale = 0.94f;

    [Tooltip("按下过渡时长。")]
    public float DownDuration = 0.12f;

    [Tooltip("抬起过渡时长。")]
    public float UpDuration = 0.14f;

    [Tooltip("文本关闭状态偏移。")]
    public Vector2 PillLabelClosedOffset = new Vector2(0f, 24f);

    [Tooltip("禁用悬停反馈。")]
    public bool DisableHover = false;

    [Tooltip("打开后自动关闭秒数。<=0 为禁用。")]
    public float AutoCloseAfter = 0f;

    private readonly List<ReplicaV3ParameterDefinition> mParameters = new List<ReplicaV3ParameterDefinition>
    {
        new ReplicaV3ParameterDefinition
        {
            Id = "open_duration",
            DisplayName = "展开时长",
            Description = "菜单项展开/收起的核心时长。",
            Kind = ReplicaV3ParameterKind.Float,
            Min = 0.1f,
            Max = 1.4f,
            Step = 0.01f
        },
        new ReplicaV3ParameterDefinition
        {
            Id = "stagger_delay",
            DisplayName = "错峰延迟",
            Description = "菜单项依次展开的间隔。",
            Kind = ReplicaV3ParameterKind.Float,
            Min = 0f,
            Max = 0.4f,
            Step = 0.01f
        },
        new ReplicaV3ParameterDefinition
        {
            Id = "hover_scale",
            DisplayName = "悬停缩放",
            Description = "悬停时菜单项放大倍率。",
            Kind = ReplicaV3ParameterKind.Float,
            Min = 1f,
            Max = 1.2f,
            Step = 0.01f
        },
        new ReplicaV3ParameterDefinition
        {
            Id = "auto_close_after",
            DisplayName = "自动关闭",
            Description = "展开后自动关闭秒数，0 为禁用。",
            Kind = ReplicaV3ParameterKind.Float,
            Min = 0f,
            Max = 10f,
            Step = 0.1f
        },
        new ReplicaV3ParameterDefinition
        {
            Id = "disable_hover",
            DisplayName = "禁用悬停",
            Description = "开启后不响应悬停与按压反馈。",
            Kind = ReplicaV3ParameterKind.Bool
        }
    };

    private readonly List<EventTrigger> mRuntimeTriggers = new List<EventTrigger>();
    private bool mMenuOpen;
    private Tween mAutoCloseTween;
    private RectTransform mLogoBubble;

    protected override void OnEffectInitialize()
    {
        AutoBind();
        ResolveTopBarOverlap();
        EnsurePillsBound();
        BindToggle();
        ApplyLabel();
        SetMenuOpen(false, true);
        SetCanvasAlpha(1f);
        SetLifecycleLooping();
    }

    protected override void OnPlayIn()
    {
        KillTrackedTweens(false);
        KillAutoClose();
        ResolveTopBarOverlap();

        if (EffectCanvasGroup != null)
        {
            EffectCanvasGroup.alpha = 0f;
            TrackTween(EffectCanvasGroup.DOFade(1f, 0.25f).SetEase(Ease.OutCubic));
        }

        SetMenuOpen(false, true);
        SetLifecycleLooping();
    }

    protected override void OnPlayOut(Action onComplete)
    {
        KillTrackedTweens(false);
        KillAutoClose();

        SetMenuOpen(false, false);

        if (EffectCanvasGroup != null)
        {
            TrackTween(EffectCanvasGroup
                .DOFade(0f, 0.2f)
                .SetEase(Ease.InCubic)
                .OnComplete(() => onComplete?.Invoke()));
            return;
        }

        onComplete?.Invoke();
    }

    protected override void OnEffectReset()
    {
        KillTrackedTweens(false);
        KillAutoClose();
        ResolveTopBarOverlap();
        ApplyLabel();
        SetMenuOpen(false, true);
        SetCanvasAlpha(1f);
        SetLifecycleLooping();
    }

    protected override void OnEffectDispose()
    {
        UnbindToggle();
        UnbindPillTriggers();
    }

    public override IReadOnlyList<ReplicaV3ParameterDefinition> GetParameterDefinitions()
    {
        return mParameters;
    }

    public override bool TryGetFloatParameter(string parameterId, out float value)
    {
        switch (parameterId)
        {
            case "open_duration":
                value = OpenDuration;
                return true;
            case "stagger_delay":
                value = StaggerDelay;
                return true;
            case "hover_scale":
                value = HoverScale;
                return true;
            case "auto_close_after":
                value = AutoCloseAfter;
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
            case "open_duration":
                OpenDuration = Mathf.Clamp(value, 0.1f, 1.4f);
                return true;
            case "stagger_delay":
                StaggerDelay = Mathf.Clamp(value, 0f, 0.4f);
                return true;
            case "hover_scale":
                HoverScale = Mathf.Clamp(value, 1f, 1.2f);
                return true;
            case "auto_close_after":
                AutoCloseAfter = Mathf.Clamp(value, 0f, 10f);
                return true;
            default:
                return false;
        }
    }

    public override bool TryGetBoolParameter(string parameterId, out bool value)
    {
        switch (parameterId)
        {
            case "disable_hover":
                value = DisableHover;
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
            case "disable_hover":
                DisableHover = value;
                if (DisableHover)
                {
                    ResetAllPills(false);
                }

                return true;
            default:
                return false;
        }
    }

    private void AutoBind()
    {
        if (Backdrop == null)
        {
            Backdrop = transform.Find("Backdrop") as RectTransform;
        }

        if (TopBar == null)
        {
            TopBar = transform.Find("Backdrop/TopBar") as RectTransform;
        }

        if (OverlayRect == null)
        {
            OverlayRect = transform.Find("Backdrop/Overlay") as RectTransform;
        }

        if (OverlayGroup == null && OverlayRect != null)
        {
            OverlayGroup = OverlayRect.GetComponent<CanvasGroup>();
        }

        if (ToggleButton == null)
        {
            var toggle = transform.Find("Backdrop/TopBar/ToggleBubble");
            if (toggle != null)
            {
                ToggleButton = toggle.GetComponent<Button>();
            }
        }

        if (mLogoBubble == null)
        {
            mLogoBubble = transform.Find("Backdrop/TopBar/LogoBubble") as RectTransform;
        }

        if (LineTop == null)
        {
            LineTop = transform.Find("Backdrop/TopBar/ToggleBubble/LineTop") as RectTransform;
        }

        if (LineBottom == null)
        {
            LineBottom = transform.Find("Backdrop/TopBar/ToggleBubble/LineBottom") as RectTransform;
        }

        if (PillLayer == null)
        {
            PillLayer = transform.Find("Backdrop/Overlay/PillLayer") as RectTransform;
        }

        if (HintText == null)
        {
            var hint = transform.Find("Backdrop/Hint");
            if (hint != null)
            {
                HintText = hint.GetComponent<Text>();
            }
        }

        if (InteractionHitSource == null)
        {
            InteractionHitSource = OverlayRect != null ? OverlayRect : (EffectRoot != null ? EffectRoot : transform as RectTransform);
        }
    }

    private void ResolveTopBarOverlap()
    {
        if (TopBar == null)
        {
            return;
        }

        if (mLogoBubble == null)
        {
            mLogoBubble = TopBar.Find("LogoBubble") as RectTransform;
        }

        var toggleRect = ToggleButton != null ? ToggleButton.transform as RectTransform : null;
        if (toggleRect == null || mLogoBubble == null)
        {
            return;
        }

        TopBar.SetAsLastSibling();
        toggleRect.SetAsLastSibling();

        var topBarWidth = TopBar.rect.width;
        if (topBarWidth <= 1f)
        {
            return;
        }

        var leftWidth = mLogoBubble.sizeDelta.x;
        var rightWidth = toggleRect.sizeDelta.x;
        var spacing = 24f;
        var required = leftWidth + rightWidth + spacing + 80f;
        if (required <= topBarWidth)
        {
            return;
        }

        var scale = Mathf.Clamp01(topBarWidth / required);
        var leftTarget = Mathf.Max(88f, leftWidth * scale);
        var rightTarget = Mathf.Max(56f, rightWidth * scale);
        mLogoBubble.sizeDelta = new Vector2(leftTarget, mLogoBubble.sizeDelta.y);
        toggleRect.sizeDelta = new Vector2(rightTarget, toggleRect.sizeDelta.y);

        var edge = Mathf.Max(24f, 40f * scale);
        mLogoBubble.anchoredPosition = new Vector2(edge, mLogoBubble.anchoredPosition.y);
        toggleRect.anchoredPosition = new Vector2(-edge, toggleRect.anchoredPosition.y);
    }

    private void EnsurePillsBound()
    {
        if (Pills == null)
        {
            Pills = new List<PillBinding>();
        }

        Pills.RemoveAll(p => p == null || p.Rect == null);
        if (Pills.Count <= 0)
        {
            CollectPillsFromLayer();
        }

        if (Pills.Count <= 0)
        {
            CreateFallbackPills();
        }

        for (var i = 0; i < Pills.Count; i++)
        {
            var pill = Pills[i];
            if (pill == null || pill.Rect == null)
            {
                continue;
            }

            if (pill.Image == null)
            {
                pill.Image = pill.Rect.GetComponent<Image>();
            }

            if (pill.Label == null)
            {
                pill.Label = pill.Rect.GetComponentInChildren<Text>(true);
            }

            if (pill.LabelRect == null && pill.Label != null)
            {
                pill.LabelRect = pill.Label.transform as RectTransform;
            }

            if (pill.Image != null)
            {
                pill.BaseBg = pill.Image.color;
            }

            if (pill.Label != null)
            {
                pill.BaseText = pill.Label.color;
            }
        }

        BindPillTriggers();
    }

    private void CollectPillsFromLayer()
    {
        if (PillLayer == null)
        {
            return;
        }

        for (var i = 0; i < PillLayer.childCount; i++)
        {
            var child = PillLayer.GetChild(i) as RectTransform;
            if (child == null)
            {
                continue;
            }

            var button = child.GetComponent<Button>();
            if (button == null)
            {
                continue;
            }

            var label = child.GetComponentInChildren<Text>(true);
            var entry = new PillBinding
            {
                Rect = child,
                Image = button.image,
                Label = label,
                LabelRect = label != null ? label.transform as RectTransform : null,
                HoverBg = new Color(0.23f, 0.51f, 0.96f, 1f),
                HoverText = Color.white
            };
            Pills.Add(entry);
        }
    }

    private void CreateFallbackPills()
    {
        if (PillLayer == null)
        {
            return;
        }

        // V2 该动效强依赖运行时 ViewBuilder。这里保留最小动态创建兜底，后续可替换为纯预制体静态层级。
        var labels = new[] { "home", "about", "projects", "blog", "contact" };
        var hoverBg = new[]
        {
            new Color(0.23f, 0.51f, 0.96f, 1f),
            new Color(0.06f, 0.68f, 0.51f, 1f),
            new Color(0.96f, 0.63f, 0.11f, 1f),
            new Color(0.93f, 0.27f, 0.26f, 1f),
            new Color(0.54f, 0.36f, 0.95f, 1f)
        };
        var positions = new[]
        {
            new Vector2(-340f, 140f),
            new Vector2(0f, 140f),
            new Vector2(340f, 140f),
            new Vector2(-190f, -80f),
            new Vector2(190f, -80f)
        };
        var rotations = new[] { -8f, 8f, 8f, 8f, -8f };
        var fallbackFont = ResolveBuiltinFont();

        for (var i = 0; i < labels.Length; i++)
        {
            var go = new GameObject($"Pill_{i}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(PillLayer, false);
            var rect = go.transform as RectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(320f, 132f);
            rect.anchoredPosition = positions[i % positions.Length];
            rect.localRotation = Quaternion.Euler(0f, 0f, rotations[i % rotations.Length]);

            var image = go.GetComponent<Image>();
            image.color = Color.white;
            var button = go.GetComponent<Button>();
            button.transition = Selectable.Transition.None;

            var labelGo = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            labelGo.transform.SetParent(go.transform, false);
            var labelRect = labelGo.transform as RectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            labelRect.anchoredPosition = PillLabelClosedOffset;

            var text = labelGo.GetComponent<Text>();
            text.text = labels[i];
            text.fontSize = 48;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(0.07f, 0.08f, 0.11f, 1f);
            if (fallbackFont != null)
            {
                text.font = fallbackFont;
            }

            Pills.Add(new PillBinding
            {
                Rect = rect,
                Image = image,
                Label = text,
                LabelRect = labelRect,
                BaseBg = image.color,
                HoverBg = hoverBg[i % hoverBg.Length],
                BaseText = text.color,
                HoverText = Color.white
            });
        }
    }

    private void BindToggle()
    {
        if (ToggleButton != null)
        {
            ToggleButton.onClick.RemoveListener(OnToggleClicked);
            ToggleButton.onClick.AddListener(OnToggleClicked);
        }
    }

    private void UnbindToggle()
    {
        if (ToggleButton != null)
        {
            ToggleButton.onClick.RemoveListener(OnToggleClicked);
        }
    }

    private void BindPillTriggers()
    {
        UnbindPillTriggers();

        for (var i = 0; i < Pills.Count; i++)
        {
            var pill = Pills[i];
            if (pill == null || pill.Rect == null)
            {
                continue;
            }

            var index = i;
            var trigger = pill.Rect.GetComponent<EventTrigger>();
            if (trigger == null)
            {
                trigger = pill.Rect.gameObject.AddComponent<EventTrigger>();
            }

            trigger.triggers = new List<EventTrigger.Entry>();
            AddTrigger(trigger, EventTriggerType.PointerEnter, _ => OnPillPointerEnter(index));
            AddTrigger(trigger, EventTriggerType.PointerExit, _ => OnPillPointerExit(index));
            AddTrigger(trigger, EventTriggerType.PointerDown, _ => OnPillPointerDown(index));
            AddTrigger(trigger, EventTriggerType.PointerUp, _ => OnPillPointerUp(index));

            mRuntimeTriggers.Add(trigger);
        }
    }

    private void UnbindPillTriggers()
    {
        for (var i = 0; i < mRuntimeTriggers.Count; i++)
        {
            if (mRuntimeTriggers[i] != null)
            {
                mRuntimeTriggers[i].triggers?.Clear();
            }
        }

        mRuntimeTriggers.Clear();
    }

    private static void AddTrigger(EventTrigger trigger, EventTriggerType type, Action<BaseEventData> handler)
    {
        if (trigger == null || handler == null)
        {
            return;
        }

        var entry = new EventTrigger.Entry { eventID = type };
        entry.callback.AddListener(data => handler(data));
        trigger.triggers.Add(entry);
    }

    private void OnToggleClicked()
    {
        SetMenuOpen(!mMenuOpen, false);
    }

    private void SetMenuOpen(bool open, bool immediate)
    {
        if (OverlayGroup == null)
        {
            return;
        }

        mMenuOpen = open;
        OverlayGroup.interactable = open;
        OverlayGroup.blocksRaycasts = open;

        AnimateMenuIcon(open, immediate);

        if (immediate)
        {
            OverlayGroup.alpha = open ? 1f : 0f;
            for (var i = 0; i < Pills.Count; i++)
            {
                var pill = Pills[i];
                if (pill == null || pill.Rect == null)
                {
                    continue;
                }

                pill.Rect.localScale = open ? Vector3.one : Vector3.zero;
                if (pill.LabelRect != null)
                {
                    pill.LabelRect.anchoredPosition = open ? Vector2.zero : PillLabelClosedOffset;
                }

                if (pill.Label != null)
                {
                    var c = pill.Label.color;
                    c.a = open ? 1f : 0f;
                    pill.Label.color = c;
                }

                if (open)
                {
                    ResetPillVisual(pill, true);
                }
            }
        }
        else
        {
            TrackTween(OverlayGroup
                .DOFade(open ? 1f : 0f, open ? Mathf.Max(0.05f, OverlayFadeInDuration) : Mathf.Max(0.05f, OverlayFadeOutDuration))
                .SetEase(open ? Ease.OutQuad : Ease.InQuad));

            for (var i = 0; i < Pills.Count; i++)
            {
                var pill = Pills[i];
                if (pill == null || pill.Rect == null)
                {
                    continue;
                }

                if (open)
                {
                    ResetPillVisual(pill, true);
                    var delay = (i * Mathf.Max(0f, StaggerDelay)) + (((i % 2) - 0.5f) * 0.04f);
                    pill.Rect.localScale = Vector3.zero;
                    TrackTween(pill.Rect
                        .DOScale(Vector3.one, Mathf.Max(0.1f, OpenDuration))
                        .SetDelay(delay)
                        .SetEase(Ease.OutBack, 1.2f));

                    if (pill.LabelRect != null)
                    {
                        pill.LabelRect.anchoredPosition = PillLabelClosedOffset;
                        TrackTween(pill.LabelRect
                            .DOAnchorPosY(0f, Mathf.Max(0.1f, OpenDuration) * 0.9f)
                            .SetDelay(delay + 0.02f)
                            .SetEase(Ease.OutCubic));
                    }

                    if (pill.Label != null)
                    {
                        var c = pill.Label.color;
                        c.a = 0f;
                        pill.Label.color = c;
                        TrackTween(pill.Label
                            .DOFade(1f, Mathf.Max(0.1f, OpenDuration) * 0.8f)
                            .SetDelay(delay + 0.02f)
                            .SetEase(Ease.OutCubic));
                    }
                }
                else
                {
                    var delay = (Pills.Count - i - 1) * 0.025f;
                    if (pill.Label != null)
                    {
                        TrackTween(pill.Label.DOFade(0f, 0.18f).SetDelay(delay).SetEase(Ease.InQuad));
                    }

                    TrackTween(pill.Rect
                        .DOScale(Vector3.zero, 0.2f)
                        .SetDelay(delay)
                        .SetEase(Ease.InBack, 1.2f));
                }
            }
        }

        if (open)
        {
            RestartAutoClose();
        }
        else
        {
            KillAutoClose();
        }
    }

    private void AnimateMenuIcon(bool open, bool immediate)
    {
        if (LineTop == null || LineBottom == null)
        {
            return;
        }

        var topY = open ? 0f : 5f;
        var bottomY = open ? 0f : -5f;
        var topRot = open ? 45f : 0f;
        var bottomRot = open ? -45f : 0f;

        if (immediate)
        {
            LineTop.anchoredPosition = new Vector2(0f, topY);
            LineTop.localRotation = Quaternion.Euler(0f, 0f, topRot);
            LineBottom.anchoredPosition = new Vector2(0f, bottomY);
            LineBottom.localRotation = Quaternion.Euler(0f, 0f, bottomRot);
            return;
        }

        var duration = Mathf.Max(0.05f, IconMorphDuration);
        TrackTween(LineTop.DOAnchorPosY(topY, duration).SetEase(Ease.OutCubic));
        TrackTween(LineBottom.DOAnchorPosY(bottomY, duration).SetEase(Ease.OutCubic));
        TrackTween(LineTop.DOLocalRotate(new Vector3(0f, 0f, topRot), duration).SetEase(Ease.OutCubic));
        TrackTween(LineBottom.DOLocalRotate(new Vector3(0f, 0f, bottomRot), duration).SetEase(Ease.OutCubic));
    }

    private void OnPillPointerEnter(int index)
    {
        if (!mMenuOpen || DisableHover || !TryGetPill(index, out var pill))
        {
            return;
        }

        TrackTween(pill.Rect.DOScale(Vector3.one * Mathf.Max(1f, HoverScale), Mathf.Max(0.05f, HoverDuration)).SetEase(Ease.OutCubic));
        if (pill.Image != null)
        {
            TrackTween(pill.Image.DOColor(pill.HoverBg, Mathf.Max(0.05f, HoverDuration)).SetEase(Ease.OutCubic));
        }

        if (pill.Label != null)
        {
            TrackTween(pill.Label.DOColor(pill.HoverText, Mathf.Max(0.05f, HoverDuration)).SetEase(Ease.OutCubic));
        }
    }

    private void OnPillPointerExit(int index)
    {
        if (!mMenuOpen || !TryGetPill(index, out var pill))
        {
            return;
        }

        ResetPillVisual(pill, false);
    }

    private void OnPillPointerDown(int index)
    {
        if (!mMenuOpen || DisableHover || !TryGetPill(index, out var pill))
        {
            return;
        }

        TrackTween(pill.Rect.DOScale(Vector3.one * Mathf.Max(0.6f, DownScale), Mathf.Max(0.05f, DownDuration)).SetEase(Ease.OutQuad));
    }

    private void OnPillPointerUp(int index)
    {
        if (!mMenuOpen || DisableHover || !TryGetPill(index, out var pill))
        {
            return;
        }

        TrackTween(pill.Rect.DOScale(Vector3.one * Mathf.Max(1f, HoverScale), Mathf.Max(0.05f, UpDuration)).SetEase(Ease.OutQuad));
    }

    private bool TryGetPill(int index, out PillBinding pill)
    {
        pill = null;
        if (index < 0 || index >= Pills.Count)
        {
            return false;
        }

        pill = Pills[index];
        return pill != null && pill.Rect != null;
    }

    private void ResetPillVisual(PillBinding pill, bool immediate)
    {
        if (pill == null || pill.Rect == null)
        {
            return;
        }

        if (immediate)
        {
            pill.Rect.localScale = Vector3.one;
            if (pill.Image != null)
            {
                pill.Image.color = pill.BaseBg;
            }

            if (pill.Label != null)
            {
                pill.Label.color = pill.BaseText;
            }

            return;
        }

        TrackTween(pill.Rect.DOScale(Vector3.one, 0.16f).SetEase(Ease.OutCubic));
        if (pill.Image != null)
        {
            TrackTween(pill.Image.DOColor(pill.BaseBg, 0.16f).SetEase(Ease.OutCubic));
        }

        if (pill.Label != null)
        {
            TrackTween(pill.Label.DOColor(pill.BaseText, 0.16f).SetEase(Ease.OutCubic));
        }
    }

    private void ResetAllPills(bool immediate)
    {
        for (var i = 0; i < Pills.Count; i++)
        {
            ResetPillVisual(Pills[i], immediate);
        }
    }

    private void RestartAutoClose()
    {
        KillAutoClose();
        if (AutoCloseAfter <= 0f)
        {
            return;
        }

        mAutoCloseTween = TrackTween(DOVirtual.DelayedCall(Mathf.Max(0.1f, AutoCloseAfter), () =>
        {
            if (mMenuOpen)
            {
                SetMenuOpen(false, false);
            }
        }));
    }

    private void KillAutoClose()
    {
        if (mAutoCloseTween != null && mAutoCloseTween.active)
        {
            mAutoCloseTween.Kill(false);
        }

        mAutoCloseTween = null;
    }

    private void ApplyLabel()
    {
        if (HintText != null)
        {
            HintText.text = string.IsNullOrWhiteSpace(Hint)
                ? "BubbleMenu  |  Click top-right bubble to toggle"
                : Hint;
        }
    }


    private void OnDrawGizmos()
    {
        var hitSource = InteractionHitSource != null
            ? InteractionHitSource
            : (OverlayRect != null ? OverlayRect : (EffectRoot != null ? EffectRoot : transform as RectTransform));
        DrawInteractionRangeGizmo(ShowInteractionRange, hitSource, InteractionRangeDependency, InteractionRangePadding);
    }
}
