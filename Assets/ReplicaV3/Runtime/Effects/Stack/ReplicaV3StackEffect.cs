using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[Serializable]
public class ReplicaV3StackCardBinds
{
    public RectTransform Root;
    public RectTransform Tilt;
    public RectTransform Visual;
    public RectTransform Shadow;
    public Image ShadowImage;


    [HideInInspector] public float RandomOffset;
    [HideInInspector] public float LayoutRotationZ;
    [HideInInspector] public float LayoutScale = 1f;
    [HideInInspector] public bool IsDragging;
}

public sealed class ReplicaV3StackEffect : ReplicaV3EffectBase
{
    [Header("组件绑定")]
    public List<ReplicaV3StackCardBinds> Cards = new List<ReplicaV3StackCardBinds>();

    [Header("交互范围")]
    public RectTransform InteractionHitSource;
    public RectTransform InteractionRangeDependency;
    public bool ShowInteractionRange = true;
    public float InteractionRangePadding = 0f;

    [Header("Layout 参数")]
    public float RotationStep = 4f;
    public float ScaleStep = 0.06f;

    [Header("Interaction 参数")]
    public bool AllowDrag = true;
    public float DragSensitivity = 200f;
    public float MaxTilt = 22f;
    public bool SendToBackOnClick = true;

    [Header("Animation 参数")]
    public float LayoutDuration = 0.32f;
    public float ReturnDuration = 0.24f;
    public float EnterOffset = 260f;
    public float ExitOffset = 260f;

    [Header("Visual 参数")]
    public Color ShadowColor = new Color(0f, 0f, 0f, 0.26f);

    private readonly List<ReplicaV3ParameterDefinition> mParameters = new List<ReplicaV3ParameterDefinition>
    {
        new ReplicaV3ParameterDefinition { Id = "rotation_step", DisplayName = "旋转梯级", Description = "每一层的叠加旋转偏差量", Kind = ReplicaV3ParameterKind.Float, Min = 0f, Max = 15f, Step = 0.5f },
        new ReplicaV3ParameterDefinition { Id = "scale_step", DisplayName = "缩放梯级", Description = "每一层的缩小量", Kind = ReplicaV3ParameterKind.Float, Min = 0f, Max = 0.2f, Step = 0.01f },
        new ReplicaV3ParameterDefinition { Id = "allow_drag", DisplayName = "允许拖拽", Description = "是否可以通过推拉卡片交互", Kind = ReplicaV3ParameterKind.Bool },
        new ReplicaV3ParameterDefinition { Id = "send_to_back_onClick", DisplayName = "点击推入底部", Description = "点击是否排到最下方", Kind = ReplicaV3ParameterKind.Bool }
    };

    private bool mInitialized;
    private int mDraggingIndex = -1;
    private Vector2 mPressPoint;
    private Vector2 mDragOffset;
    private List<int> mOrder = new List<int>();

    protected override void OnEffectInitialize()
    {
        SetCanvasAlpha(1f);
        SetLifecycleLooping();

        mOrder.Clear();
        for (int i = 0; i < Cards.Count; i++)
        {
            mOrder.Add(i);
            Cards[i].RandomOffset = ComputeRandomOffset(i);
            BindEvents(Cards[i].Root.gameObject, i);
        }

        mInitialized = true;
        ApplyLayout(false);
        UpdateInteractivity();
    }

    private void BindEvents(GameObject target, int index)
    {
        if (target == null) return;
        EventTrigger trigger = target.GetComponent<EventTrigger>();
        if (trigger == null) trigger = target.AddComponent<EventTrigger>();
        trigger.triggers.Clear();

        EventTrigger.Entry beginDrag = new EventTrigger.Entry { eventID = EventTriggerType.BeginDrag };
        beginDrag.callback.AddListener((data) => OnCardBeginDrag(index, (PointerEventData)data));
        trigger.triggers.Add(beginDrag);

        EventTrigger.Entry drag = new EventTrigger.Entry { eventID = EventTriggerType.Drag };
        drag.callback.AddListener((data) => OnCardDrag(index, (PointerEventData)data));
        trigger.triggers.Add(drag);

        EventTrigger.Entry endDrag = new EventTrigger.Entry { eventID = EventTriggerType.EndDrag };
        endDrag.callback.AddListener((data) => OnCardEndDrag(index, (PointerEventData)data));
        trigger.triggers.Add(endDrag);

        EventTrigger.Entry ptrClick = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
        ptrClick.callback.AddListener((data) => OnCardClick(index, (PointerEventData)data));
        trigger.triggers.Add(ptrClick);
    }

    protected override void OnEffectTick(float deltaTime, float unscaledDeltaTime)
    {
    }

    protected override void OnPlayIn()
    {
        KillTrackedTweens(false);


        RectTransform rt = EffectRoot != null ? EffectRoot : transform as RectTransform;


        if (rt != null)
        {
            rt.anchoredPosition = new Vector2(0, -EnterOffset);
            TrackTween(rt.DOAnchorPos(Vector2.zero, 0.4f).SetEase(Ease.OutCubic));
        }

        SetCanvasAlpha(0f);
        if (EffectCanvasGroup != null)
        {
            TrackTween(EffectCanvasGroup.DOFade(1f, 0.35f).SetEase(Ease.OutCubic));
        }

        SetLifecycleLooping();
        if (!mInitialized && Cards.Count > 0)
        {
            OnEffectInitialize();
        }
        else
        {
            ApplyLayout(true);
        }
    }

    protected override void OnPlayOut(Action onComplete)
    {
        KillTrackedTweens(false);
        RectTransform rt = EffectRoot != null ? EffectRoot : transform as RectTransform;


        if (rt != null)
        {
            TrackTween(rt.DOAnchorPos(new Vector2(0, ExitOffset), 0.3f).SetEase(Ease.InCubic));
        }

        if (EffectCanvasGroup != null)
        {
            TrackTween(EffectCanvasGroup.DOFade(0f, 0.25f).SetEase(Ease.InCubic).OnComplete(() => onComplete?.Invoke()));
            return;
        }

        onComplete?.Invoke();
    }

    private void ApplyLayout(bool animated)
    {
        if (mOrder.Count == 0) return;
        int count = mOrder.Count;
        float duration = Mathf.Max(0.08f, LayoutDuration);

        for (int i = 0; i < count; i++)
        {
            int cardIndex = mOrder[i];
            var card = Cards[cardIndex];

            int depth = count - i - 1;
            float rotation = (depth * RotationStep) + card.RandomOffset;
            float scale = 1f + (i * ScaleStep) - (count * ScaleStep);
            scale = Mathf.Clamp(scale, 0.72f, 1.08f);

            card.LayoutRotationZ = rotation;
            card.LayoutScale = scale;

            if (card.Root != null) card.Root.SetSiblingIndex(i);

            if (card.IsDragging) continue;

            if (card.Root != null && card.Tilt != null && card.Visual != null)
            {
                if (!animated)
                {
                    card.Root.anchoredPosition = Vector2.zero;
                    card.Tilt.localRotation = Quaternion.identity;
                    card.Visual.localRotation = Quaternion.Euler(0f, 0f, rotation);
                    card.Visual.localScale = new Vector3(scale, scale, 1f);
                }
                else
                {
                    TrackTween(card.Root.DOAnchorPos(Vector2.zero, duration).SetEase(Ease.OutCubic));
                    TrackTween(card.Visual.DOLocalRotate(new Vector3(0f, 0f, rotation), duration).SetEase(Ease.OutBack));
                    TrackTween(card.Visual.DOScale(new Vector3(scale, scale, 1f), duration).SetEase(Ease.OutCubic));
                }
            }
        }
    }

    private void UpdateInteractivity()
    {
        for (int i = 0; i < mOrder.Count; i++)
        {
            int cardIndex = mOrder[i];
            var card = Cards[cardIndex];
            bool interactive = i == mOrder.Count - 1;

            // Just disable/enable raycast to prevent interacting with cards underneath

            Image img = card.Root != null ? card.Root.GetComponent<Image>() : null;
            if (img != null) img.raycastTarget = interactive;
        }
    }

    private bool IsTopCard(int index)
    {
        return mOrder.Count > 0 && mOrder[mOrder.Count - 1] == index;
    }

    private void OnCardBeginDrag(int index, PointerEventData eventData)
    {
        if (!AllowDrag || !IsTopCard(index) || mDraggingIndex != -1) return;

        mDraggingIndex = index;
        var card = Cards[index];
        card.IsDragging = true;
        mDragOffset = Vector2.zero;

        RectTransform rt = EffectRoot != null ? EffectRoot : transform as RectTransform;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, eventData.position, ResolveInternalCamera(eventData), out mPressPoint))
        {
            mPressPoint = Vector2.zero;
        }

        if (card.Root != null) card.Root.DOKill();
        if (card.Visual != null) card.Visual.DOKill();
        if (card.Tilt != null) card.Tilt.DOKill();
    }

    private void OnCardDrag(int index, PointerEventData eventData)
    {
        if (mDraggingIndex != index) return;
        var card = Cards[index];

        RectTransform rt = EffectRoot != null ? EffectRoot : transform as RectTransform;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, eventData.position, ResolveInternalCamera(eventData), out Vector2 localPoint))
        {
            return;
        }

        mDragOffset = localPoint - mPressPoint;
        if (card.Root != null) card.Root.anchoredPosition = mDragOffset;

        float normalizedX = Mathf.Clamp(mDragOffset.x / 120f, -1f, 1f);
        float normalizedY = Mathf.Clamp(mDragOffset.y / 120f, -1f, 1f);
        float rotX = -normalizedY * MaxTilt;
        float rotY = normalizedX * MaxTilt;

        if (card.Tilt != null) card.Tilt.localRotation = Quaternion.Euler(rotX, rotY, 0f);

        float lift = Mathf.Clamp01(mDragOffset.magnitude / 260f);
        float shadowAlpha = Mathf.Lerp(ShadowColor.a, 0.12f, lift);

        if (card.Shadow != null)
        {
            card.Shadow.anchoredPosition = new Vector2(rotY * 1.2f, -10f - (rotX * 0.8f) - (18f * lift));
            float shadowScale = 1f + (0.22f * lift);
            card.Shadow.localScale = new Vector3(shadowScale, shadowScale, 1f);
        }

        if (card.ShadowImage != null)
        {
            Color color = card.ShadowImage.color;
            color.a = shadowAlpha;
            card.ShadowImage.color = color;
        }
    }

    private void OnCardEndDrag(int index, PointerEventData eventData)
    {
        if (mDraggingIndex != index) return;
        var card = Cards[index];

        card.IsDragging = false;
        mDraggingIndex = -1;

        if (!IsTopCard(index))
        {
            TweenCardBack(card, ReturnDuration);
            return;
        }

        bool shouldSendToBack = Mathf.Abs(mDragOffset.x) > DragSensitivity || Mathf.Abs(mDragOffset.y) > DragSensitivity;
        if (shouldSendToBack)
        {
            SendToBack(index, true);
        }
        else
        {
            TweenCardBack(card, ReturnDuration);
        }
    }

    private void OnCardClick(int index, PointerEventData eventData)
    {
        if (!SendToBackOnClick || mDraggingIndex != -1) return;
        if (!IsTopCard(index)) return;

        if (Mathf.Abs(mDragOffset.x) > 20f || Mathf.Abs(mDragOffset.y) > 20f)
            return; // Ignore clicks if dragged slightly

        SendToBack(index, true);
    }

    private void SendToBack(int index, bool animated)
    {
        if (!mOrder.Remove(index)) return;

        mOrder.Insert(0, index);
        ApplyLayout(animated);
        UpdateInteractivity();
    }

    private void TweenCardBack(ReplicaV3StackCardBinds card, float duration)
    {
        float safe = Mathf.Max(0.06f, duration);
        if (card.Tilt != null) card.Tilt.localRotation = Quaternion.identity;

        if (card.Root != null) TrackTween(card.Root.DOAnchorPos(Vector2.zero, safe).SetEase(Ease.OutCubic));
        if (card.Tilt != null) TrackTween(card.Tilt.DOLocalRotate(Vector3.zero, safe).SetEase(Ease.OutCubic));
        if (card.Visual != null) TrackTween(card.Visual.DOLocalRotate(new Vector3(0f, 0f, card.LayoutRotationZ), safe).SetEase(Ease.OutCubic));
        if (card.Visual != null) TrackTween(card.Visual.DOScale(new Vector3(card.LayoutScale, card.LayoutScale, 1f), safe).SetEase(Ease.OutCubic));

        if (card.ShadowImage != null)
        {
            Color shadowColor = card.ShadowImage.color;
            shadowColor.a = ShadowColor.a;
            card.ShadowImage.color = shadowColor;
        }

        if (card.Shadow != null)
        {
            card.Shadow.anchoredPosition = new Vector2(0f, -10f);
            card.Shadow.localScale = Vector3.one;
        }
    }

    private float ComputeRandomOffset(int index)
    {
        unchecked
        {
            int hash = 17 * 31 + index.GetHashCode();
            float normalized = ((hash & 0x7fffffff) % 1000) / 999f;
            return (normalized - 0.5f) * 10f;
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
            case "rotation_step": value = RotationStep; return true;
            case "scale_step": value = ScaleStep; return true;
            default: value = 0f; return false;
        }
    }

    public override bool TrySetFloatParameter(string parameterId, float value)
    {
        switch (parameterId)
        {
            case "rotation_step": RotationStep = value; ApplyLayout(true); return true;
            case "scale_step": ScaleStep = value; ApplyLayout(true); return true;
            default: return false;
        }
    }

    public override bool TryGetBoolParameter(string parameterId, out bool value)
    {
        switch (parameterId)
        {
            case "allow_drag": value = AllowDrag; return true;
            case "send_to_back_onClick": value = SendToBackOnClick; return true;
            default: value = false; return false;
        }
    }

    public override bool TrySetBoolParameter(string parameterId, bool value)
    {
        switch (parameterId)
        {
            case "allow_drag": AllowDrag = value; return true;
            case "send_to_back_onClick": SendToBackOnClick = value; return true;
            default: return false;
        }
    }

    private void OnDrawGizmos()
    {
        var hitSource = InteractionHitSource != null ? InteractionHitSource : (EffectRoot != null ? EffectRoot : transform as RectTransform);
        DrawInteractionRangeGizmo(ShowInteractionRange, hitSource, InteractionRangeDependency, InteractionRangePadding);
    }
}
