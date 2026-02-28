using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public sealed class ReplicaV3TextCursorEffect : ReplicaV3EffectBase
{
    private sealed class TrailPoint
    {
        public int Id;
        public RectTransform Rect;
        public Text Label;
        public Vector2 BasePos;
        public float Angle;
        public float RandomX;
        public float RandomY;
        public float RandomRotate;
        public float Phase;
        public bool Exiting;
        public Tween ExitFadeTween;
        public Tween ExitScaleTween;
        public Tween DestroyTween;
    }

    [Header("组件绑定")]
    [Tooltip("特效的总容器")]
    public RectTransform MotionRoot;
    [Tooltip("拖尾文字预制体节点")]
    public RectTransform TrailPointPrefab;

    [Header("交互范围")]
    public RectTransform InteractionHitSource;
    public RectTransform InteractionRangeDependency;
    public bool ShowInteractionRange = true;
    public float InteractionRangePadding = 0f;

    [Header("Behavior 参数")]
    public string DisplayText = "TextCursor";
    public float Spacing = 30f;
    [Range(1, 64)] public int MaxPoints = 64;
    public bool FollowMouseDirection = false;
    public float RemovalIntervalMs = 50f;
    public float ExitDuration = 0.4f;
    public bool RandomFloat = true;

    [Header("Float 参数")]
    public float FloatPeriod = 2f;
    public float RandomOffsetRange = 5f;
    public float RandomRotateRange = 5f;

    [Header("Transition")]
    public float EnterOffset = 160f;
    public float ExitOffset = 160f;

    private readonly List<ReplicaV3ParameterDefinition> mParameters = new List<ReplicaV3ParameterDefinition>
    {
        new ReplicaV3ParameterDefinition { Id = "spacing", DisplayName = "拖尾间距", Description = "鼠标滑动生成文字的距离间隔", Kind = ReplicaV3ParameterKind.Float, Min = 10f, Max = 100f, Step = 5f },
        new ReplicaV3ParameterDefinition { Id = "removal_interval", DisplayName = "移除间隔(ms)", Description = "停止滑动后文字移除的延迟", Kind = ReplicaV3ParameterKind.Float, Min = 10f, Max = 200f, Step = 10f }
    };

    private readonly List<TrailPoint> mPoints = new List<TrailPoint>();
    private int mNextId;
    private Vector2 mLastPointerPos;
    private bool mHasLastPointer;
    private float mIdleTime;
    private float mRemovalTimer;
    private float mTime;

    protected override void OnEffectInitialize()
    {
        EnsureBindings();
        UpdateMotionRootSize();
        SetCanvasAlpha(1f);
        SetLifecycleLooping();

        if (TrailPointPrefab != null)
        {
            TrailPointPrefab.gameObject.SetActive(false);
        }

        if (mPoints.Count == 0)
        {
            AddPoint(Vector2.zero, 0f);
        }
    }

    protected override void OnEffectTick(float deltaTime, float unscaledDeltaTime)
    {
        EnsureBindings();
        UpdateMotionRootSize();
        mTime += deltaTime;
        UpdatePointerTrail(deltaTime);
        UpdateFloat(deltaTime);
        UpdateRemoval(deltaTime);
    }

    protected override void OnEffectReset()
    {
        KillTrackedTweens(false);
        EnsureBindings();

        for (var i = mPoints.Count - 1; i >= 0; i--)
        {
            DestroyPoint(mPoints[i]);
        }

        mPoints.Clear();
        mHasLastPointer = false;
        mIdleTime = 0f;
        mRemovalTimer = 0f;
        mTime = 0f;
        mNextId = 0;

        if (MotionRoot != null)
        {
            MotionRoot.anchoredPosition = Vector2.zero;
        }

        if (TrailPointPrefab != null)
        {
            TrailPointPrefab.gameObject.SetActive(false);
        }

        AddPoint(Vector2.zero, 0f);

        SetCanvasAlpha(1f);
        SetLifecycleLooping();
    }

    protected override void OnPlayIn()
    {
        EnsureBindings();
        KillTrackedTweens(false);
        if (MotionRoot != null)
        {
            MotionRoot.anchoredPosition = new Vector2(0f, -EnterOffset);
            TrackTween(MotionRoot.DOAnchorPos(Vector2.zero, 0.4f).SetEase(Ease.OutCubic));
        }

        SetCanvasAlpha(0f);
        if (EffectCanvasGroup != null)
        {
            TrackTween(EffectCanvasGroup.DOFade(1f, 0.3f).SetEase(Ease.OutCubic));
        }

        SetLifecycleLooping();
    }

    protected override void OnPlayOut(Action onComplete)
    {
        EnsureBindings();
        KillTrackedTweens(false);
        if (MotionRoot != null)
        {
            TrackTween(MotionRoot.DOAnchorPos(new Vector2(0f, ExitOffset), 0.3f).SetEase(Ease.InCubic));
        }

        if (EffectCanvasGroup != null)
        {
            TrackTween(EffectCanvasGroup.DOFade(0f, 0.25f).SetEase(Ease.InCubic).OnComplete(() => onComplete?.Invoke()));
            return;
        }

        onComplete?.Invoke();
    }

    private void EnsureBindings()
    {
        if (EffectRoot == null)
        {
            EffectRoot = transform as RectTransform;
        }

        if (MotionRoot == null)
        {
            var motion = transform.Find("MotionRoot");
            if (motion == null && EffectRoot != null)
            {
                motion = EffectRoot.Find("MotionRoot");
            }

            MotionRoot = motion as RectTransform;
        }

        if (MotionRoot == null)
        {
            var go = new GameObject("MotionRoot", typeof(RectTransform));
            go.transform.SetParent(transform, false);
            MotionRoot = go.transform as RectTransform;
        }

        if (TrailPointPrefab == null && MotionRoot != null)
        {
            var template = MotionRoot.Find("Overlay");
            if (template != null)
            {
                TrailPointPrefab = template as RectTransform;
            }
        }

        if (TrailPointPrefab == null)
        {
            TrailPointPrefab = CreateRuntimeTrailTemplate();
        }

        if (TrailPointPrefab != null && TrailPointPrefab.GetComponent<Text>() == null)
        {
            var text = TrailPointPrefab.GetComponentInChildren<Text>(true);
            if (text == null)
            {
                var label = TrailPointPrefab.gameObject.AddComponent<Text>();
                label.font = ResolveBuiltinFont();
                label.fontSize = 24;
                label.alignment = TextAnchor.MiddleCenter;
                label.color = Color.white;
                label.raycastTarget = false;
            }
        }

        if (InteractionHitSource == null)
        {
            InteractionHitSource = MotionRoot != null ? MotionRoot : (EffectRoot != null ? EffectRoot : transform as RectTransform);
        }
    }

    private void UpdateMotionRootSize()
    {
        if (MotionRoot == null || EffectRoot == null)
        {
            return;
        }

        var size = EffectRoot.rect.size;
        if (size.x <= 0.01f || size.y <= 0.01f)
        {
            return;
        }

        MotionRoot.anchorMin = new Vector2(0.5f, 0.5f);
        MotionRoot.anchorMax = new Vector2(0.5f, 0.5f);
        MotionRoot.pivot = new Vector2(0.5f, 0.5f);
        MotionRoot.anchoredPosition = Vector2.zero;
        MotionRoot.sizeDelta = size;
    }

    private RectTransform CreateRuntimeTrailTemplate()
    {
        if (MotionRoot == null)
        {
            return null;
        }

        var go = new GameObject("TrailPointTemplate", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        go.transform.SetParent(MotionRoot, false);
        var rect = go.transform as RectTransform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(180f, 42f);

        var text = go.GetComponent<Text>();
        text.text = "Replica";
        text.font = ResolveBuiltinFont();
        text.fontSize = 24;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.raycastTarget = false;

        go.SetActive(false);
        return rect;
    }

    private void UpdatePointerTrail(float dt)
    {
        if (MotionRoot == null || TrailPointPrefab == null) return;

        Vector2 local;
        bool isPointerValid = RectTransformUtility.ScreenPointToLocalPointInRectangle(
            MotionRoot, Input.mousePosition, ResolveInternalCamera(), out local);

        if (!isPointerValid || !MotionRoot.rect.Contains(local))
        {
            mHasLastPointer = false;
            mIdleTime += dt;
            return;
        }

        bool moved = false;

        if (!mHasLastPointer || mPoints.Count == 0)
        {
            AddPoint(local, 0f);
            moved = true;
        }
        else
        {
            TrailPoint last = mPoints[mPoints.Count - 1];
            float dx = local.x - last.BasePos.x;
            float dy = local.y - last.BasePos.y;
            float dist = Mathf.Sqrt(dx * dx + dy * dy);
            float spacing = Mathf.Max(1f, Spacing);

            if (dist >= spacing)
            {
                int steps = Mathf.FloorToInt(dist / spacing);
                float angle = FollowMouseDirection ? Mathf.Atan2(dy, dx) * Mathf.Rad2Deg : 0f;
                for (int i = 1; i <= steps; i++)
                {
                    float t = (spacing * i) / dist;
                    Vector2 p = new Vector2(last.BasePos.x + dx * t, last.BasePos.y + dy * t);
                    AddPoint(p, angle);
                }
                moved = true;
            }
        }

        if (moved)
        {
            mIdleTime = 0f;
            mRemovalTimer = 0f;
        }
        else
        {
            mIdleTime += dt;
        }

        mLastPointerPos = local;
        mHasLastPointer = true;
    }

    private void AddPoint(Vector2 pos, float angle)
    {
        EnsureBindings();
        if (MotionRoot == null || TrailPointPrefab == null)
        {
            return;
        }

        int max = Mathf.Clamp(MaxPoints, 1, 64);

        GameObject pointObj = Instantiate(TrailPointPrefab.gameObject, MotionRoot);
        pointObj.SetActive(true);
        RectTransform rect = pointObj.GetComponent<RectTransform>();
        Text label = pointObj.GetComponent<Text>();
        if (label == null)
        {
            label = pointObj.GetComponentInChildren<Text>(true);
        }

        if (label == null)
        {
            label = pointObj.AddComponent<Text>();
            label.font = ResolveBuiltinFont();
            label.fontSize = 24;
            label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
        }

        if (label != null) label.text = DisplayText;
        if (label != null) label.raycastTarget = false;

        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);

        var width = label != null ? Mathf.Max(60f, label.preferredWidth + 12f) : 120f;
        var height = label != null ? Mathf.Max(24f, label.preferredHeight + 8f) : 38f;
        if (rect.sizeDelta.sqrMagnitude <= 1f)
        {
            rect.sizeDelta = new Vector2(width, height);
        }

        rect.anchoredPosition = pos;
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;

        if (FollowMouseDirection)
        {
            rect.localRotation = Quaternion.Euler(0f, 0f, angle);
        }

        TrailPoint point = new TrailPoint
        {
            Id = mNextId++,
            Rect = rect,
            Label = label,
            BasePos = pos,
            Angle = angle,
            RandomX = UnityEngine.Random.Range(-RandomOffsetRange, RandomOffsetRange),
            RandomY = UnityEngine.Random.Range(-RandomOffsetRange, RandomOffsetRange),
            RandomRotate = UnityEngine.Random.Range(-RandomRotateRange, RandomRotateRange),
            Phase = UnityEngine.Random.value * 10f,
            Exiting = false
        };

        if (label != null)
        {
            var visibleColor = label.color;
            visibleColor.a = 1f;
            label.color = visibleColor;
        }

        mPoints.Add(point);

        while (mPoints.Count > max)
        {
            RemoveOldest();
        }
    }

    private void UpdateRemoval(float dt)
    {
        if (mPoints.Count <= 1 || mIdleTime <= 0.10f) return;

        float interval = Mathf.Max(0.01f, RemovalIntervalMs / 1000f);
        mRemovalTimer += dt;
        if (mRemovalTimer < interval) return;

        mRemovalTimer = 0f;
        RemoveOldest();
    }

    private void RemoveOldest()
    {
        for (int i = 0; i < mPoints.Count; i++)
        {
            TrailPoint p = mPoints[i];
            if (p == null || p.Exiting) continue;

            p.Exiting = true;
            StartExit(p);
            mPoints.RemoveAt(i);
            return;
        }
    }

    private void StartExit(TrailPoint point)
    {
        if (point == null || point.Rect == null) return;

        float duration = Mathf.Max(0.01f, ExitDuration);

        point.ExitFadeTween?.Kill();
        point.ExitScaleTween?.Kill();
        point.DestroyTween?.Kill();

        if (point.Label != null)
        {
            point.ExitFadeTween = TrackTween(point.Label.DOFade(0f, duration).SetEase(Ease.OutCubic));
        }
        point.DestroyTween = TrackTween(DOVirtual.DelayedCall(duration + 0.02f, () => DestroyPoint(point), false));
    }

    private void DestroyPoint(TrailPoint point)
    {
        if (point == null) return;

        point.ExitFadeTween?.Kill();
        point.ExitScaleTween?.Kill();
        point.DestroyTween?.Kill();

        if (point.Rect != null && point.Rect.gameObject != null)
        {
            Destroy(point.Rect.gameObject);
        }

        point.Rect = null;
        point.Label = null;
    }

    private void UpdateFloat(float dt)
    {
        if (mPoints.Count == 0 || !RandomFloat) return;

        float period = Mathf.Max(0.1f, FloatPeriod);
        float w = (mTime / period) * (Mathf.PI * 2f);

        for (int i = 0; i < mPoints.Count; i++)
        {
            TrailPoint p = mPoints[i];
            if (p == null || p.Rect == null || p.Exiting) continue;

            float ox = Mathf.Sin(w + p.Phase) * p.RandomX;
            float oy = Mathf.Sin(w + p.Phase * 1.3f) * p.RandomY;
            float or = Mathf.Sin(w + p.Phase * 0.7f) * p.RandomRotate;

            p.Rect.anchoredPosition = p.BasePos + new Vector2(ox, oy);
            p.Rect.localRotation = Quaternion.Euler(0f, 0f, p.Angle + or);
        }
    }

    private Camera ResolveInternalCamera()
    {
        var target = InteractionHitSource != null ? InteractionHitSource : (MotionRoot != null ? MotionRoot : transform as RectTransform);
        return ResolveReliableEventCamera(target);
    }

    public override IReadOnlyList<ReplicaV3ParameterDefinition> GetParameterDefinitions() => mParameters;

    public override bool TryGetFloatParameter(string parameterId, out float value)
    {
        switch (parameterId)
        {
            case "spacing": value = Spacing; return true;
            case "removal_interval": value = RemovalIntervalMs; return true;
        }
        value = 0f;
        return false;
    }

    public override bool TrySetFloatParameter(string parameterId, float value)
    {
        switch (parameterId)
        {
            case "spacing": Spacing = value; return true;
            case "removal_interval": RemovalIntervalMs = value; return true;
        }
        return false;
    }

    public override bool TryGetBoolParameter(string parameterId, out bool value)
    {
        value = false;
        return false;
    }

    public override bool TrySetBoolParameter(string parameterId, bool value)
    {
        return false;
    }

    private void OnDrawGizmos()
    {
        var hitSource = InteractionHitSource != null ? InteractionHitSource : (MotionRoot != null ? MotionRoot : transform as RectTransform);
        DrawInteractionRangeGizmo(ShowInteractionRange, hitSource, InteractionRangeDependency, InteractionRangePadding);
    }
}
