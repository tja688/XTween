using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class ReplicaV3GridMotionEffect : ReplicaV3EffectBase
{
    [Header("组件绑定（GridMotion）")]
    [Tooltip("主要可交互判定组件。为空时回退到 EffectRoot。")]
    public RectTransform InteractionHitSource;

    [Tooltip("交互范围跟随依赖组件。为空时回退到 InteractionHitSource。")]
    public RectTransform InteractionRangeDependency;

    [Tooltip("编辑器中显示可交互范围。默认开启，可手动关闭。")]
    public bool ShowInteractionRange = true;

    [Tooltip("交互范围扩展像素。")]
    public float InteractionRangePadding = 0f;

    [Tooltip("行容器根节点。")]
    public RectTransform RowsRoot;

    [Tooltip("每一行可移动内容节点。")]
    public List<RectTransform> RowContents = new List<RectTransform>();

    [Tooltip("行内 GridLayout。用于自适应 cell 尺寸。")]
    public List<GridLayoutGroup> RowGridLayouts = new List<GridLayoutGroup>();

    [Header("参数（可在参数面板实时调）")]
    [Tooltip("行移动最大振幅。")]
    public float MaxMoveAmount = 300f;

    [Tooltip("基础跟随时长。")]
    public float BaseFollowDuration = 0.8f;

    [Tooltip("每行惯性附加时长数组。")]
    public float[] InertiaFactors = { 0.6f, 0.4f, 0.3f, 0.2f };

    [Tooltip("网格行数（用于布局重算）。")]
    public int RowCount = 4;

    [Tooltip("网格列数（用于布局重算）。")]
    public int ColumnCount = 7;

    [Tooltip("格子间距（用于布局重算）。")]
    public float Gap = 16f;

    [Tooltip("是否使用非缩放时间。")]
    public bool UseUnscaledTime = true;

    private readonly List<ReplicaV3ParameterDefinition> mParameters = new List<ReplicaV3ParameterDefinition>
    {
        new ReplicaV3ParameterDefinition
        {
            Id = "max_move_amount",
            DisplayName = "最大位移",
            Description = "指针驱动的每行最大横向位移。",
            Kind = ReplicaV3ParameterKind.Float,
            Min = 0f,
            Max = 800f,
            Step = 2f
        },
        new ReplicaV3ParameterDefinition
        {
            Id = "base_follow_duration",
            DisplayName = "基础跟随时长",
            Description = "值越大跟随越缓。",
            Kind = ReplicaV3ParameterKind.Float,
            Min = 0.05f,
            Max = 2f,
            Step = 0.01f
        },
        new ReplicaV3ParameterDefinition
        {
            Id = "use_unscaled_time",
            DisplayName = "使用非缩放时间",
            Description = "开启后忽略 Time.timeScale。",
            Kind = ReplicaV3ParameterKind.Bool
        }
    };

    private float[] mCurrentRowX = Array.Empty<float>();
    private float mLastLayoutWidth = -1f;
    private float mLastLayoutHeight = -1f;

    protected override void OnEffectInitialize()
    {
        EnsureBindings();
        EnsureRowBuffers();
        ResetRows();

        SetCanvasAlpha(1f);
        SetLifecycleLooping();
    }

    protected override void OnEffectTick(float deltaTime, float unscaledDeltaTime)
    {
        var dt = UseUnscaledTime ? Mathf.Max(0f, unscaledDeltaTime) : Mathf.Max(0f, deltaTime);
        EnsureLayout();
        UpdateMotion(dt);
    }

    protected override void OnPlayIn()
    {
        KillTrackedTweens(false);
        SetCanvasAlpha(0f);
        if (EffectCanvasGroup != null)
        {
            TrackTween(EffectCanvasGroup
                .DOFade(1f, 0.25f)
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
                .DOFade(0f, 0.2f)
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
        EnsureRowBuffers();
        ResetRows();

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
            case "max_move_amount":
                value = MaxMoveAmount;
                return true;
            case "base_follow_duration":
                value = BaseFollowDuration;
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
            case "max_move_amount":
                MaxMoveAmount = Mathf.Clamp(value, 0f, 800f);
                return true;
            case "base_follow_duration":
                BaseFollowDuration = Mathf.Clamp(value, 0.05f, 2f);
                return true;
            default:
                return false;
        }
    }

    public override bool TryGetBoolParameter(string parameterId, out bool value)
    {
        switch (parameterId)
        {
            case "use_unscaled_time":
                value = UseUnscaledTime;
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
            case "use_unscaled_time":
                UseUnscaledTime = value;
                return true;
            default:
                return false;
        }
    }

    private void EnsureBindings()
    {
        if (RowsRoot == null && EffectRoot != null)
        {
            RowsRoot = EffectRoot.Find("Container/RowsRoot") as RectTransform;
            if (RowsRoot == null)
            {
                RowsRoot = EffectRoot.Find("RowsRoot") as RectTransform;
            }
        }

        if (InteractionHitSource == null)
        {
            InteractionHitSource = EffectRoot != null ? EffectRoot : transform as RectTransform;
        }

        AutoCollectRows();

        if (string.IsNullOrWhiteSpace(EffectKey))
        {
            EffectKey = "grid-motion-v3";
        }

        if (string.IsNullOrWhiteSpace(EffectDisplayName))
        {
            EffectDisplayName = "GridMotion V3";
        }

        if (string.IsNullOrWhiteSpace(UsageDescription))
        {
            UsageDescription = "网格行按指针横向位移产生错位惯性跟随。";
        }
    }

    private void AutoCollectRows()
    {
        if (RowsRoot == null)
        {
            return;
        }

        if (RowContents == null)
        {
            RowContents = new List<RectTransform>();
        }

        if (RowGridLayouts == null)
        {
            RowGridLayouts = new List<GridLayoutGroup>();
        }

        if (RowContents.Count == 0)
        {
            for (var i = 0; i < RowsRoot.childCount; i++)
            {
                var slot = RowsRoot.GetChild(i) as RectTransform;
                if (slot == null)
                {
                    continue;
                }

                var row = slot.childCount > 0 ? slot.GetChild(0) as RectTransform : null;
                if (row == null)
                {
                    row = slot;
                }

                if (row == null)
                {
                    continue;
                }

                RowContents.Add(row);
            }
        }

        if (RowGridLayouts.Count == 0)
        {
            for (var i = 0; i < RowContents.Count; i++)
            {
                var row = RowContents[i];
                if (row == null)
                {
                    continue;
                }

                var grid = row.GetComponent<GridLayoutGroup>();
                if (grid != null)
                {
                    RowGridLayouts.Add(grid);
                }
            }
        }
    }

    private void EnsureRowBuffers()
    {
        var count = RowContents != null ? RowContents.Count : 0;
        if (count <= 0)
        {
            mCurrentRowX = Array.Empty<float>();
            return;
        }

        if (mCurrentRowX == null || mCurrentRowX.Length != count)
        {
            mCurrentRowX = new float[count];
        }
    }

    private void ResetRows()
    {
        mLastLayoutWidth = -1f;
        mLastLayoutHeight = -1f;
        if (mCurrentRowX != null)
        {
            for (var i = 0; i < mCurrentRowX.Length; i++)
            {
                mCurrentRowX[i] = 0f;
            }
        }

        if (RowContents == null)
        {
            return;
        }

        for (var i = 0; i < RowContents.Count; i++)
        {
            var row = RowContents[i];
            if (row == null)
            {
                continue;
            }

            var pos = row.anchoredPosition;
            pos.x = 0f;
            pos.y = 0f;
            row.anchoredPosition = pos;
        }
    }

    private void EnsureLayout()
    {
        if (RowsRoot == null || RowGridLayouts == null || RowGridLayouts.Count == 0)
        {
            return;
        }

        var width = Mathf.Max(1f, RowsRoot.rect.width);
        var height = Mathf.Max(1f, RowsRoot.rect.height);
        if (Mathf.Abs(width - mLastLayoutWidth) < 0.5f && Mathf.Abs(height - mLastLayoutHeight) < 0.5f)
        {
            return;
        }

        mLastLayoutWidth = width;
        mLastLayoutHeight = height;

        var rows = Mathf.Max(1, RowCount);
        var cols = Mathf.Max(1, ColumnCount);
        var gap = Mathf.Max(0f, Gap);

        var cellW = Mathf.Max(1f, (width - (gap * (cols - 1))) / cols);
        var cellH = Mathf.Max(1f, (height - (gap * (rows - 1))) / rows);

        for (var i = 0; i < RowGridLayouts.Count; i++)
        {
            var grid = RowGridLayouts[i];
            if (grid == null)
            {
                continue;
            }

            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = cols;
            grid.spacing = new Vector2(gap, gap);
            grid.cellSize = new Vector2(cellW, cellH);
        }
    }

    private void UpdateMotion(float dt)
    {
        if (RowContents == null || RowContents.Count == 0)
        {
            return;
        }

        EnsureRowBuffers();

        var t = 0.5f;
        var hit = InteractionHitSource != null ? InteractionHitSource : (EffectRoot != null ? EffectRoot : transform as RectTransform);
        if (hit != null)
        {
            var screenPos = (Vector2)Input.mousePosition;
            if (RectTransformUtility.RectangleContainsScreenPoint(hit, screenPos, ResolveInteractionCamera()))
            {
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(hit, screenPos, ResolveInteractionCamera(), out var localPoint))
                {
                    var width = Mathf.Max(1f, hit.rect.width);
                    t = Mathf.Clamp01((localPoint.x / width) + 0.5f);
                }
            }
        }

        var maxMove = Mathf.Max(0f, MaxMoveAmount);
        var baseDuration = Mathf.Max(0.01f, BaseFollowDuration);
        var inertia = InertiaFactors != null && InertiaFactors.Length > 0 ? InertiaFactors : new[] { 0.6f, 0.4f, 0.3f, 0.2f };

        for (var i = 0; i < RowContents.Count; i++)
        {
            var row = RowContents[i];
            if (row == null)
            {
                continue;
            }

            var direction = i % 2 == 0 ? 1f : -1f;
            var targetX = ((t * maxMove) - (maxMove * 0.5f)) * direction;

            var duration = baseDuration + Mathf.Max(0f, inertia[i % inertia.Length]);
            var tau = Mathf.Max(0.01f, duration / 3f);
            var follow = 1f - Mathf.Exp(-dt / tau);

            var current = mCurrentRowX[i];
            current = Mathf.Lerp(current, targetX, Mathf.Clamp01(follow));
            mCurrentRowX[i] = current;

            var pos = row.anchoredPosition;
            pos.x = current;
            pos.y = 0f;
            row.anchoredPosition = pos;
        }
    }

    private Camera ResolveInteractionCamera(PointerEventData eventData = null)
    {
        var target = InteractionHitSource != null
            ? InteractionHitSource
            : (EffectRoot != null ? EffectRoot : transform as RectTransform);

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
            : (EffectRoot != null ? EffectRoot : transform as RectTransform);
        DrawInteractionRangeGizmo(ShowInteractionRange, hitSource, InteractionRangeDependency, InteractionRangePadding);
    }
}
