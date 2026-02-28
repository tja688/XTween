using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public sealed class ReplicaV3TargetCursorInteractable : MonoBehaviour
{
    private RectTransform mRect;

    private void Awake() => mRect = GetComponent<RectTransform>();


    private void OnEnable()
    {
        if (mRect != null) ReplicaV3TargetCursorEffect.ActiveTargets.Add(mRect);
    }


    private void OnDisable()
    {
        if (mRect != null) ReplicaV3TargetCursorEffect.ActiveTargets.Remove(mRect);
    }
}

public sealed class ReplicaV3TargetCursorEffect : ReplicaV3EffectBase
{
    public static readonly HashSet<RectTransform> ActiveTargets = new HashSet<RectTransform>();

    [Header("组件绑定")]
    [Tooltip("动效根节点，用来控制整个准星在屏幕上的坐标")]
    public RectTransform MotionRoot;
    [Tooltip("中心小圆点")]
    public RectTransform Dot;
    [Tooltip("包围盒四周的角标组件（左上、右上、右下、左下）")]
    public RectTransform[] Corners = new RectTransform[4];

    [Header("交互范围")]
    public RectTransform InteractionHitSource;
    public RectTransform InteractionRangeDependency;
    public bool ShowInteractionRange = true;
    public float InteractionRangePadding = 0f;

    [Header("Behavior 参数")]
    public float SpinDuration = 2f;
    public float HoverDuration = 0.2f;
    public bool HideDefaultCursor = true;
    public bool ParallaxOn = true;
    public float BorderWidth = 3f;
    public float CornerSize = 12f;
    [Tooltip("是否自动捕捉 Unity 的所有 Selectable 组件（Button, Toggle 等）")]
    public bool AutoDiscoverSelectables = true;


    private readonly List<ReplicaV3ParameterDefinition> mParameters = new List<ReplicaV3ParameterDefinition>
    {
        new ReplicaV3ParameterDefinition { Id = "spin_duration", DisplayName = "自旋耗时(秒)", Description = "完整旋转一圈所需的时间", Kind = ReplicaV3ParameterKind.Float, Min = 0.5f, Max = 10f, Step = 0.5f },
        new ReplicaV3ParameterDefinition { Id = "hover_duration", DisplayName = "吸附耗时", Description = "吸附到目标的过程时间", Kind = ReplicaV3ParameterKind.Float, Min = 0.05f, Max = 1f, Step = 0.05f },
        new ReplicaV3ParameterDefinition { Id = "parallax_on", DisplayName = "开启视差", Description = "是否开启吸附时的微量视差偏移", Kind = ReplicaV3ParameterKind.Bool }
    };

    private Vector2 mCurrentDotPos;
    private Vector2 mDotVelocity;
    private float mActiveStrength;
    private float mActiveVelocity;

    private float mRotationAngle;
    private float mRotationVelocity;
    private Vector3[] mWorldCorners = new Vector3[4];

    private Vector2[] mCornerCurrentPos = new Vector2[4];
    private Vector2[] mCornerVelocity = new Vector2[4];

    protected override void OnEffectInitialize()
    {
        EnsureBindings();
        SetCanvasAlpha(1f);
        SetLifecycleLooping();

        if (EffectCanvasGroup != null)
        {
            EffectCanvasGroup.interactable = false;
            EffectCanvasGroup.blocksRaycasts = false;
        }

        if (HideDefaultCursor) Cursor.visible = false;


        mCurrentDotPos = Vector2.zero;
        if (MotionRoot != null) MotionRoot.anchoredPosition = mCurrentDotPos;
        for (int i = 0; i < 4; i++)
        {
            mCornerCurrentPos[i] = GetIdleCornerPos(i);
            if (Corners != null && i < Corners.Length && Corners[i] != null)
                Corners[i].anchoredPosition = mCornerCurrentPos[i];
        }
    }

    protected override void OnEffectEnable()
    {
        if (HideDefaultCursor)
        {
            Cursor.visible = false;
        }
    }

    protected override void OnEffectDisable()
    {
        RestoreSystemCursorIfNeeded();
    }

    protected override void OnEffectDispose()
    {
        RestoreSystemCursorIfNeeded();
    }

    private Vector2 GetIdleCornerPos(int index)
    {
        float cx = CornerSize;
        switch (index)
        {
            case 0: return new Vector2(-cx, cx); // TL
            case 1: return new Vector2(cx, cx);  // TR
            case 2: return new Vector2(cx, -cx); // BR
            case 3: return new Vector2(-cx, -cx);// BL
        }
        return Vector2.zero;
    }

    protected override void OnEffectTick(float deltaTime, float unscaledDeltaTime)
    {
        EnsureBindings();
        if (MotionRoot == null || Dot == null || Corners == null || Corners.Length < 4) return;

        float dt = deltaTime;


        Vector2 localPoint = Vector2.zero;
        RectTransform rt = EffectRoot != null ? EffectRoot : transform as RectTransform;
        var camera = ResolveInternalCamera();
        RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, Input.mousePosition, camera, out localPoint);
        localPoint = ClampPointInsideRect(rt, localPoint, Mathf.Max(16f, CornerSize * 2f));

        mCurrentDotPos = Vector2.SmoothDamp(mCurrentDotPos, localPoint, ref mDotVelocity, 0.05f, float.MaxValue, dt);

        MotionRoot.anchoredPosition = mCurrentDotPos;
        Dot.anchoredPosition = Vector2.zero;

        RectTransform activeTarget = null;

        // 查找标记的 TargetCursorInteractable

        foreach (var target in ActiveTargets)
        {
            if (target != null && target.gameObject.activeInHierarchy && RectTransformUtility.RectangleContainsScreenPoint(target, Input.mousePosition, camera))
            {
                activeTarget = target;
                break;
            }
        }

        // 查找全局 Selectable
        if (activeTarget == null && AutoDiscoverSelectables)
        {
            var selectables = UnityEngine.UI.Selectable.allSelectablesArray;
            for (int i = 0; i < selectables.Length; i++)
            {
                var sel = selectables[i];
                if (sel != null && sel.interactable && sel.gameObject.activeInHierarchy)
                {
                    RectTransform sRect = sel.transform as RectTransform;
                    if (sRect != null && RectTransformUtility.RectangleContainsScreenPoint(sRect, Input.mousePosition, camera))
                    {
                        activeTarget = sRect;
                        break;
                    }
                }
            }
        }

        float targetStrength = activeTarget != null ? 1f : 0f;
        mActiveStrength = Mathf.SmoothDamp(mActiveStrength, targetStrength, ref mActiveVelocity, HoverDuration, float.MaxValue, dt);

        if (SpinDuration > 0)
        {
            if (activeTarget != null)
            {
                float nearest90 = Mathf.Round(mRotationAngle / 90f) * 90f;
                mRotationAngle = Mathf.SmoothDampAngle(mRotationAngle, nearest90, ref mRotationVelocity, 0.12f, float.MaxValue, dt);
            }
            else
            {
                mRotationAngle -= (360f / SpinDuration) * dt;
            }
            MotionRoot.localRotation = Quaternion.Euler(0, 0, mRotationAngle);
        }

        Vector2[] targetCornerPos = new Vector2[4];
        for (int i = 0; i < 4; i++) targetCornerPos[i] = GetIdleCornerPos(i);

        if (activeTarget != null)
        {
            float nearest90 = Mathf.Round(mRotationAngle / 90f) * 90f;
            int steps = Mathf.RoundToInt(nearest90 / 90f);

            activeTarget.GetWorldCorners(mWorldCorners);
            // 0: BL, 1: TL, 2: TR, 3: BR
            Vector2 bl = MotionRoot.InverseTransformPoint(mWorldCorners[0]);
            Vector2 tl = MotionRoot.InverseTransformPoint(mWorldCorners[1]);
            Vector2 tr = MotionRoot.InverseTransformPoint(mWorldCorners[2]);
            Vector2 br = MotionRoot.InverseTransformPoint(mWorldCorners[3]);

            float border = BorderWidth;
            float cxhalf = CornerSize * 0.5f;

            Vector2[] baseTargets = new Vector2[4];
            baseTargets[0] = tl + new Vector2(cxhalf - border, -cxhalf + border);

            baseTargets[1] = tr + new Vector2(-cxhalf + border, -cxhalf + border);

            baseTargets[2] = br + new Vector2(-cxhalf + border, cxhalf - border);

            baseTargets[3] = bl + new Vector2(cxhalf - border, cxhalf - border);


            if (ParallaxOn && mActiveStrength >= 0.8f)
            {
                Vector2 targetCenterLocal = MotionRoot.InverseTransformPoint(activeTarget.position);
                Vector2 parallaxOffset = -targetCenterLocal * 0.1f;
                for (int i = 0; i < 4; i++) baseTargets[i] += parallaxOffset;
            }

            for (int i = 0; i < 4; i++)
            {
                int cornerIndex = ((i + steps) % 4 + 4) % 4;
                targetCornerPos[cornerIndex] = baseTargets[i];
            }
        }

        float smoothTime = activeTarget != null ? 0.08f : 0.2f;
        for (int i = 0; i < 4; i++)
        {
            mCornerCurrentPos[i] = Vector2.SmoothDamp(mCornerCurrentPos[i], targetCornerPos[i], ref mCornerVelocity[i], smoothTime, float.MaxValue, dt);
            if (Corners[i] != null) Corners[i].anchoredPosition = mCornerCurrentPos[i];
        }
    }

    private static Vector2 ClampPointInsideRect(RectTransform rectTransform, Vector2 point, float margin)
    {
        if (rectTransform == null)
        {
            return point;
        }

        var rect = rectTransform.rect;
        var minX = rect.xMin + margin;
        var maxX = rect.xMax - margin;
        var minY = rect.yMin + margin;
        var maxY = rect.yMax - margin;

        if (minX > maxX)
        {
            var midX = (rect.xMin + rect.xMax) * 0.5f;
            minX = midX;
            maxX = midX;
        }

        if (minY > maxY)
        {
            var midY = (rect.yMin + rect.yMax) * 0.5f;
            minY = midY;
            maxY = midY;
        }

        point.x = Mathf.Clamp(point.x, minX, maxX);
        point.y = Mathf.Clamp(point.y, minY, maxY);
        return point;
    }

    protected override void OnPlayIn()
    {
        EnsureBindings();
        KillTrackedTweens(false);
        SetCanvasAlpha(0f);
        if (EffectCanvasGroup != null)
        {
            TrackTween(EffectCanvasGroup.DOFade(1f, 0.3f).SetEase(Ease.OutCubic));
        }

        if (HideDefaultCursor) Cursor.visible = false;
        SetLifecycleLooping();
    }

    protected override void OnPlayOut(Action onComplete)
    {
        KillTrackedTweens(false);
        if (EffectCanvasGroup != null)
        {
            TrackTween(EffectCanvasGroup.DOFade(0f, 0.2f).SetEase(Ease.InCubic).OnComplete(() =>
            {
                if (HideDefaultCursor) Cursor.visible = true;
                onComplete?.Invoke();
            }));
            return;
        }

        if (HideDefaultCursor) Cursor.visible = true;
        onComplete?.Invoke();
    }

    private RectTransform EnsureBindings()
    {
        if (MotionRoot == null)
        {
            var motion = transform.Find("MotionRoot");
            if (motion == null && EffectRoot != null)
            {
                motion = EffectRoot.Find("MotionRoot");
            }

            MotionRoot = motion as RectTransform;
        }

        if (Dot == null && MotionRoot != null)
        {
            Dot = MotionRoot.Find("Dot") as RectTransform;
        }

        if (Corners == null || Corners.Length < 4)
        {
            Corners = new RectTransform[4];
        }

        if (MotionRoot != null)
        {
            for (var i = 0; i < 4; i++)
            {
                if (Corners[i] == null)
                {
                    Corners[i] = MotionRoot.Find($"Corner_{i}") as RectTransform;
                }
            }
        }

        if (InteractionHitSource == null)
        {
            InteractionHitSource = EffectRoot != null ? EffectRoot : transform as RectTransform;
        }

        if (EffectCanvasGroup != null)
        {
            EffectCanvasGroup.interactable = false;
            EffectCanvasGroup.blocksRaycasts = false;
        }

        return InteractionHitSource;
    }

    private void RestoreSystemCursorIfNeeded()
    {
        if (HideDefaultCursor)
        {
            Cursor.visible = true;
        }
    }

    private Camera ResolveInternalCamera(PointerEventData eventData = null)
    {
        var target = InteractionHitSource != null ? InteractionHitSource : (EffectRoot != null ? EffectRoot : transform as RectTransform);
        if (eventData != null) return ResolveReliableEventCamera(eventData, target);
        return ResolveReliableEventCamera(target);
    }

    public override IReadOnlyList<ReplicaV3ParameterDefinition> GetParameterDefinitions() => mParameters;

    public override bool TryGetFloatParameter(string parameterId, out float value)
    {
        switch (parameterId)
        {
            case "spin_duration": value = SpinDuration; return true;
            case "hover_duration": value = HoverDuration; return true;
            default: value = 0f; return false;
        }
    }

    public override bool TrySetFloatParameter(string parameterId, float value)
    {
        switch (parameterId)
        {
            case "spin_duration": SpinDuration = value; return true;
            case "hover_duration": HoverDuration = value; return true;
            default: return false;
        }
    }

    public override bool TryGetBoolParameter(string parameterId, out bool value)
    {
        if (parameterId == "parallax_on") { value = ParallaxOn; return true; }
        value = false;
        return false;
    }

    public override bool TrySetBoolParameter(string parameterId, bool value)
    {
        if (parameterId == "parallax_on") { ParallaxOn = value; return true; }
        return false;
    }

    private void OnDrawGizmos()
    {
        var hitSource = InteractionHitSource != null ? InteractionHitSource : (EffectRoot != null ? EffectRoot : transform as RectTransform);
        DrawInteractionRangeGizmo(ShowInteractionRange, hitSource, InteractionRangeDependency, InteractionRangePadding);
    }
}
