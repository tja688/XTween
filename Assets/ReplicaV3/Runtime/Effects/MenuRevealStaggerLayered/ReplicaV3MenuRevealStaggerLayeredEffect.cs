using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public sealed class ReplicaV3MenuRevealStaggerLayeredEffect : ReplicaV3EffectBase
{
    [Header("组件绑定（MenuRevealStaggerLayered）")]
    [Tooltip("菜单面板主体。")]
    public RectTransform Panel;

    [Tooltip("分层前置面板列表。")]
    public List<RectTransform> PreLayers = new List<RectTransform>();

    [Tooltip("菜单项标签（会做 Y 位移与旋转）。")]
    public List<RectTransform> ItemLabels = new List<RectTransform>();

    [Tooltip("菜单项数字文本（会做淡入淡出）。")]
    public List<Text> ItemNumbers = new List<Text>();

    [Tooltip("社交链接文本根节点（RectTransform）。")]
    public List<RectTransform> SocialLinks = new List<RectTransform>();

    [Tooltip("社交标题文本。")]
    public Text SocialTitle;

    [Tooltip("Toggle 图标节点。")]
    public RectTransform ToggleIcon;

    [Tooltip("Toggle 双行文案容器。")]
    public RectTransform ToggleTextStack;

    [Tooltip("Toggle 顶部文案（Menu）。")]
    public Text ToggleTextTop;

    [Tooltip("遮罩点击层（控制穿透）。")]
    public CanvasGroup DismissGroup;

    [Header("参数（可在参数面板实时调）")]
    [Tooltip("当前是否打开菜单。")]
    public bool IsOpen = false;

    [Tooltip("层级错峰间隔。")]
    public float LayerStagger = 0.04f;

    [Tooltip("层级滑入时长。")]
    public float LayerSlideDuration = 0.35f;

    [Tooltip("面板滑入时长。")]
    public float PanelSlideDuration = 0.42f;

    [Tooltip("菜单项交错间隔。")]
    public float ItemStagger = 0.06f;

    [Tooltip("菜单项滑入时长。")]
    public float ItemSlideDuration = 0.55f;

    [Tooltip("社交标题淡入时长。")]
    public float SocialFadeDuration = 0.3f;

    [Tooltip("社交链接过渡时长。")]
    public float SocialLinkDuration = 0.35f;

    [Tooltip("图标打开旋转时长。")]
    public float IconRotateDurationOpen = 0.45f;

    [Tooltip("图标关闭旋转时长。")]
    public float IconRotateDurationClose = 0.25f;

    [Tooltip("Toggle 文案打开位移时长。")]
    public float ToggleTextMoveDuration = 0.3f;

    [Tooltip("Toggle 文案关闭位移时长。")]
    public float ToggleTextMoveCloseDuration = 0.25f;

    [Tooltip("关闭总滑出时长。")]
    public float CloseSlideDuration = 0.22f;

    [Tooltip("菜单项隐藏 Y。")]
    public float ItemHiddenY = -98f;

    [Tooltip("菜单项隐藏旋转。")]
    public float ItemHiddenRotZ = -10f;

    [Tooltip("社交链接隐藏额外 Y 偏移。")]
    public float SocialHiddenYOffset = 25f;

    [Tooltip("面板宽度（用于关闭时离场距离）。")]
    public float PanelWidth = 470f;

    [Tooltip("强调色。")]
    public Color AccentColor = new Color(0.32f, 0.15f, 1f, 1f);

    [Tooltip("Toggle 关闭态文字颜色。")]
    public Color ToggleClosedColor = new Color(0.93f, 0.95f, 1f, 1f);

    [Tooltip("Toggle 打开态文字颜色。")]
    public Color ToggleOpenColor = new Color(0.12f, 0.14f, 0.22f, 1f);

    [Tooltip("社交链接颜色。")]
    public Color SocialLinkColor = new Color(0.12f, 0.13f, 0.20f, 1f);

    private readonly List<ReplicaV3ParameterDefinition> mParameters = new List<ReplicaV3ParameterDefinition>
    {
        new ReplicaV3ParameterDefinition
        {
            Id = "is_open",
            DisplayName = "菜单打开",
            Description = "切换菜单展开/收起状态。",
            Kind = ReplicaV3ParameterKind.Bool
        },
        new ReplicaV3ParameterDefinition
        {
            Id = "layer_stagger",
            DisplayName = "层级错峰",
            Description = "分层滑入的起始时间间隔。",
            Kind = ReplicaV3ParameterKind.Float,
            Min = 0f,
            Max = 0.2f,
            Step = 0.005f
        },
        new ReplicaV3ParameterDefinition
        {
            Id = "item_stagger",
            DisplayName = "项错峰",
            Description = "菜单项逐个出现的间隔。",
            Kind = ReplicaV3ParameterKind.Float,
            Min = 0f,
            Max = 0.2f,
            Step = 0.005f
        }
    };

    protected override void OnEffectInitialize()
    {
        EnsureBindings();
        ApplyPose(instant: true, IsOpen);
        SetCanvasAlpha(1f);
        SetLifecycleLooping();
    }

    protected override void OnPlayIn()
    {
        EnsureBindings();
        if (IsOpen)
        {
            AnimateOpen();
        }
        else
        {
            ApplyPose(instant: true, open: false);
        }

        SetLifecycleLooping();
    }

    protected override void OnPlayOut(Action onComplete)
    {
        EnsureBindings();
        if (!IsOpen)
        {
            onComplete?.Invoke();
            return;
        }

        AnimateClose(onComplete);
    }

    protected override void OnEffectReset()
    {
        KillTrackedTweens(false);
        EnsureBindings();
        ApplyPose(instant: true, IsOpen);
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
            case "layer_stagger":
                value = LayerStagger;
                return true;
            case "item_stagger":
                value = ItemStagger;
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
            case "layer_stagger":
                LayerStagger = Mathf.Clamp(value, 0f, 0.2f);
                return true;
            case "item_stagger":
                ItemStagger = Mathf.Clamp(value, 0f, 0.2f);
                return true;
            default:
                return false;
        }
    }

    public override bool TryGetBoolParameter(string parameterId, out bool value)
    {
        switch (parameterId)
        {
            case "is_open":
                value = IsOpen;
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
            case "is_open":
                if (IsOpen == value)
                {
                    return true;
                }

                IsOpen = value;
                if (IsOpen)
                {
                    AnimateOpen();
                }
                else
                {
                    AnimateClose();
                }

                return true;
            default:
                return false;
        }
    }

    public void ToggleMenu()
    {
        TrySetBoolParameter("is_open", !IsOpen);
    }

    public void CloseMenuIfOpen()
    {
        if (IsOpen)
        {
            TrySetBoolParameter("is_open", false);
        }
    }

    private void EnsureBindings()
    {
        if (Panel == null && EffectRoot != null)
        {
            Panel = EffectRoot.Find("Panel") as RectTransform;
        }

        if ((PreLayers == null || PreLayers.Count == 0) && EffectRoot != null)
        {
            PreLayers = new List<RectTransform>();
            var preLayerRoot = EffectRoot.Find("PreLayers");
            if (preLayerRoot != null)
            {
                for (var i = 0; i < preLayerRoot.childCount; i++)
                {
                    var layer = preLayerRoot.GetChild(i) as RectTransform;
                    if (layer != null)
                    {
                        PreLayers.Add(layer);
                    }
                }
            }
        }

        if ((ItemLabels == null || ItemLabels.Count == 0) && Panel != null)
        {
            ItemLabels = new List<RectTransform>();
            var menuList = Panel.Find("PanelInner/MenuList");
            if (menuList != null)
            {
                for (var i = 0; i < menuList.childCount; i++)
                {
                    var item = menuList.GetChild(i);
                    var label = item.Find("Label") as RectTransform;
                    if (label != null)
                    {
                        ItemLabels.Add(label);
                    }
                }
            }
        }

        if ((ItemNumbers == null || ItemNumbers.Count == 0) && Panel != null)
        {
            ItemNumbers = new List<Text>();
            var menuList = Panel.Find("PanelInner/MenuList");
            if (menuList != null)
            {
                for (var i = 0; i < menuList.childCount; i++)
                {
                    var number = menuList.GetChild(i).Find("Number")?.GetComponent<Text>();
                    if (number != null)
                    {
                        ItemNumbers.Add(number);
                    }
                }
            }
        }

        if ((SocialLinks == null || SocialLinks.Count == 0) && Panel != null)
        {
            SocialLinks = new List<RectTransform>();
            var socialRoot = Panel.Find("PanelInner/Socials");
            if (socialRoot != null)
            {
                for (var i = 0; i < socialRoot.childCount; i++)
                {
                    var child = socialRoot.GetChild(i) as RectTransform;
                    if (child != null && child.name.StartsWith("Social_", StringComparison.Ordinal))
                    {
                        SocialLinks.Add(child);
                    }
                }
            }
        }

        if (SocialTitle == null && Panel != null)
        {
            SocialTitle = Panel.Find("PanelInner/Socials/SocialTitle")?.GetComponent<Text>();
        }

        if (ToggleIcon == null && EffectRoot != null)
        {
            ToggleIcon = EffectRoot.Find("TopBar/Toggle/Icon") as RectTransform;
        }

        if (ToggleTextStack == null && EffectRoot != null)
        {
            ToggleTextStack = EffectRoot.Find("TopBar/Toggle/ToggleLabelMask/ToggleTextStack") as RectTransform;
        }

        if (ToggleTextTop == null && ToggleTextStack != null)
        {
            ToggleTextTop = ToggleTextStack.Find("MenuLine")?.GetComponent<Text>();
        }

        if (DismissGroup == null && EffectRoot != null)
        {
            DismissGroup = EffectRoot.Find("Dismiss")?.GetComponent<CanvasGroup>();
        }

        if (string.IsNullOrWhiteSpace(EffectKey))
        {
            EffectKey = "menu-reveal-stagger-layered-v3";
        }

        if (string.IsNullOrWhiteSpace(EffectDisplayName))
        {
            EffectDisplayName = "MenuRevealStaggerLayered V3";
        }

        if (string.IsNullOrWhiteSpace(UsageDescription))
        {
            UsageDescription = "菜单面板分层交错揭示，支持参数面板控制打开状态与错峰节奏。";
        }
    }

    private void ApplyPose(bool instant, bool open)
    {
        if (instant)
        {
            KillTrackedTweens(false);
        }

        if (open)
        {
            ApplyOpenPose();
        }
        else
        {
            ApplyClosedPose();
        }
    }

    private void ApplyClosedPose()
    {
        var offscreen = GetOffscreenDistance();

        if (PreLayers != null)
        {
            for (var i = 0; i < PreLayers.Count; i++)
            {
                var layer = PreLayers[i];
                if (layer != null)
                {
                    layer.anchoredPosition = new Vector2(offscreen, 0f);
                }
            }
        }

        if (Panel != null)
        {
            Panel.anchoredPosition = new Vector2(offscreen, 0f);
        }

        if (ToggleIcon != null)
        {
            ToggleIcon.localRotation = Quaternion.identity;
        }

        if (ToggleTextStack != null)
        {
            ToggleTextStack.anchoredPosition = Vector2.zero;
        }

        if (ToggleTextTop != null)
        {
            ToggleTextTop.color = ToggleClosedColor;
        }

        if (DismissGroup != null)
        {
            DismissGroup.blocksRaycasts = false;
            DismissGroup.interactable = false;
        }

        SetItemHiddenPose();
    }

    private void ApplyOpenPose()
    {
        if (PreLayers != null)
        {
            for (var i = 0; i < PreLayers.Count; i++)
            {
                var layer = PreLayers[i];
                if (layer != null)
                {
                    layer.anchoredPosition = Vector2.zero;
                }
            }
        }

        if (Panel != null)
        {
            Panel.anchoredPosition = Vector2.zero;
        }

        if (ToggleIcon != null)
        {
            ToggleIcon.localRotation = Quaternion.Euler(0f, 0f, 225f);
        }

        if (ToggleTextStack != null)
        {
            ToggleTextStack.anchoredPosition = new Vector2(0f, 30f);
        }

        if (ToggleTextTop != null)
        {
            ToggleTextTop.color = ToggleOpenColor;
        }

        if (DismissGroup != null)
        {
            DismissGroup.blocksRaycasts = true;
            DismissGroup.interactable = true;
        }

        for (var i = 0; i < ItemLabels.Count; i++)
        {
            if (ItemLabels[i] != null)
            {
                ItemLabels[i].anchoredPosition = Vector2.zero;
                ItemLabels[i].localRotation = Quaternion.identity;
            }

            if (ItemNumbers != null && i < ItemNumbers.Count && ItemNumbers[i] != null)
            {
                var c = AccentColor;
                ItemNumbers[i].color = new Color(c.r, c.g, c.b, 1f);
            }
        }

        for (var i = 0; i < SocialLinks.Count; i++)
        {
            if (SocialLinks[i] != null)
            {
                var openY = -52f - (i * 34f);
                SocialLinks[i].anchoredPosition = new Vector2(0f, openY);

                var text = SocialLinks[i].GetComponent<Text>();
                if (text != null)
                {
                    var c = SocialLinkColor;
                    text.color = new Color(c.r, c.g, c.b, 1f);
                }
            }
        }

        if (SocialTitle != null)
        {
            SocialTitle.color = AccentColor;
        }
    }

    private void AnimateOpen()
    {
        KillTrackedTweens(false);
        EnsureBindings();

        IsOpen = true;
        if (DismissGroup != null)
        {
            DismissGroup.blocksRaycasts = true;
            DismissGroup.interactable = true;
        }

        SetItemHiddenPose();

        for (var i = 0; i < PreLayers.Count; i++)
        {
            var layer = PreLayers[i];
            if (layer == null)
            {
                continue;
            }

            TrackTween(layer
                .DOAnchorPosX(0f, LayerSlideDuration)
                .SetEase(Ease.OutQuart)
                .SetDelay(i * LayerStagger));
        }

        var panelDelay = Mathf.Max(0.05f, PreLayers.Count * LayerStagger);
        if (Panel != null)
        {
            TrackTween(Panel
                .DOAnchorPosX(0f, PanelSlideDuration)
                .SetEase(Ease.OutQuart)
                .SetDelay(panelDelay));
        }

        var itemsStart = panelDelay + 0.06f;
        for (var i = 0; i < ItemLabels.Count; i++)
        {
            var label = ItemLabels[i];
            if (label == null)
            {
                continue;
            }

            var delay = itemsStart + (i * ItemStagger);
            TrackTween(label
                .DOAnchorPosY(0f, ItemSlideDuration)
                .SetEase(Ease.OutQuart)
                .SetDelay(delay));
            TrackTween(label
                .DOLocalRotate(Vector3.zero, ItemSlideDuration)
                .SetEase(Ease.OutQuart)
                .SetDelay(delay));

            if (ItemNumbers != null && i < ItemNumbers.Count && ItemNumbers[i] != null)
            {
                TrackTween(ItemNumbers[i]
                    .DOFade(1f, 0.3f)
                    .SetEase(Ease.OutQuad)
                    .SetDelay(delay + 0.06f));
            }
        }

        var socialsStart = panelDelay + 0.15f;
        if (SocialTitle != null)
        {
            TrackTween(SocialTitle
                .DOFade(1f, SocialFadeDuration)
                .SetEase(Ease.OutQuad)
                .SetDelay(socialsStart));
        }

        for (var i = 0; i < SocialLinks.Count; i++)
        {
            var link = SocialLinks[i];
            if (link == null)
            {
                continue;
            }

            var delay = socialsStart + 0.03f + (i * 0.05f);
            var targetY = -52f - (i * 34f);
            TrackTween(link
                .DOAnchorPosY(targetY, SocialLinkDuration)
                .SetEase(Ease.OutCubic)
                .SetDelay(delay));

            var text = link.GetComponent<Text>();
            if (text != null)
            {
                TrackTween(text
                    .DOFade(1f, SocialLinkDuration)
                    .SetDelay(delay));
            }
        }

        if (ToggleIcon != null)
        {
            TrackTween(ToggleIcon
                .DOLocalRotate(new Vector3(0f, 0f, 225f), IconRotateDurationOpen)
                .SetEase(Ease.OutQuart));
        }

        if (ToggleTextStack != null)
        {
            TrackTween(ToggleTextStack
                .DOAnchorPosY(30f, ToggleTextMoveDuration)
                .SetEase(Ease.OutQuart));
        }

        if (ToggleTextTop != null)
        {
            var start = ToggleTextTop.color;
            TrackTween(DOTween
                .To(() => 0f, t => ToggleTextTop.color = Color.Lerp(start, ToggleOpenColor, t), 1f, 0.2f)
                .SetDelay(0.1f)
                .SetEase(Ease.OutQuad));
        }
    }

    private void AnimateClose(Action onComplete = null)
    {
        KillTrackedTweens(false);
        EnsureBindings();

        IsOpen = false;
        var offscreen = GetOffscreenDistance();

        if (Panel != null)
        {
            TrackTween(Panel
                .DOAnchorPosX(offscreen, CloseSlideDuration)
                .SetEase(Ease.InCubic));
        }

        for (var i = 0; i < PreLayers.Count; i++)
        {
            var layer = PreLayers[i];
            if (layer == null)
            {
                continue;
            }

            TrackTween(layer
                .DOAnchorPosX(offscreen, CloseSlideDuration)
                .SetEase(Ease.InCubic));
        }

        TrackTween(DOVirtual.DelayedCall(CloseSlideDuration, () =>
        {
            SetItemHiddenPose();
            if (DismissGroup != null)
            {
                DismissGroup.blocksRaycasts = false;
                DismissGroup.interactable = false;
            }

            if (SocialTitle != null)
            {
                var a = AccentColor;
                SocialTitle.color = new Color(a.r, a.g, a.b, 0f);
            }

            onComplete?.Invoke();
        }));

        if (ToggleIcon != null)
        {
            TrackTween(ToggleIcon
                .DOLocalRotate(Vector3.zero, IconRotateDurationClose)
                .SetEase(Ease.InOutCubic));
        }

        if (ToggleTextStack != null)
        {
            TrackTween(ToggleTextStack
                .DOAnchorPosY(0f, ToggleTextMoveCloseDuration)
                .SetEase(Ease.InOutCubic));
        }

        if (ToggleTextTop != null)
        {
            var start = ToggleTextTop.color;
            TrackTween(DOTween
                .To(() => 0f, t => ToggleTextTop.color = Color.Lerp(start, ToggleClosedColor, t), 1f, 0.2f)
                .SetEase(Ease.OutQuad));
        }
    }

    private void SetItemHiddenPose()
    {
        for (var i = 0; i < ItemLabels.Count; i++)
        {
            if (ItemLabels[i] != null)
            {
                ItemLabels[i].anchoredPosition = new Vector2(0f, ItemHiddenY);
                ItemLabels[i].localRotation = Quaternion.Euler(0f, 0f, ItemHiddenRotZ);
            }

            if (ItemNumbers != null && i < ItemNumbers.Count && ItemNumbers[i] != null)
            {
                var a = AccentColor;
                ItemNumbers[i].color = new Color(a.r, a.g, a.b, 0f);
            }
        }

        for (var i = 0; i < SocialLinks.Count; i++)
        {
            var link = SocialLinks[i];
            if (link == null)
            {
                continue;
            }

            var openY = -52f - (i * 34f);
            link.anchoredPosition = new Vector2(0f, openY - SocialHiddenYOffset);

            var text = link.GetComponent<Text>();
            if (text != null)
            {
                var c = SocialLinkColor;
                text.color = new Color(c.r, c.g, c.b, 0f);
            }
        }

        if (SocialTitle != null)
        {
            var a = AccentColor;
            SocialTitle.color = new Color(a.r, a.g, a.b, 0f);
        }
    }

    private float GetOffscreenDistance()
    {
        return Mathf.Max(480f, PanelWidth + 80f);
    }
}
