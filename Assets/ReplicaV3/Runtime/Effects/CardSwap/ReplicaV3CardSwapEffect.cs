using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class ReplicaV3CardSwapEffect : ReplicaV3EffectBase,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerMoveHandler,
    IPointerClickHandler
{
    [Header("交互范围")]
    [Tooltip("主要可交互判定组件。为空时自动回退到 Surface 或 EffectRoot。")]
    public RectTransform InteractionHitSource;

    [Tooltip("交互范围跟随依赖组件。为空时回退到 InteractionHitSource。")]
    public RectTransform InteractionRangeDependency;

    [Tooltip("编辑器中显示可交互范围。默认开启，可手动关闭。")]
    public bool ShowInteractionRange = true;

    [Tooltip("交互范围扩展像素。")]
    public float InteractionRangePadding = 0f;

    [Header("组件绑定（CardSwap）")]
    [Tooltip("主交互背景层。")]
    public RectTransform Backdrop;

    [Tooltip("卡片交换舞台。")]
    public RectTransform Surface;

    [Tooltip("卡片容器。")]
    public RectTransform Deck;

    [Tooltip("顶部提示文本。")]
    public Text HintLabel;

    [Header("文案")]
    [Tooltip("顶部提示文案。")]
    public string Hint = "CardSwap  |  Auto swaps top card to back";

    [Tooltip("卡片底部说明。")]
    public string Caption = "Click the front card to swap now";

    [Header("布局参数")]
    [Tooltip("卡片横向间距。")]
    public float CardDistance = 72f;

    [Tooltip("卡片纵向层叠偏移。")]
    public float VerticalDistance = 66f;

    [Header("交换参数")]
    [Tooltip("自动交换间隔（秒）。")]
    public float SwapDelay = 1.1f;

    [Tooltip("悬停时是否暂停自动交换。")]
    public bool PauseOnHover = true;

    [Tooltip("点击连发时可缓存的最大交换次数。")]
    public int MaxQueuedSwaps = 6;

    [Tooltip("顶层卡片甩出时长。")]
    public float OutDuration = 0.22f;

    [Tooltip("顶层卡片甩出 Y 距离。")]
    public float OutDistanceY = 500f;

    [Tooltip("队列重排动画时长。")]
    public float LayoutDuration = 0.34f;

    [Tooltip("层级重排错峰延迟。")]
    public float LayoutDelayStep = 0.03f;

    [Range(0f, 1f)]
    [Tooltip("重排启动重叠比例。0=等甩出结束，1=立刻重排。")]
    public float LayoutOverlap = 0.62f;

    [Header("过渡参数")]
    [Tooltip("PlayIn 偏移距离。")]
    public float EnterOffset = 220f;

    [Tooltip("PlayOut 偏移距离。")]
    public float ExitOffset = 220f;

    [Tooltip("进出场过渡时长。")]
    public float TransitionDuration = 0.32f;

    [Tooltip("是否使用非缩放时间。")]
    public bool UseUnscaledTime = true;

    private readonly List<ReplicaV3ParameterDefinition> mParameters = new List<ReplicaV3ParameterDefinition>
    {
        new ReplicaV3ParameterDefinition
        {
            Id = "swap_delay",
            DisplayName = "交换间隔",
            Description = "自动把顶层卡片移到队尾的间隔秒数。",
            Kind = ReplicaV3ParameterKind.Float,
            Min = 0.2f,
            Max = 10f,
            Step = 0.05f
        },
        new ReplicaV3ParameterDefinition
        {
            Id = "card_distance",
            DisplayName = "横向间距",
            Description = "卡片沿 X 轴的排列间距。",
            Kind = ReplicaV3ParameterKind.Float,
            Min = 20f,
            Max = 240f,
            Step = 2f
        },
        new ReplicaV3ParameterDefinition
        {
            Id = "out_duration",
            DisplayName = "甩出时长",
            Description = "顶层卡片下落甩出的时间。",
            Kind = ReplicaV3ParameterKind.Float,
            Min = 0.05f,
            Max = 1.2f,
            Step = 0.01f
        },
        new ReplicaV3ParameterDefinition
        {
            Id = "layout_duration",
            DisplayName = "重排时长",
            Description = "其余卡片回位重排动画时长。",
            Kind = ReplicaV3ParameterKind.Float,
            Min = 0.05f,
            Max = 2f,
            Step = 0.01f
        },
        new ReplicaV3ParameterDefinition
        {
            Id = "pause_on_hover",
            DisplayName = "悬停暂停",
            Description = "鼠标悬停在卡片区域时暂停自动交换。",
            Kind = ReplicaV3ParameterKind.Bool
        }
    };

    private readonly List<CardRuntime> mCards = new List<CardRuntime>();

    private Vector2 mSurfaceBasePosition;
    private bool mPointerInside;
    private bool mHasPointer;
    private Vector2 mPointerLocal;
    private bool mHoverPaused;
    private bool mSwapInProgress;
    private int mQueuedSwaps;
    private float mSwapTimer;

    protected override void OnEffectInitialize()
    {
        EnsureBindings();
        CacheCards();
        ApplyTexts();
        ApplySlotLayout(false, 0f);
        mSwapTimer = Mathf.Max(0f, SwapDelay) * 0.3f;
        mSwapInProgress = false;
        mQueuedSwaps = 0;
        SetCanvasAlpha(1f);
        SetLifecycleLooping();
    }

    protected override void OnEffectTick(float deltaTime, float unscaledDeltaTime)
    {
        var dt = UseUnscaledTime ? Mathf.Max(0f, unscaledDeltaTime) : Mathf.Max(0f, deltaTime);
        UpdateHoverPauseState();

        if (mCards.Count <= 1)
        {
            return;
        }

        if (!PauseOnHover || !mHoverPaused)
        {
            mSwapTimer += dt;
            if (mSwapTimer >= Mathf.Max(0.2f, SwapDelay))
            {
                mSwapTimer = 0f;
                EnqueueSwapRequest(false);
            }
        }

        TryConsumeQueuedSwap(false);
    }

    protected override void OnPlayIn()
    {
        KillTrackedTweens(false);
        EnsureBindings();
        CacheCards();
        ApplyTexts();
        mSwapInProgress = false;
        mQueuedSwaps = 0;
        mPointerInside = false;
        mHasPointer = false;
        mSwapTimer = 0f;
        ApplySlotLayout(false, 0f);

        if (Surface != null)
        {
            Surface.anchoredPosition = mSurfaceBasePosition + new Vector2(0f, EnterOffset);
            TrackTween(Surface
                .DOAnchorPos(mSurfaceBasePosition, Mathf.Max(0.08f, TransitionDuration))
                .SetEase(Ease.OutCubic)
                .SetUpdate(UseUnscaledTime));
        }

        SetCanvasAlpha(0f);
        if (EffectCanvasGroup != null)
        {
            TrackTween(EffectCanvasGroup
                .DOFade(1f, Mathf.Max(0.08f, TransitionDuration))
                .SetEase(Ease.OutCubic)
                .SetUpdate(UseUnscaledTime));
        }

        SetLifecycleLooping();
    }

    protected override void OnPlayOut(Action onComplete)
    {
        KillTrackedTweens(false);
        mSwapInProgress = false;
        mQueuedSwaps = 0;
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

        if (Surface != null)
        {
            required++;
            TrackTween(Surface
                .DOAnchorPos(mSurfaceBasePosition + new Vector2(0f, -ExitOffset), Mathf.Max(0.08f, TransitionDuration))
                .SetEase(Ease.InCubic)
                .SetUpdate(UseUnscaledTime)
                .OnComplete(TryFinish));
        }

        if (EffectCanvasGroup != null)
        {
            required++;
            TrackTween(EffectCanvasGroup
                .DOFade(0f, Mathf.Max(0.08f, TransitionDuration))
                .SetEase(Ease.InCubic)
                .SetUpdate(UseUnscaledTime)
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
        CacheCards();
        ApplyTexts();
        mSwapInProgress = false;
        mQueuedSwaps = 0;
        mPointerInside = false;
        mHasPointer = false;
        mHoverPaused = false;
        mSwapTimer = Mathf.Max(0f, SwapDelay) * 0.3f;
        ApplySlotLayout(false, 0f);

        if (Surface != null)
        {
            Surface.anchoredPosition = mSurfaceBasePosition;
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
            case "swap_delay":
                value = SwapDelay;
                return true;
            case "card_distance":
                value = CardDistance;
                return true;
            case "out_duration":
                value = OutDuration;
                return true;
            case "layout_duration":
                value = LayoutDuration;
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
            case "swap_delay":
                SwapDelay = Mathf.Clamp(value, 0.2f, 10f);
                return true;
            case "card_distance":
                CardDistance = Mathf.Clamp(value, 20f, 240f);
                ApplySlotLayout(true, Mathf.Max(0.05f, LayoutDuration * 0.5f));
                return true;
            case "out_duration":
                OutDuration = Mathf.Clamp(value, 0.05f, 1.2f);
                return true;
            case "layout_duration":
                LayoutDuration = Mathf.Clamp(value, 0.05f, 2f);
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
            default:
                return false;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        mPointerInside = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        mPointerInside = false;
        mHasPointer = false;
        mHoverPaused = false;
    }

    public void OnPointerMove(PointerEventData eventData)
    {
        if (eventData == null)
        {
            return;
        }

        var target = EnsureBindings();
        if (target == null)
        {
            return;
        }

        var safeCam = ResolveInteractionCamera(eventData);
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(target, eventData.position, safeCam, out var local))
        {
            mPointerLocal = local;
            mHasPointer = true;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData == null || mCards.Count <= 0)
        {
            return;
        }

        if (mSwapInProgress)
        {
            if (IsPointerInsideInteractionRange(eventData))
            {
                EnqueueSwapRequest(true);
            }

            return;
        }

        var front = mCards[0];
        if (front == null || front.Rect == null)
        {
            return;
        }

        var safeCam = ResolveInteractionCamera(eventData);
        if (!RectTransformUtility.RectangleContainsScreenPoint(front.Rect, eventData.position, safeCam))
        {
            return;
        }

        EnqueueSwapRequest(true);
    }

    private Camera ResolveInteractionCamera(PointerEventData eventData = null)
    {
        var target = InteractionHitSource != null
            ? InteractionHitSource
            : (Surface != null ? Surface : (EffectRoot != null ? EffectRoot : transform as RectTransform));

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
            : (Surface != null ? Surface : (EffectRoot != null ? EffectRoot : transform as RectTransform));
        DrawInteractionRangeGizmo(ShowInteractionRange, hitSource, InteractionRangeDependency, InteractionRangePadding);
    }

    private RectTransform EnsureBindings()
    {
        if (Backdrop == null && EffectRoot != null)
        {
            var t = EffectRoot.Find("Backdrop");
            if (t != null)
            {
                Backdrop = t as RectTransform;
            }
        }

        if (Surface == null && Backdrop != null)
        {
            var t = Backdrop.Find("Surface");
            if (t != null)
            {
                Surface = t as RectTransform;
            }
        }

        if (Deck == null && Surface != null)
        {
            var t = Surface.Find("Deck");
            if (t != null)
            {
                Deck = t as RectTransform;
            }
        }

        if (HintLabel == null && Backdrop != null)
        {
            var hint = Backdrop.Find("Hint");
            if (hint != null)
            {
                HintLabel = hint.GetComponent<Text>();
            }
        }

        if (InteractionHitSource == null)
        {
            InteractionHitSource = Surface != null ? Surface : (EffectRoot != null ? EffectRoot : transform as RectTransform);
        }

        if (Surface != null)
        {
            mSurfaceBasePosition = Surface.anchoredPosition;
        }

        if (string.IsNullOrWhiteSpace(EffectKey))
        {
            EffectKey = "card-swap-v3";
        }

        if (string.IsNullOrWhiteSpace(EffectDisplayName))
        {
            EffectDisplayName = "CardSwap V3";
        }

        if (string.IsNullOrWhiteSpace(UsageDescription))
        {
            UsageDescription = "卡片自动轮换，点击顶层卡片可立即触发交换。";
        }

        return InteractionHitSource;
    }

    private void CacheCards()
    {
        mCards.Clear();
        if (Deck == null)
        {
            return;
        }

        for (var i = 0; i < Deck.childCount; i++)
        {
            var child = Deck.GetChild(i) as RectTransform;
            if (child == null)
            {
                continue;
            }

            var caption = child.Find("Caption");
            mCards.Add(new CardRuntime
            {
                Rect = child,
                Visual = child,
                Caption = caption != null ? caption.GetComponent<Text>() : child.GetComponentInChildren<Text>(true)
            });
        }
    }

    private void ApplyTexts()
    {
        if (HintLabel != null)
        {
            HintLabel.text = string.IsNullOrWhiteSpace(Hint) ? "CardSwap  |  Auto swaps top card to back" : Hint;
        }

        var caption = string.IsNullOrWhiteSpace(Caption) ? "Click the front card to swap now" : Caption;
        for (var i = 0; i < mCards.Count; i++)
        {
            if (mCards[i] != null && mCards[i].Caption != null)
            {
                mCards[i].Caption.text = caption;
            }
        }
    }

    private void UpdateHoverPauseState()
    {
        if (!PauseOnHover || !mPointerInside || !mHasPointer || InteractionHitSource == null)
        {
            mHoverPaused = false;
            return;
        }

        var halfW = Mathf.Max(1f, InteractionHitSource.rect.width * 0.5f);
        var halfH = Mathf.Max(1f, InteractionHitSource.rect.height * 0.5f);
        mHoverPaused = Mathf.Abs(mPointerLocal.x) <= halfW && Mathf.Abs(mPointerLocal.y) <= halfH;
    }

    private bool IsPointerInsideInteractionRange(PointerEventData eventData)
    {
        if (eventData == null)
        {
            return false;
        }

        var target = EnsureBindings();
        if (target == null)
        {
            return false;
        }

        var safeCam = ResolveInteractionCamera(eventData);
        return RectTransformUtility.RectangleContainsScreenPoint(target, eventData.position, safeCam);
    }

    private void EnqueueSwapRequest(bool fromManualClick)
    {
        if (mCards.Count <= 1)
        {
            return;
        }

        var maxQueued = Mathf.Max(1, MaxQueuedSwaps);
        mQueuedSwaps = Mathf.Min(maxQueued, mQueuedSwaps + 1);
        if (fromManualClick)
        {
            mSwapTimer = 0f;
        }

        TryConsumeQueuedSwap(fromManualClick);
    }

    private void TryConsumeQueuedSwap(bool ignoreHoverPause)
    {
        if (mSwapInProgress || mQueuedSwaps <= 0 || mCards.Count <= 1)
        {
            return;
        }

        if (!ignoreHoverPause && PauseOnHover && mHoverPaused)
        {
            return;
        }

        mQueuedSwaps--;
        StartSwap();
    }

    private void StartSwap()
    {
        if (mSwapInProgress || mCards.Count <= 1)
        {
            return;
        }

        var front = mCards[0];
        if (front == null || front.Rect == null)
        {
            return;
        }

        mSwapInProgress = true;
        front.Rect.DOKill(false);
        if (front.Visual != null)
        {
            front.Visual.DOKill(false);
        }

        var outDuration = Mathf.Max(0.05f, OutDuration);
        var layoutDuration = Mathf.Max(0.05f, LayoutDuration);
        var overlap = Mathf.Clamp01(LayoutOverlap);
        var reorderDelay = outDuration * (1f - overlap);
        var delayStep = Mathf.Max(0f, LayoutDelayStep);
        var sourceCount = mCards.Count;

        TrackTween(front.Rect
            .DOAnchorPosY(front.Rect.anchoredPosition.y - Mathf.Abs(OutDistanceY), outDuration)
            .SetEase(Ease.OutQuad)
            .SetUpdate(UseUnscaledTime));

        TrackTween(DOVirtual.DelayedCall(reorderDelay, () =>
            {
                if (mCards.Count <= 1)
                {
                    return;
                }

                mCards.Remove(front);
                mCards.Add(front);
                ApplySlotLayout(true, layoutDuration);
            }, UseUnscaledTime));

        var finishDelay = reorderDelay + layoutDuration + (Mathf.Max(0, sourceCount - 1) * delayStep);
        TrackTween(DOVirtual.DelayedCall(Mathf.Max(0.05f, finishDelay), () =>
        {
            mSwapInProgress = false;
            TryConsumeQueuedSwap(false);
        }, UseUnscaledTime));
    }

    private void ApplySlotLayout(bool animate, float duration)
    {
        var count = mCards.Count;
        if (count == 0)
        {
            return;
        }

        var centerBias = (count - 1) * 0.5f;
        for (var i = 0; i < count; i++)
        {
            var card = mCards[i];
            if (card == null || card.Rect == null || card.Visual == null)
            {
                continue;
            }

            var slotPos = new Vector2(
                (i - centerBias) * CardDistance,
                (centerBias - i) * VerticalDistance * 0.72f);
            var slotScale = Mathf.Clamp(1f - (i * 0.07f), 0.70f, 1f);
            var slotRot = i * 3.5f;

            card.Rect.SetSiblingIndex(count - i - 1);
            card.Rect.DOKill(false);
            card.Visual.DOKill(false);

            if (!animate)
            {
                card.Rect.anchoredPosition = slotPos;
                card.Visual.localScale = new Vector3(slotScale, slotScale, 1f);
                card.Visual.localRotation = Quaternion.Euler(0f, 0f, slotRot);
                continue;
            }

            var delay = i * Mathf.Max(0f, LayoutDelayStep);
            TrackTween(card.Rect
                .DOAnchorPos(slotPos, duration)
                .SetDelay(delay)
                .SetEase(Ease.OutCubic)
                .SetUpdate(UseUnscaledTime));
            TrackTween(card.Visual
                .DOScale(new Vector3(slotScale, slotScale, 1f), duration)
                .SetDelay(delay)
                .SetEase(Ease.OutCubic)
                .SetUpdate(UseUnscaledTime));
            TrackTween(card.Visual
                .DOLocalRotate(new Vector3(0f, 0f, slotRot), duration)
                .SetDelay(delay)
                .SetEase(Ease.OutCubic)
                .SetUpdate(UseUnscaledTime));
        }
    }

    private sealed class CardRuntime
    {
        public RectTransform Rect;
        public RectTransform Visual;
        public Text Caption;
    }
}
