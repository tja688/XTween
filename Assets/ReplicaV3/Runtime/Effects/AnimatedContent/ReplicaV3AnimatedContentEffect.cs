using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public sealed class ReplicaV3AnimatedContentEffect : ReplicaV3EffectBase
{
    [Header("组件绑定（AnimatedContent）")]
    [Tooltip("主要可交互判定组件。通常绑定到内容容器。")]
    public RectTransform InteractionHitSource;

    [Tooltip("交互范围跟随依赖组件。为空时回退到 InteractionHitSource。")]
    public RectTransform InteractionRangeDependency;

    [Tooltip("编辑器中显示可交互范围。默认开启，可手动关闭。")]
    public bool ShowInteractionRange = true;

    [Tooltip("交互范围扩展像素。")]
    public float InteractionRangePadding = 0f;

    [Tooltip("动效主容器。")]
    public RectTransform Container;

    [Tooltip("动效主容器透明度控制。")]
    public CanvasGroup ContainerGroup;

    [Tooltip("主文案文本。")]
    public Text LabelText;

    [Header("文案")]
    [Tooltip("主文案。")]
    public string Label = "AnimatedContent";

    [Header("参数（可在参数面板实时调）")]
    [Tooltip("入场位移距离。")]
    public float Distance = 100f;

    [Tooltip("位移轴向：true 为水平，false 为垂直。")]
    public bool Horizontal = false;

    [Tooltip("反向位移。")]
    public bool Reverse = false;

    [Tooltip("入场时长。")]
    public float Duration = 0.8f;

    [Tooltip("入场延迟。")]
    public float Delay = 0f;

    [Tooltip("初始透明度。")]
    public float InitialOpacity = 0f;

    [Tooltip("是否动画透明度。")]
    public bool AnimateOpacity = true;

    [Tooltip("初始缩放。")]
    public float InitialScale = 1f;

    [Tooltip("播放一次后锁定。")]
    public bool PlayOnce = true;

    [Tooltip("自动消失前延迟。<=0 时禁用。")]
    public float DisappearAfter = 0f;

    [Tooltip("自动消失时长。")]
    public float DisappearDuration = 0.5f;

    [Tooltip("自动消失缩放。")]
    public float DisappearScale = 0.8f;

    [Tooltip("PlayOut 位移距离。")]
    public float ExitOffset = 220f;

    private readonly List<ReplicaV3ParameterDefinition> mParameters = new List<ReplicaV3ParameterDefinition>
    {
        new ReplicaV3ParameterDefinition
        {
            Id = "distance",
            DisplayName = "位移距离",
            Description = "入场初始偏移距离。",
            Kind = ReplicaV3ParameterKind.Float,
            Min = 0f,
            Max = 480f,
            Step = 2f
        },
        new ReplicaV3ParameterDefinition
        {
            Id = "duration",
            DisplayName = "入场时长",
            Description = "内容回位与缩放时长。",
            Kind = ReplicaV3ParameterKind.Float,
            Min = 0.08f,
            Max = 2f,
            Step = 0.01f
        },
        new ReplicaV3ParameterDefinition
        {
            Id = "delay",
            DisplayName = "入场延迟",
            Description = "播放前等待时间。",
            Kind = ReplicaV3ParameterKind.Float,
            Min = 0f,
            Max = 2f,
            Step = 0.01f
        },
        new ReplicaV3ParameterDefinition
        {
            Id = "disappear_after",
            DisplayName = "消失延迟",
            Description = "<=0 为禁用自动消失。",
            Kind = ReplicaV3ParameterKind.Float,
            Min = 0f,
            Max = 6f,
            Step = 0.05f
        },
        new ReplicaV3ParameterDefinition
        {
            Id = "play_once",
            DisplayName = "只播放一次",
            Description = "开启后仅首次 PlayIn 生效。",
            Kind = ReplicaV3ParameterKind.Bool
        },
        new ReplicaV3ParameterDefinition
        {
            Id = "animate_opacity",
            DisplayName = "透明度动画",
            Description = "关闭后仅位移与缩放。",
            Kind = ReplicaV3ParameterKind.Bool
        }
    };

    private bool mPlayedOnce;
    private Tween mDelayTween;
    private Tween mDisappearTween;

    protected override void OnEffectInitialize()
    {
        AutoBind();
        ApplyLabel();
        ResetInitialState();
        SetCanvasAlpha(1f);
        SetLifecycleLooping();
    }

    protected override void OnPlayIn()
    {
        if (PlayOnce && mPlayedOnce)
        {
            SetLifecycleLooping();
            return;
        }

        mPlayedOnce = true;
        KillTrackedTweens(false);
        KillScheduledTweens();

        AutoBind();
        ApplyLabel();
        ResetInitialState();

        var delay = Mathf.Max(0f, Delay);
        if (delay > 0f)
        {
            mDelayTween = TrackTween(DOVirtual.DelayedCall(delay, StartPlayInTween));
        }
        else
        {
            StartPlayInTween();
        }

        SetLifecycleLooping();
    }

    protected override void OnPlayOut(Action onComplete)
    {
        KillTrackedTweens(false);
        KillScheduledTweens();

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

        if (Container != null)
        {
            required++;
            var target = ExitMotionOffset();
            TrackTween(Container
                .DOAnchorPos(target, Mathf.Max(0.08f, Duration))
                .SetEase(Ease.InCubic)
                .OnComplete(TryFinish));
            TrackTween(Container
                .DOScale(Vector3.one * Mathf.Max(0.01f, DisappearScale), Mathf.Max(0.08f, Duration))
                .SetEase(Ease.InCubic));
        }

        if (ContainerGroup != null)
        {
            required++;
            TrackTween(ContainerGroup
                .DOFade(0f, Mathf.Max(0.08f, Duration))
                .SetEase(Ease.InCubic)
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
        KillScheduledTweens();

        AutoBind();
        ApplyLabel();
        ResetInitialState();
        mPlayedOnce = false;

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
            case "distance":
                value = Distance;
                return true;
            case "duration":
                value = Duration;
                return true;
            case "delay":
                value = Delay;
                return true;
            case "disappear_after":
                value = DisappearAfter;
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
            case "distance":
                Distance = Mathf.Clamp(value, 0f, 480f);
                return true;
            case "duration":
                Duration = Mathf.Clamp(value, 0.08f, 2f);
                return true;
            case "delay":
                Delay = Mathf.Clamp(value, 0f, 2f);
                return true;
            case "disappear_after":
                DisappearAfter = Mathf.Clamp(value, 0f, 6f);
                return true;
            default:
                return false;
        }
    }

    public override bool TryGetBoolParameter(string parameterId, out bool value)
    {
        switch (parameterId)
        {
            case "play_once":
                value = PlayOnce;
                return true;
            case "animate_opacity":
                value = AnimateOpacity;
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
            case "play_once":
                PlayOnce = value;
                return true;
            case "animate_opacity":
                AnimateOpacity = value;
                return true;
            default:
                return false;
        }
    }

    private void AutoBind()
    {

        if (Container == null)
        {
            var containerTransform = transform.Find("Container");
            if (containerTransform != null)
            {
                Container = containerTransform as RectTransform;
            }
        }

        if (ContainerGroup == null && Container != null)
        {
            ContainerGroup = Container.GetComponent<CanvasGroup>();
        }

        if (LabelText == null)
        {
            var labelTransform = transform.Find("Container/Label");
            if (labelTransform != null)
            {
                LabelText = labelTransform.GetComponent<Text>();
            }
        }

        if (InteractionHitSource == null)
        {
            InteractionHitSource = Container != null ? Container : (EffectRoot != null ? EffectRoot : transform as RectTransform);
        }
    }

    private void StartPlayInTween()
    {
        if (Container == null)
        {
            return;
        }

        var duration = Mathf.Max(0.08f, Duration);
        Container.anchoredPosition = EnterMotionOffset();
        Container.localScale = Vector3.one * Mathf.Max(0.01f, InitialScale);

        TrackTween(Container.DOAnchorPos(Vector2.zero, duration).SetEase(Ease.OutCubic));
        TrackTween(Container.DOScale(Vector3.one, duration).SetEase(Ease.OutCubic));

        if (ContainerGroup != null)
        {
            var startAlpha = AnimateOpacity ? Mathf.Clamp01(InitialOpacity) : 1f;
            ContainerGroup.alpha = startAlpha;
            var fade = AnimateOpacity ? 1f : startAlpha;
            TrackTween(ContainerGroup.DOFade(fade, duration).SetEase(Ease.OutCubic).OnComplete(HandlePlayInComplete));
            return;
        }

        HandlePlayInComplete();
    }

    private void HandlePlayInComplete()
    {
        if (DisappearAfter <= 0f || Container == null)
        {
            return;
        }

        mDisappearTween = TrackTween(DOVirtual.DelayedCall(Mathf.Max(0f, DisappearAfter), PlayDisappear));
    }

    private void PlayDisappear()
    {
        if (Container == null)
        {
            return;
        }

        var duration = Mathf.Max(0.08f, DisappearDuration);
        TrackTween(Container.DOAnchorPos(ExitMotionOffset(), duration).SetEase(Ease.InCubic));
        TrackTween(Container.DOScale(Vector3.one * Mathf.Max(0.01f, DisappearScale), duration).SetEase(Ease.InCubic));

        if (ContainerGroup != null)
        {
            var targetAlpha = AnimateOpacity ? Mathf.Clamp01(InitialOpacity) : 0f;
            TrackTween(ContainerGroup.DOFade(targetAlpha, duration).SetEase(Ease.InCubic));
        }
    }

    private void ResetInitialState()
    {
        if (Container != null)
        {
            Container.anchoredPosition = EnterMotionOffset();
            Container.localScale = Vector3.one * Mathf.Max(0.01f, InitialScale);
        }

        if (ContainerGroup != null)
        {
            ContainerGroup.alpha = AnimateOpacity ? Mathf.Clamp01(InitialOpacity) : 1f;
        }
    }

    private Vector2 EnterMotionOffset()
    {
        var amount = Mathf.Max(0f, Distance);
        if (Horizontal)
        {
            return new Vector2(Reverse ? -amount : amount, 0f);
        }

        return new Vector2(0f, Reverse ? amount : -amount);
    }

    private Vector2 ExitMotionOffset()
    {
        var amount = Mathf.Max(0f, ExitOffset);
        if (Horizontal)
        {
            return new Vector2(Reverse ? amount : -amount, 0f);
        }

        return new Vector2(0f, Reverse ? -amount : amount);
    }

    private void ApplyLabel()
    {
        if (LabelText != null)
        {
            LabelText.text = string.IsNullOrWhiteSpace(Label) ? "AnimatedContent" : Label;
        }
    }

    private void KillScheduledTweens()
    {
        if (mDelayTween != null && mDelayTween.active)
        {
            mDelayTween.Kill(false);
        }

        if (mDisappearTween != null && mDisappearTween.active)
        {
            mDisappearTween.Kill(false);
        }

        mDelayTween = null;
        mDisappearTween = null;
    }

    private void OnDrawGizmos()
    {
        var hitSource = InteractionHitSource != null
            ? InteractionHitSource
            : (Container != null ? Container : (EffectRoot != null ? EffectRoot : transform as RectTransform));
        DrawInteractionRangeGizmo(ShowInteractionRange, hitSource, InteractionRangeDependency, InteractionRangePadding);
    }
}
