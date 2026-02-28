using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class ReplicaV3CircularTextEffect : ReplicaV3EffectBase,
    IPointerMoveHandler,
    IPointerExitHandler
{
    [Header("交互范围")]
    [Tooltip("主要可交互判定组件。为空时回退到 RingHolder。")]
    public RectTransform InteractionHitSource;

    [Tooltip("交互范围跟随依赖组件。为空时回退到 InteractionHitSource。")]
    public RectTransform InteractionRangeDependency;

    [Tooltip("编辑器中显示可交互范围。默认开启，可手动关闭。")]
    public bool ShowInteractionRange = true;

    [Tooltip("交互范围扩展像素。")]
    public float InteractionRangePadding = 0f;

    [Header("组件绑定（CircularText）")]
    [Tooltip("过渡内容根节点，通常是 RingHolder。")]
    public RectTransform ContentRoot;

    [Tooltip("圆环区域。用于悬停判定。")]
    public RectTransform RingHolder;

    [Tooltip("实际旋转的 Ring 容器。")]
    public RectTransform Ring;

    [Tooltip("顶部标题文本。")]
    public Text TitleLabel;

    [Tooltip("中心文本。")]
    public Text CoreLabel;

    [Header("文本参数")]
    [Tooltip("环形文字内容。")]
    public string CircularText = "CIRCULAR MOTION REPLICA";

    [Tooltip("旋转一圈耗时（秒）。")]
    public float SpinDuration = 20f;

    [Tooltip("字距圆半径。")]
    public float RingRadius = 170f;

    [Tooltip("单字符尺寸。")]
    public Vector2 LetterSize = new Vector2(40f, 40f);

    [Tooltip("单字符字体大小。")]
    public int LetterFontSize = 28;

    [Header("交互参数")]
    [Tooltip("悬停加速倍数。")]
    public float HoverSpeedMultiplier = 4f;

    [Tooltip("悬停时圆环缩放。")]
    public float HoverScale = 0.92f;

    [Tooltip("速度/缩放响应系数。")]
    public float Response = 9f;

    [Tooltip("是否启用悬停加速。")]
    public bool EnableHoverBoost = true;

    [Tooltip("是否顺时针旋转。")]
    public bool Clockwise = true;

    [Header("过渡参数")]
    [Tooltip("PlayIn 偏移距离。")]
    public float EnterOffset = 140f;

    [Tooltip("PlayOut 偏移距离。")]
    public float ExitOffset = 140f;

    [Tooltip("进出场过渡时长。")]
    public float TransitionDuration = 0.32f;

    [Tooltip("是否使用非缩放时间。")]
    public bool UseUnscaledTime = true;

    private readonly List<ReplicaV3ParameterDefinition> mParameters = new List<ReplicaV3ParameterDefinition>
    {
        new ReplicaV3ParameterDefinition
        {
            Id = "spin_duration",
            DisplayName = "旋转周期",
            Description = "转满 360° 的耗时。",
            Kind = ReplicaV3ParameterKind.Float,
            Min = 2f,
            Max = 60f,
            Step = 0.2f
        },
        new ReplicaV3ParameterDefinition
        {
            Id = "hover_speed_multiplier",
            DisplayName = "悬停加速倍数",
            Description = "鼠标停在圆环内时的速度倍率。",
            Kind = ReplicaV3ParameterKind.Float,
            Min = 1f,
            Max = 12f,
            Step = 0.1f
        },
        new ReplicaV3ParameterDefinition
        {
            Id = "hover_scale",
            DisplayName = "悬停缩放",
            Description = "悬停时 Ring 缩放。",
            Kind = ReplicaV3ParameterKind.Float,
            Min = 0.6f,
            Max = 1.1f,
            Step = 0.01f
        },
        new ReplicaV3ParameterDefinition
        {
            Id = "response",
            DisplayName = "响应速度",
            Description = "越大越快逼近目标速度和缩放。",
            Kind = ReplicaV3ParameterKind.Float,
            Min = 0.2f,
            Max = 20f,
            Step = 0.2f
        },
        new ReplicaV3ParameterDefinition
        {
            Id = "enable_hover_boost",
            DisplayName = "悬停加速",
            Description = "关闭后始终保持基础旋转速度。",
            Kind = ReplicaV3ParameterKind.Bool
        }
    };

    private readonly List<RectTransform> mLetters = new List<RectTransform>();

    private float mBaseSpeed;
    private float mCurrentSpeed;
    private float mTargetSpeed;
    private float mCurrentScale = 1f;
    private float mTargetScale = 1f;
    private bool mHoverInside;
    private Vector2 mContentBasePosition;

    protected override void OnEffectInitialize()
    {
        EnsureBindings();
        RebuildLetters();
        ResetMotionTargets();
        SetCanvasAlpha(1f);
        SetLifecycleLooping();
    }

    protected override void OnEffectTick(float deltaTime, float unscaledDeltaTime)
    {
        UpdateHoverFromPointer();

        var dt = UseUnscaledTime ? Mathf.Max(0f, unscaledDeltaTime) : Mathf.Max(0f, deltaTime);
        var response = Mathf.Max(0.1f, Response);
        var lerp = 1f - Mathf.Exp(-response * dt);

        mCurrentSpeed = Mathf.Lerp(mCurrentSpeed, mTargetSpeed, lerp);
        mCurrentScale = Mathf.Lerp(mCurrentScale, mTargetScale, lerp);

        if (Ring != null)
        {
            var dir = Clockwise ? 1f : -1f;
            Ring.localRotation *= Quaternion.Euler(0f, 0f, mCurrentSpeed * dt * dir);
            Ring.localScale = Vector3.one * mCurrentScale;
        }
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
        EnsureBindings();
        RebuildLetters();
        ResetMotionTargets();
        mHoverInside = false;

        if (ContentRoot != null)
        {
            ContentRoot.anchoredPosition = mContentBasePosition;
        }

        if (Ring != null)
        {
            Ring.localRotation = Quaternion.identity;
            Ring.localScale = Vector3.one;
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
            case "spin_duration":
                value = SpinDuration;
                return true;
            case "hover_speed_multiplier":
                value = HoverSpeedMultiplier;
                return true;
            case "hover_scale":
                value = HoverScale;
                return true;
            case "response":
                value = Response;
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
            case "spin_duration":
                SpinDuration = Mathf.Clamp(value, 2f, 60f);
                ResetMotionTargets();
                return true;
            case "hover_speed_multiplier":
                HoverSpeedMultiplier = Mathf.Clamp(value, 1f, 12f);
                UpdateHoverTargets(mHoverInside);
                return true;
            case "hover_scale":
                HoverScale = Mathf.Clamp(value, 0.6f, 1.1f);
                UpdateHoverTargets(mHoverInside);
                return true;
            case "response":
                Response = Mathf.Clamp(value, 0.2f, 20f);
                return true;
            default:
                return false;
        }
    }

    public override bool TryGetBoolParameter(string parameterId, out bool value)
    {
        switch (parameterId)
        {
            case "enable_hover_boost":
                value = EnableHoverBoost;
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
            case "enable_hover_boost":
                EnableHoverBoost = value;
                UpdateHoverTargets(mHoverInside);
                return true;
            default:
                return false;
        }
    }

    public void OnPointerMove(PointerEventData eventData)
    {
        if (eventData == null || RingHolder == null)
        {
            return;
        }

        var safeCam = ResolveInteractionCamera(eventData);
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(RingHolder, eventData.position, safeCam, out var local))
        {
            return;
        }

        var halfW = Mathf.Max(1f, RingHolder.rect.width * 0.5f);
        var halfH = Mathf.Max(1f, RingHolder.rect.height * 0.5f);
        var inside = Mathf.Abs(local.x) <= halfW && Mathf.Abs(local.y) <= halfH;
        if (inside != mHoverInside)
        {
            mHoverInside = inside;
            UpdateHoverTargets(mHoverInside);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!mHoverInside)
        {
            return;
        }

        mHoverInside = false;
        UpdateHoverTargets(false);
    }

    private void UpdateHoverFromPointer()
    {
        if (RingHolder == null)
        {
            if (mHoverInside)
            {
                mHoverInside = false;
                UpdateHoverTargets(false);
            }

            return;
        }

        var camera = ResolveInteractionCamera();
        var pointer = (Vector2)Input.mousePosition;
        var inside = false;

        if (RectTransformUtility.RectangleContainsScreenPoint(RingHolder, pointer, camera) &&
            RectTransformUtility.ScreenPointToLocalPointInRectangle(RingHolder, pointer, camera, out var local))
        {
            var halfW = Mathf.Max(1f, RingHolder.rect.width * 0.5f);
            var halfH = Mathf.Max(1f, RingHolder.rect.height * 0.5f);
            inside = Mathf.Abs(local.x) <= halfW && Mathf.Abs(local.y) <= halfH;
        }

        if (inside == mHoverInside)
        {
            return;
        }

        mHoverInside = inside;
        UpdateHoverTargets(inside);
    }

    private Camera ResolveInteractionCamera(PointerEventData eventData = null)
    {
        var target = InteractionHitSource != null
            ? InteractionHitSource
            : (RingHolder != null ? RingHolder : (EffectRoot != null ? EffectRoot : transform as RectTransform));

        if (eventData != null)
        {
            return ResolveReliableEventCamera(eventData, target);
        }

        return ResolveReliableEventCamera(target);
    }

    private void EnsureBindings()
    {
        if (RingHolder == null && EffectRoot != null)
        {
            var holder = EffectRoot.Find("RingHolder");
            if (holder != null)
            {
                RingHolder = holder as RectTransform;
            }
        }

        if (ContentRoot == null)
        {
            ContentRoot = RingHolder;
        }

        if (Ring == null && RingHolder != null)
        {
            var ring = RingHolder.Find("Ring");
            if (ring != null)
            {
                Ring = ring as RectTransform;
            }
        }

        if (TitleLabel == null && EffectRoot != null)
        {
            var title = EffectRoot.Find("Title");
            if (title != null)
            {
                TitleLabel = title.GetComponent<Text>();
            }
        }

        if (CoreLabel == null && RingHolder != null)
        {
            var core = RingHolder.Find("Core/CoreLabel");
            if (core != null)
            {
                CoreLabel = core.GetComponent<Text>();
            }
        }

        if (InteractionHitSource == null)
        {
            InteractionHitSource = RingHolder != null ? RingHolder : (EffectRoot != null ? EffectRoot : transform as RectTransform);
        }

        if (ContentRoot != null)
        {
            mContentBasePosition = ContentRoot.anchoredPosition;
        }

        if (TitleLabel != null)
        {
            TitleLabel.text = "CircularText  |  hover to speed up";
        }

        if (CoreLabel != null)
        {
            CoreLabel.text = "360";
        }

        if (string.IsNullOrWhiteSpace(EffectKey))
        {
            EffectKey = "circular-text-v3";
        }

        if (string.IsNullOrWhiteSpace(EffectDisplayName))
        {
            EffectDisplayName = "CircularText V3";
        }

        if (string.IsNullOrWhiteSpace(UsageDescription))
        {
            UsageDescription = "环形文字持续旋转，鼠标悬停时可触发加速和缩放反馈。";
        }
    }

    private void ResetMotionTargets()
    {
        mBaseSpeed = 360f / Mathf.Max(0.1f, SpinDuration);
        mCurrentSpeed = mBaseSpeed;
        mTargetSpeed = mBaseSpeed;
        mCurrentScale = 1f;
        mTargetScale = 1f;
        if (Ring != null)
        {
            Ring.localScale = Vector3.one;
        }
    }

    private void UpdateHoverTargets(bool inside)
    {
        if (!EnableHoverBoost)
        {
            mTargetSpeed = mBaseSpeed;
            mTargetScale = 1f;
            return;
        }

        if (inside)
        {
            mTargetSpeed = mBaseSpeed * Mathf.Max(1f, HoverSpeedMultiplier);
            mTargetScale = Mathf.Clamp(HoverScale, 0.6f, 1.1f);
        }
        else
        {
            mTargetSpeed = mBaseSpeed;
            mTargetScale = 1f;
        }
    }

    private void RebuildLetters()
    {
        if (Ring == null)
        {
            return;
        }

        for (var i = Ring.childCount - 1; i >= 0; i--)
        {
            Destroy(Ring.GetChild(i).gameObject);
        }

        mLetters.Clear();

        var safe = string.IsNullOrWhiteSpace(CircularText) ? "CIRCULAR" : CircularText;
        var chars = safe.ToCharArray();
        if (chars.Length == 0)
        {
            chars = "CIRCULAR".ToCharArray();
        }

        var radius = Mathf.Max(1f, RingRadius);
        for (var i = 0; i < chars.Length; i++)
        {
            var letter = CreateLetter($"Letter_{i}", chars[i].ToString());
            var letterRect = letter.rectTransform;
            letterRect.SetParent(Ring, false);
            letterRect.anchorMin = new Vector2(0.5f, 0.5f);
            letterRect.anchorMax = new Vector2(0.5f, 0.5f);
            letterRect.pivot = new Vector2(0.5f, 0.5f);
            letterRect.sizeDelta = LetterSize;

            var angle = (360f / chars.Length) * i * Mathf.Deg2Rad;
            var pos = new Vector2(Mathf.Sin(angle), Mathf.Cos(angle)) * radius;
            letterRect.anchoredPosition = pos;
            letterRect.localRotation = Quaternion.Euler(0f, 0f, -angle * Mathf.Rad2Deg);
            mLetters.Add(letterRect);
        }
    }

    private Text CreateLetter(string name, string text)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        var label = go.GetComponent<Text>();
        label.text = text;
        label.font = ResolveBuiltinFont();
        label.fontSize = Mathf.Max(8, LetterFontSize);
        label.fontStyle = FontStyle.Bold;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = new Color(1f, 1f, 1f, 0.96f);
        label.raycastTarget = false;
        return label;
    }

    private void OnDrawGizmos()
    {
        var hit = InteractionHitSource != null
            ? InteractionHitSource
            : (RingHolder != null ? RingHolder : (EffectRoot != null ? EffectRoot : transform as RectTransform));
        DrawInteractionRangeGizmo(ShowInteractionRange, hit, InteractionRangeDependency, InteractionRangePadding);
    }
}
