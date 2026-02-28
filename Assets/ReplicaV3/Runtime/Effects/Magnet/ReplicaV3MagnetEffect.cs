using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class ReplicaV3MagnetEffect : ReplicaV3EffectBase, IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler
{
    [Header("组件绑定（Magnet）")]
    [Tooltip("磁吸交互区域。用于计算鼠标与中心的相对偏移。")]
    public RectTransform InteractionSurface;

    [Tooltip("主要可交互判定组件。为空时回退到 InteractionSurface。")]
    public RectTransform InteractionHitSource;

    [Tooltip("交互范围跟随依赖组件。为空时回退到 InteractionHitSource。")]
    public RectTransform InteractionRangeDependency;

    [Tooltip("编辑器中显示可交互范围。默认开启，可手动关闭。")]
    public bool ShowInteractionRange = true;

    [Tooltip("交互范围扩展像素。")]
    public float InteractionRangePadding = 0f;

    [Tooltip("实际位移的容器。")]
    public RectTransform FloatingRoot;

    [Tooltip("实际旋转的容器。通常与 FloatingRoot 相同。")]
    public RectTransform TiltRoot;

    [Tooltip("按钮标题文字。")]
    public Text TitleText;

    [Tooltip("提示说明文字。")]
    public Text HintText;

    [Header("参数（可在参数面板实时调）")]
    [Tooltip("磁吸生效范围扩展像素。")]
    public float Padding = 80f;

    [Tooltip("磁吸强度。越小吸附越明显。")]
    public float MagnetStrength = 2f;

    [Tooltip("最大偏移距离（像素）。")]
    public float MaxOffset = 92f;

    [Tooltip("最大倾斜角度（度）。")]
    public float MaxTilt = 10f;

    [Tooltip("回弹时长（秒）。")]
    public float ReturnDuration = 0.36f;

    [Tooltip("跟随平滑度。越大越跟手。")]
    public float FollowSmoothing = 12f;

    [Tooltip("禁用磁吸。开启后会回到中心。")]
    public bool DisableMagnet = false;

    [Header("文案")]
    [Tooltip("按钮主标题文案。")]
    public string Title = "Hover Me";

    [Tooltip("提示文案。")]
    public string Hint = "Move pointer near the card";

    private readonly List<ReplicaV3ParameterDefinition> mParameters = new List<ReplicaV3ParameterDefinition>
    {
        new ReplicaV3ParameterDefinition
        {
            Id = "magnet_strength",
            DisplayName = "磁吸强度",
            Description = "值越小吸附越明显。",
            Kind = ReplicaV3ParameterKind.Float,
            Min = 0.5f,
            Max = 8f,
            Step = 0.1f
        },
        new ReplicaV3ParameterDefinition
        {
            Id = "max_offset",
            DisplayName = "最大偏移",
            Description = "卡片偏移极限，防止位移过大。",
            Kind = ReplicaV3ParameterKind.Float,
            Min = 10f,
            Max = 200f,
            Step = 2f
        },
        new ReplicaV3ParameterDefinition
        {
            Id = "max_tilt",
            DisplayName = "最大倾斜",
            Description = "卡片倾斜角度上限。",
            Kind = ReplicaV3ParameterKind.Float,
            Min = 0f,
            Max = 24f,
            Step = 0.5f
        },
        new ReplicaV3ParameterDefinition
        {
            Id = "return_duration",
            DisplayName = "回弹时长",
            Description = "鼠标离开后回到中心的时间。",
            Kind = ReplicaV3ParameterKind.Float,
            Min = 0.05f,
            Max = 1.2f,
            Step = 0.05f
        },
        new ReplicaV3ParameterDefinition
        {
            Id = "disable_magnet",
            DisplayName = "禁用磁吸",
            Description = "开启后卡片保持静止。",
            Kind = ReplicaV3ParameterKind.Bool
        }
    };

    private bool mPointerInside;
    private bool mHasPointer;
    private Vector2 mPointerLocal;
    private Vector2 mCurrentOffset;

    protected override void OnEffectInitialize()
    {
        EnsureInteractionBindings();
        ApplyLabels();
        SetCanvasAlpha(1f);
        StartReturnMotion(0.01f);
        SetLifecycleLooping();
    }

    protected override void OnEffectTick(float deltaTime, float unscaledDeltaTime)
    {
        if (DisableMagnet)
        {
            if ((FloatingRoot != null && FloatingRoot.anchoredPosition.sqrMagnitude > 0.0001f) ||
                (TiltRoot != null && TiltRoot.localEulerAngles.sqrMagnitude > 0.0001f))
            {
                StartReturnMotion(Mathf.Max(0.05f, ReturnDuration));
            }

            return;
        }

        if (!mPointerInside || !mHasPointer || InteractionSurface == null || FloatingRoot == null)
        {
            return;
        }

        var rect = InteractionSurface.rect;
        var halfW = Mathf.Max(1f, rect.width * 0.5f);
        var halfH = Mathf.Max(1f, rect.height * 0.5f);
        var inRange = Mathf.Abs(mPointerLocal.x) <= halfW + Mathf.Max(0f, Padding) &&
                      Mathf.Abs(mPointerLocal.y) <= halfH + Mathf.Max(0f, Padding);
        if (!inRange)
        {
            return;
        }

        var targetOffset = new Vector2(
            mPointerLocal.x / Mathf.Max(0.1f, MagnetStrength),
            mPointerLocal.y / Mathf.Max(0.1f, MagnetStrength));
        targetOffset = ClampOffset(targetOffset);

        var follow = Mathf.Max(1f, FollowSmoothing);
        var lerpT = 1f - Mathf.Exp(-follow * Mathf.Max(0f, unscaledDeltaTime));
        mCurrentOffset = Vector2.Lerp(mCurrentOffset, targetOffset, lerpT);
        FloatingRoot.anchoredPosition = mCurrentOffset;

        if (TiltRoot != null)
        {
            var normalized = MaxOffset <= 0.001f ? Vector2.zero : mCurrentOffset / MaxOffset;
            var tiltX = -normalized.y * MaxTilt;
            var tiltY = normalized.x * MaxTilt;
            TiltRoot.localRotation = Quaternion.Euler(tiltX, tiltY, 0f);
        }
    }

    protected override void OnPlayIn()
    {
        KillTrackedTweens(false);
        SetCanvasAlpha(0f);
        if (EffectCanvasGroup != null)
        {
            TrackTween(EffectCanvasGroup.DOFade(1f, 0.30f).SetEase(Ease.OutCubic));
        }

        if (FloatingRoot != null)
        {
            FloatingRoot.localScale = new Vector3(0.92f, 0.92f, 1f);
            TrackTween(FloatingRoot.DOScale(Vector3.one, 0.34f).SetEase(Ease.OutBack));
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

        if (EffectCanvasGroup != null)
        {
            required++;
            TrackTween(EffectCanvasGroup.DOFade(0f, 0.24f).SetEase(Ease.InCubic).OnComplete(TryFinish));
        }

        if (FloatingRoot != null)
        {
            required++;
            TrackTween(FloatingRoot.DOScale(new Vector3(0.92f, 0.92f, 1f), 0.24f).SetEase(Ease.InCubic).OnComplete(TryFinish));
        }

        if (required == 0)
        {
            onComplete?.Invoke();
        }
    }

    protected override void OnEffectReset()
    {
        EnsureInteractionBindings();
        ApplyLabels();
        mPointerInside = false;
        mHasPointer = false;
        mCurrentOffset = Vector2.zero;
        StartReturnMotion(0.01f);
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
            case "magnet_strength":
                value = MagnetStrength;
                return true;
            case "max_offset":
                value = MaxOffset;
                return true;
            case "max_tilt":
                value = MaxTilt;
                return true;
            case "return_duration":
                value = ReturnDuration;
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
            case "magnet_strength":
                MagnetStrength = Mathf.Clamp(value, 0.5f, 8f);
                return true;
            case "max_offset":
                MaxOffset = Mathf.Clamp(value, 10f, 200f);
                return true;
            case "max_tilt":
                MaxTilt = Mathf.Clamp(value, 0f, 24f);
                return true;
            case "return_duration":
                ReturnDuration = Mathf.Clamp(value, 0.05f, 1.2f);
                return true;
            default:
                return false;
        }
    }

    public override bool TryGetBoolParameter(string parameterId, out bool value)
    {
        switch (parameterId)
        {
            case "disable_magnet":
                value = DisableMagnet;
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
            case "disable_magnet":
                DisableMagnet = value;
                if (DisableMagnet)
                {
                    StartReturnMotion(Mathf.Max(0.05f, ReturnDuration));
                }

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
        StartReturnMotion(Mathf.Max(0.05f, ReturnDuration));
    }

    public void OnPointerMove(PointerEventData eventData)
    {
        var surface = EnsureInteractionBindings();
        if (surface == null || eventData == null)
        {
            return;
        }

        var safeCam = ResolveReliableEventCamera(eventData, surface);
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                surface,
                eventData.position,
                safeCam,
                out var local))
        {
            mPointerLocal = local;
            mHasPointer = true;
        }
    }

    private RectTransform EnsureInteractionBindings()
    {
        if (InteractionSurface == null)
        {
            InteractionSurface = InteractionHitSource != null
                ? InteractionHitSource
                : (EffectRoot != null ? EffectRoot : transform as RectTransform);
        }

        if (InteractionHitSource == null)
        {
            InteractionHitSource = InteractionSurface != null
                ? InteractionSurface
                : (EffectRoot != null ? EffectRoot : transform as RectTransform);
        }

        return InteractionSurface != null ? InteractionSurface : InteractionHitSource;
    }

    private void StartReturnMotion(float duration)
    {
        if (FloatingRoot != null)
        {
            TrackTween(FloatingRoot.DOAnchorPos(Vector2.zero, Mathf.Max(0.01f, duration)).SetEase(Ease.OutQuad));
        }

        if (TiltRoot != null)
        {
            TrackTween(TiltRoot.DOLocalRotate(Vector3.zero, Mathf.Max(0.01f, duration)).SetEase(Ease.OutQuad));
        }

        mCurrentOffset = Vector2.zero;
    }

    private Vector2 ClampOffset(Vector2 offset)
    {
        var max = Mathf.Max(0f, MaxOffset);
        if (max <= 0.001f)
        {
            return offset;
        }

        var mag = offset.magnitude;
        if (mag <= max || mag <= 0.0001f)
        {
            return offset;
        }

        return offset * (max / mag);
    }

    private void ApplyLabels()
    {
        if (TitleText != null)
        {
            TitleText.text = string.IsNullOrWhiteSpace(Title) ? "Hover Me" : Title;
        }

        if (HintText != null)
        {
            HintText.text = string.IsNullOrWhiteSpace(Hint) ? "Move pointer near the card" : Hint;
        }
    }

    private void OnDrawGizmos()
    {
        var hitSource = InteractionHitSource != null
            ? InteractionHitSource
            : (InteractionSurface != null ? InteractionSurface : (EffectRoot != null ? EffectRoot : transform as RectTransform));
        DrawInteractionRangeGizmo(ShowInteractionRange, hitSource, InteractionRangeDependency, InteractionRangePadding);
    }
}
