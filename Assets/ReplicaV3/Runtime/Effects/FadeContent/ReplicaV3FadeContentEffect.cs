using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public sealed class ReplicaV3FadeContentEffect : ReplicaV3EffectBase
{
    [Header("组件绑定（FadeContent）")]
    [Tooltip("卡片节点。")]
    public RectTransform Card;

    [Tooltip("标题文本。")]
    public Text TitleText;

    [Tooltip("正文文本。")]
    public Text BodyText;

    [Header("文案")]
    [Tooltip("标题文案。")]
    public string Title = "Fade Content";

    [Tooltip("正文文案。")]
    public string Body = "Basic but essential transition wrapper.";

    [Header("参数（可在参数面板实时调）")]
    [Range(0f, 1f)]
    [Tooltip("初始透明度。")]
    public float InitialOpacity = 0f;

    [Tooltip("是否启用缩放模拟模糊。")]
    public bool SimulateBlurWithScale = false;

    [Tooltip("缩放模拟模糊起始倍率。")]
    public float BlurStartScale = 1.02f;

    [Tooltip("自动消失延迟（秒）。")]
    public float DisappearAfter = 0f;

    [Tooltip("自动消失时长（秒）。")]
    public float DisappearDuration = 0.5f;

    [Tooltip("进入偏移。")]
    public Vector2 EnterOffset = new Vector2(0f, -140f);

    [Tooltip("退出偏移。")]
    public Vector2 ExitOffset = new Vector2(0f, 140f);

    [Tooltip("进入时长。")]
    public float EnterDuration = 0.6f;

    [Tooltip("退出时长。")]
    public float ExitDuration = 0.4f;

    private readonly List<ReplicaV3ParameterDefinition> mParameters = new List<ReplicaV3ParameterDefinition>
    {
        new ReplicaV3ParameterDefinition { Id = "initial_opacity", DisplayName = "初始透明度", Description = "进入前透明度基准。", Kind = ReplicaV3ParameterKind.Float, Min = 0f, Max = 1f, Step = 0.01f },
        new ReplicaV3ParameterDefinition { Id = "blur_start_scale", DisplayName = "模糊起始缩放", Description = "开启模拟模糊时的起始缩放。", Kind = ReplicaV3ParameterKind.Float, Min = 0.6f, Max = 1.3f, Step = 0.01f },
        new ReplicaV3ParameterDefinition { Id = "disappear_after", DisplayName = "自动消失延迟", Description = "0 表示不自动消失。", Kind = ReplicaV3ParameterKind.Float, Min = 0f, Max = 10f, Step = 0.1f },
        new ReplicaV3ParameterDefinition { Id = "simulate_blur", DisplayName = "模拟模糊", Description = "开启后进入时会从缩放值恢复到 1。", Kind = ReplicaV3ParameterKind.Bool }
    };

    private Tween mDisappearTween;

    protected override void OnEffectInitialize()
    {
        ApplyLabels();
        SetCanvasAlpha(1f);
        SetLifecycleLooping();
    }

    protected override void OnPlayIn()
    {
        KillTrackedTweens(false);
        KillDisappearTween();

        var duration = Mathf.Max(0.05f, EnterDuration);
        if (Card != null)
        {
            Card.anchoredPosition = EnterOffset;
            Card.localScale = SimulateBlurWithScale
                ? Vector3.one * Mathf.Max(0.6f, BlurStartScale)
                : Vector3.one;

            TrackTween(Card.DOAnchorPos(Vector2.zero, duration).SetEase(Ease.OutCubic));
            if (SimulateBlurWithScale)
            {
                TrackTween(Card.DOScale(Vector3.one, duration).SetEase(Ease.OutCubic));
            }
        }

        if (EffectCanvasGroup != null)
        {
            EffectCanvasGroup.alpha = Mathf.Clamp01(InitialOpacity);
            TrackTween(EffectCanvasGroup.DOFade(1f, duration).SetEase(Ease.OutCubic));
        }

        if (DisappearAfter > 0f)
        {
            mDisappearTween = TrackTween(DOVirtual.DelayedCall(Mathf.Max(0f, DisappearAfter), () =>
            {
                var cached = ExitDuration;
                ExitDuration = Mathf.Max(0.05f, DisappearDuration);
                PlayOut(() => ExitDuration = cached);
            }, true));
        }

        SetLifecycleLooping();
    }

    protected override void OnPlayOut(Action onComplete)
    {
        KillTrackedTweens(false);
        KillDisappearTween();

        var duration = Mathf.Max(0.05f, ExitDuration);
        var done = 0;
        var required = 0;

        void TryDone()
        {
            done++;
            if (done >= required)
            {
                onComplete?.Invoke();
            }
        }

        if (Card != null)
        {
            required++;
            TrackTween(Card.DOAnchorPos(ExitOffset, duration).SetEase(Ease.InCubic).OnComplete(TryDone));
        }

        if (EffectCanvasGroup != null)
        {
            required++;
            TrackTween(EffectCanvasGroup.DOFade(Mathf.Clamp01(InitialOpacity), duration).SetEase(Ease.InCubic).OnComplete(TryDone));
        }

        if (required == 0)
        {
            onComplete?.Invoke();
        }
    }

    protected override void OnEffectReset()
    {
        KillTrackedTweens(false);
        KillDisappearTween();
        ApplyLabels();

        if (Card != null)
        {
            Card.anchoredPosition = Vector2.zero;
            Card.localScale = Vector3.one;
        }

        SetCanvasAlpha(1f);
        SetLifecycleLooping();
    }

    public override IReadOnlyList<ReplicaV3ParameterDefinition> GetParameterDefinitions() => mParameters;

    public override bool TryGetFloatParameter(string parameterId, out float value)
    {
        switch (parameterId)
        {
            case "initial_opacity": value = InitialOpacity; return true;
            case "blur_start_scale": value = BlurStartScale; return true;
            case "disappear_after": value = DisappearAfter; return true;
            default: value = 0f; return false;
        }
    }

    public override bool TrySetFloatParameter(string parameterId, float value)
    {
        switch (parameterId)
        {
            case "initial_opacity": InitialOpacity = Mathf.Clamp01(value); return true;
            case "blur_start_scale": BlurStartScale = Mathf.Clamp(value, 0.6f, 1.3f); return true;
            case "disappear_after": DisappearAfter = Mathf.Clamp(value, 0f, 10f); return true;
            default: return false;
        }
    }

    public override bool TryGetBoolParameter(string parameterId, out bool value)
    {
        if (parameterId == "simulate_blur")
        {
            value = SimulateBlurWithScale;
            return true;
        }

        value = false;
        return false;
    }

    public override bool TrySetBoolParameter(string parameterId, bool value)
    {
        if (parameterId == "simulate_blur")
        {
            SimulateBlurWithScale = value;
            return true;
        }

        return false;
    }

    private void KillDisappearTween()
    {
        if (mDisappearTween == null)
        {
            return;
        }

        mDisappearTween.Kill(false);
        mDisappearTween = null;
    }

    private void ApplyLabels()
    {
        if (TitleText != null)
        {
            TitleText.text = string.IsNullOrWhiteSpace(Title) ? "Fade Content" : Title;
        }

        if (BodyText != null)
        {
            BodyText.text = string.IsNullOrWhiteSpace(Body) ? "Basic but essential transition wrapper." : Body;
        }
    }
}
