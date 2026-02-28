using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class ReplicaV3ScrollStackEffect : ReplicaV3EffectBase
{
    [Header("组件绑定")]
    [Tooltip("ScrollRect组件，用于获取滚动输入")]
    public ScrollRect Scroller;
    [Tooltip("存放所有卡片的容器层")]
    public RectTransform ContentTransform;
    [Tooltip("尾部占位组件（用于预留底部滚动空间）")]
    public RectTransform EndSpacer;
    [Tooltip("需要进行Stack计算的卡片层级")]
    public List<RectTransform> Items = new List<RectTransform>();

    [Header("交互范围")]
    [Tooltip("主要可交互判定组件。")]
    public RectTransform InteractionHitSource;
    [Tooltip("交互范围跟随依赖组件。为空时回退到 InteractionHitSource。")]
    public RectTransform InteractionRangeDependency;
    [Tooltip("编辑器中显示可交互范围。默认开启，可手动关闭。")]
    public bool ShowInteractionRange = true;
    [Tooltip("交互范围扩展像素。")]
    public float InteractionRangePadding = 0f;

    [Header("Spacing & Layout 参数")]
    public float ItemDistance = 100f;
    public float ItemStackDistance = 30f;
    [Tooltip("Percentage (0-1) of container height where stacking starts")]
    [Range(0, 1)] public float StackPosition = 0.2f;
    [Tooltip("Percentage (0-1) of container height where scaling finishes")]
    [Range(0, 1)] public float ScaleEndPosition = 0.1f;

    [Header("Visual Styles 参数")]
    public float BaseScale = 0.85f;
    public float ItemScale = 0.03f;
    public float RotationAmount = 0f;

    [Header("Manual Feel 参数")]
    public float ManualSensitivity = 2.25f;
    public float ManualBurst = 0.012f;
    public float ManualBurstClamp = 220f;
    public float ManualSmoothTime = 0.08f;
    public float ManualMaxSpeed = 15000f;
    public float ManualSettleToRaw = 9f;
    public float ManualOverscroll = 120f;

    [Header("Programmatic Drive 参数")]
    public float ProgrammaticSmoothTime = 0.12f;
    public float ProgrammaticMaxSpeed = 12000f;
    public float ProgrammaticSnapThreshold = 1.5f;
    public bool SyncScrollerOnProgrammaticDrive = true;

    private readonly List<ReplicaV3ParameterDefinition> mParameters = new List<ReplicaV3ParameterDefinition>
    {
        new ReplicaV3ParameterDefinition { Id = "item_distance", DisplayName = "卡片间距", Description = "相邻两张卡片的基础距离", Kind = ReplicaV3ParameterKind.Float, Min = 0f, Max = 500f, Step = 1f },
        new ReplicaV3ParameterDefinition { Id = "item_stack_distance", DisplayName = "堆叠间距", Description = "堆叠状态下卡片之间的像素距离", Kind = ReplicaV3ParameterKind.Float, Min = 0f, Max = 100f, Step = 1f },
        new ReplicaV3ParameterDefinition { Id = "stack_position", DisplayName = "堆叠触发位", Description = "视口高度百分比", Kind = ReplicaV3ParameterKind.Float, Min = 0f, Max = 1f, Step = 0.01f },
        new ReplicaV3ParameterDefinition { Id = "scale_end_position", DisplayName = "缩放完成位", Description = "视口高度百分比", Kind = ReplicaV3ParameterKind.Float, Min = 0f, Max = 1f, Step = 0.01f },
        new ReplicaV3ParameterDefinition { Id = "base_scale", DisplayName = "基础缩放", Description = "第一张卡片堆叠时的缩放系数", Kind = ReplicaV3ParameterKind.Float, Min = 0.5f, Max = 1f, Step = 0.01f },
        new ReplicaV3ParameterDefinition { Id = "item_scale", DisplayName = "增量缩放", Description = "后续每张卡片的缩放衰减量", Kind = ReplicaV3ParameterKind.Float, Min = 0f, Max = 0.1f, Step = 0.01f },
        new ReplicaV3ParameterDefinition { Id = "rotation_amount", DisplayName = "旋转量", Description = "堆叠时卡片的倾斜旋转量", Kind = ReplicaV3ParameterKind.Float, Min = -45f, Max = 45f, Step = 1f }
    };

    private float _currentScrollY;
    private float _styledTargetScrollY;
    private float _styledVelocityY;
    private float _lastRawScrollY;
    private bool _hasRawScrollSample;
    private bool _programmaticDriving;
    private float _programmaticTargetY;
    private readonly List<float> _baseCardTops = new List<float>();

    protected override void OnEffectInitialize()
    {
        SetCanvasAlpha(1f);
        SetLifecycleLooping();
        InitializeCardPositions();
    }

    private void InitializeCardPositions()
    {
        _baseCardTops.Clear();
        if (Items == null || Items.Count == 0) return;

        float currentY = 0f;
        for (int i = 0; i < Items.Count; i++)
        {
            if (Items[i] != null)
            {
                Items[i].anchoredPosition = new Vector2(0, -currentY);
                _baseCardTops.Add(currentY);
                currentY += Items[i].rect.height + ItemDistance;
            }
            else
            {
                _baseCardTops.Add(currentY);
            }
        }

        if (ContentTransform != null)
        {
            if (EndSpacer != null)
            {
                EndSpacer.anchoredPosition = new Vector2(0, -currentY);
                ContentTransform.sizeDelta = new Vector2(0, currentY + EndSpacer.rect.height);
            }
            else
            {
                ContentTransform.sizeDelta = new Vector2(0, currentY);
            }
        }

        _currentScrollY = 0;
        _styledTargetScrollY = 0;
        _styledVelocityY = 0;
        _lastRawScrollY = 0;
        _hasRawScrollSample = false;
        _programmaticDriving = false;
        _programmaticTargetY = 0;
        if (Scroller != null && Scroller.content != null)
        {
            Scroller.content.anchoredPosition = Vector2.zero;
        }
    }

    protected override void OnEffectTick(float deltaTime, float unscaledDeltaTime)
    {
        if (Scroller == null || Scroller.content == null || Scroller.viewport == null)
            return;

        float dt = Mathf.Max(0.0001f, deltaTime);
        float rawScrollY = Scroller.content.anchoredPosition.y;

        if (!_hasRawScrollSample)
        {
            _hasRawScrollSample = true;
            _lastRawScrollY = rawScrollY;
            _styledTargetScrollY = rawScrollY;
            _currentScrollY = rawScrollY;
        }

        float rawDelta = rawScrollY - _lastRawScrollY;
        _lastRawScrollY = rawScrollY;

        if (_programmaticDriving)
        {
            _styledTargetScrollY = ClampToScrollBounds(_programmaticTargetY, false);
            _currentScrollY = Mathf.SmoothDamp(
                _currentScrollY,
                _styledTargetScrollY,

                ref _styledVelocityY,

                Mathf.Max(0.03f, ProgrammaticSmoothTime),

                Mathf.Max(100f, ProgrammaticMaxSpeed),

                dt);

            if (SyncScrollerOnProgrammaticDrive)
            {
                Vector2 synced = Scroller.content.anchoredPosition;
                synced.y = _currentScrollY;
                Scroller.content.anchoredPosition = synced;
                _lastRawScrollY = _currentScrollY;
            }

            if (Mathf.Abs(_currentScrollY - _styledTargetScrollY) <= Mathf.Max(0.1f, ProgrammaticSnapThreshold))
            {
                _currentScrollY = _styledTargetScrollY;
                _styledVelocityY = 0f;
                _programmaticDriving = false;
            }
        }
        else
        {
            if (Mathf.Abs(rawDelta) > 0.001f)
            {
                float boostedDelta = rawDelta * Mathf.Max(1f, ManualSensitivity);
                float burst = Mathf.Sign(rawDelta) * Mathf.Min(Mathf.Abs(rawDelta) * Mathf.Abs(rawDelta) * Mathf.Max(0f, ManualBurst), Mathf.Max(0f, ManualBurstClamp));
                _styledTargetScrollY += boostedDelta + burst;
            }

            _styledTargetScrollY = Mathf.Lerp(_styledTargetScrollY, rawScrollY, Mathf.Clamp01(dt * Mathf.Max(0f, ManualSettleToRaw)));
            _styledTargetScrollY = ClampToScrollBounds(_styledTargetScrollY, true);


            _currentScrollY = Mathf.SmoothDamp(
                _currentScrollY,
                _styledTargetScrollY,

                ref _styledVelocityY,

                Mathf.Max(0.02f, ManualSmoothTime),

                Mathf.Max(100f, ManualMaxSpeed),

                dt);
        }

        UpdateTransforms();
    }

    private float ClampToScrollBounds(float value, bool allowOverscroll)
    {
        float max = GetMaxScrollY();
        if (!allowOverscroll)
            return Mathf.Clamp(value, 0f, max);

        float overscroll = Mathf.Max(0f, ManualOverscroll);
        return Mathf.Clamp(value, -overscroll, max + overscroll);
    }

    private float GetMaxScrollY()
    {
        if (ContentTransform == null || Scroller == null || Scroller.viewport == null)
            return 0f;
        return Mathf.Max(0f, ContentTransform.rect.height - Scroller.viewport.rect.height);
    }

    private void UpdateTransforms()
    {
        if (Scroller == null || Scroller.viewport == null || Items.Count == 0) return;

        float viewportHeight = Scroller.viewport.rect.height;
        float stackPosPx = Mathf.Clamp01(StackPosition) * viewportHeight;
        float scaleEndPosPx = Mathf.Clamp01(ScaleEndPosition) * viewportHeight;

        float endElementTop = 0;
        if (EndSpacer != null)
        {
            endElementTop = Mathf.Abs(EndSpacer.anchoredPosition.y);
        }

        for (int i = 0; i < Items.Count; i++)
        {
            if (i >= _baseCardTops.Count) break;

            RectTransform item = Items[i];
            if (item == null) continue;

            float cardBaseTop = _baseCardTops[i];

            float triggerStart = cardBaseTop - stackPosPx - ItemStackDistance * i;
            float triggerEnd = cardBaseTop - scaleEndPosPx;
            float pinStart = triggerStart;
            float pinEnd = endElementTop - viewportHeight / 2f;

            float scaleProgress = 0;
            if (_currentScrollY > triggerStart)
            {
                float range = triggerEnd - triggerStart;
                scaleProgress = range > 0.001f ? Mathf.Clamp01((_currentScrollY - triggerStart) / range) : 1f;
            }

            scaleProgress = EaseOutExpo(scaleProgress);

            float targetScale = BaseScale + i * ItemScale;
            float scale = 1f - scaleProgress * (1f - targetScale);
            float rotation = RotationAmount != 0 ? i * RotationAmount * scaleProgress : 0;

            float translateY = 0;
            if (_currentScrollY >= pinStart && _currentScrollY <= pinEnd)
            {
                translateY = -(stackPosPx + i * ItemStackDistance) - _currentScrollY + cardBaseTop;
            }
            else if (_currentScrollY > pinEnd)
            {
                translateY = -(stackPosPx + i * ItemStackDistance) - pinEnd + cardBaseTop;
            }

            if (_currentScrollY >= pinStart - 32f && _currentScrollY <= pinStart + 32f)
            {
                float enterBlend = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(pinStart - 32f, pinStart + 32f, _currentScrollY));
                translateY *= enterBlend;
            }

            item.localScale = new Vector3(scale, scale, 1f);
            item.localRotation = Quaternion.Euler(0, 0, rotation);

            Vector2 pos = item.anchoredPosition;
            pos.y = -cardBaseTop + translateY;
            item.anchoredPosition = pos;
        }
    }

    private static float EaseOutExpo(float t)
    {
        t = Mathf.Clamp01(t);
        if (t >= 1f) return 1f;
        return 1f - Mathf.Pow(2f, -10f * t);
    }

    protected override void OnPlayIn()
    {
        KillTrackedTweens(false);
        SetCanvasAlpha(0f);
        if (EffectCanvasGroup != null)
        {
            TrackTween(EffectCanvasGroup.DOFade(1f, 0.25f).SetEase(Ease.OutCubic));
        }
        SetLifecycleLooping();
        InitializeCardPositions();
    }

    protected override void OnPlayOut(Action onComplete)
    {
        KillTrackedTweens(false);
        if (EffectCanvasGroup != null)
        {
            TrackTween(EffectCanvasGroup.DOFade(0f, 0.2f).SetEase(Ease.InCubic).OnComplete(() => onComplete?.Invoke()));
            return;
        }
        onComplete?.Invoke();
    }

    public void DriveToLayer(int layerIndex, bool animated = true)
    {
        if (Scroller == null || Scroller.viewport == null || _baseCardTops.Count == 0) return;

        layerIndex = Mathf.Clamp(layerIndex, 0, _baseCardTops.Count - 1);
        float viewportHeight = Scroller.viewport.rect.height;
        float stackPosPx = Mathf.Clamp01(StackPosition) * viewportHeight;
        float cardBaseTop = _baseCardTops[layerIndex];
        float target = ClampToScrollBounds(cardBaseTop - stackPosPx - (ItemStackDistance * layerIndex), false);
        SetProgrammaticTarget(target, animated);
    }

    private void SetProgrammaticTarget(float targetY, bool animated)
    {
        _programmaticTargetY = ClampToScrollBounds(targetY, false);
        _styledTargetScrollY = _programmaticTargetY;

        if (!animated)
        {
            _programmaticDriving = false;
            _styledVelocityY = 0f;
            _currentScrollY = _programmaticTargetY;

            if (Scroller != null && Scroller.content != null)
            {
                Vector2 synced = Scroller.content.anchoredPosition;
                synced.y = _currentScrollY;
                Scroller.content.anchoredPosition = synced;
                _lastRawScrollY = _currentScrollY;
            }
            return;
        }
        _programmaticDriving = true;
    }

    public override IReadOnlyList<ReplicaV3ParameterDefinition> GetParameterDefinitions() => mParameters;

    public override bool TryGetFloatParameter(string parameterId, out float value)
    {
        switch (parameterId)
        {
            case "item_distance": value = ItemDistance; return true;
            case "item_stack_distance": value = ItemStackDistance; return true;
            case "stack_position": value = StackPosition; return true;
            case "scale_end_position": value = ScaleEndPosition; return true;
            case "base_scale": value = BaseScale; return true;
            case "item_scale": value = ItemScale; return true;
            case "rotation_amount": value = RotationAmount; return true;
            default: value = 0f; return false;
        }
    }

    public override bool TrySetFloatParameter(string parameterId, float value)
    {
        switch (parameterId)
        {
            case "item_distance": ItemDistance = value; InitializeCardPositions(); return true;
            case "item_stack_distance": ItemStackDistance = value; return true;
            case "stack_position": StackPosition = Mathf.Clamp01(value); return true;
            case "scale_end_position": ScaleEndPosition = Mathf.Clamp01(value); return true;
            case "base_scale": BaseScale = Mathf.Clamp01(value); return true;
            case "item_scale": ItemScale = value; return true;
            case "rotation_amount": RotationAmount = value; return true;
            default: return false;
        }
    }

    private Camera ResolveInteractionCamera(PointerEventData eventData = null)
    {
        var target = InteractionHitSource != null ? InteractionHitSource : (ContentTransform != null ? ContentTransform : (EffectRoot != null ? EffectRoot : transform as RectTransform));
        if (eventData != null) return ResolveReliableEventCamera(eventData, target);
        return ResolveReliableEventCamera(target);
    }

    private void OnDrawGizmos()
    {
        var hitSource = InteractionHitSource != null ? InteractionHitSource : (ContentTransform != null ? ContentTransform : (EffectRoot != null ? EffectRoot : transform as RectTransform));
        DrawInteractionRangeGizmo(ShowInteractionRange, hitSource, InteractionRangeDependency, InteractionRangePadding);
    }
}
