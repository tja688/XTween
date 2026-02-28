using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public sealed class ReplicaV3CounterRollupEffect : ReplicaV3EffectBase
{
    [Header("交互范围")]
    [Tooltip("主要可交互判定组件。为空时回退到 CounterRect。")]
    public RectTransform InteractionHitSource;

    [Tooltip("交互范围跟随依赖组件。为空时回退到 InteractionHitSource。")]
    public RectTransform InteractionRangeDependency;

    [Tooltip("编辑器中显示可交互范围。默认开启，可手动关闭。")]
    public bool ShowInteractionRange = true;

    [Tooltip("交互范围扩展像素。")]
    public float InteractionRangePadding = 0f;

    [Header("组件绑定（CounterRollup）")]
    [Tooltip("过渡主体容器，通常是 CounterPanel。")]
    public RectTransform ContentRoot;

    [Tooltip("数字列容器，通常是 Counter。")]
    public RectTransform CounterRect;

    [Tooltip("顶部提示文本。")]
    public Text HintLabel;

    [Header("数值参数")]
    [Tooltip("初始值。")]
    public int StartValue = 17284;

    [Tooltip("目标值最小值。")]
    public int MinTarget = 800;

    [Tooltip("目标值最大值。")]
    public int MaxTarget = 99999;

    [Tooltip("目标刷新间隔（秒）。")]
    public float ChangeInterval = 2.3f;

    [Tooltip("是否自动刷新目标。")]
    public bool AutoRoll = true;

    [Header("滚动参数")]
    [Tooltip("滚动时长。")]
    public float TweenDuration = 1.05f;

    [Tooltip("滚动缓动。")]
    public Ease TweenEase = Ease.OutCubic;

    [Tooltip("是否使用非缩放时间。")]
    public bool UseUnscaledTime = true;

    [Header("过渡参数")]
    [Tooltip("PlayIn 偏移距离。")]
    public float EnterOffset = 160f;

    [Tooltip("PlayOut 偏移距离。")]
    public float ExitOffset = 160f;

    [Tooltip("进出场过渡时长。")]
    public float TransitionDuration = 0.3f;

    private readonly List<ReplicaV3ParameterDefinition> mParameters = new List<ReplicaV3ParameterDefinition>
    {
        new ReplicaV3ParameterDefinition
        {
            Id = "tween_duration",
            DisplayName = "滚动时长",
            Description = "单次数字滚动动画时长。",
            Kind = ReplicaV3ParameterKind.Float,
            Min = 0.05f,
            Max = 3f,
            Step = 0.05f
        },
        new ReplicaV3ParameterDefinition
        {
            Id = "change_interval",
            DisplayName = "刷新间隔",
            Description = "自动切换目标值间隔。",
            Kind = ReplicaV3ParameterKind.Float,
            Min = 1f,
            Max = 8f,
            Step = 0.1f
        },
        new ReplicaV3ParameterDefinition
        {
            Id = "min_target",
            DisplayName = "最小目标",
            Description = "随机目标最小值。",
            Kind = ReplicaV3ParameterKind.Float,
            Min = 0f,
            Max = 999999f,
            Step = 50f
        },
        new ReplicaV3ParameterDefinition
        {
            Id = "max_target",
            DisplayName = "最大目标",
            Description = "随机目标最大值。",
            Kind = ReplicaV3ParameterKind.Float,
            Min = 0f,
            Max = 999999f,
            Step = 50f
        },
        new ReplicaV3ParameterDefinition
        {
            Id = "auto_roll",
            DisplayName = "自动滚动",
            Description = "关闭后仅保留当前值。",
            Kind = ReplicaV3ParameterKind.Bool
        }
    };

    private readonly List<DigitColumnRuntime> mColumns = new List<DigitColumnRuntime>();

    private float mDisplayedValue;
    private float mTargetTimer;
    private Tween mRollTween;
    private Vector2 mContentBasePosition;

    protected override void OnEffectInitialize()
    {
        EnsureBindings();
        CacheColumns();
        ApplyHint();

        mDisplayedValue = StartValue;
        mTargetTimer = 0f;
        RefreshDigits();

        SetCanvasAlpha(1f);
        SetLifecycleLooping();
    }

    protected override void OnEffectTick(float deltaTime, float unscaledDeltaTime)
    {
        if (!AutoRoll)
        {
            return;
        }

        var dt = UseUnscaledTime ? Mathf.Max(0f, unscaledDeltaTime) : Mathf.Max(0f, deltaTime);
        mTargetTimer += dt;
        if (mTargetTimer < Mathf.Max(1f, ChangeInterval))
        {
            return;
        }

        mTargetTimer = 0f;
        var min = Mathf.Min(MinTarget, MaxTarget);
        var max = Mathf.Max(MinTarget, MaxTarget);
        AnimateTo(Random.Range(min, max + 1));
    }

    protected override void OnPlayIn()
    {
        KillTrackedTweens(false);
        EnsureBindings();

        if (ContentRoot != null)
        {
            ContentRoot.anchoredPosition = mContentBasePosition + new Vector2(0f, EnterOffset);
            TrackTween(ContentRoot
                .DOAnchorPos(mContentBasePosition, Mathf.Max(0.08f, TransitionDuration))
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
                .DOAnchorPos(mContentBasePosition + new Vector2(0f, -ExitOffset), Mathf.Max(0.08f, TransitionDuration))
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
        KillRollTween();
        EnsureBindings();
        CacheColumns();
        ApplyHint();

        mDisplayedValue = StartValue;
        mTargetTimer = 0f;
        RefreshDigits();

        if (ContentRoot != null)
        {
            ContentRoot.anchoredPosition = mContentBasePosition;
        }

        SetCanvasAlpha(1f);
        SetLifecycleLooping();
    }

    protected override void OnEffectDispose()
    {
        KillRollTween();
    }

    public override IReadOnlyList<ReplicaV3ParameterDefinition> GetParameterDefinitions()
    {
        return mParameters;
    }

    public override bool TryGetFloatParameter(string parameterId, out float value)
    {
        switch (parameterId)
        {
            case "tween_duration":
                value = TweenDuration;
                return true;
            case "change_interval":
                value = ChangeInterval;
                return true;
            case "min_target":
                value = MinTarget;
                return true;
            case "max_target":
                value = MaxTarget;
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
            case "tween_duration":
                TweenDuration = Mathf.Clamp(value, 0.05f, 3f);
                return true;
            case "change_interval":
                ChangeInterval = Mathf.Clamp(value, 1f, 8f);
                return true;
            case "min_target":
                MinTarget = Mathf.Clamp(Mathf.RoundToInt(value), 0, 999999);
                return true;
            case "max_target":
                MaxTarget = Mathf.Clamp(Mathf.RoundToInt(value), 0, 999999);
                return true;
            default:
                return false;
        }
    }

    public override bool TryGetBoolParameter(string parameterId, out bool value)
    {
        switch (parameterId)
        {
            case "auto_roll":
                value = AutoRoll;
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
            case "auto_roll":
                AutoRoll = value;
                return true;
            default:
                return false;
        }
    }

    private void EnsureBindings()
    {
        if (ContentRoot == null && EffectRoot != null)
        {
            var panel = EffectRoot.Find("Backdrop/CounterPanel");
            if (panel != null)
            {
                ContentRoot = panel as RectTransform;
            }
        }

        if (CounterRect == null && ContentRoot != null)
        {
            var counter = ContentRoot.Find("Counter");
            if (counter != null)
            {
                CounterRect = counter as RectTransform;
            }
        }

        if (HintLabel == null && EffectRoot != null)
        {
            var hint = EffectRoot.Find("Backdrop/Hint");
            if (hint != null)
            {
                HintLabel = hint.GetComponent<Text>();
            }
        }

        if (InteractionHitSource == null)
        {
            InteractionHitSource = CounterRect != null ? CounterRect : (EffectRoot != null ? EffectRoot : transform as RectTransform);
        }

        if (ContentRoot != null)
        {
            mContentBasePosition = ContentRoot.anchoredPosition;
        }

        if (string.IsNullOrWhiteSpace(EffectKey))
        {
            EffectKey = "counter-rollup-v3";
        }

        if (string.IsNullOrWhiteSpace(EffectDisplayName))
        {
            EffectDisplayName = "CounterRollup V3";
        }

        if (string.IsNullOrWhiteSpace(UsageDescription))
        {
            UsageDescription = "数字列按位滚动变化，模拟计数器/老虎机式翻滚效果。";
        }
    }

    private void ApplyHint()
    {
        if (HintLabel != null)
        {
            HintLabel.text = "Counter  |  Digits roll with spring transitions";
        }
    }

    private void CacheColumns()
    {
        mColumns.Clear();
        if (CounterRect == null)
        {
            return;
        }

        for (var i = 0; i < CounterRect.childCount; i++)
        {
            var child = CounterRect.GetChild(i) as RectTransform;
            if (child == null)
            {
                continue;
            }

            var numbersRoot = child.Find("Numbers") as RectTransform;
            if (numbersRoot == null)
            {
                continue;
            }

            var numbers = new List<RectTransform>(10);
            for (var n = 0; n < numbersRoot.childCount; n++)
            {
                var number = numbersRoot.GetChild(n) as RectTransform;
                if (number != null)
                {
                    numbers.Add(number);
                }
            }

            if (numbers.Count == 0)
            {
                continue;
            }

            mColumns.Add(new DigitColumnRuntime
            {
                Place = ParsePlace(child.name, i),
                Root = child,
                Numbers = numbers
            });
        }
    }

    private void AnimateTo(int target)
    {
        KillRollTween();

        mRollTween = DOTween.To(
                () => mDisplayedValue,
                value =>
                {
                    mDisplayedValue = value;
                    RefreshDigits();
                },
                target,
                Mathf.Max(0.05f, TweenDuration))
            .SetEase(TweenEase)
            .SetUpdate(UseUnscaledTime);
        TrackTween(mRollTween);
    }

    private void RefreshDigits()
    {
        for (var i = 0; i < mColumns.Count; i++)
        {
            var col = mColumns[i];
            if (col == null || col.Root == null || col.Numbers == null)
            {
                continue;
            }

            var place = Mathf.Max(1, col.Place);
            var placeValue = Mathf.FloorToInt(mDisplayedValue / place) % 10;
            var digitHeight = Mathf.Max(1f, col.Root.sizeDelta.y);

            for (var n = 0; n < col.Numbers.Count; n++)
            {
                var numberRect = col.Numbers[n];
                if (numberRect == null)
                {
                    continue;
                }

                var offset = (10 + n - placeValue) % 10;
                if (offset > 5)
                {
                    offset -= 10;
                }

                numberRect.anchoredPosition = new Vector2(0f, -offset * digitHeight);
            }
        }
    }

    private void KillRollTween()
    {
        if (mRollTween != null && mRollTween.active)
        {
            mRollTween.Kill(false);
        }

        mRollTween = null;
    }

    private static int ParsePlace(string name, int fallbackIndex)
    {
        if (!string.IsNullOrWhiteSpace(name))
        {
            var token = name.Split('_');
            if (token.Length > 1 && int.TryParse(token[token.Length - 1], out var parsed))
            {
                return Mathf.Max(1, parsed);
            }
        }

        var power = Mathf.Max(0, 4 - fallbackIndex);
        return (int)Mathf.Pow(10f, power);
    }

    private void OnDrawGizmos()
    {
        var hit = InteractionHitSource != null
            ? InteractionHitSource
            : (CounterRect != null ? CounterRect : (EffectRoot != null ? EffectRoot : transform as RectTransform));
        DrawInteractionRangeGizmo(ShowInteractionRange, hit, InteractionRangeDependency, InteractionRangePadding);
    }

    private sealed class DigitColumnRuntime
    {
        public int Place;
        public RectTransform Root;
        public List<RectTransform> Numbers;
    }
}
