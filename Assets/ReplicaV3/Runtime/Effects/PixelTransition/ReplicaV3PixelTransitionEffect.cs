using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class ReplicaV3PixelTransitionEffect : ReplicaV3EffectBase, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler, ISelectHandler, IDeselectHandler
{
    [Header("交互范围")]
    [Tooltip("主要可交互判定组件。为空时回退到 Card。")]
    public RectTransform InteractionHitSource;

    [Tooltip("交互范围跟随依赖组件。为空时回退到 InteractionHitSource。")]
    public RectTransform InteractionRangeDependency;

    [Tooltip("编辑器中显示可交互范围。默认开启，可手动关闭。")]
    public bool ShowInteractionRange = true;

    [Tooltip("交互范围扩展像素。")]
    public float InteractionRangePadding = 0f;

    [Header("组件绑定（PixelTransition）")]
    [Tooltip("卡片根节点。")]
    public RectTransform Card;

    [Tooltip("默认层节点。")]
    public RectTransform DefaultLayer;

    [Tooltip("默认层图片。")]
    public Image DefaultImage;

    [Tooltip("默认层文本。")]
    public Text DefaultLabel;

    [Tooltip("激活层节点。")]
    public RectTransform ActiveLayer;

    [Tooltip("激活层图片。")]
    public Image ActiveImage;

    [Tooltip("激活层文本。")]
    public Text ActiveLabel;

    [Tooltip("像素块集合（可直接拖拽全部像素 Image）。")]
    public List<Image> PixelBlocks = new List<Image>();

    [Header("参数（可在参数面板实时调）")]
    [Tooltip("单次像素化转场时长。")]
    public float AnimationStepDuration = 0.3f;

    [Tooltip("只触发一次，离开时不反向恢复。")]
    public bool Once = false;

    [Tooltip("像素网格尺寸（仅用于参数展示，实际以 PixelBlocks 数量为准）。")]
    public int GridSize = 7;

    [Header("文案/资源")]
    [Tooltip("默认态文案。")]
    public string DefaultLabelText = "DEFAULT";

    [Tooltip("激活态文案。")]
    public string ActiveLabelText = "ACTIVE";

    [Tooltip("默认态精灵。")]
    public Sprite DefaultSprite;

    [Tooltip("激活态精灵。")]
    public Sprite ActiveSprite;

    [Header("进入/退出")]
    [Tooltip("进入偏移。")]
    public Vector2 EnterOffset = new Vector2(0f, -120f);

    [Tooltip("退出偏移。")]
    public Vector2 ExitOffset = new Vector2(0f, 120f);

    [Tooltip("进入/退出过渡时长。")]
    public float TransitionDuration = 0.45f;

    private readonly List<ReplicaV3ParameterDefinition> mParameters = new List<ReplicaV3ParameterDefinition>
    {
        new ReplicaV3ParameterDefinition { Id = "step_duration", DisplayName = "转场时长", Description = "像素化展开/收起耗时。", Kind = ReplicaV3ParameterKind.Float, Min = 0.05f, Max = 2f, Step = 0.01f },
        new ReplicaV3ParameterDefinition { Id = "grid_size", DisplayName = "网格尺寸", Description = "仅展示参数，实际由像素块数量决定。", Kind = ReplicaV3ParameterKind.Float, Min = 2f, Max = 20f, Step = 1f },
        new ReplicaV3ParameterDefinition { Id = "once", DisplayName = "只触发一次", Description = "开启后退出不恢复默认层。", Kind = ReplicaV3ParameterKind.Bool }
    };

    private readonly List<int> mPixelOrder = new List<int>();
    private bool mIsActive;
    private bool mAnimating;

    protected override void OnEffectInitialize()
    {
        EnsureBindings();
        ApplyModel();
        HideAllPixels();
        SetActiveLayer(false);
        SetCanvasAlpha(1f);
        SetLifecycleLooping();
    }

    protected override void OnPlayIn()
    {
        EnsureBindings();
        KillTrackedTweens(false);

        var duration = Mathf.Max(0.05f, TransitionDuration);
        if (Card != null)
        {
            Card.anchoredPosition = EnterOffset;
            TrackTween(Card.DOAnchorPos(Vector2.zero, duration).SetEase(Ease.OutCubic));
        }

        if (EffectCanvasGroup != null)
        {
            SetCanvasAlpha(0f);
            TrackTween(EffectCanvasGroup.DOFade(1f, duration).SetEase(Ease.OutCubic));
        }

        SetLifecycleLooping();
    }

    protected override void OnPlayOut(Action onComplete)
    {
        KillTrackedTweens(false);
        var duration = Mathf.Max(0.05f, TransitionDuration * 0.8f);
        var pending = 0;
        var finished = 0;

        void TryComplete()
        {
            finished++;
            if (finished >= pending)
            {
                onComplete?.Invoke();
            }
        }

        if (Card != null)
        {
            pending++;
            TrackTween(Card.DOAnchorPos(ExitOffset, duration).SetEase(Ease.InCubic).OnComplete(TryComplete));
        }

        if (EffectCanvasGroup != null)
        {
            pending++;
            TrackTween(EffectCanvasGroup.DOFade(0f, duration).SetEase(Ease.InCubic).OnComplete(TryComplete));
        }

        if (pending == 0)
        {
            onComplete?.Invoke();
        }
    }

    protected override void OnEffectReset()
    {
        KillTrackedTweens(false);
        mAnimating = false;
        mIsActive = false;
        ApplyModel();
        HideAllPixels();
        SetActiveLayer(false);
        if (Card != null)
        {
            Card.anchoredPosition = Vector2.zero;
        }

        SetCanvasAlpha(1f);
        SetLifecycleLooping();
    }

    public override IReadOnlyList<ReplicaV3ParameterDefinition> GetParameterDefinitions() => mParameters;

    public override bool TryGetFloatParameter(string parameterId, out float value)
    {
        switch (parameterId)
        {
            case "step_duration": value = AnimationStepDuration; return true;
            case "grid_size": value = GridSize; return true;
            default: value = 0f; return false;
        }
    }

    public override bool TrySetFloatParameter(string parameterId, float value)
    {
        switch (parameterId)
        {
            case "step_duration": AnimationStepDuration = Mathf.Clamp(value, 0.05f, 2f); return true;
            case "grid_size": GridSize = Mathf.RoundToInt(Mathf.Clamp(value, 2f, 20f)); return true;
            default: return false;
        }
    }

    public override bool TryGetBoolParameter(string parameterId, out bool value)
    {
        switch (parameterId)
        {
            case "once": value = Once; return true;
            default: value = false; return false;
        }
    }

    public override bool TrySetBoolParameter(string parameterId, bool value)
    {
        switch (parameterId)
        {
            case "once": Once = value; return true;
            default: return false;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (mAnimating || mIsActive)
        {
            return;
        }

        AnimatePixels(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (mAnimating || !mIsActive || Once)
        {
            return;
        }

        AnimatePixels(false);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (mAnimating)
        {
            return;
        }

        if (!mIsActive)
        {
            AnimatePixels(true);
        }
        else if (!Once)
        {
            AnimatePixels(false);
        }
    }

    public void OnSelect(BaseEventData eventData)
    {
        if (mAnimating || mIsActive)
        {
            return;
        }

        AnimatePixels(true);
    }

    public void OnDeselect(BaseEventData eventData)
    {
        if (mAnimating || !mIsActive || Once)
        {
            return;
        }

        AnimatePixels(false);
    }

    private Camera ResolveInteractionCamera(PointerEventData eventData = null)
    {
        var target = InteractionHitSource != null
            ? InteractionHitSource
            : (Card != null ? Card : (EffectRoot != null ? EffectRoot : transform as RectTransform));

        if (eventData != null)
        {
            return ResolveReliableEventCamera(eventData, target);
        }

        return ResolveReliableEventCamera(target);
    }

    private void EnsureBindings()
    {
        if (Card == null)
        {
            Card = EffectRoot != null ? EffectRoot : transform as RectTransform;
        }

        if (InteractionHitSource == null)
        {
            InteractionHitSource = Card;
        }
    }

    private void ApplyModel()
    {
        if (DefaultLabel != null)
        {
            DefaultLabel.text = string.IsNullOrWhiteSpace(DefaultLabelText) ? "DEFAULT" : DefaultLabelText;
        }

        if (ActiveLabel != null)
        {
            ActiveLabel.text = string.IsNullOrWhiteSpace(ActiveLabelText) ? "ACTIVE" : ActiveLabelText;
        }

        if (DefaultImage != null)
        {
            DefaultImage.sprite = DefaultSprite;
            DefaultImage.type = Image.Type.Simple;
            DefaultImage.preserveAspect = true;
            DefaultImage.color = DefaultSprite != null ? Color.white : new Color(1f, 1f, 1f, 0.001f);
        }

        if (ActiveImage != null)
        {
            ActiveImage.sprite = ActiveSprite;
            ActiveImage.type = Image.Type.Simple;
            ActiveImage.preserveAspect = true;
            ActiveImage.color = ActiveSprite != null ? Color.white : new Color(1f, 1f, 1f, 0.001f);
        }
    }

    private void AnimatePixels(bool activate)
    {
        EnsureBindings();
        KillTrackedTweens(false);

        mIsActive = activate;
        mAnimating = true;
        HideAllPixels();

        var total = PixelBlocks != null ? PixelBlocks.Count : 0;
        if (total <= 0)
        {
            SetActiveLayer(activate);
            mAnimating = false;
            return;
        }

        EnsureOrder(total);
        ShuffleOrder();

        var stepDuration = Mathf.Max(0.01f, AnimationStepDuration);
        var seq = DOTween.Sequence();
        int lastShownIndex = -1;
        int lastHiddenIndex = -1;

        // Phase 1: Fade In (Pixels appear)
        seq.Append(DOVirtual.Float(0f, 1f, stepDuration, x =>
        {
            int targetIndex = Mathf.FloorToInt(x * total);
            for (int i = lastShownIndex + 1; i <= targetIndex && i < total; i++)
            {
                var index = mPixelOrder[i];
                if (index >= 0 && index < PixelBlocks.Count && PixelBlocks[index] != null)
                {
                    PixelBlocks[index].gameObject.SetActive(true);
                }
            }
            lastShownIndex = Mathf.Max(lastShownIndex, targetIndex);
        }).SetEase(Ease.Linear));

        // Switch Layer
        seq.AppendCallback(() => SetActiveLayer(activate));

        // Phase 2: Fade Out (Pixels disappear)
        seq.Append(DOVirtual.Float(0f, 1f, stepDuration, x =>
        {
            int targetIndex = Mathf.FloorToInt(x * total);
            for (int i = lastHiddenIndex + 1; i <= targetIndex && i < total; i++)
            {
                var index = mPixelOrder[i];
                if (index >= 0 && index < PixelBlocks.Count && PixelBlocks[index] != null)
                {
                    PixelBlocks[index].gameObject.SetActive(false);
                }
            }
            lastHiddenIndex = Mathf.Max(lastHiddenIndex, targetIndex);
        }).SetEase(Ease.Linear));

        seq.OnComplete(() => mAnimating = false);
        seq.OnKill(() => mAnimating = false);


        TrackTween(seq);
    }

    private void SetActiveLayer(bool active)
    {
        if (DefaultLayer != null)
        {
            DefaultLayer.gameObject.SetActive(!active);
        }

        if (ActiveLayer != null)
        {
            ActiveLayer.gameObject.SetActive(active);
        }
    }

    private void HideAllPixels()
    {
        if (PixelBlocks == null)
        {
            return;
        }

        for (var i = 0; i < PixelBlocks.Count; i++)
        {
            var pixel = PixelBlocks[i];
            if (pixel != null)
            {
                pixel.gameObject.SetActive(false);
            }
        }
    }

    private void EnsureOrder(int total)
    {
        if (mPixelOrder.Count == total)
        {
            return;
        }

        mPixelOrder.Clear();
        for (var i = 0; i < total; i++)
        {
            mPixelOrder.Add(i);
        }
    }

    private void ShuffleOrder()
    {
        var rng = new System.Random(Environment.TickCount);
        for (var i = mPixelOrder.Count - 1; i > 0; i--)
        {
            var j = rng.Next(i + 1);
            (mPixelOrder[i], mPixelOrder[j]) = (mPixelOrder[j], mPixelOrder[i]);
        }
    }

    private void OnDrawGizmos()
    {
        var hitSource = InteractionHitSource != null
            ? InteractionHitSource
            : (Card != null ? Card : (EffectRoot != null ? EffectRoot : transform as RectTransform));
        DrawInteractionRangeGizmo(ShowInteractionRange, hitSource, InteractionRangeDependency, InteractionRangePadding);
    }
}

