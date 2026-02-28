using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public sealed class ReplicaV3CountUpEffect : ReplicaV3EffectBase
{
    [Header("组件绑定（CountUp）")]
    [Tooltip("标题文本（动效名称或用途）。")]
    public Text TitleText;

    [Tooltip("数值文本。")]
    public Text ValueText;

    [Tooltip("提示文本。")]
    public Text HintText;

    [Tooltip("数值脉冲节点（计数完成时会做轻微缩放）。")]
    public RectTransform ValuePulseRoot;

    [Tooltip("进度条 Fill Image（可选）。")]
    public Image ProgressFillImage;

    [Header("参数（可在参数面板实时调）")]
    [Tooltip("每轮计数起始值。")]
    public float StartValue = 0f;

    [Tooltip("随机目标最小值。")]
    public float RandomTargetMin = 1200f;

    [Tooltip("随机目标最大值。")]
    public float RandomTargetMax = 90000f;

    [Tooltip("单轮计数动画时长。")]
    public float CountDuration = 1.8f;

    [Tooltip("每轮计数开始前延迟。")]
    public float StartDelay = 0.15f;

    [Tooltip("自动循环时，两轮计数间隔。")]
    public float CycleInterval = 2.4f;

    [Tooltip("小数位数。")]
    public int DecimalCount = 0;

    [Tooltip("千分位分隔符。")]
    public string ThousandsSeparator = ",";

    [Tooltip("是否自动循环。")]
    public bool AutoLoop = true;

    [Tooltip("是否倒计数（从目标值回到起始值）。")]
    public bool CountDown = false;

    [Header("文案")]
    [Tooltip("标题文案。")]
    public string Title = "Count Up";

    [Tooltip("提示文案。")]
    public string Hint = "Auto retargeting demo";

    private readonly List<ReplicaV3ParameterDefinition> mParameters = new List<ReplicaV3ParameterDefinition>
    {
        new ReplicaV3ParameterDefinition
        {
            Id = "count_duration",
            DisplayName = "计数时长",
            Description = "单轮数字滚动时长。",
            Kind = ReplicaV3ParameterKind.Float,
            Min = 0.2f,
            Max = 6f,
            Step = 0.1f
        },
        new ReplicaV3ParameterDefinition
        {
            Id = "cycle_interval",
            DisplayName = "循环间隔",
            Description = "自动循环时的轮次间隔。",
            Kind = ReplicaV3ParameterKind.Float,
            Min = 0.2f,
            Max = 8f,
            Step = 0.1f
        },
        new ReplicaV3ParameterDefinition
        {
            Id = "start_delay",
            DisplayName = "开始延迟",
            Description = "每轮计数正式启动前的等待时间。",
            Kind = ReplicaV3ParameterKind.Float,
            Min = 0f,
            Max = 2f,
            Step = 0.05f
        },
        new ReplicaV3ParameterDefinition
        {
            Id = "auto_loop",
            DisplayName = "自动循环",
            Description = "关闭后只在手动 Reset/PlayIn 时执行一次。",
            Kind = ReplicaV3ParameterKind.Bool
        },
        new ReplicaV3ParameterDefinition
        {
            Id = "count_down",
            DisplayName = "倒计数模式",
            Description = "开启后从随机目标值回落到起始值。",
            Kind = ReplicaV3ParameterKind.Bool
        }
    };

    private float mDisplayedValue;
    private float mCurrentTarget;
    private float mCycleTimer;
    private bool mCounting;
    private Tween mCycleDelayTween;
    private Tween mCountTween;
    private Tween mPulseTween;

    protected override void OnEffectInitialize()
    {
        ApplyLabels();
        mDisplayedValue = StartValue;
        RefreshValueLabel();
        UpdateProgressVisual(mDisplayedValue);
        BeginCycle(true);
        SetLifecycleLooping();
    }

    protected override void OnEffectTick(float deltaTime, float unscaledDeltaTime)
    {
        if (!AutoLoop || mCounting)
        {
            return;
        }

        mCycleTimer += Mathf.Max(0f, unscaledDeltaTime);
        if (mCycleTimer < Mathf.Max(0.2f, CycleInterval))
        {
            return;
        }

        mCycleTimer = 0f;
        BeginCycle(false);
    }

    protected override void OnPlayIn()
    {
        KillTrackedTweens(false);
        SetCanvasAlpha(0f);
        if (EffectCanvasGroup != null)
        {
            TrackTween(EffectCanvasGroup.DOFade(1f, 0.32f).SetEase(Ease.OutCubic));
        }

        BeginCycle(false);
        SetLifecycleLooping();
    }

    protected override void OnPlayOut(Action onComplete)
    {
        KillTrackedTweens(false);
        KillCountTweens();
        mCounting = false;
        if (EffectCanvasGroup != null)
        {
            TrackTween(EffectCanvasGroup
                .DOFade(0f, 0.24f)
                .SetEase(Ease.InCubic)
                .OnComplete(() => onComplete?.Invoke()));
            return;
        }

        onComplete?.Invoke();
    }

    protected override void OnEffectReset()
    {
        KillTrackedTweens(false);
        KillCountTweens();
        mCycleTimer = 0f;
        mCounting = false;
        mDisplayedValue = StartValue;
        RefreshValueLabel();
        UpdateProgressVisual(mDisplayedValue);
        SetCanvasAlpha(1f);
        BeginCycle(true);
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
            case "count_duration":
                value = CountDuration;
                return true;
            case "cycle_interval":
                value = CycleInterval;
                return true;
            case "start_delay":
                value = StartDelay;
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
            case "count_duration":
                CountDuration = Mathf.Clamp(value, 0.2f, 6f);
                return true;
            case "cycle_interval":
                CycleInterval = Mathf.Clamp(value, 0.2f, 8f);
                return true;
            case "start_delay":
                StartDelay = Mathf.Clamp(value, 0f, 2f);
                return true;
            default:
                return false;
        }
    }

    public override bool TryGetBoolParameter(string parameterId, out bool value)
    {
        switch (parameterId)
        {
            case "auto_loop":
                value = AutoLoop;
                return true;
            case "count_down":
                value = CountDown;
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
            case "auto_loop":
                AutoLoop = value;
                return true;
            case "count_down":
                CountDown = value;
                BeginCycle(false);
                return true;
            default:
                return false;
        }
    }

    private void BeginCycle(bool immediate)
    {
        KillCountTweens();

        mCurrentTarget = Random.Range(
            Mathf.Min(RandomTargetMin, RandomTargetMax),
            Mathf.Max(RandomTargetMin, RandomTargetMax));

        var fromValue = mDisplayedValue;
        var targetValue = CountDown ? StartValue : mCurrentTarget;
        if (!CountDown)
        {
            fromValue = Mathf.Approximately(mDisplayedValue, 0f) ? StartValue : mDisplayedValue;
        }
        else
        {
            fromValue = mCurrentTarget;
        }

        mDisplayedValue = fromValue;
        RefreshValueLabel();
        UpdateProgressVisual(mDisplayedValue);

        var delay = immediate ? 0f : Mathf.Max(0f, StartDelay);
        mCounting = true;

        mCycleDelayTween = TrackTween(DOVirtual.DelayedCall(delay, () =>
        {
            var value = fromValue;
            mCountTween = TrackTween(DOTween.To(
                    () => value,
                    v =>
                    {
                        value = v;
                        mDisplayedValue = v;
                        RefreshValueLabel();
                        UpdateProgressVisual(mDisplayedValue);
                    },
                    targetValue,
                    Mathf.Max(0.2f, CountDuration))
                .SetEase(Ease.OutCubic)
                .OnComplete(() =>
                {
                    mDisplayedValue = targetValue;
                    RefreshValueLabel();
                    UpdateProgressVisual(mDisplayedValue);
                    PlayValuePulse();
                    mCounting = false;
                }));
        }));
    }

    private void PlayValuePulse()
    {
        if (ValuePulseRoot == null)
        {
            return;
        }

        ValuePulseRoot.localScale = Vector3.one;
        if (mPulseTween != null && mPulseTween.active)
        {
            mPulseTween.Kill(false);
            mPulseTween = null;
        }

        mPulseTween = TrackTween(ValuePulseRoot
            .DOPunchScale(new Vector3(0.06f, 0.06f, 0f), 0.26f, 5, 0.6f)
            .SetEase(Ease.OutCubic));
    }

    private void KillCountTweens()
    {
        if (mCycleDelayTween != null && mCycleDelayTween.active)
        {
            mCycleDelayTween.Kill(false);
        }

        if (mCountTween != null && mCountTween.active)
        {
            mCountTween.Kill(false);
        }

        if (mPulseTween != null && mPulseTween.active)
        {
            mPulseTween.Kill(false);
        }

        mCycleDelayTween = null;
        mCountTween = null;
        mPulseTween = null;
    }

    private void RefreshValueLabel()
    {
        if (ValueText == null)
        {
            return;
        }

        ValueText.text = FormatValue(mDisplayedValue);
    }

    private void UpdateProgressVisual(float value)
    {
        if (ProgressFillImage == null || ProgressFillImage.type != Image.Type.Filled)
        {
            return;
        }

        var min = Mathf.Min(StartValue, RandomTargetMin, RandomTargetMax);
        var max = Mathf.Max(StartValue, RandomTargetMin, RandomTargetMax);
        if (Mathf.Abs(max - min) <= 0.0001f)
        {
            ProgressFillImage.fillAmount = 0f;
            return;
        }

        var normalized = Mathf.InverseLerp(min, max, value);
        ProgressFillImage.fillAmount = Mathf.Clamp01(normalized);
    }

    private string FormatValue(float value)
    {
        string raw;
        if (DecimalCount <= 0)
        {
            raw = Mathf.RoundToInt(value).ToString();
        }
        else
        {
            raw = value.ToString($"F{DecimalCount}");
        }

        if (string.IsNullOrEmpty(ThousandsSeparator))
        {
            return raw;
        }

        var sign = string.Empty;
        var number = raw;
        if (number.StartsWith("-", StringComparison.Ordinal))
        {
            sign = "-";
            number = number.Substring(1);
        }

        var dotIndex = number.IndexOf('.');
        var integerPart = dotIndex >= 0 ? number.Substring(0, dotIndex) : number;
        var fractionPart = dotIndex >= 0 ? number.Substring(dotIndex) : string.Empty;

        for (var i = integerPart.Length - 3; i > 0; i -= 3)
        {
            integerPart = integerPart.Insert(i, ThousandsSeparator);
        }

        return sign + integerPart + fractionPart;
    }

    private void ApplyLabels()
    {
        if (TitleText != null)
        {
            TitleText.text = string.IsNullOrWhiteSpace(Title) ? "Count Up" : Title;
        }

        if (HintText != null)
        {
            HintText.text = string.IsNullOrWhiteSpace(Hint) ? "Auto retargeting demo" : Hint;
        }
    }
}
