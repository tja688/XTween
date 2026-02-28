using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public abstract class ReplicaV3EffectBase : MonoBehaviour, IReplicaV3ParameterSource
{
    private static readonly List<ReplicaV3ParameterDefinition> sEmptyParameters = new List<ReplicaV3ParameterDefinition>(0);

    [Header("基础标识")]
    [Tooltip("动效唯一键。跨项目复用时建议保持稳定，便于脚本和配置识别。")]
    public string EffectKey = "replica-v3-effect";

    [Tooltip("动效展示名称。用于列表按钮、调试说明面板。")]
    public string EffectDisplayName = "Replica V3 Effect";

    [TextArea(3, 10)]
    [Tooltip("这个动效的中文使用说明，会在参数面板中展示。")]
    public string UsageDescription = "右侧参数面板可实时调参，Context Menu 支持演示/纯净模式切换。";

    [Header("核心挂点")]
    [Tooltip("动效根节点。通常就是当前预制体根 RectTransform。")]
    public RectTransform EffectRoot;

    [Tooltip("统一控制透明度和交互状态的 CanvasGroup。")]
    public CanvasGroup EffectCanvasGroup;

    [Header("演示/纯净模式")]
    [Tooltip("是否默认进入演示模式。")]
    public bool StartInDemoMode = true;

    [Tooltip("演示模式专属对象。切换到纯净模式后会自动隐藏。")]
    public List<GameObject> DemoOnlyObjects = new List<GameObject>();

    [Tooltip("演示模式下需要保留原色的图形。纯净模式会统一替换成 CleanTintColor。")]
    public List<Graphic> DemoTintGraphics = new List<Graphic>();

    [Tooltip("纯净模式使用的统一视觉色。")]
    public Color CleanTintColor = new Color(0.95f, 0.95f, 0.95f, 1f);

    public ReplicaV3LifecycleState LifecycleState { get; private set; } = ReplicaV3LifecycleState.Uninitialized;
    public ReplicaV3CanvasMode CanvasMode { get; private set; } = ReplicaV3CanvasMode.Overlay;
    public bool IsDemoMode { get; private set; } = true;

    private readonly List<Tween> mTrackedTweens = new List<Tween>();
    private readonly List<ReplicaV3GraphicSnapshot> mGraphicSnapshots = new List<ReplicaV3GraphicSnapshot>();
    private readonly List<ReplicaV3ObjectSnapshot> mObjectSnapshots = new List<ReplicaV3ObjectSnapshot>();

    private bool mInitialized;
    private bool mLoggedMissingRoot;
    private bool mAutoSizedRootOnce;

    protected bool IsInitialized => mInitialized;
    protected bool IsPaused => LifecycleState == ReplicaV3LifecycleState.Paused;

    private void Awake()
    {
        EnsureBaseReferences();
        CacheDemoSnapshots();
        SetDemoMode(StartInDemoMode);
    }

    private void OnEnable()
    {
        EnsureInitialized();
        OnEffectEnable();
    }

    private void Update()
    {
        if (!mInitialized)
        {
            return;
        }

        EnsureRootSizeFromParent();

        if (LifecycleState == ReplicaV3LifecycleState.Paused ||
            LifecycleState == ReplicaV3LifecycleState.Stopped ||
            LifecycleState == ReplicaV3LifecycleState.Disposed)
        {
            return;
        }

        OnEffectTick(Time.deltaTime, Time.unscaledDeltaTime);
    }

    private void OnDisable()
    {
        OnEffectDisable();
    }

    private void OnDestroy()
    {
        KillTrackedTweens(false);
        OnEffectDispose();
        LifecycleState = ReplicaV3LifecycleState.Disposed;
    }

    public void EnsureInitialized()
    {
        if (mInitialized)
        {
            return;
        }

        EnsureBaseReferences();
        OnEffectInitialize();
        mInitialized = true;
        LifecycleState = ReplicaV3LifecycleState.Ready;
    }

    public void PlayIn()
    {
        EnsureInitialized();
        LifecycleState = ReplicaV3LifecycleState.PlayingIn;
        OnPlayIn();
    }

    public void PlayOut(Action onComplete = null)
    {
        EnsureInitialized();
        LifecycleState = ReplicaV3LifecycleState.PlayingOut;
        OnPlayOut(() =>
        {
            LifecycleState = ReplicaV3LifecycleState.Stopped;
            if (onComplete != null)
            {
                onComplete();
            }
        });
    }

    public void PauseEffect()
    {
        if (!mInitialized)
        {
            return;
        }

        LifecycleState = ReplicaV3LifecycleState.Paused;
        for (var i = 0; i < mTrackedTweens.Count; i++)
        {
            if (mTrackedTweens[i] != null && mTrackedTweens[i].active)
            {
                mTrackedTweens[i].Pause();
            }
        }
    }

    public void ResumeEffect()
    {
        if (!mInitialized)
        {
            return;
        }

        if (LifecycleState != ReplicaV3LifecycleState.Paused)
        {
            return;
        }

        for (var i = 0; i < mTrackedTweens.Count; i++)
        {
            if (mTrackedTweens[i] != null && mTrackedTweens[i].active)
            {
                mTrackedTweens[i].Play();
            }
        }

        LifecycleState = ReplicaV3LifecycleState.PlayingLoop;
    }

    public void ResetEffect()
    {
        EnsureInitialized();
        KillTrackedTweens(false);
        LifecycleState = ReplicaV3LifecycleState.Ready;
        OnEffectReset();
    }

    public void SetCanvasMode(ReplicaV3CanvasMode canvasMode)
    {
        CanvasMode = canvasMode;
        OnCanvasModeChanged(canvasMode);
    }

    public void SetDemoMode(bool demoMode)
    {
        IsDemoMode = demoMode;
        if (demoMode)
        {
            RestoreDemoSnapshots();
        }
        else
        {
            ApplyCleanModeSnapshots();
        }
    }

    [ContextMenu("ReplicaV3/切换到演示模式")]
    public void ContextSetDemoMode()
    {
        SetDemoMode(true);
    }

    [ContextMenu("ReplicaV3/切换到纯净模式")]
    public void ContextSetCleanMode()
    {
        SetDemoMode(false);
    }

    [ContextMenu("ReplicaV3/重置动效")]
    public void ContextResetEffect()
    {
        ResetEffect();
    }

    public virtual IReadOnlyList<ReplicaV3ParameterDefinition> GetParameterDefinitions()
    {
        return sEmptyParameters;
    }

    public virtual bool TryGetFloatParameter(string parameterId, out float value)
    {
        value = 0f;
        return false;
    }

    public virtual bool TrySetFloatParameter(string parameterId, float value)
    {
        return false;
    }

    public virtual bool TryGetBoolParameter(string parameterId, out bool value)
    {
        value = false;
        return false;
    }

    public virtual bool TrySetBoolParameter(string parameterId, bool value)
    {
        return false;
    }

    protected static Font ResolveBuiltinFont()
    {
        // Unity 6 removed Arial.ttf from built-ins. LegacyRuntime.ttf is the replacement.
        Font font = null;
        try
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
        catch (ArgumentException) { }

        if (font == null)
        {
            try
            {
                font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }
            catch (ArgumentException) { }
        }

        return font;
    }

    protected void SetLifecycleLooping()
    {
        LifecycleState = ReplicaV3LifecycleState.PlayingLoop;
    }

    protected void SetCanvasAlpha(float alpha)
    {
        if (EffectCanvasGroup == null)
        {
            return;
        }

        EffectCanvasGroup.alpha = Mathf.Clamp01(alpha);
    }

    protected Tween TrackTween(Tween tween)
    {
        if (tween == null)
        {
            return null;
        }

        mTrackedTweens.Add(tween);
        tween.onKill += () =>
        {
            mTrackedTweens.Remove(tween);
        };
        return tween;
    }

    protected void KillTrackedTweens(bool complete)
    {
        for (var i = mTrackedTweens.Count - 1; i >= 0; i--)
        {
            var tween = mTrackedTweens[i];
            if (tween != null && tween.active)
            {
                tween.Kill(complete);
            }
        }

        mTrackedTweens.Clear();
    }

    protected static Camera ResolveReliableEventCamera(PointerEventData eventData, Transform targetTransform)
    {
        var eventCamera = eventData != null
            ? (eventData.enterEventCamera ?? eventData.pressEventCamera)
            : null;
        return ResolveReliableEventCameraInternal(eventCamera, targetTransform);
    }

    protected static Camera ResolveReliableEventCamera(Transform targetTransform)
    {
        return ResolveReliableEventCameraInternal(null, targetTransform);
    }

    protected static void DrawInteractionRangeGizmo(
        bool showInteractionRange,
        RectTransform interactionHitSource,
        RectTransform interactionRangeDependency,
        float interactionRangePadding)
    {
#if UNITY_EDITOR
        if (!showInteractionRange)
        {
            return;
        }

        var target = interactionRangeDependency != null ? interactionRangeDependency : interactionHitSource;
        if (target == null)
        {
            return;
        }

        var rect = target.rect;
        rect.xMin -= interactionRangePadding;
        rect.xMax += interactionRangePadding;
        rect.yMin -= interactionRangePadding;
        rect.yMax += interactionRangePadding;

        var corners = new[]
        {
            new Vector3(rect.xMin, rect.yMin, 0f),
            new Vector3(rect.xMax, rect.yMin, 0f),
            new Vector3(rect.xMax, rect.yMax, 0f),
            new Vector3(rect.xMin, rect.yMax, 0f)
        };

        Handles.matrix = target.localToWorldMatrix;
        var line = new Color(0.25f, 0.8f, 1f, 0.95f);
        var fill = new Color(0.25f, 0.8f, 1f, 0.06f);
        Handles.DrawSolidRectangleWithOutline(corners, fill, line);

        Handles.Label(
            target.TransformPoint(new Vector3(rect.xMin, rect.yMax + 14f, 0f)),
            $"交互范围（依赖判定组件: {target.name}）");
#endif
    }

    private static Camera ResolveReliableEventCameraInternal(Camera eventCamera, Transform targetTransform)
    {
        if (eventCamera != null)
        {
            return eventCamera;
        }

        if (targetTransform != null)
        {
            var canvas = targetTransform.GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                {
                    return null;
                }

                if (canvas.worldCamera != null)
                {
                    return canvas.worldCamera;
                }
            }
        }

        return Camera.main;
    }

    protected virtual void OnEffectInitialize()
    {
    }

    protected virtual void OnEffectEnable()
    {
    }

    protected virtual void OnEffectDisable()
    {
    }

    protected virtual void OnEffectTick(float deltaTime, float unscaledDeltaTime)
    {
    }

    protected virtual void OnEffectReset()
    {
    }

    protected virtual void OnCanvasModeChanged(ReplicaV3CanvasMode canvasMode)
    {
    }

    protected virtual void OnEffectDispose()
    {
    }

    protected abstract void OnPlayIn();
    protected abstract void OnPlayOut(Action onComplete);

    private void EnsureBaseReferences()
    {
        if (EffectRoot == null)
        {
            EffectRoot = transform as RectTransform;
        }

        EnsureRootSizeFromParent();

        if (EffectCanvasGroup == null)
        {
            EffectCanvasGroup = GetComponent<CanvasGroup>();
            if (EffectCanvasGroup == null)
            {
                EffectCanvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        if (EffectRoot == null && !mLoggedMissingRoot)
        {
            mLoggedMissingRoot = true;
            Debug.LogWarning($"[{name}] 缺少 RectTransform，动效将无法正确布局。", this);
        }
    }

    private void EnsureRootSizeFromParent()
    {
        if (EffectRoot == null)
        {
            return;
        }

        if (EffectRoot.rect.width > 1f && EffectRoot.rect.height > 1f)
        {
            mAutoSizedRootOnce = true;
            return;
        }

        if (mAutoSizedRootOnce)
        {
            return;
        }

        var parent = EffectRoot.parent as RectTransform;
        if (parent == null)
        {
            return;
        }

        Canvas.ForceUpdateCanvases();
        var parentSize = parent.rect.size;
        if (parentSize.x <= 1f || parentSize.y <= 1f)
        {
            return;
        }

        EffectRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, parentSize.x);
        EffectRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, parentSize.y);

        if (EffectRoot.rect.width > 1f && EffectRoot.rect.height > 1f)
        {
            mAutoSizedRootOnce = true;
        }
    }

    private void CacheDemoSnapshots()
    {
        mGraphicSnapshots.Clear();
        mObjectSnapshots.Clear();

        for (var i = 0; i < DemoOnlyObjects.Count; i++)
        {
            var go = DemoOnlyObjects[i];
            if (go == null)
            {
                continue;
            }

            var objectSnapshot = new ReplicaV3ObjectSnapshot
            {
                Target = go,
                ActiveSelf = go.activeSelf
            };
            mObjectSnapshots.Add(objectSnapshot);
        }

        for (var i = 0; i < DemoTintGraphics.Count; i++)
        {
            var graphic = DemoTintGraphics[i];
            if (graphic == null)
            {
                continue;
            }

            var graphicSnapshot = new ReplicaV3GraphicSnapshot
            {
                Target = graphic,
                Color = graphic.color
            };
            mGraphicSnapshots.Add(graphicSnapshot);
        }
    }

    private void RestoreDemoSnapshots()
    {
        for (var i = 0; i < mObjectSnapshots.Count; i++)
        {
            var snapshot = mObjectSnapshots[i];
            if (snapshot.Target == null)
            {
                continue;
            }

            snapshot.Target.SetActive(snapshot.ActiveSelf);
        }

        for (var i = 0; i < mGraphicSnapshots.Count; i++)
        {
            var snapshot = mGraphicSnapshots[i];
            if (snapshot.Target == null)
            {
                continue;
            }

            snapshot.Target.color = snapshot.Color;
        }
    }

    private void ApplyCleanModeSnapshots()
    {
        for (var i = 0; i < mObjectSnapshots.Count; i++)
        {
            var snapshot = mObjectSnapshots[i];
            if (snapshot.Target == null)
            {
                continue;
            }

            snapshot.Target.SetActive(false);
        }

        for (var i = 0; i < mGraphicSnapshots.Count; i++)
        {
            var snapshot = mGraphicSnapshots[i];
            if (snapshot.Target == null)
            {
                continue;
            }

            snapshot.Target.color = CleanTintColor;
        }
    }

    private struct ReplicaV3GraphicSnapshot
    {
        public Graphic Target;
        public Color Color;
    }

    private struct ReplicaV3ObjectSnapshot
    {
        public GameObject Target;
        public bool ActiveSelf;
    }
}
