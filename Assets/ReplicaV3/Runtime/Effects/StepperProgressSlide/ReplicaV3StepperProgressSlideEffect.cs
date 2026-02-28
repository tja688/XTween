using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public sealed class ReplicaV3StepperProgressSlideEffect : ReplicaV3EffectBase
{
    [Header("组件绑定")]
    public RectTransform Card;
    public RectTransform Viewport;
    public Button BackButton;
    public Button NextButton;
    public Text NextLabel;
    public RectTransform CompletionRect;
    public Image CompletionImage;

    [Tooltip("步数节点背景或图案")]
    public List<Image> StepNodeImages = new List<Image>();
    [Tooltip("步数节点上的数字或标记")]
    public List<Text> StepNodeTexts = new List<Text>();
    [Tooltip("节点间的连接层（用来做进度填充）")]
    public List<Image> ConnectorFills = new List<Image>();
    [Tooltip("底部显示内容，应当与步骤数一致")]
    public List<RectTransform> StepContents = new List<RectTransform>();
    [Tooltip("支持点击切换的按钮触发器")]
    public List<Button> StepButtons = new List<Button>();

    [Header("交互范围")]
    public RectTransform InteractionHitSource;
    public RectTransform InteractionRangeDependency;
    public bool ShowInteractionRange = true;
    public float InteractionRangePadding = 0f;

    [Header("Motion 参数")]
    public float SlideOutDuration = 0.36f;
    public float SlideInDuration = 0.38f;
    public float HeightDuration = 0.35f;
    public float ConnectorFillDuration = 0.30f;
    public float ViewportHeightPadding = 12f;

    [Header("Visual 参数")]
    public Color NodeInactive = new Color(0.12f, 0.14f, 0.19f, 1f);
    public Color NodeActive = new Color(0.32f, 0.15f, 1f, 1f);
    public Color NodeComplete = new Color(0.32f, 0.15f, 1f, 1f);
    public Color NodeTextInactive = new Color(0.63f, 0.66f, 0.74f, 1f);
    public Color CompletionColor = new Color(0.18f, 0.52f, 0.34f, 0.92f);

    [Header("Animation 参数")]
    public float EnterOffset = 180f;
    public float ExitOffset = 180f;

    private readonly List<ReplicaV3ParameterDefinition> mParameters = new List<ReplicaV3ParameterDefinition>
    {
        new ReplicaV3ParameterDefinition { Id = "slide_duration", DisplayName = "滑块切页时间", Description = "步骤之间滑动切换时间", Kind = ReplicaV3ParameterKind.Float, Min = 0.1f, Max = 1.5f, Step = 0.05f }
    };

    private int mCurrentStep;
    private bool mCompleted;
    private Vector2 mBaseCardPos;

    protected override void OnEffectInitialize()
    {
        EnsureBindings();
        SetCanvasAlpha(1f);
        SetLifecycleLooping();

        if (Card != null) mBaseCardPos = Card.anchoredPosition;

        WireEvents();
        SwitchStep(0, 0, false);
    }

    private void EnsureBindings()
    {
        if (EffectRoot == null)
        {
            EffectRoot = transform as RectTransform;
        }

        if (Card == null && EffectRoot != null)
        {
            Card = EffectRoot.Find("StepperCard") as RectTransform;
        }

        if (Viewport == null && Card != null)
        {
            Viewport = Card.Find("ViewportFrame/Viewport") as RectTransform;
        }

        if (BackButton == null && Card != null)
        {
            var back = Card.Find("Footer/Back");
            if (back != null)
            {
                BackButton = back.GetComponent<Button>();
            }
        }

        if (NextButton == null && Card != null)
        {
            var next = Card.Find("Footer/Next");
            if (next != null)
            {
                NextButton = next.GetComponent<Button>();
            }
        }

        if (NextLabel == null && NextButton != null)
        {
            var nextLabel = NextButton.transform.Find("NextLabel");
            if (nextLabel != null)
            {
                NextLabel = nextLabel.GetComponent<Text>();
            }
        }

        if (CompletionRect == null && Viewport != null)
        {
            CompletionRect = Viewport.Find("Completion") as RectTransform;
        }

        if (CompletionImage == null && CompletionRect != null)
        {
            CompletionImage = CompletionRect.GetComponent<Image>();
        }

        if (InteractionHitSource == null)
        {
            InteractionHitSource = Card != null ? Card : (EffectRoot != null ? EffectRoot : transform as RectTransform);
        }

        if (Card != null)
        {
            var stepRow = Card.Find("StepRow");
            if (stepRow != null && (StepButtons.Count == 0 || StepNodeImages.Count == 0 || StepNodeTexts.Count == 0))
            {
                StepButtons.Clear();
                StepNodeImages.Clear();
                StepNodeTexts.Clear();
                ConnectorFills.Clear();

                for (var i = 0; i < stepRow.childCount; i++)
                {
                    var step = stepRow.GetChild(i) as RectTransform;
                    if (step == null)
                    {
                        continue;
                    }

                    var nodeButton = step.Find("Node")?.GetComponent<Button>();
                    var nodeImage = step.Find("Node/NodeVisual")?.GetComponent<Image>();
                    var nodeText = step.Find("Node/NodeVisual/NodeLabel")?.GetComponent<Text>();

                    if (nodeButton != null)
                    {
                        StepButtons.Add(nodeButton);
                    }

                    if (nodeImage != null)
                    {
                        StepNodeImages.Add(nodeImage);
                    }

                    if (nodeText != null)
                    {
                        StepNodeTexts.Add(nodeText);
                    }

                    var connectorFill = step.Find("Connector/Fill")?.GetComponent<Image>();
                    if (connectorFill != null)
                    {
                        ConnectorFills.Add(connectorFill);
                    }
                }
            }
        }

        if (StepContents.Count == 0 && Viewport != null)
        {
            StepContents.Clear();
            for (var i = 0; i < Viewport.childCount; i++)
            {
                var child = Viewport.GetChild(i) as RectTransform;
                if (child == null)
                {
                    continue;
                }

                if (child.name.StartsWith("Content_", StringComparison.Ordinal))
                {
                    StepContents.Add(child);
                }
            }
        }
    }

    private void WireEvents()
    {
        EnsureBindings();
        UnwireEvents();

        for (int i = 0; i < StepButtons.Count; i++)
        {
            int capture = i;
            if (StepButtons[i] != null) StepButtons[i].onClick.AddListener(() => OnStepClicked(capture));
        }

        if (BackButton != null) BackButton.onClick.AddListener(OnBackClicked);
        if (NextButton != null) NextButton.onClick.AddListener(OnNextClicked);
    }

    private void UnwireEvents()
    {
        for (int i = 0; i < StepButtons.Count; i++)
        {
            if (StepButtons[i] != null) StepButtons[i].onClick.RemoveAllListeners();
        }

        if (BackButton != null) BackButton.onClick.RemoveAllListeners();
        if (NextButton != null) NextButton.onClick.RemoveAllListeners();
    }

    protected override void OnPlayIn()
    {
        EnsureBindings();
        KillTrackedTweens(false);
        if (Card != null)
        {
            Card.anchoredPosition = mBaseCardPos + new Vector2(0, -EnterOffset);
            TrackTween(Card.DOAnchorPos(mBaseCardPos, 0.4f).SetEase(Ease.OutCubic));
        }

        SetCanvasAlpha(0f);
        if (EffectCanvasGroup != null)
        {
            TrackTween(EffectCanvasGroup.DOFade(1f, 0.3f).SetEase(Ease.OutCubic));
        }

        SetLifecycleLooping();
        SwitchStep(0, 0, false);
    }

    protected override void OnPlayOut(Action onComplete)
    {
        EnsureBindings();
        KillTrackedTweens(false);
        if (Card != null)
        {
            TrackTween(Card.DOAnchorPos(mBaseCardPos + new Vector2(0, ExitOffset), 0.3f).SetEase(Ease.InCubic));
        }

        if (EffectCanvasGroup != null)
        {
            TrackTween(EffectCanvasGroup.DOFade(0f, 0.25f).SetEase(Ease.InCubic).OnComplete(() => onComplete?.Invoke()));
            return;
        }

        onComplete?.Invoke();
    }

    protected override void OnEffectTick(float deltaTime, float unscaledDeltaTime)
    {
    }

    protected override void OnEffectReset()
    {
        KillTrackedTweens(false);
        EnsureBindings();
        UnwireEvents();
        WireEvents();

        mCompleted = false;
        mCurrentStep = 0;

        if (CompletionRect != null)
        {
            CompletionRect.gameObject.SetActive(false);
        }

        if (Card != null)
        {
            Card.anchoredPosition = mBaseCardPos;
        }

        if (NextButton != null)
        {
            NextButton.onClick.RemoveAllListeners();
            NextButton.onClick.AddListener(OnNextClicked);
            NextButton.interactable = true;
        }

        if (BackButton != null)
        {
            BackButton.interactable = false;
        }

        if (NextLabel != null)
        {
            NextLabel.text = "Continue";
        }

        SwitchStep(0, 0, false);
        SetCanvasAlpha(1f);
        SetLifecycleLooping();
    }

    protected override void OnEffectDispose()
    {
        UnwireEvents();
    }

    private void OnStepClicked(int target)
    {
        if (mCompleted || target == mCurrentStep) return;
        int direction = target > mCurrentStep ? 1 : -1;
        SwitchStep(target, direction, true);
    }

    private void OnBackClicked()
    {
        if (mCompleted || mCurrentStep <= 0) return;
        SwitchStep(mCurrentStep - 1, -1, true);
    }

    private void OnNextClicked()
    {
        if (mCompleted) return;
        if (mCurrentStep < StepContents.Count - 1)
        {
            SwitchStep(mCurrentStep + 1, 1, true);
        }
        else
        {
            CompleteFlow();
        }
    }

    private void SwitchStep(int target, int direction, bool animate)
    {
        EnsureBindings();
        if (StepContents.Count == 0) return;
        target = Mathf.Clamp(target, 0, StepContents.Count - 1);
        mCompleted = false;

        if (CompletionRect != null) CompletionRect.gameObject.SetActive(false);

        int previous = Mathf.Clamp(mCurrentStep, 0, StepContents.Count - 1);
        mCurrentStep = target;

        for (int i = 0; i < StepContents.Count; i++)
        {
            if (StepContents[i] == null) continue;
            bool shouldActive = i == previous || i == mCurrentStep;
            StepContents[i].gameObject.SetActive(shouldActive);
        }

        RectTransform incoming = StepContents[mCurrentStep];
        RectTransform outgoing = StepContents[previous];

        if (incoming != null) incoming.SetAsLastSibling();
        UpdateStepVisuals(animate);

        float targetHeight = incoming != null ? incoming.sizeDelta.y + ViewportHeightPadding : 0;
        TweenViewportHeight(targetHeight, animate ? HeightDuration : 0f);

        if (!animate || previous == mCurrentStep || incoming == null || outgoing == null)
        {
            if (incoming != null) incoming.anchoredPosition = Vector2.zero;
            if (outgoing != null)
            {
                outgoing.anchoredPosition = Vector2.zero;
                outgoing.gameObject.SetActive(previous == mCurrentStep);
            }
            return;
        }

        incoming.DOKill();
        outgoing.DOKill();

        float width = Mathf.Max(280f, Viewport != null ? Viewport.rect.width : 500f);
        float enterFrom = direction > 0 ? width : -width;
        float exitTo = direction > 0 ? -width * 0.5f : width * 0.5f;

        incoming.anchoredPosition = new Vector2(enterFrom, 0f);
        TrackTween(outgoing.DOAnchorPosX(exitTo, SlideOutDuration).SetEase(Ease.OutCubic));
        TrackTween(incoming.DOAnchorPosX(0f, SlideInDuration).SetEase(Ease.OutCubic).OnComplete(() =>
        {
            if (outgoing != null && outgoing != incoming) outgoing.gameObject.SetActive(false);
        }));
    }

    private void TweenViewportHeight(float targetHeight, float duration)
    {
        if (Viewport == null) return;
        Viewport.DOKill();
        if (duration <= 0.0001f)
        {
            Viewport.sizeDelta = new Vector2(Viewport.sizeDelta.x, targetHeight);
            return;
        }
        TrackTween(Viewport.DOSizeDelta(new Vector2(Viewport.sizeDelta.x, targetHeight), duration).SetEase(Ease.OutCubic));
    }

    private void CompleteFlow()
    {
        mCompleted = true;
        for (int i = 0; i < StepContents.Count; i++)
        {
            if (StepContents[i] != null) StepContents[i].DOKill();
            if (StepContents[i] != null) StepContents[i].gameObject.SetActive(false);
        }

        UpdateStepVisuals(true);

        if (BackButton != null) BackButton.interactable = false;
        if (NextButton != null)
        {
            NextButton.interactable = true;
            NextButton.onClick.RemoveAllListeners();
            NextButton.onClick.AddListener(RestartFlow);
        }
        if (NextLabel != null) NextLabel.text = "Restart";

        TweenViewportHeight(0f, 0.30f);

        if (CompletionRect != null && CompletionImage != null)
        {
            TrackTween(DOVirtual.DelayedCall(0.30f, () =>
            {
                CompletionRect.gameObject.SetActive(true);
                Color c = CompletionColor;
                c.a = 0f;
                CompletionImage.color = c;
                TrackTween(CompletionImage.DOFade(CompletionColor.a, 0.28f).SetEase(Ease.OutQuad));
            }, false));
        }
    }

    private void RestartFlow()
    {
        if (NextButton != null)
        {
            NextButton.onClick.RemoveAllListeners();
            NextButton.onClick.AddListener(OnNextClicked);
        }
        if (NextLabel != null) NextLabel.text = "Continue";
        if (BackButton != null) BackButton.interactable = true;
        SwitchStep(0, 1, true);
    }

    private void UpdateStepVisuals(bool animate)
    {
        for (int i = 0; i < StepNodeImages.Count; i++)
        {
            int state = i < mCurrentStep ? 2 : i == mCurrentStep ? 1 : 0;
            if (mCompleted) state = 2;

            Image node = StepNodeImages[i];
            Text label = i < StepNodeTexts.Count ? StepNodeTexts[i] : null;

            Color targetColor = state == 0 ? NodeInactive : state == 1 ? NodeActive : NodeComplete;
            Color textColor = state == 0 ? NodeTextInactive : Color.white;

            if (node != null)
            {
                if (animate) TrackTween(node.DOColor(targetColor, 0.24f).SetEase(Ease.OutQuad));
                else node.color = targetColor;
            }

            if (label != null)
            {
                if (animate) TrackTween(label.DOColor(textColor, 0.24f).SetEase(Ease.OutQuad));
                else label.color = textColor;
                label.text = state == 2 ? "\u2713" : state == 1 ? "\u2022" : (i + 1).ToString();
            }
        }

        for (int i = 0; i < ConnectorFills.Count; i++)
        {
            Image fillImage = ConnectorFills[i];
            if (fillImage != null)
            {
                float fillTarget = mCompleted || mCurrentStep > i ? 1f : 0f;
                if (animate) TrackTween(fillImage.DOFillAmount(fillTarget, ConnectorFillDuration).SetEase(Ease.OutCubic));
                else fillImage.fillAmount = fillTarget;
            }
        }

        if (BackButton != null) BackButton.interactable = !mCompleted && mCurrentStep > 0;
        if (!mCompleted && NextLabel != null)
        {
            var lastStepIndex = Mathf.Max(0, StepContents.Count - 1);
            NextLabel.text = mCurrentStep >= lastStepIndex ? "Complete" : "Continue";
        }
    }

    public override IReadOnlyList<ReplicaV3ParameterDefinition> GetParameterDefinitions() => mParameters;

    public override bool TryGetFloatParameter(string parameterId, out float value)
    {
        if (parameterId == "slide_duration")
        {
            value = SlideInDuration;
            return true;
        }
        value = 0f;
        return false;
    }

    public override bool TrySetFloatParameter(string parameterId, float value)
    {
        if (parameterId == "slide_duration")
        {
            SlideInDuration = value;
            SlideOutDuration = value * 0.95f;
            return true;
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
        var hitSource = InteractionHitSource != null ? InteractionHitSource : (Card != null ? Card : (EffectRoot != null ? EffectRoot : transform as RectTransform));
        DrawInteractionRangeGizmo(ShowInteractionRange, hitSource, InteractionRangeDependency, InteractionRangePadding);
    }
}
