using System;
using System.Collections.Generic;
using System.Globalization;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public sealed class ReplicaV3RotatingTextEffect : ReplicaV3EffectBase
{
    [Header("组件绑定（RotatingText）")]
    [Tooltip("整体内容根节点（用于进出场位移）。")]
    public RectTransform ContentRoot;

    [Tooltip("文字显示视口（建议挂 RectMask2D）。")]
    public RectTransform Viewport;

    [Tooltip("可选文本模板。未指定时将自动创建。")]
    public Text TextTemplate;

    [Header("参数（可在参数面板实时调）")]
    [Tooltip("候选文案列表。")]
    public string[] Texts = { "Fast", "Clean", "Reusable" };

    [Tooltip("自动轮换间隔。")]
    public float RotationInterval = 2f;

    [Tooltip("是否自动轮换。")]
    public bool Auto = true;

    [Tooltip("是否循环回第一项。")]
    public bool Loop = true;

    [Tooltip("进入时 Y 偏移。")]
    public float EnterOffsetY = 120f;

    [Tooltip("退出时 Y 偏移。")]
    public float ExitOffsetY = 150f;

    [Tooltip("进入时长。")]
    public float EnterDuration = 0.35f;

    [Tooltip("退出时长。")]
    public float ExitDuration = 0.3f;

    [Tooltip("元素错峰间隔。")]
    public float StaggerDuration = 0.03f;

    [Tooltip("字体大小。")]
    public int FontSize = 64;

    [Tooltip("字体样式。")]
    public FontStyle FontStyle = FontStyle.Bold;

    [Tooltip("对齐方式。")]
    public TextAnchor Alignment = TextAnchor.MiddleCenter;

    [Tooltip("文本颜色。")]
    public Color TextColor = Color.white;

    [Tooltip("切分方式。")]
    public ReplicaV3RotatingTextSplitBy SplitBy = ReplicaV3RotatingTextSplitBy.Characters;

    [Tooltip("错峰起点。")]
    public ReplicaV3RotatingTextStaggerFrom StaggerFrom = ReplicaV3RotatingTextStaggerFrom.First;

    [Header("进入/退出")]
    [Tooltip("整体进入偏移。")]
    public Vector2 EnterOffset = new Vector2(0f, -160f);

    [Tooltip("整体退出偏移。")]
    public Vector2 ExitOffset = new Vector2(0f, 160f);

    [Tooltip("整体进出场时长。")]
    public float TransitionDuration = 0.4f;

    private readonly List<ReplicaV3ParameterDefinition> mParameters = new List<ReplicaV3ParameterDefinition>
    {
        new ReplicaV3ParameterDefinition { Id = "rotation_interval", DisplayName = "轮换间隔", Description = "自动轮换的触发间隔。", Kind = ReplicaV3ParameterKind.Float, Min = 0.2f, Max = 10f, Step = 0.1f },
        new ReplicaV3ParameterDefinition { Id = "stagger_duration", DisplayName = "错峰间隔", Description = "字符/单词的错峰时间。", Kind = ReplicaV3ParameterKind.Float, Min = 0f, Max = 0.4f, Step = 0.01f },
        new ReplicaV3ParameterDefinition { Id = "enter_duration", DisplayName = "进入时长", Description = "新文案进入动画时长。", Kind = ReplicaV3ParameterKind.Float, Min = 0.05f, Max = 2f, Step = 0.01f },
        new ReplicaV3ParameterDefinition { Id = "exit_duration", DisplayName = "退出时长", Description = "旧文案退出动画时长。", Kind = ReplicaV3ParameterKind.Float, Min = 0.05f, Max = 2f, Step = 0.01f },
        new ReplicaV3ParameterDefinition { Id = "auto", DisplayName = "自动轮换", Description = "是否定时切换到下一条文案。", Kind = ReplicaV3ParameterKind.Bool },
        new ReplicaV3ParameterDefinition { Id = "loop", DisplayName = "循环", Description = "到末尾后是否回到第一条。", Kind = ReplicaV3ParameterKind.Bool }
    };

    private readonly List<Tween> mSwitchTweens = new List<Tween>();
    private float mAutoTimer;
    private int mCurrentIndex;
    private ReplicaV3RotatingTextContainer mCurrent;

    protected override void OnEffectInitialize()
    {
        EnsureBindings();
        mCurrentIndex = 0;
        mAutoTimer = 0f;
        ShowCurrentText(false);
        SetCanvasAlpha(1f);
        SetLifecycleLooping();
    }

    protected override void OnEffectTick(float deltaTime, float unscaledDeltaTime)
    {
        if (!Auto || RotationInterval <= 0.01f)
        {
            return;
        }

        mAutoTimer += Mathf.Max(0f, unscaledDeltaTime);
        if (mAutoTimer < Mathf.Max(0.2f, RotationInterval))
        {
            return;
        }

        mAutoTimer = 0f;
        Next(true);
    }

    protected override void OnPlayIn()
    {
        EnsureBindings();
        KillTrackedTweens(false);

        var duration = Mathf.Max(0.05f, TransitionDuration);
        if (ContentRoot != null)
        {
            ContentRoot.anchoredPosition = EnterOffset;
            TrackTween(ContentRoot.DOAnchorPos(Vector2.zero, duration).SetEase(Ease.OutCubic));
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
        KillSwitchTweens();

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

        if (ContentRoot != null)
        {
            pending++;
            TrackTween(ContentRoot.DOAnchorPos(ExitOffset, duration).SetEase(Ease.InCubic).OnComplete(TryComplete));
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
        KillSwitchTweens();
        DestroyContainer(ref mCurrent);
        mCurrentIndex = 0;
        mAutoTimer = 0f;
        ShowCurrentText(false);
        if (ContentRoot != null)
        {
            ContentRoot.anchoredPosition = Vector2.zero;
        }

        SetCanvasAlpha(1f);
        SetLifecycleLooping();
    }

    protected override void OnEffectDispose()
    {
        KillSwitchTweens();
        DestroyContainer(ref mCurrent);
    }

    public override IReadOnlyList<ReplicaV3ParameterDefinition> GetParameterDefinitions() => mParameters;

    public override bool TryGetFloatParameter(string parameterId, out float value)
    {
        switch (parameterId)
        {
            case "rotation_interval": value = RotationInterval; return true;
            case "stagger_duration": value = StaggerDuration; return true;
            case "enter_duration": value = EnterDuration; return true;
            case "exit_duration": value = ExitDuration; return true;
            default: value = 0f; return false;
        }
    }

    public override bool TrySetFloatParameter(string parameterId, float value)
    {
        switch (parameterId)
        {
            case "rotation_interval": RotationInterval = Mathf.Clamp(value, 0.2f, 10f); return true;
            case "stagger_duration": StaggerDuration = Mathf.Clamp(value, 0f, 0.4f); return true;
            case "enter_duration": EnterDuration = Mathf.Clamp(value, 0.05f, 2f); return true;
            case "exit_duration": ExitDuration = Mathf.Clamp(value, 0.05f, 2f); return true;
            default: return false;
        }
    }

    public override bool TryGetBoolParameter(string parameterId, out bool value)
    {
        switch (parameterId)
        {
            case "auto": value = Auto; return true;
            case "loop": value = Loop; return true;
            default: value = false; return false;
        }
    }

    public override bool TrySetBoolParameter(string parameterId, bool value)
    {
        switch (parameterId)
        {
            case "auto": Auto = value; return true;
            case "loop": Loop = value; return true;
            default: return false;
        }
    }

    private void EnsureBindings()
    {
        if (ContentRoot == null)
        {
            ContentRoot = EffectRoot != null ? EffectRoot : transform as RectTransform;
        }

        if (Viewport == null)
        {
            Viewport = ContentRoot;
        }
    }

    private void Next(bool animated)
    {
        var texts = SafeTexts(Texts);
        if (texts.Length <= 1)
        {
            return;
        }

        var nextIndex = mCurrentIndex == texts.Length - 1 ? (Loop ? 0 : mCurrentIndex) : mCurrentIndex + 1;
        if (nextIndex == mCurrentIndex)
        {
            return;
        }

        SwitchTo(nextIndex, animated);
    }

    private void SwitchTo(int index, bool animated)
    {
        var texts = SafeTexts(Texts);
        if (texts.Length <= 0)
        {
            return;
        }

        index = Mathf.Clamp(index, 0, texts.Length - 1);
        mCurrentIndex = index;
        mAutoTimer = 0f;

        if (!animated || mCurrent == null)
        {
            KillSwitchTweens();
            DestroyContainer(ref mCurrent);
            mCurrent = BuildContainer(Viewport, texts[mCurrentIndex]);
            ApplyElementAlpha(mCurrent.Elements, 1f);
            if (mCurrent.Root != null)
            {
                mCurrent.Root.anchoredPosition = Vector2.zero;
            }
            return;
        }

        KillSwitchTweens();

        var outgoing = mCurrent;
        var incoming = BuildContainer(Viewport, texts[mCurrentIndex]);
        if (incoming.Root != null)
        {
            incoming.Root.anchoredPosition = new Vector2(0f, EnterOffsetY);
        }
        ApplyElementAlpha(incoming.Elements, 0f);

        var enterDuration = Mathf.Max(0.01f, EnterDuration);
        var exitDuration = Mathf.Max(0.01f, ExitDuration);

        if (incoming.Root != null)
        {
            mSwitchTweens.Add(TrackTween(incoming.Root.DOAnchorPosY(0f, enterDuration).SetEase(Ease.OutCubic)));
        }

        if (outgoing.Root != null)
        {
            mSwitchTweens.Add(TrackTween(outgoing.Root.DOAnchorPosY(-ExitOffsetY, exitDuration).SetEase(Ease.InCubic)));
        }

        var randomIndex = incoming.Elements.Count > 0 ? UnityEngine.Random.Range(0, incoming.Elements.Count) : 0;
        var maxEnterDelay = FadeElements(incoming.Elements, 1f, enterDuration, Ease.OutCubic, randomIndex);
        var maxExitDelay = FadeElements(outgoing.Elements, 0f, exitDuration, Ease.InCubic, randomIndex);
        var cleanupDelay = Mathf.Max(enterDuration + maxEnterDelay, exitDuration + maxExitDelay) + 0.05f;
        mSwitchTweens.Add(TrackTween(DOVirtual.DelayedCall(cleanupDelay, () =>
        {
            if (outgoing.Root != null)
            {
                Destroy(outgoing.Root.gameObject);
            }
        })));

        mCurrent = incoming;
    }

    private void ShowCurrentText(bool animated)
    {
        var texts = SafeTexts(Texts);
        if (texts.Length <= 0)
        {
            texts = new[] { string.Empty };
        }

        mCurrentIndex = Mathf.Clamp(mCurrentIndex, 0, texts.Length - 1);
        SwitchTo(mCurrentIndex, animated);
    }

    private float FadeElements(List<Text> elements, float targetAlpha, float duration, Ease ease, int randomIndex)
    {
        if (elements == null || elements.Count <= 0)
        {
            return 0f;
        }

        var total = elements.Count;
        var maxDelay = 0f;
        for (var i = 0; i < total; i++)
        {
            var element = elements[i];
            if (element == null)
            {
                continue;
            }

            var delay = GetStaggerDelay(i, total, randomIndex);
            maxDelay = Mathf.Max(maxDelay, delay);
            mSwitchTweens.Add(TrackTween(element.DOFade(targetAlpha, duration).SetDelay(delay).SetEase(ease)));
        }

        return maxDelay;
    }

    private float GetStaggerDelay(int index, int total, int randomIndex)
    {
        var step = Mathf.Max(0f, StaggerDuration);
        if (step <= 0.0001f)
        {
            return 0f;
        }

        switch (StaggerFrom)
        {
            case ReplicaV3RotatingTextStaggerFrom.Last:
                return (total - 1 - index) * step;
            case ReplicaV3RotatingTextStaggerFrom.Center:
                return Mathf.Abs((total / 2) - index) * step;
            case ReplicaV3RotatingTextStaggerFrom.Random:
                return Mathf.Abs(randomIndex - index) * step;
            case ReplicaV3RotatingTextStaggerFrom.First:
            default:
                return index * step;
        }
    }

    private ReplicaV3RotatingTextContainer BuildContainer(RectTransform parent, string text)
    {
        var root = new GameObject("TextContainer", typeof(RectTransform)).GetComponent<RectTransform>();
        root.SetParent(parent, false);
        root.anchorMin = new Vector2(0.5f, 0.5f);
        root.anchorMax = new Vector2(0.5f, 0.5f);
        root.pivot = new Vector2(0.5f, 0.5f);
        root.anchoredPosition = Vector2.zero;
        root.sizeDelta = Vector2.zero;

        var elements = new List<Text>();
        if (SplitBy == ReplicaV3RotatingTextSplitBy.Lines)
        {
            var vlg = root.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.spacing = 0f;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = false;
            vlg.childForceExpandHeight = false;

            var fitter = root.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var lines = (text ?? string.Empty).Split('\n');
            for (var i = 0; i < lines.Length; i++)
            {
                elements.Add(CreateTextElement($"Line_{i}", root, lines[i]));
            }
        }
        else
        {
            var hlg = root.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.spacing = 0f;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;

            var fitter = root.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            if (SplitBy == ReplicaV3RotatingTextSplitBy.Words)
            {
                var words = (text ?? string.Empty).Split(' ');
                for (var i = 0; i < words.Length; i++)
                {
                    elements.Add(CreateTextElement($"Word_{i}", root, words[i]));
                    if (i < words.Length - 1)
                    {
                        elements.Add(CreateTextElement($"Space_{i}", root, " "));
                    }
                }
            }
            else
            {
                var words = (text ?? string.Empty).Split(' ');
                var elementIndex = 0;
                for (var w = 0; w < words.Length; w++)
                {
                    var wordRoot = new GameObject($"Word_{w}", typeof(RectTransform)).GetComponent<RectTransform>();
                    wordRoot.SetParent(root, false);
                    var whlg = wordRoot.gameObject.AddComponent<HorizontalLayoutGroup>();
                    whlg.childAlignment = TextAnchor.MiddleCenter;
                    whlg.spacing = 0f;
                    whlg.childControlWidth = true;
                    whlg.childControlHeight = true;
                    whlg.childForceExpandWidth = false;
                    whlg.childForceExpandHeight = false;

                    var chars = SplitGraphemes(words[w]);
                    for (var c = 0; c < chars.Count; c++)
                    {
                        elements.Add(CreateTextElement($"Char_{elementIndex++}", wordRoot, chars[c]));
                    }

                    if (w < words.Length - 1)
                    {
                        elements.Add(CreateTextElement($"Space_{elementIndex++}", root, " "));
                    }
                }
            }
        }

        return new ReplicaV3RotatingTextContainer
        {
            Root = root,
            Elements = elements
        };
    }

    private Text CreateTextElement(string name, Transform parent, string content)
    {
        Text text;
        if (TextTemplate != null)
        {
            text = Instantiate(TextTemplate, parent, false);
            text.gameObject.name = name;
            text.gameObject.SetActive(true);
        }
        else
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(parent, false);
            text = go.GetComponent<Text>();
        }

        if (text.font == null)
        {
            text.font = ResolveBuiltinFont();
        }

        text.text = content;
        text.fontSize = FontSize;
        text.fontStyle = FontStyle;
        text.alignment = Alignment;
        text.color = TextColor;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;

        var rect = (RectTransform)text.transform;
        rect.sizeDelta = Vector2.zero;
        return text;
    }

    private static List<string> SplitGraphemes(string text)
    {
        var list = new List<string>();
        if (string.IsNullOrEmpty(text))
        {
            return list;
        }

        var enumerator = StringInfo.GetTextElementEnumerator(text);
        while (enumerator.MoveNext())
        {
            list.Add(enumerator.GetTextElement());
        }

        return list;
    }

    private static void ApplyElementAlpha(List<Text> elements, float alpha)
    {
        for (var i = 0; i < elements.Count; i++)
        {
            var element = elements[i];
            if (element == null)
            {
                continue;
            }

            var color = element.color;
            color.a = alpha;
            element.color = color;
        }
    }

    private void DestroyContainer(ref ReplicaV3RotatingTextContainer container)
    {
        if (container != null && container.Root != null)
        {
            Destroy(container.Root.gameObject);
        }

        container = null;
    }

    private void KillSwitchTweens()
    {
        for (var i = 0; i < mSwitchTweens.Count; i++)
        {
            var tween = mSwitchTweens[i];
            if (tween != null && tween.active)
            {
                tween.Kill(false);
            }
        }

        mSwitchTweens.Clear();
    }

    private static string[] SafeTexts(string[] texts)
    {
        if (texts == null || texts.Length == 0)
        {
            return Array.Empty<string>();
        }

        var hasAny = false;
        for (var i = 0; i < texts.Length; i++)
        {
            if (!string.IsNullOrWhiteSpace(texts[i]))
            {
                hasAny = true;
                break;
            }
        }

        return hasAny ? texts : Array.Empty<string>();
    }
}

public enum ReplicaV3RotatingTextSplitBy
{
    Characters,
    Words,
    Lines
}

public enum ReplicaV3RotatingTextStaggerFrom
{
    First,
    Last,
    Center,
    Random
}

public sealed class ReplicaV3RotatingTextContainer
{
    public RectTransform Root;
    public List<Text> Elements;
}

