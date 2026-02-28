using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class ReplicaV3DockMagnifyToolbarEffect : ReplicaV3EffectBase, IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler, IPointerDownHandler
{
    [Header("组件绑定（DockMagnifyToolbar）")]
    [Tooltip("Dock 面板根节点。")]
    public RectTransform DockPanel;

    [Tooltip("主要可交互判定组件。为空时回退到 DockPanel。")]
    public RectTransform InteractionHitSource;

    [Tooltip("交互范围跟随依赖组件。为空时回退到 InteractionHitSource。")]
    public RectTransform InteractionRangeDependency;

    [Tooltip("编辑器中显示可交互范围。默认开启，可手动关闭。")]
    public bool ShowInteractionRange = true;

    [Tooltip("交互范围扩展像素。")]
    public float InteractionRangePadding = 0f;

    [Tooltip("图标根节点列表。")]
    public List<RectTransform> ItemRoots = new List<RectTransform>();

    [Tooltip("图标背景列表。顺序与 ItemRoots 一致。")]
    public List<Graphic> ItemBackgrounds = new List<Graphic>();

    [Tooltip("图标文本列表。顺序与 ItemRoots 一致。")]
    public List<Text> ItemIcons = new List<Text>();

    [Tooltip("标签根节点列表。顺序与 ItemRoots 一致。")]
    public List<RectTransform> LabelRoots = new List<RectTransform>();

    [Tooltip("标签 CanvasGroup 列表。顺序与 ItemRoots 一致。")]
    public List<CanvasGroup> LabelGroups = new List<CanvasGroup>();

    [Header("参数（可在参数面板实时调）")]
    [Tooltip("基础图标尺寸。")]
    public float BaseItemSize = 74f;

    [Tooltip("放大图标尺寸。")]
    public float MagnifiedItemSize = 114f;

    [Tooltip("影响距离。")]
    public float InfluenceDistance = 260f;

    [Tooltip("尺寸回弹速度。")]
    public float SpringSpeed = 12f;

    [Tooltip("标签渐显速度。")]
    public float LabelFadeSpeed = 8f;

    [Tooltip("Dock 收起高度。")]
    public float PanelHeightClosed = 96f;

    [Tooltip("Dock 展开高度。")]
    public float PanelHeightOpen = 136f;

    [Tooltip("高度插值速度。")]
    public float PanelHeightLerpSpeed = 8f;

    [Tooltip("点击反馈开关。")]
    public bool EnableClickPunch = true;

    [Header("点击反馈")]
    [Tooltip("点击 punch 强度。")]
    public Vector3 ClickPunchScale = new Vector3(0.12f, 0.12f, 0.12f);

    [Tooltip("点击 punch 时长。")]
    public float ClickPunchDuration = 0.22f;

    [Tooltip("点击 punch 弹性。")]
    public float ClickPunchElasticity = 0.4f;

    [Header("进入/退出")]
    [Tooltip("进入过渡时长。")]
    public float TransitionDuration = 0.26f;

    private readonly List<ReplicaV3ParameterDefinition> mParameters = new List<ReplicaV3ParameterDefinition>
    {
        new ReplicaV3ParameterDefinition { Id = "magnified_size", DisplayName = "放大尺寸", Description = "图标放大后的目标尺寸。", Kind = ReplicaV3ParameterKind.Float, Min = 60f, Max = 180f, Step = 1f },
        new ReplicaV3ParameterDefinition { Id = "influence_distance", DisplayName = "影响距离", Description = "鼠标对邻近图标影响半径。", Kind = ReplicaV3ParameterKind.Float, Min = 50f, Max = 420f, Step = 2f },
        new ReplicaV3ParameterDefinition { Id = "spring_speed", DisplayName = "回弹速度", Description = "图标尺寸追随速度。", Kind = ReplicaV3ParameterKind.Float, Min = 1f, Max = 30f, Step = 0.5f },
        new ReplicaV3ParameterDefinition { Id = "panel_open_height", DisplayName = "展开高度", Description = "Dock 面板悬停时高度。", Kind = ReplicaV3ParameterKind.Float, Min = 40f, Max = 260f, Step = 1f },
        new ReplicaV3ParameterDefinition { Id = "enable_click_punch", DisplayName = "点击反馈", Description = "开启后点击图标触发弹跳反馈。", Kind = ReplicaV3ParameterKind.Bool }
    };

    private readonly List<Vector2> mBasePositions = new List<Vector2>();
    private readonly List<float> mCurrentSizes = new List<float>();
    private readonly List<Color> mBaseColors = new List<Color>();

    private bool mPanelHovered;
    private bool mHasPointer;
    private Vector2 mPointerLocal;
    private int mHoverIndex = -1;
    private float mCurrentPanelHeight;

    protected override void OnEffectInitialize()
    {
        EnsureBindings();
        CaptureRuntimeCaches();
        ResetRuntimeVisual();
        SetCanvasAlpha(1f);
        SetLifecycleLooping();
    }

    protected override void OnEffectTick(float deltaTime, float unscaledDeltaTime)
    {
        EnsureBindings();
        UpdatePointerStateFromMouse();

        var dt = Mathf.Max(0f, unscaledDeltaTime);
        UpdatePanelHeight(dt);
        UpdateMagnification(dt);
        UpdateLabels(dt);

        if (EnableClickPunch && mPanelHovered && mHasPointer && Input.GetMouseButtonDown(0))
        {
            PunchHoveredItem();
        }
    }

    protected override void OnPlayIn()
    {
        EnsureBindings();
        KillTrackedTweens(false);
        SetCanvasAlpha(0f);
        if (EffectCanvasGroup != null)
        {
            TrackTween(EffectCanvasGroup.DOFade(1f, Mathf.Max(0.05f, TransitionDuration)).SetEase(Ease.OutCubic));
        }

        SetLifecycleLooping();
    }

    protected override void OnPlayOut(Action onComplete)
    {
        EnsureBindings();
        KillTrackedTweens(false);
        if (EffectCanvasGroup != null)
        {
            TrackTween(EffectCanvasGroup
                .DOFade(0f, Mathf.Max(0.05f, TransitionDuration))
                .SetEase(Ease.InCubic)
                .OnComplete(() => onComplete?.Invoke()));
            return;
        }

        onComplete?.Invoke();
    }

    protected override void OnEffectReset()
    {
        EnsureBindings();
        CaptureRuntimeCaches();
        mPanelHovered = false;
        mHasPointer = false;
        mPointerLocal = Vector2.zero;
        mHoverIndex = -1;
        ResetRuntimeVisual();
        SetCanvasAlpha(1f);
        SetLifecycleLooping();
    }

    public override IReadOnlyList<ReplicaV3ParameterDefinition> GetParameterDefinitions() => mParameters;

    public override bool TryGetFloatParameter(string parameterId, out float value)
    {
        switch (parameterId)
        {
            case "magnified_size": value = MagnifiedItemSize; return true;
            case "influence_distance": value = InfluenceDistance; return true;
            case "spring_speed": value = SpringSpeed; return true;
            case "panel_open_height": value = PanelHeightOpen; return true;
            default: value = 0f; return false;
        }
    }

    public override bool TrySetFloatParameter(string parameterId, float value)
    {
        switch (parameterId)
        {
            case "magnified_size": MagnifiedItemSize = Mathf.Clamp(value, 60f, 180f); return true;
            case "influence_distance": InfluenceDistance = Mathf.Clamp(value, 50f, 420f); return true;
            case "spring_speed": SpringSpeed = Mathf.Clamp(value, 1f, 30f); return true;
            case "panel_open_height": PanelHeightOpen = Mathf.Clamp(value, 40f, 260f); return true;
            default: return false;
        }
    }

    public override bool TryGetBoolParameter(string parameterId, out bool value)
    {
        if (parameterId == "enable_click_punch")
        {
            value = EnableClickPunch;
            return true;
        }

        value = false;
        return false;
    }

    public override bool TrySetBoolParameter(string parameterId, bool value)
    {
        if (parameterId == "enable_click_punch")
        {
            EnableClickPunch = value;
            return true;
        }

        return false;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        mPanelHovered = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        mPanelHovered = false;
        mHasPointer = false;
        mHoverIndex = -1;
    }

    public void OnPointerMove(PointerEventData eventData)
    {
        var target = EnsureBindings();
        if (eventData == null || target == null)
        {
            return;
        }

        var cam = ResolveReliableEventCamera(eventData, target);
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(target, eventData.position, cam, out var local))
        {
            return;
        }

        mPointerLocal = local;
        mHasPointer = true;
        mHoverIndex = ResolveHoverIndex(local.x);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        PunchHoveredItem();
    }

    private RectTransform EnsureBindings()
    {
        if (DockPanel == null)
        {
            if (EffectRoot != null)
            {
                var panel = EffectRoot.Find("Backdrop/DockPanel");
                if (panel != null)
                {
                    DockPanel = panel as RectTransform;
                }
            }

            if (DockPanel == null)
            {
                DockPanel = EffectRoot != null ? EffectRoot : transform as RectTransform;
            }
        }

        PruneInvalidItemBindings();
        if (!HasUsableItemBindings())
        {
            ItemRoots.Clear();
            CollectExistingItemBindings();
        }

        if (!HasUsableItemBindings())
        {
            CreateFallbackItems();
            ItemRoots.Clear();
            CollectExistingItemBindings();
        }

        RebuildDerivedBindingLists();

        if (EffectCanvasGroup != null)
        {
            EffectCanvasGroup.blocksRaycasts = false;
            EffectCanvasGroup.interactable = false;
        }

        if (InteractionHitSource == null)
        {
            InteractionHitSource = DockPanel != null ? DockPanel : (EffectRoot != null ? EffectRoot : transform as RectTransform);
        }

        return InteractionHitSource;
    }

    private void CollectExistingItemBindings()
    {
        if (DockPanel == null)
        {
            return;
        }

        if (ItemRoots.Count > 0)
        {
            return;
        }

        for (var i = 0; i < DockPanel.childCount; i++)
        {
            var child = DockPanel.GetChild(i) as RectTransform;
            if (child == null)
            {
                continue;
            }

            var hasIcon = child.Find("Icon")?.GetComponent<Text>() != null;
            var hasLabelRoot = child.Find("LabelRoot") != null;
            if (child.name.StartsWith("Item_", StringComparison.Ordinal) || hasIcon || hasLabelRoot)
            {
                ItemRoots.Add(child);
            }
        }
    }

    private void PruneInvalidItemBindings()
    {
        if (DockPanel == null || ItemRoots.Count <= 0)
        {
            return;
        }

        for (var i = ItemRoots.Count - 1; i >= 0; i--)
        {
            if (!IsUsableItem(ItemRoots[i]))
            {
                ItemRoots.RemoveAt(i);
            }
        }
    }

    private bool HasUsableItemBindings()
    {
        if (ItemRoots.Count <= 0)
        {
            return false;
        }

        var usableCount = 0;
        for (var i = 0; i < ItemRoots.Count; i++)
        {
            if (IsUsableItem(ItemRoots[i]))
            {
                usableCount++;
            }
        }

        return usableCount >= 3;
    }

    private bool IsUsableItem(RectTransform item)
    {
        if (item == null || DockPanel == null)
        {
            return false;
        }

        if (item == DockPanel || item.parent != DockPanel)
        {
            return false;
        }

        var hasIcon = item.Find("Icon")?.GetComponent<Text>() != null;
        var hasLabelRoot = item.Find("LabelRoot") != null;
        return hasIcon || hasLabelRoot || item.name.StartsWith("Item_", StringComparison.Ordinal);
    }

    private void RebuildDerivedBindingLists()
    {
        ItemBackgrounds.Clear();
        ItemIcons.Clear();
        LabelRoots.Clear();
        LabelGroups.Clear();

        for (var i = 0; i < ItemRoots.Count; i++)
        {
            var item = ItemRoots[i];
            if (item == null)
            {
                ItemBackgrounds.Add(null);
                ItemIcons.Add(null);
                LabelRoots.Add(null);
                LabelGroups.Add(null);
                continue;
            }

            ItemBackgrounds.Add(item.GetComponent<Graphic>());
            ItemIcons.Add(item.Find("Icon")?.GetComponent<Text>());

            var labelRoot = item.Find("LabelRoot") as RectTransform;
            LabelRoots.Add(labelRoot);
            LabelGroups.Add(labelRoot != null ? labelRoot.GetComponent<CanvasGroup>() : null);
        }
    }

    private void CreateFallbackItems()
    {
        if (DockPanel == null)
        {
            return;
        }

        PruneFallbackBackdropNoise();

        var labels = new[] { "Finder", "Music", "Mail", "Code", "Photos", "Prefs" };
        var glyphs = new[] { "F", "M", "@", "C", "P", "S" };
        var colors = new[]
        {
            new Color(0.24f, 0.42f, 0.73f, 1f),
            new Color(0.24f, 0.62f, 0.50f, 1f),
            new Color(0.72f, 0.40f, 0.30f, 1f),
            new Color(0.54f, 0.39f, 0.78f, 1f),
            new Color(0.73f, 0.56f, 0.30f, 1f),
            new Color(0.38f, 0.56f, 0.79f, 1f)
        };

        var count = labels.Length;
        var totalWidth = (count * BaseItemSize) + ((count - 1) * 16f);
        var startX = (-0.5f * totalWidth) + (0.5f * BaseItemSize);

        for (var i = 0; i < count; i++)
        {
            var itemGo = new GameObject($"Item_{i}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            itemGo.transform.SetParent(DockPanel, false);
            var itemRect = itemGo.transform as RectTransform;
            itemRect.anchorMin = new Vector2(0.5f, 0f);
            itemRect.anchorMax = new Vector2(0.5f, 0f);
            itemRect.pivot = new Vector2(0.5f, 0f);
            itemRect.sizeDelta = new Vector2(BaseItemSize, BaseItemSize);
            itemRect.anchoredPosition = new Vector2(startX + (i * (BaseItemSize + 16f)), 12f);

            var itemImage = itemGo.GetComponent<Image>();
            itemImage.color = colors[i % colors.Length];

            var button = itemGo.GetComponent<Button>();
            button.transition = Selectable.Transition.None;

            var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            iconGo.transform.SetParent(itemGo.transform, false);
            var iconRect = iconGo.transform as RectTransform;
            iconRect.anchorMin = Vector2.zero;
            iconRect.anchorMax = Vector2.one;
            iconRect.offsetMin = Vector2.zero;
            iconRect.offsetMax = Vector2.zero;

            var iconText = iconGo.GetComponent<Text>();
            iconText.text = glyphs[i];
            iconText.font = ResolveBuiltinFont();
            iconText.fontSize = 32;
            iconText.fontStyle = FontStyle.Bold;
            iconText.alignment = TextAnchor.MiddleCenter;
            iconText.color = Color.white;
            iconText.raycastTarget = false;

            var labelGo = new GameObject("LabelRoot", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
            labelGo.transform.SetParent(itemGo.transform, false);
            var labelRect = labelGo.transform as RectTransform;
            labelRect.anchorMin = new Vector2(0.5f, 0f);
            labelRect.anchorMax = new Vector2(0.5f, 0f);
            labelRect.pivot = new Vector2(0.5f, 0f);
            labelRect.sizeDelta = new Vector2(128f, 34f);
            labelRect.anchoredPosition = new Vector2(0f, BaseItemSize + 10f);

            var labelImage = labelGo.GetComponent<Image>();
            labelImage.color = new Color(0.03f, 0.05f, 0.10f, 0.95f);
            labelImage.raycastTarget = false;

            var labelTextGo = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            labelTextGo.transform.SetParent(labelGo.transform, false);
            var labelTextRect = labelTextGo.transform as RectTransform;
            labelTextRect.anchorMin = Vector2.zero;
            labelTextRect.anchorMax = Vector2.one;
            labelTextRect.offsetMin = Vector2.zero;
            labelTextRect.offsetMax = Vector2.zero;

            var labelText = labelTextGo.GetComponent<Text>();
            labelText.text = labels[i];
            labelText.font = ResolveBuiltinFont();
            labelText.fontSize = 18;
            labelText.fontStyle = FontStyle.Bold;
            labelText.alignment = TextAnchor.MiddleCenter;
            labelText.color = new Color(0.90f, 0.95f, 1f, 1f);
            labelText.raycastTarget = false;

            var labelGroup = labelGo.GetComponent<CanvasGroup>();
            labelGroup.alpha = 0f;
            labelGroup.blocksRaycasts = false;
            labelGroup.interactable = false;
        }
    }

    private void PruneFallbackBackdropNoise()
    {
        var backdrop = DockPanel.parent as RectTransform;
        if (backdrop == null)
        {
            return;
        }

        for (var i = 0; i < backdrop.childCount; i++)
        {
            var child = backdrop.GetChild(i) as RectTransform;
            if (child == null || child == DockPanel)
            {
                continue;
            }

            if (string.Equals(child.name, "Hint", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var hasIcon = child.Find("Icon") != null;
            var hasLabel = child.Find("LabelRoot") != null;
            if (hasIcon || hasLabel)
            {
                continue;
            }

            if (child.GetComponent<Image>() != null)
            {
                child.gameObject.SetActive(false);
            }
        }
    }

    private void CaptureRuntimeCaches()
    {
        mBasePositions.Clear();
        mCurrentSizes.Clear();
        mBaseColors.Clear();

        for (var i = 0; i < ItemRoots.Count; i++)
        {
            var item = ItemRoots[i];
            mBasePositions.Add(item != null ? item.anchoredPosition : Vector2.zero);
            mCurrentSizes.Add(Mathf.Max(1f, BaseItemSize));

            if (i < ItemBackgrounds.Count && ItemBackgrounds[i] != null)
            {
                mBaseColors.Add(ItemBackgrounds[i].color);
            }
            else
            {
                mBaseColors.Add(Color.white);
            }
        }

        mCurrentPanelHeight = PanelHeightClosed;
    }

    private void UpdatePointerStateFromMouse()
    {
        var target = EnsureBindings();
        if (target == null)
        {
            mPanelHovered = false;
            mHasPointer = false;
            mHoverIndex = -1;
            return;
        }

        var camera = ResolveReliableEventCamera(target);
        var screenPos = (Vector2)Input.mousePosition;
        var hovered = RectTransformUtility.RectangleContainsScreenPoint(target, screenPos, camera);
        if (!hovered)
        {
            mPanelHovered = false;
            mHasPointer = false;
            mHoverIndex = -1;
            return;
        }

        mPanelHovered = true;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(target, screenPos, camera, out var local))
        {
            mPointerLocal = local;
            mHasPointer = true;
            mHoverIndex = ResolveHoverIndex(local.x);
        }
        else
        {
            mHasPointer = false;
            mHoverIndex = -1;
        }
    }

    private void PunchHoveredItem()
    {
        var index = mHoverIndex;
        if (index < 0 || index >= ItemRoots.Count || !EnableClickPunch)
        {
            return;
        }

        var item = ItemRoots[index];
        if (item == null)
        {
            return;
        }

        TrackTween(item.DOPunchScale(ClickPunchScale, Mathf.Max(0.01f, ClickPunchDuration), 1, Mathf.Clamp01(ClickPunchElasticity)));
    }

    private void ResetRuntimeVisual()
    {
        var safeSize = Mathf.Max(1f, BaseItemSize);
        for (var i = 0; i < ItemRoots.Count; i++)
        {
            var item = ItemRoots[i];
            if (item != null)
            {
                item.sizeDelta = new Vector2(safeSize, safeSize);
                item.localScale = Vector3.one;
                item.anchoredPosition = i < mBasePositions.Count ? mBasePositions[i] : item.anchoredPosition;
            }

            if (i < mCurrentSizes.Count)
            {
                mCurrentSizes[i] = safeSize;
            }

            if (i < LabelGroups.Count && LabelGroups[i] != null)
            {
                LabelGroups[i].alpha = 0f;
            }
        }

        if (DockPanel != null)
        {
            DockPanel.sizeDelta = new Vector2(DockPanel.sizeDelta.x, PanelHeightClosed);
            mCurrentPanelHeight = PanelHeightClosed;
        }
    }

    private void UpdatePanelHeight(float dt)
    {
        if (DockPanel == null)
        {
            return;
        }

        var targetHeight = mPanelHovered ? PanelHeightOpen : PanelHeightClosed;
        mCurrentPanelHeight = Mathf.Lerp(mCurrentPanelHeight, targetHeight, dt * Mathf.Max(0.01f, PanelHeightLerpSpeed));
        DockPanel.sizeDelta = new Vector2(DockPanel.sizeDelta.x, mCurrentPanelHeight);
    }

    private void UpdateMagnification(float dt)
    {
        var baseSize = Mathf.Max(1f, BaseItemSize);
        var maxSize = Mathf.Max(baseSize, MagnifiedItemSize);
        var spring = Mathf.Max(0.01f, SpringSpeed);
        var influenceDistance = Mathf.Max(1f, InfluenceDistance);

        for (var i = 0; i < ItemRoots.Count; i++)
        {
            var item = ItemRoots[i];
            if (item == null)
            {
                continue;
            }

            var basePos = i < mBasePositions.Count ? mBasePositions[i] : item.anchoredPosition;
            var distance = mHasPointer && mPanelHovered
                ? Mathf.Abs(mPointerLocal.x - basePos.x)
                : float.PositiveInfinity;

            var influence = mPanelHovered ? Mathf.Clamp01(1f - (distance / influenceDistance)) : 0f;
            var eased = influence * influence * (3f - (2f * influence));
            var targetSize = Mathf.Lerp(baseSize, maxSize, eased);

            if (i >= mCurrentSizes.Count)
            {
                mCurrentSizes.Add(baseSize);
            }

            mCurrentSizes[i] = Mathf.Lerp(mCurrentSizes[i], targetSize, dt * spring);
            item.sizeDelta = new Vector2(mCurrentSizes[i], mCurrentSizes[i]);
            item.anchoredPosition = new Vector2(basePos.x, basePos.y + ((mCurrentSizes[i] - baseSize) * 0.22f));

            if (i < ItemIcons.Count && ItemIcons[i] != null)
            {
                ItemIcons[i].fontSize = Mathf.RoundToInt(Mathf.Lerp(32f, 44f, eased));
            }

            if (i < ItemBackgrounds.Count && ItemBackgrounds[i] != null)
            {
                var baseColor = i < mBaseColors.Count ? mBaseColors[i] : ItemBackgrounds[i].color;
                var dimmed = baseColor * 0.85f;
                dimmed.a = baseColor.a;
                var hoverBlend = Mathf.Max(eased, mHoverIndex == i ? 1f : 0f);
                ItemBackgrounds[i].color = Color.Lerp(dimmed, baseColor, hoverBlend);
            }

            if (i < LabelRoots.Count && LabelRoots[i] != null && i < LabelGroups.Count && LabelGroups[i] != null)
            {
                var labelY = mCurrentSizes[i] + Mathf.Lerp(8f, 20f, LabelGroups[i].alpha);
                LabelRoots[i].anchoredPosition = new Vector2(0f, labelY);
            }
        }
    }

    private void UpdateLabels(float dt)
    {
        var speed = Mathf.Max(0.01f, LabelFadeSpeed);
        for (var i = 0; i < LabelGroups.Count; i++)
        {
            var group = LabelGroups[i];
            if (group == null)
            {
                continue;
            }

            var target = (mPanelHovered && mHoverIndex == i) ? 1f : 0f;
            group.alpha = Mathf.MoveTowards(group.alpha, target, dt * speed);
        }
    }

    private int ResolveHoverIndex(float pointerX)
    {
        var best = -1;
        var bestDist = float.PositiveInfinity;
        for (var i = 0; i < ItemRoots.Count; i++)
        {
            if (ItemRoots[i] == null)
            {
                continue;
            }

            var basePos = i < mBasePositions.Count ? mBasePositions[i] : ItemRoots[i].anchoredPosition;
            var dist = Mathf.Abs(pointerX - basePos.x);
            if (dist < bestDist)
            {
                bestDist = dist;
                best = i;
            }
        }

        if (best < 0)
        {
            return -1;
        }

        return bestDist <= Mathf.Max(8f, InfluenceDistance) ? best : -1;
    }

    private void OnDrawGizmos()
    {
        var hitSource = InteractionHitSource != null
            ? InteractionHitSource
            : (DockPanel != null ? DockPanel : (EffectRoot != null ? EffectRoot : transform as RectTransform));
        DrawInteractionRangeGizmo(ShowInteractionRange, hitSource, InteractionRangeDependency, InteractionRangePadding);
    }
}
