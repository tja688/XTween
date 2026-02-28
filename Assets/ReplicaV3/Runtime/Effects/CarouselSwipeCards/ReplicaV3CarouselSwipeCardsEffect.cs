using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class ReplicaV3CarouselSwipeCardsEffect : ReplicaV3EffectBase,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler,
    IPointerEnterHandler,
    IPointerExitHandler
{
    [Header("交互范围")]
    [Tooltip("主要可交互判定组件。为空时自动回退到 EffectRoot。")]
    public RectTransform InteractionHitSource;

    [Tooltip("交互范围跟随依赖组件。为空时回退到 InteractionHitSource。")]
    public RectTransform InteractionRangeDependency;

    [Tooltip("编辑器中显示可交互范围。默认开启，可手动关闭。")]
    public bool ShowInteractionRange = true;

    [Tooltip("交互范围扩展像素。")]
    public float InteractionRangePadding = 0f;

    [Header("组件绑定（Carousel）")]
    [Tooltip("卡片舞台。")]
    public RectTransform CardStage;

    [Tooltip("指示器容器。")]
    public RectTransform IndicatorsRoot;

    [Tooltip("外框容器，用于 PlayIn/PlayOut。")]
    public RectTransform FrameRoot;

    [Tooltip("标题提示文本。")]
    public Text HintLabel;

    [Header("行为参数")]
    [Tooltip("自动轮播间隔（秒）。")]
    public float AutoplayDelay = 3.1f;

    [Tooltip("是否自动轮播。")]
    public bool Autoplay = true;

    [Tooltip("悬停时暂停自动轮播。")]
    public bool PauseOnHover = true;

    [Tooltip("是否循环播放。")]
    public bool Loop = true;

    [Header("拖拽参数")]
    [Tooltip("拖拽位移缩放。")]
    public float DragMoveScale = 0.25f;

    [Tooltip("拖拽最大偏移。")]
    public float DragClamp = 80f;

    [Tooltip("判定切页所需最小拖拽距离。")]
    public float SwipeThreshold = 80f;

    [Header("动画参数")]
    [Tooltip("松手后舞台回位时长。")]
    public float StageReturnDuration = 0.18f;

    [Tooltip("卡片位移/旋转/缩放时长。")]
    public float CardMoveDuration = 0.45f;

    [Tooltip("卡片透明度过渡时长。")]
    public float CardFadeDuration = 0.35f;

    [Tooltip("是否使用非缩放时间。")]
    public bool UseUnscaledTime = true;

    [Header("排布参数")]
    [Tooltip("中心卡片到左右卡片的 X 步长。")]
    public float CardStepX = 320f;

    [Tooltip("中心卡片到侧卡片的 Y 步长。")]
    public float CardStepY = 18f;

    [Tooltip("卡片旋转步长。")]
    public float CardRotateStep = 9f;

    [Header("文案")]
    [Tooltip("顶部提示。")]
    public string Hint = "Carousel  |  Drag cards or click indicators";

    [Header("缺省卡片（仅当 prefab 未预置卡片时使用）")]
    public List<CardSeed> CardSeeds = new List<CardSeed>();

    private readonly List<ReplicaV3ParameterDefinition> mParameters = new List<ReplicaV3ParameterDefinition>
    {
        new ReplicaV3ParameterDefinition
        {
            Id = "autoplay_delay",
            DisplayName = "自动轮播间隔",
            Description = "自动切换到下一张卡片的间隔秒数。",
            Kind = ReplicaV3ParameterKind.Float,
            Min = 1.2f,
            Max = 10f,
            Step = 0.1f
        },
        new ReplicaV3ParameterDefinition
        {
            Id = "swipe_threshold",
            DisplayName = "切换阈值",
            Description = "拖拽超过该像素触发切换。",
            Kind = ReplicaV3ParameterKind.Float,
            Min = 20f,
            Max = 280f,
            Step = 2f
        },
        new ReplicaV3ParameterDefinition
        {
            Id = "card_move_duration",
            DisplayName = "卡片移动时长",
            Description = "卡片位置与旋转变化速度。",
            Kind = ReplicaV3ParameterKind.Float,
            Min = 0.08f,
            Max = 2f,
            Step = 0.02f
        },
        new ReplicaV3ParameterDefinition
        {
            Id = "autoplay",
            DisplayName = "自动轮播",
            Description = "开启后按固定间隔自动切换。",
            Kind = ReplicaV3ParameterKind.Bool
        },
        new ReplicaV3ParameterDefinition
        {
            Id = "pause_on_hover",
            DisplayName = "悬停暂停",
            Description = "鼠标悬停时暂停自动切换。",
            Kind = ReplicaV3ParameterKind.Bool
        },
        new ReplicaV3ParameterDefinition
        {
            Id = "loop",
            DisplayName = "循环播放",
            Description = "关闭后到头不再循环。",
            Kind = ReplicaV3ParameterKind.Bool
        }
    };

    private readonly List<CardRuntime> mCards = new List<CardRuntime>();
    private readonly List<Image> mIndicators = new List<Image>();

    private int mCurrentIndex;
    private float mAutoplayTimer;
    private bool mDragging;
    private bool mHovered;
    private Vector2 mDragStartPointer;
    private float mDragStartStageX;
    private Vector3 mFrameBaseScale = Vector3.one;

    protected override void OnEffectInitialize()
    {
        EnsureBindings();
        EnsureCardsAndIndicators();
        SetIndex(0, true);
        SetCanvasAlpha(1f);
        SetLifecycleLooping();
    }

    protected override void OnEffectTick(float deltaTime, float unscaledDeltaTime)
    {
        if (!Autoplay || mDragging || (PauseOnHover && mHovered) || mCards.Count <= 1)
        {
            return;
        }

        var dt = UseUnscaledTime ? Mathf.Max(0f, unscaledDeltaTime) : Mathf.Max(0f, deltaTime);
        mAutoplayTimer += dt;
        if (mAutoplayTimer < Mathf.Max(1.2f, AutoplayDelay))
        {
            return;
        }

        mAutoplayTimer = 0f;
        SetIndex(mCurrentIndex + 1, false);
    }

    protected override void OnPlayIn()
    {
        KillTrackedTweens(false);
        EnsureBindings();
        EnsureCardsAndIndicators();

        if (FrameRoot != null)
        {
            FrameRoot.localScale = mFrameBaseScale * 0.96f;
            TrackTween(FrameRoot
                .DOScale(mFrameBaseScale, 0.28f)
                .SetEase(Ease.OutCubic)
                .SetUpdate(UseUnscaledTime));
        }

        SetCanvasAlpha(0f);
        if (EffectCanvasGroup != null)
        {
            TrackTween(EffectCanvasGroup
                .DOFade(1f, 0.28f)
                .SetEase(Ease.OutCubic)
                .SetUpdate(UseUnscaledTime));
        }

        SetLifecycleLooping();
    }

    protected override void OnPlayOut(Action onComplete)
    {
        KillTrackedTweens(false);
        if (EffectCanvasGroup != null)
        {
            TrackTween(EffectCanvasGroup
                .DOFade(0f, 0.22f)
                .SetEase(Ease.InCubic)
                .SetUpdate(UseUnscaledTime)
                .OnComplete(() => onComplete?.Invoke()));
            return;
        }

        onComplete?.Invoke();
    }

    protected override void OnEffectReset()
    {
        KillTrackedTweens(false);
        EnsureBindings();
        EnsureCardsAndIndicators();

        mDragging = false;
        mHovered = false;
        mAutoplayTimer = 0f;
        SetIndex(0, true);

        if (CardStage != null)
        {
            CardStage.anchoredPosition = new Vector2(0f, CardStage.anchoredPosition.y);
        }

        if (FrameRoot != null)
        {
            FrameRoot.localScale = mFrameBaseScale;
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
            case "autoplay_delay":
                value = AutoplayDelay;
                return true;
            case "swipe_threshold":
                value = SwipeThreshold;
                return true;
            case "card_move_duration":
                value = CardMoveDuration;
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
            case "autoplay_delay":
                AutoplayDelay = Mathf.Clamp(value, 1.2f, 10f);
                return true;
            case "swipe_threshold":
                SwipeThreshold = Mathf.Clamp(value, 20f, 280f);
                return true;
            case "card_move_duration":
                CardMoveDuration = Mathf.Clamp(value, 0.08f, 2f);
                return true;
            default:
                return false;
        }
    }

    public override bool TryGetBoolParameter(string parameterId, out bool value)
    {
        switch (parameterId)
        {
            case "autoplay":
                value = Autoplay;
                return true;
            case "pause_on_hover":
                value = PauseOnHover;
                return true;
            case "loop":
                value = Loop;
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
            case "autoplay":
                Autoplay = value;
                return true;
            case "pause_on_hover":
                PauseOnHover = value;
                return true;
            case "loop":
                Loop = value;
                return true;
            default:
                return false;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        mHovered = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        mHovered = false;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (CardStage == null || mCards.Count <= 1)
        {
            return;
        }

        mDragging = true;
        mAutoplayTimer = 0f;
        CardStage.DOKill(false);
        mDragStartPointer = eventData != null ? eventData.position : Vector2.zero;
        mDragStartStageX = CardStage.anchoredPosition.x;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!mDragging || CardStage == null || eventData == null)
        {
            return;
        }

        var delta = eventData.position - mDragStartPointer;
        var targetX = Mathf.Clamp(mDragStartStageX + (delta.x * DragMoveScale), -Mathf.Abs(DragClamp), Mathf.Abs(DragClamp));
        CardStage.anchoredPosition = new Vector2(targetX, CardStage.anchoredPosition.y);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!mDragging || CardStage == null)
        {
            return;
        }

        mDragging = false;
        var delta = eventData != null ? eventData.position - mDragStartPointer : Vector2.zero;
        if (delta.x <= -Mathf.Abs(SwipeThreshold))
        {
            SetIndex(mCurrentIndex + 1, false);
        }
        else if (delta.x >= Mathf.Abs(SwipeThreshold))
        {
            SetIndex(mCurrentIndex - 1, false);
        }
        else
        {
            AnimateCards(false);
        }
    }

    private void SetIndex(int nextIndex, bool immediate)
    {
        if (mCards.Count == 0)
        {
            return;
        }

        if (Loop)
        {
            if (nextIndex < 0)
            {
                nextIndex = mCards.Count - 1;
            }
            else if (nextIndex >= mCards.Count)
            {
                nextIndex = 0;
            }
        }
        else
        {
            nextIndex = Mathf.Clamp(nextIndex, 0, mCards.Count - 1);
        }

        mCurrentIndex = nextIndex;
        AnimateCards(immediate);
        UpdateIndicators();
        mAutoplayTimer = 0f;
    }

    private void AnimateCards(bool immediate)
    {
        if (CardStage != null)
        {
            CardStage.DOKill(false);
            TrackTween(CardStage
                .DOAnchorPosX(0f, immediate ? 0f : Mathf.Max(0.01f, StageReturnDuration))
                .SetEase(Ease.OutCubic)
                .SetUpdate(UseUnscaledTime));
        }

        var count = mCards.Count;
        for (var i = 0; i < count; i++)
        {
            var card = mCards[i];
            if (card == null || card.Rect == null || card.Group == null)
            {
                continue;
            }

            var delta = WrapDelta(i - mCurrentIndex, count);
            var abs = Mathf.Abs(delta);

            var targetX = delta * CardStepX;
            var targetY = -abs * CardStepY;
            var targetScale = abs == 0 ? 1f : (abs == 1 ? 0.86f : 0.74f);
            var targetRot = -delta * CardRotateStep;
            var targetAlpha = abs == 0 ? 1f : (abs == 1 ? 0.58f : 0.18f);

            card.Rect.SetSiblingIndex(abs == 0 ? count - 1 : Mathf.Max(0, count - 1 - abs));
            card.Rect.DOKill(false);
            card.Group.DOKill(false);

            if (immediate)
            {
                card.Rect.anchoredPosition = new Vector2(targetX, targetY);
                card.Rect.localScale = Vector3.one * targetScale;
                card.Rect.localRotation = Quaternion.Euler(0f, 0f, targetRot);
                card.Group.alpha = targetAlpha;
                continue;
            }

            var moveDuration = Mathf.Max(0.01f, CardMoveDuration);
            TrackTween(card.Rect
                .DOAnchorPos(new Vector2(targetX, targetY), moveDuration)
                .SetEase(Ease.OutCubic)
                .SetUpdate(UseUnscaledTime));
            TrackTween(card.Rect
                .DOScale(Vector3.one * targetScale, moveDuration)
                .SetEase(Ease.OutCubic)
                .SetUpdate(UseUnscaledTime));
            TrackTween(card.Rect
                .DOLocalRotate(new Vector3(0f, 0f, targetRot), moveDuration)
                .SetEase(Ease.OutCubic)
                .SetUpdate(UseUnscaledTime));
            TrackTween(card.Group
                .DOFade(targetAlpha, Mathf.Max(0.01f, CardFadeDuration))
                .SetEase(Ease.OutCubic)
                .SetUpdate(UseUnscaledTime));
        }
    }

    private void UpdateIndicators()
    {
        for (var i = 0; i < mIndicators.Count; i++)
        {
            var image = mIndicators[i];
            if (image == null)
            {
                continue;
            }

            var active = i == mCurrentIndex;
            image.color = active ? new Color(1f, 1f, 1f, 1f) : new Color(1f, 1f, 1f, 0.3f);
            image.rectTransform.localScale = active ? Vector3.one * 1.2f : Vector3.one;
        }
    }

    private void EnsureBindings()
    {
        if (CardStage == null && EffectRoot != null)
        {
            var stage = EffectRoot.Find("Backdrop/Frame/CardStage");
            if (stage != null)
            {
                CardStage = stage as RectTransform;
            }
        }

        if (IndicatorsRoot == null && EffectRoot != null)
        {
            var root = EffectRoot.Find("Backdrop/Frame/Indicators");
            if (root != null)
            {
                IndicatorsRoot = root as RectTransform;
            }
        }

        if (FrameRoot == null && CardStage != null)
        {
            FrameRoot = CardStage.parent as RectTransform;
        }

        if (HintLabel == null && EffectRoot != null)
        {
            var hint = EffectRoot.Find("Backdrop/Hint");
            if (hint != null)
            {
                HintLabel = hint.GetComponent<Text>();
            }
        }

        if (HintLabel != null)
        {
            HintLabel.text = string.IsNullOrWhiteSpace(Hint) ? "Carousel  |  Drag cards or click indicators" : Hint;
        }

        if (InteractionHitSource == null)
        {
            InteractionHitSource = EffectRoot != null ? EffectRoot : transform as RectTransform;
        }

        if (FrameRoot != null)
        {
            mFrameBaseScale = FrameRoot.localScale;
        }

        if (string.IsNullOrWhiteSpace(EffectKey))
        {
            EffectKey = "carousel-swipe-cards-v3";
        }

        if (string.IsNullOrWhiteSpace(EffectDisplayName))
        {
            EffectDisplayName = "CarouselSwipeCards V3";
        }

        if (string.IsNullOrWhiteSpace(UsageDescription))
        {
            UsageDescription = "支持拖拽与指示器点击切换，自动轮播可在参数面板开关。";
        }
    }

    private void EnsureCardsAndIndicators()
    {
        CollectCardsFromStage();
        if (mCards.Count == 0)
        {
            BuildFallbackCards();
            CollectCardsFromStage();
        }

        CollectIndicators();
        if (mIndicators.Count == 0 && mCards.Count > 0)
        {
            BuildFallbackIndicators();
            CollectIndicators();
        }

        UpdateIndicators();
    }

    private void CollectCardsFromStage()
    {
        mCards.Clear();
        if (CardStage == null)
        {
            return;
        }

        for (var i = 0; i < CardStage.childCount; i++)
        {
            var child = CardStage.GetChild(i) as RectTransform;
            if (child == null)
            {
                continue;
            }

            var group = child.GetComponent<CanvasGroup>();
            if (group == null)
            {
                group = child.gameObject.AddComponent<CanvasGroup>();
            }

            mCards.Add(new CardRuntime
            {
                Rect = child,
                Group = group
            });
        }
    }

    private void CollectIndicators()
    {
        mIndicators.Clear();
        if (IndicatorsRoot == null)
        {
            return;
        }

        for (var i = 0; i < IndicatorsRoot.childCount; i++)
        {
            var child = IndicatorsRoot.GetChild(i) as RectTransform;
            if (child == null)
            {
                continue;
            }

            var image = child.GetComponent<Image>();
            if (image == null)
            {
                continue;
            }

            var button = child.GetComponent<Button>();
            if (button == null)
            {
                button = child.gameObject.AddComponent<Button>();
                button.targetGraphic = image;
            }

            var index = i;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => SetIndex(index, false));
            mIndicators.Add(image);
        }
    }

    private void BuildFallbackCards()
    {
        if (CardStage == null)
        {
            return;
        }

        var seeds = GetSafeCardSeeds();
        for (var i = 0; i < seeds.Count; i++)
        {
            var seed = seeds[i];
            var itemRect = CreatePanel($"Item_{i}", CardStage, seed.CardColor);
            itemRect.anchorMin = new Vector2(0.5f, 0.5f);
            itemRect.anchorMax = new Vector2(0.5f, 0.5f);
            itemRect.pivot = new Vector2(0.5f, 0.5f);
            itemRect.sizeDelta = new Vector2(430f, 470f);
            itemRect.anchoredPosition = Vector2.zero;

            var group = itemRect.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 1f;
            group.blocksRaycasts = false;
            group.interactable = false;

            var outline = itemRect.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(1f, 1f, 1f, 0.24f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            var accent = CreatePanel("Accent", itemRect, seed.AccentColor);
            accent.anchorMin = new Vector2(0f, 1f);
            accent.anchorMax = new Vector2(1f, 1f);
            accent.pivot = new Vector2(0.5f, 1f);
            accent.sizeDelta = new Vector2(0f, 66f);
            accent.anchoredPosition = Vector2.zero;

            var title = CreateText("Title", itemRect, seed.Title, 40, FontStyle.Bold, TextAnchor.UpperLeft, Color.white);
            var titleRect = (RectTransform)title.transform;
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0f, 1f);
            titleRect.sizeDelta = new Vector2(-34f, 96f);
            titleRect.anchoredPosition = new Vector2(28f, -92f);

            var desc = CreateText("Description", itemRect, seed.Description, 28, FontStyle.Normal, TextAnchor.UpperLeft, new Color(1f, 1f, 1f, 0.92f));
            var descRect = (RectTransform)desc.transform;
            descRect.anchorMin = new Vector2(0f, 0f);
            descRect.anchorMax = new Vector2(1f, 1f);
            descRect.pivot = new Vector2(0f, 0f);
            descRect.sizeDelta = new Vector2(-54f, -220f);
            descRect.anchoredPosition = new Vector2(24f, 28f);
        }
    }

    private void BuildFallbackIndicators()
    {
        if (IndicatorsRoot == null)
        {
            return;
        }

        var row = IndicatorsRoot.GetComponent<HorizontalLayoutGroup>();
        if (row == null)
        {
            row = IndicatorsRoot.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.childAlignment = TextAnchor.MiddleCenter;
            row.childControlHeight = false;
            row.childControlWidth = false;
            row.childForceExpandHeight = false;
            row.childForceExpandWidth = false;
            row.spacing = 18f;
        }

        for (var i = 0; i < mCards.Count; i++)
        {
            var go = new GameObject($"Indicator_{i}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(IndicatorsRoot, false);
            rect.sizeDelta = new Vector2(14f, 14f);

            var image = go.GetComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.3f);

            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
        }
    }

    private List<CardSeed> GetSafeCardSeeds()
    {
        if (CardSeeds != null && CardSeeds.Count > 0)
        {
            return CardSeeds;
        }

        return new List<CardSeed>
        {
            new CardSeed("Text Animations", "Cool text animations for your projects.", new Color(0.20f, 0.29f, 0.56f, 1f), new Color(0.42f, 0.58f, 0.94f, 0.9f)),
            new CardSeed("Animations", "Smooth animation recipes for interaction states.", new Color(0.10f, 0.47f, 0.53f, 1f), new Color(0.19f, 0.79f, 0.71f, 0.9f)),
            new CardSeed("Components", "Reusable building blocks for rapid interfaces.", new Color(0.52f, 0.31f, 0.72f, 1f), new Color(0.72f, 0.52f, 0.97f, 0.9f)),
            new CardSeed("Backgrounds", "Layered surfaces and visual atmosphere.", new Color(0.70f, 0.43f, 0.28f, 1f), new Color(0.94f, 0.65f, 0.31f, 0.9f)),
            new CardSeed("Common UI", "Shared controls and practical UI patterns.", new Color(0.74f, 0.26f, 0.40f, 1f), new Color(0.97f, 0.44f, 0.58f, 0.9f))
        };
    }

    private static RectTransform CreatePanel(string name, Transform parent, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        var rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        var image = go.GetComponent<Image>();
        image.color = color;
        return rect;
    }

    private static Text CreateText(string name, Transform parent, string text, int size, FontStyle style, TextAnchor anchor, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        var rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);

        var label = go.GetComponent<Text>();
        label.text = text;
        label.fontSize = size;
        label.fontStyle = style;
        label.alignment = anchor;
        label.color = color;
        label.raycastTarget = false;
        label.font = ResolveBuiltinFont();
        return label;
    }

    private static int WrapDelta(int delta, int count)
    {
        if (count <= 0)
        {
            return delta;
        }

        var half = count / 2;
        while (delta > half)
        {
            delta -= count;
        }

        while (delta < -half)
        {
            delta += count;
        }

        return delta;
    }

    private void OnDrawGizmos()
    {
        var hit = InteractionHitSource != null ? InteractionHitSource : (EffectRoot != null ? EffectRoot : transform as RectTransform);
        DrawInteractionRangeGizmo(ShowInteractionRange, hit, InteractionRangeDependency, InteractionRangePadding);
    }

    [Serializable]
    public sealed class CardSeed
    {
        public string Title;
        public string Description;
        public Color CardColor = Color.white;
        public Color AccentColor = Color.white;

        public CardSeed()
        {
        }

        public CardSeed(string title, string description, Color cardColor, Color accentColor)
        {
            Title = title;
            Description = description;
            CardColor = cardColor;
            AccentColor = accentColor;
        }
    }

    private sealed class CardRuntime
    {
        public RectTransform Rect;
        public CanvasGroup Group;
    }
}
