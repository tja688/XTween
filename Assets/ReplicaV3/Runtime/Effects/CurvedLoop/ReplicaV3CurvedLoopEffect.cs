using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class ReplicaV3CurvedLoopEffect : ReplicaV3EffectBase,
    IPointerDownHandler,
    IPointerUpHandler,
    IDragHandler
{
    [Header("交互范围")]
    [Tooltip("主要可交互判定组件。为空时回退到 InteractionHitSource。")]
    public RectTransform InteractionHitSource;

    [Tooltip("交互范围跟随依赖组件。为空时回退到 InteractionHitSource。")]
    public RectTransform InteractionRangeDependency;

    [Tooltip("编辑器中显示可交互范围。默认开启，可手动关闭。")]
    public bool ShowInteractionRange = true;

    [Tooltip("交互范围扩展像素。")]
    public float InteractionRangePadding = 0f;

    [Header("组件绑定（CurvedLoop）")]
    [Tooltip("过渡主体容器，通常是 Viewport。")]
    public RectTransform ContentRoot;

    [Tooltip("文字轨道容器。")]
    public RectTransform Track;

    [Tooltip("标题文本。")]
    public Text TitleLabel;

    [Header("文本参数")]
    [Tooltip("循环文字内容。")]
    public string MarqueeText = "CURVED LOOP DRAG INTERACTION";

    [Tooltip("文本重复次数。")]
    public int RepeatCount = 10;

    [Tooltip("字距。")]
    public float DefaultSpacing = 44f;

    [Tooltip("字符尺寸。")]
    public Vector2 LetterSize = new Vector2(54f, 64f);

    [Tooltip("字符字号。")]
    public int LetterFontSize = 56;

    [Header("曲线参数")]
    [Tooltip("曲线总宽度。")]
    public float CurveWidth = 1520f;

    [Tooltip("基线 Y。")]
    public float BaselineY = -20f;

    [Tooltip("弯曲幅度。")]
    public float CurveAmount = 220f;

    [Header("运动参数")]
    [Tooltip("自动滚动速度。")]
    public float Speed = 160f;

    [Tooltip("是否可拖拽交互。")]
    public bool Interactive = true;

    [Tooltip("拖拽位移缩放。")]
    public float DragScale = 1.2f;

    [Tooltip("释放后改变方向的最小拖拽阈值。")]
    public float DragDirectionThreshold = 0.1f;

    [Tooltip("是否使用非缩放时间。")]
    public bool UseUnscaledTime = true;

    [Header("过渡参数")]
    [Tooltip("PlayIn 偏移距离。")]
    public float EnterOffset = 140f;

    [Tooltip("PlayOut 偏移距离。")]
    public float ExitOffset = 140f;

    [Tooltip("进出场过渡时长。")]
    public float TransitionDuration = 0.3f;

    private readonly List<ReplicaV3ParameterDefinition> mParameters = new List<ReplicaV3ParameterDefinition>
    {
        new ReplicaV3ParameterDefinition
        {
            Id = "speed",
            DisplayName = "滚动速度",
            Description = "自动循环滚动速度。",
            Kind = ReplicaV3ParameterKind.Float,
            Min = 0f,
            Max = 800f,
            Step = 5f
        },
        new ReplicaV3ParameterDefinition
        {
            Id = "curve_amount",
            DisplayName = "弯曲幅度",
            Description = "二次曲线控制点高度。",
            Kind = ReplicaV3ParameterKind.Float,
            Min = -600f,
            Max = 600f,
            Step = 5f
        },
        new ReplicaV3ParameterDefinition
        {
            Id = "default_spacing",
            DisplayName = "字距",
            Description = "字符沿轨道分布间距。",
            Kind = ReplicaV3ParameterKind.Float,
            Min = 12f,
            Max = 140f,
            Step = 1f
        },
        new ReplicaV3ParameterDefinition
        {
            Id = "interactive",
            DisplayName = "允许拖拽",
            Description = "开启后可拖拽改变方向和偏移。",
            Kind = ReplicaV3ParameterKind.Bool
        }
    };

    private readonly List<RectTransform> mLetterRects = new List<RectTransform>();
    private readonly List<float> mLetterAdvances = new List<float>();

    private float mOffset;
    private float mDirection = -1f;
    private float mLastDragDelta;
    private bool mDragging;
    private float mLoopLength = 1f;
    private Vector2 mContentBasePosition;

    protected override void OnEffectInitialize()
    {
        EnsureBindings();
        RebuildLetters();
        ResetRuntimeState();
        SetCanvasAlpha(1f);
        SetLifecycleLooping();
    }

    protected override void OnEffectTick(float deltaTime, float unscaledDeltaTime)
    {
        var dt = UseUnscaledTime ? Mathf.Max(0f, unscaledDeltaTime) : Mathf.Max(0f, deltaTime);
        if (!mDragging)
        {
            mOffset += mDirection * Speed * dt;
        }

        LayoutLetters();
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
        ResetRuntimeState();

        if (ContentRoot != null)
        {
            ContentRoot.anchoredPosition = mContentBasePosition;
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
            case "speed":
                value = Speed;
                return true;
            case "curve_amount":
                value = CurveAmount;
                return true;
            case "default_spacing":
                value = DefaultSpacing;
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
            case "speed":
                Speed = Mathf.Clamp(value, 0f, 800f);
                return true;
            case "curve_amount":
                CurveAmount = Mathf.Clamp(value, -600f, 600f);
                LayoutLetters();
                return true;
            case "default_spacing":
                DefaultSpacing = Mathf.Clamp(value, 12f, 140f);
                RebuildLetters();
                LayoutLetters();
                return true;
            default:
                return false;
        }
    }

    public override bool TryGetBoolParameter(string parameterId, out bool value)
    {
        switch (parameterId)
        {
            case "interactive":
                value = Interactive;
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
            case "interactive":
                Interactive = value;
                return true;
            default:
                return false;
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!Interactive)
        {
            return;
        }

        mDragging = true;
        mLastDragDelta = 0f;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!Interactive || !mDragging || eventData == null)
        {
            return;
        }

        var deltaX = eventData.delta.x;
        mOffset += deltaX * DragScale;
        mLastDragDelta = deltaX;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!Interactive)
        {
            return;
        }

        mDragging = false;
        if (Mathf.Abs(mLastDragDelta) > Mathf.Abs(DragDirectionThreshold))
        {
            mDirection = mLastDragDelta >= 0f ? 1f : -1f;
        }
    }

    private void EnsureBindings()
    {
        if (ContentRoot == null && EffectRoot != null)
        {
            var viewport = EffectRoot.Find("Viewport");
            if (viewport != null)
            {
                ContentRoot = viewport as RectTransform;
            }
        }

        if (Track == null && ContentRoot != null)
        {
            var track = ContentRoot.Find("Track");
            if (track != null)
            {
                Track = track as RectTransform;
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

        if (InteractionHitSource == null)
        {
            InteractionHitSource = EffectRoot != null ? EffectRoot : transform as RectTransform;
        }

        if (ContentRoot != null)
        {
            mContentBasePosition = ContentRoot.anchoredPosition;
        }

        if (TitleLabel != null)
        {
            TitleLabel.text = "CurvedLoop  |  drag to change direction";
        }

        if (string.IsNullOrWhiteSpace(EffectKey))
        {
            EffectKey = "curved-loop-v3";
        }

        if (string.IsNullOrWhiteSpace(EffectDisplayName))
        {
            EffectDisplayName = "CurvedLoop V3";
        }

        if (string.IsNullOrWhiteSpace(UsageDescription))
        {
            UsageDescription = "文字沿二次曲线路径循环流动，支持拖拽改变方向。";
        }
    }

    private void ResetRuntimeState()
    {
        mDirection = -1f;
        mDragging = false;
        mLastDragDelta = 0f;
        mOffset = 0f;
        LayoutLetters();
    }

    private void RebuildLetters()
    {
        if (Track == null)
        {
            return;
        }

        for (var i = Track.childCount - 1; i >= 0; i--)
        {
            Destroy(Track.GetChild(i).gameObject);
        }

        mLetterRects.Clear();
        mLetterAdvances.Clear();

        var prepared = PrepareText(MarqueeText, Mathf.Max(1, RepeatCount));
        var spacing = Mathf.Max(1f, DefaultSpacing);

        for (var i = 0; i < prepared.Length; i++)
        {
            var letter = CreateLetter($"Letter_{i}", prepared[i].ToString());
            var rect = letter.rectTransform;
            rect.SetParent(Track, false);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = LetterSize;

            mLetterRects.Add(rect);
            mLetterAdvances.Add(i * spacing);
        }

        mLoopLength = Mathf.Max(1f, mLetterRects.Count * spacing);
    }

    private void LayoutLetters()
    {
        if (mLetterRects.Count == 0)
        {
            return;
        }

        var loop = Mathf.Max(1f, mLoopLength);
        var width = Mathf.Max(1f, CurveWidth);
        var start = new Vector2(-width * 0.5f, BaselineY);
        var control = new Vector2(0f, CurveAmount);
        var end = new Vector2(width * 0.5f, BaselineY);

        for (var i = 0; i < mLetterRects.Count; i++)
        {
            var advance = i < mLetterAdvances.Count ? mLetterAdvances[i] : 0f;
            var distance = Mathf.Repeat(advance + mOffset, loop);
            var t = distance / loop;

            var point = EvaluateQuadratic(start, control, end, t);
            var tangent = EvaluateQuadraticTangent(start, control, end, t);
            var angle = Mathf.Atan2(tangent.y, tangent.x) * Mathf.Rad2Deg;

            var rect = mLetterRects[i];
            if (rect == null)
            {
                continue;
            }

            rect.anchoredPosition = point;
            rect.localRotation = Quaternion.Euler(0f, 0f, angle);
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
        label.color = Color.white;
        label.raycastTarget = false;
        return label;
    }

    private static string PrepareText(string raw, int repeatCount)
    {
        var safe = string.IsNullOrWhiteSpace(raw) ? "CURVED LOOP" : raw;
        safe = safe.TrimEnd() + "\u00A0";

        var buffer = string.Empty;
        for (var i = 0; i < repeatCount; i++)
        {
            buffer += safe;
        }

        return buffer;
    }

    private static Vector2 EvaluateQuadratic(Vector2 a, Vector2 b, Vector2 c, float t)
    {
        var u = 1f - t;
        return (u * u * a) + (2f * u * t * b) + (t * t * c);
    }

    private static Vector2 EvaluateQuadraticTangent(Vector2 a, Vector2 b, Vector2 c, float t)
    {
        return (2f * (1f - t) * (b - a)) + (2f * t * (c - b));
    }

    private void OnDrawGizmos()
    {
        var hit = InteractionHitSource != null ? InteractionHitSource : (EffectRoot != null ? EffectRoot : transform as RectTransform);
        DrawInteractionRangeGizmo(ShowInteractionRange, hit, InteractionRangeDependency, InteractionRangePadding);
    }
}
