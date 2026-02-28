using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class ReplicaV3SpotlightCardEffect : ReplicaV3EffectBase, IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler
{
    [Header("组件绑定")]
    [Tooltip("卡片本体的RectTransform")]
    public RectTransform CardBoard;
    [Tooltip("跟随鼠标移动的聚光灯图片")]
    public RectTransform Spotlight;
    [Tooltip("控制聚光灯整体Alpha的组件")]
    public CanvasGroup SpotlightGroup;
    [Tooltip("边框图片，用于颜色渐变")]
    public Image BorderImage;

    [Header("交互范围")]
    public RectTransform InteractionHitSource;
    public RectTransform InteractionRangeDependency;
    public bool ShowInteractionRange = true;
    public float InteractionRangePadding = 0f;

    [Header("Motion 参数")]
    public float SpotlightFollowSpeed = 6f;
    public float SpotlightAlphaFollowSpeed = 4f;
    public float BorderColorFollowSpeed = 4f;
    [Range(0f, 1f)] public float HoverSpotlightAlpha = 0.6f;

    [Header("Visual 参数")]
    public Color BorderIdleColor = new Color(0.22f, 0.28f, 0.38f, 0.70f);
    public Color BorderHoverColor = new Color(0.44f, 0.86f, 1f, 0.72f);


    [Header("Animation")]
    public float EnterOffset = 220f;
    public float ExitOffset = 220f;

    private readonly List<ReplicaV3ParameterDefinition> mParameters = new List<ReplicaV3ParameterDefinition>
    {
        new ReplicaV3ParameterDefinition { Id = "follow_speed", DisplayName = "跟随速度", Description = "聚光灯跟随指针的速度", Kind = ReplicaV3ParameterKind.Float, Min = 1f, Max = 20f, Step = 1f },
        new ReplicaV3ParameterDefinition { Id = "hover_alpha", DisplayName = "悬浮透明度", Description = "聚光灯在悬浮时的透明度", Kind = ReplicaV3ParameterKind.Float, Min = 0f, Max = 1f, Step = 0.05f }
    };

    private bool mHovered;
    private Vector2 mTargetLocalPos;
    private Vector2 mCurrentLocalPos;
    private float mTargetAlpha;
    private float mCurrentAlpha;

    protected override void OnEffectInitialize()
    {
        SetCanvasAlpha(1f);
        SetLifecycleLooping();
        ResetState();
    }

    private void ResetState()
    {
        mHovered = false;
        mTargetLocalPos = Vector2.zero;
        mCurrentLocalPos = Vector2.zero;
        mTargetAlpha = 0f;
        mCurrentAlpha = 0f;

        if (SpotlightGroup != null) SpotlightGroup.alpha = 0f;
        if (BorderImage != null) BorderImage.color = BorderIdleColor;
    }

    protected override void OnEffectTick(float deltaTime, float unscaledDeltaTime)
    {
        float dt = deltaTime;
        UpdateFollow(dt);
        ApplyVisual(dt);
    }

    private void UpdateFollow(float dt)
    {
        if (Spotlight == null) return;


        float follow = 1f - Mathf.Pow(1f - Mathf.Clamp01(0.15f), Mathf.Max(1f, dt * 60f));
        if (SpotlightFollowSpeed > 0f)
        {
            follow = 1f - Mathf.Exp(-SpotlightFollowSpeed * dt);
        }
        mCurrentLocalPos = Vector2.Lerp(mCurrentLocalPos, mTargetLocalPos, Mathf.Clamp01(follow));
    }

    private void ApplyVisual(float dt)
    {
        if (Spotlight != null)
        {
            Spotlight.anchoredPosition = mCurrentLocalPos;
        }

        mTargetAlpha = mHovered ? Mathf.Clamp01(HoverSpotlightAlpha) : 0f;
        float alphaLerp = 1f - Mathf.Exp(-Mathf.Max(0.01f, SpotlightAlphaFollowSpeed) * dt);
        mCurrentAlpha = Mathf.Lerp(mCurrentAlpha, mTargetAlpha, Mathf.Clamp01(alphaLerp));

        if (SpotlightGroup != null)
        {
            SpotlightGroup.alpha = mCurrentAlpha;
        }

        if (BorderImage != null)
        {
            Color targetBorderColor = mHovered ? BorderHoverColor : BorderIdleColor;
            float colorLerp = 1f - Mathf.Exp(-Mathf.Max(0.01f, BorderColorFollowSpeed) * dt);
            BorderImage.color = Color.Lerp(BorderImage.color, targetBorderColor, Mathf.Clamp01(colorLerp));
        }
    }

    public void OnPointerEnter(PointerEventData eventData) => UpdatePointerTargets(eventData);
    public void OnPointerMove(PointerEventData eventData) => UpdatePointerTargets(eventData);
    public void OnPointerExit(PointerEventData eventData)
    {
        mHovered = false;
    }

    private void UpdatePointerTargets(PointerEventData eventData)
    {
        if (CardBoard == null)
        {
            mHovered = false;
            return;
        }
        Camera cam = ResolveInteractionCamera(eventData);
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(CardBoard, eventData.position, cam, out Vector2 localPoint))
        {
            mHovered = true;
            mTargetLocalPos = localPoint;
        }
        else
        {
            mHovered = false;
        }
    }

    protected override void OnPlayIn()
    {
        KillTrackedTweens(false);
        if (CardBoard != null)
        {
            CardBoard.anchoredPosition = new Vector2(0, EnterOffset);
            TrackTween(CardBoard.DOAnchorPos(Vector2.zero, 0.4f).SetEase(Ease.OutCubic));
        }


        SetCanvasAlpha(0f);
        if (EffectCanvasGroup != null)
        {
            TrackTween(EffectCanvasGroup.DOFade(1f, 0.3f).SetEase(Ease.OutCubic));
        }

        SetLifecycleLooping();
        ResetState();
    }

    protected override void OnPlayOut(Action onComplete)
    {
        KillTrackedTweens(false);


        if (CardBoard != null)
        {
            TrackTween(CardBoard.DOAnchorPos(new Vector2(0, -ExitOffset), 0.3f).SetEase(Ease.InCubic));
        }

        if (EffectCanvasGroup != null)
        {
            TrackTween(EffectCanvasGroup.DOFade(0f, 0.25f).SetEase(Ease.InCubic).OnComplete(() => onComplete?.Invoke()));
            return;
        }
        onComplete?.Invoke();
    }

    public override IReadOnlyList<ReplicaV3ParameterDefinition> GetParameterDefinitions() => mParameters;

    public override bool TryGetFloatParameter(string parameterId, out float value)
    {
        switch (parameterId)
        {
            case "follow_speed": value = SpotlightFollowSpeed; return true;
            case "hover_alpha": value = HoverSpotlightAlpha; return true;
            default: value = 0f; return false;
        }
    }

    public override bool TrySetFloatParameter(string parameterId, float value)
    {
        switch (parameterId)
        {
            case "follow_speed": SpotlightFollowSpeed = value; return true;
            case "hover_alpha": HoverSpotlightAlpha = Mathf.Clamp01(value); return true;
            default: return false;
        }
    }

    private Camera ResolveInteractionCamera(PointerEventData eventData = null)
    {
        var target = InteractionHitSource != null ? InteractionHitSource : (CardBoard != null ? CardBoard : (EffectRoot != null ? EffectRoot : transform as RectTransform));
        if (eventData != null) return ResolveReliableEventCamera(eventData, target);
        return ResolveReliableEventCamera(target);
    }

    private void OnDrawGizmos()
    {
        var hitSource = InteractionHitSource != null ? InteractionHitSource : (CardBoard != null ? CardBoard : (EffectRoot != null ? EffectRoot : transform as RectTransform));
        DrawInteractionRangeGizmo(ShowInteractionRange, hitSource, InteractionRangeDependency, InteractionRangePadding);
    }
}
