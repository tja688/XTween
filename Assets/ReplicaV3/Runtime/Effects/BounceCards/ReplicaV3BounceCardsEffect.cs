using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public sealed class ReplicaV3BounceCardsEffect : ReplicaV3EffectBase
{
    [Header("组件绑定（BounceCards）")]
    [Tooltip("主要可交互判定组件。通常绑定到卡片容器。")]
    public RectTransform InteractionHitSource;

    [Tooltip("交互范围跟随依赖组件。为空时回退到 InteractionHitSource。")]
    public RectTransform InteractionRangeDependency;

    [Tooltip("编辑器中显示可交互范围。默认开启，可手动关闭。")]
    public bool ShowInteractionRange = true;

    [Tooltip("交互范围扩展像素。")]
    public float InteractionRangePadding = 0f;

    [Tooltip("主背景节点（PlayIn/PlayOut 会位移该节点）。")]
    public RectTransform Backdrop;

    [Tooltip("卡片容器（Card_x 的父节点）。")]
    public RectTransform CardsContainer;

    [Tooltip("提示文案文本。")]
    public Text HintText;

    [Header("文案")]
    [Tooltip("提示文案。")]
    public string Hint = "BounceCards  |  Hover a card to push siblings";

    [Header("参数（可在参数面板实时调）")]
    [Tooltip("悬停时兄弟卡片在 X 方向推开距离。")]
    public float HoverPushOffset = 160f;

    [Tooltip("悬停卡片抬升高度。")]
    public float HoverLiftY = 18f;

    [Tooltip("悬停过渡时长。")]
    public float HoverDuration = 0.4f;

    [Tooltip("兄弟卡片延迟步进。")]
    public float HoverSiblingDelayStep = 0.03f;

    [Tooltip("OutBack 超调系数。")]
    public float HoverOvershoot = 1.4f;

    [Tooltip("入场首个卡片延迟。")]
    public float EntryDelay = 0.42f;

    [Tooltip("入场错峰间隔。")]
    public float EntryStagger = 0.08f;

    [Tooltip("单卡入场缩放时长。")]
    public float EntryDuration = 0.7f;

    [Tooltip("PlayIn 背景位移距离。")]
    public float EnterOffset = 220f;

    [Tooltip("PlayOut 背景位移距离。")]
    public float ExitOffset = 220f;

    [Tooltip("禁用悬停推挤。")]
    public bool DisableHover = false;

    private readonly List<ReplicaV3ParameterDefinition> mParameters = new List<ReplicaV3ParameterDefinition>
    {
        new ReplicaV3ParameterDefinition
        {
            Id = "hover_push_offset",
            DisplayName = "推挤距离",
            Description = "悬停时兄弟卡片横向偏移距离。",
            Kind = ReplicaV3ParameterKind.Float,
            Min = 0f,
            Max = 360f,
            Step = 2f
        },
        new ReplicaV3ParameterDefinition
        {
            Id = "hover_lift_y",
            DisplayName = "悬停抬升",
            Description = "悬停卡片抬升高度。",
            Kind = ReplicaV3ParameterKind.Float,
            Min = 0f,
            Max = 80f,
            Step = 1f
        },
        new ReplicaV3ParameterDefinition
        {
            Id = "hover_duration",
            DisplayName = "悬停时长",
            Description = "hover/reset 的过渡时长。",
            Kind = ReplicaV3ParameterKind.Float,
            Min = 0.05f,
            Max = 1.2f,
            Step = 0.01f
        },
        new ReplicaV3ParameterDefinition
        {
            Id = "entry_duration",
            DisplayName = "入场时长",
            Description = "单个卡片缩放入场时长。",
            Kind = ReplicaV3ParameterKind.Float,
            Min = 0.1f,
            Max = 1.5f,
            Step = 0.01f
        },
        new ReplicaV3ParameterDefinition
        {
            Id = "disable_hover",
            DisplayName = "禁用悬停",
            Description = "开启后卡片不响应鼠标悬停推挤。",
            Kind = ReplicaV3ParameterKind.Bool
        }
    };

    private readonly List<RectTransform> mCards = new List<RectTransform>();
    private readonly List<Vector2> mBaseOffsets = new List<Vector2>();
    private readonly List<float> mBaseRotations = new List<float>();
    private readonly List<Tween> mHoverTweens = new List<Tween>();

    private int mCurrentHoveredIndex = -1;
    private float mEntryBlockRemaining;

    protected override void OnEffectInitialize()
    {
        CacheBindings();
        ApplyLabels();
        ResetCardsImmediate();
        SetCanvasAlpha(1f);
        SetLifecycleLooping();
    }

    protected override void OnEffectTick(float deltaTime, float unscaledDeltaTime)
    {
        if (DisableHover)
        {
            if (mCurrentHoveredIndex >= 0)
            {
                mCurrentHoveredIndex = -1;
                AnimateReset();
            }

            return;
        }

        if (mEntryBlockRemaining > 0f)
        {
            mEntryBlockRemaining = Mathf.Max(0f, mEntryBlockRemaining - Mathf.Max(0f, unscaledDeltaTime));
            return;
        }

        var hovered = DetermineHoveredIndex();
        if (hovered == mCurrentHoveredIndex)
        {
            return;
        }

        mCurrentHoveredIndex = hovered;
        if (hovered < 0)
        {
            AnimateReset();
        }
        else
        {
            AnimateHover(hovered);
        }
    }

    protected override void OnPlayIn()
    {
        KillTrackedTweens(false);

        CacheBindings();
        ApplyLabels();
        ResetCardsImmediate();

        var duration = 0.30f;
        var basePos = new Vector2(0f, -10f);
        if (Backdrop != null)
        {
            var offset = new Vector2(0f, Mathf.Abs(EnterOffset));
            Backdrop.anchoredPosition = basePos + offset;
            TrackTween(Backdrop.DOAnchorPos(basePos, duration).SetEase(Ease.OutCubic));
        }

        SetCanvasAlpha(0f);
        if (EffectCanvasGroup != null)
        {
            TrackTween(EffectCanvasGroup.DOFade(1f, duration).SetEase(Ease.OutCubic));
        }

        PlayEntryAnimation();
        SetLifecycleLooping();
    }

    protected override void OnPlayOut(Action onComplete)
    {
        KillTrackedTweens(false);
        var duration = 0.24f;
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

        if (Backdrop != null)
        {
            required++;
            var target = new Vector2(0f, -10f) + new Vector2(0f, Mathf.Abs(ExitOffset));
            TrackTween(Backdrop.DOAnchorPos(target, duration).SetEase(Ease.InCubic).OnComplete(TryFinish));
        }

        if (EffectCanvasGroup != null)
        {
            required++;
            TrackTween(EffectCanvasGroup.DOFade(0f, duration).SetEase(Ease.InCubic).OnComplete(TryFinish));
            return;
        }

        if (required == 0)
        {
            onComplete?.Invoke();
        }
    }

    protected override void OnEffectReset()
    {
        KillTrackedTweens(false);
        CacheBindings();
        ApplyLabels();
        ResetCardsImmediate();
        if (Backdrop != null)
        {
            Backdrop.anchoredPosition = new Vector2(0f, -10f);
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
            case "hover_push_offset":
                value = HoverPushOffset;
                return true;
            case "hover_lift_y":
                value = HoverLiftY;
                return true;
            case "hover_duration":
                value = HoverDuration;
                return true;
            case "entry_duration":
                value = EntryDuration;
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
            case "hover_push_offset":
                HoverPushOffset = Mathf.Clamp(value, 0f, 360f);
                return true;
            case "hover_lift_y":
                HoverLiftY = Mathf.Clamp(value, 0f, 80f);
                return true;
            case "hover_duration":
                HoverDuration = Mathf.Clamp(value, 0.05f, 1.2f);
                return true;
            case "entry_duration":
                EntryDuration = Mathf.Clamp(value, 0.1f, 1.5f);
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
                    AnimateReset();
                }

                return true;
            default:
                return false;
        }
    }

    private void CacheBindings()
    {
        if (CardsContainer == null && InteractionHitSource != null)
        {
            CardsContainer = InteractionHitSource;
        }

        if (InteractionHitSource == null)
        {
            InteractionHitSource = CardsContainer;
        }

        mCards.Clear();
        mBaseOffsets.Clear();
        mBaseRotations.Clear();
        if (CardsContainer == null)
        {
            return;
        }

        for (var i = 0; i < CardsContainer.childCount; i++)
        {
            var child = CardsContainer.GetChild(i) as RectTransform;
            if (child == null)
            {
                continue;
            }

            mCards.Add(child);
            mBaseOffsets.Add(child.anchoredPosition);
            mBaseRotations.Add(child.localEulerAngles.z);
        }
    }

    private void ApplyLabels()
    {
        if (HintText != null)
        {
            HintText.text = string.IsNullOrWhiteSpace(Hint)
                ? "BounceCards  |  Hover a card to push siblings"
                : Hint;
        }
    }

    private int DetermineHoveredIndex()
    {
        if (mCards.Count <= 0)
        {
            return -1;
        }

        var cameraTarget = InteractionHitSource != null
            ? InteractionHitSource
            : (InteractionRangeDependency != null ? InteractionRangeDependency : CardsContainer);
        var eventCamera = ResolveReliableEventCamera(cameraTarget != null ? cameraTarget : EffectRoot);

        var screenPoint = Input.mousePosition;
        if (!IsPointerWithinInteractionRange(screenPoint, eventCamera))
        {
            return -1;
        }

        for (var i = mCards.Count - 1; i >= 0; i--)
        {
            var card = mCards[i];
            if (card == null)
            {
                continue;
            }

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(card, screenPoint, eventCamera, out var localPoint))
            {
                continue;
            }

            var rect = card.rect;
            if (localPoint.x >= rect.xMin && localPoint.x <= rect.xMax &&
                localPoint.y >= rect.yMin && localPoint.y <= rect.yMax)
            {
                return i;
            }
        }

        return -1;
    }

    private bool IsPointerWithinInteractionRange(Vector2 screenPoint, Camera eventCamera)
    {
        var target = InteractionRangeDependency != null ? InteractionRangeDependency : InteractionHitSource;
        if (target == null)
        {
            return true;
        }

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(target, screenPoint, eventCamera, out var localPoint))
        {
            return false;
        }

        var rect = target.rect;
        rect.xMin -= InteractionRangePadding;
        rect.xMax += InteractionRangePadding;
        rect.yMin -= InteractionRangePadding;
        rect.yMax += InteractionRangePadding;

        return localPoint.x >= rect.xMin && localPoint.x <= rect.xMax &&
               localPoint.y >= rect.yMin && localPoint.y <= rect.yMax;
    }

    private void PlayEntryAnimation()
    {
        var count = mCards.Count;
        var safeEntryDelay = Mathf.Max(0f, EntryDelay);
        var safeEntryStagger = Mathf.Max(0f, EntryStagger);
        var safeEntryDuration = Mathf.Max(0.1f, EntryDuration);
        mEntryBlockRemaining = count > 0
            ? safeEntryDelay + ((count - 1) * safeEntryStagger) + safeEntryDuration
            : 0f;

        for (var i = 0; i < count; i++)
        {
            var card = mCards[i];
            if (card == null)
            {
                continue;
            }

            card.localScale = Vector3.zero;
            TrackTween(card
                .DOScale(Vector3.one, safeEntryDuration)
                .SetDelay(safeEntryDelay + (i * safeEntryStagger))
                .SetEase(Ease.OutElastic));
        }
    }

    private void ResetCardsImmediate()
    {
        KillHoverTweens();

        for (var i = 0; i < mCards.Count; i++)
        {
            var card = mCards[i];
            if (card == null)
            {
                continue;
            }

            card.anchoredPosition = mBaseOffsets[i];
            card.localRotation = Quaternion.Euler(0f, 0f, mBaseRotations[i]);
            card.localScale = Vector3.one;
        }

        mCurrentHoveredIndex = -1;
        mEntryBlockRemaining = 0f;
    }

    private void AnimateHover(int hoveredIndex)
    {
        KillHoverTweens();

        var safeDuration = Mathf.Max(0.05f, HoverDuration);
        for (var i = 0; i < mCards.Count; i++)
        {
            var card = mCards[i];
            if (card == null)
            {
                continue;
            }

            if (i == hoveredIndex)
            {
                TrackHoverTween(card.DOAnchorPos(
                        mBaseOffsets[i] + new Vector2(0f, HoverLiftY),
                        safeDuration)
                    .SetEase(Ease.OutBack, Mathf.Max(0f, HoverOvershoot)));
                TrackHoverTween(card.DOLocalRotate(
                        Vector3.zero,
                        safeDuration)
                    .SetEase(Ease.OutBack, Mathf.Max(0f, HoverOvershoot)));
                continue;
            }

            var direction = i < hoveredIndex ? -1f : 1f;
            var distance = Mathf.Abs(i - hoveredIndex);
            var targetPos = mBaseOffsets[i] + new Vector2(direction * HoverPushOffset, 0f);
            var delay = Mathf.Max(0f, HoverSiblingDelayStep) * distance;
            var targetRot = new Vector3(0f, 0f, mBaseRotations[i]);

            TrackHoverTween(card.DOAnchorPos(targetPos, safeDuration)
                .SetDelay(delay)
                .SetEase(Ease.OutBack, Mathf.Max(0f, HoverOvershoot)));
            TrackHoverTween(card.DOLocalRotate(targetRot, safeDuration)
                .SetDelay(delay)
                .SetEase(Ease.OutBack, Mathf.Max(0f, HoverOvershoot)));
        }
    }

    private void AnimateReset()
    {
        KillHoverTweens();

        var safeDuration = Mathf.Max(0.05f, HoverDuration);
        for (var i = 0; i < mCards.Count; i++)
        {
            var card = mCards[i];
            if (card == null)
            {
                continue;
            }

            TrackHoverTween(card.DOAnchorPos(mBaseOffsets[i], safeDuration)
                .SetEase(Ease.OutBack, Mathf.Max(0f, HoverOvershoot)));
            TrackHoverTween(card.DOLocalRotate(new Vector3(0f, 0f, mBaseRotations[i]), safeDuration)
                .SetEase(Ease.OutBack, Mathf.Max(0f, HoverOvershoot)));
        }
    }

    private void TrackHoverTween(Tween tween)
    {
        if (tween == null)
        {
            return;
        }

        mHoverTweens.Add(tween);
        TrackTween(tween);
    }

    private void KillHoverTweens()
    {
        for (var i = mHoverTweens.Count - 1; i >= 0; i--)
        {
            var tween = mHoverTweens[i];
            if (tween != null && tween.IsActive())
            {
                tween.Kill(false);
            }
        }

        mHoverTweens.Clear();
    }

    private void OnDrawGizmos()
    {
        var hitSource = InteractionHitSource != null ? InteractionHitSource : CardsContainer;
        DrawInteractionRangeGizmo(ShowInteractionRange, hitSource, InteractionRangeDependency, InteractionRangePadding);
    }
}
