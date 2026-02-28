using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class ReplicaV3ParameterPanelController : MonoBehaviour
{
    [Header("基础挂点")]
    [Tooltip("可折叠区域的主体（折叠时会隐藏）。")]
    public RectTransform PanelBodyRoot;

    [Tooltip("折叠按钮。")]
    public Button FoldButton;

    [Tooltip("折叠按钮文本。")]
    public Text FoldButtonLabel;

    [Tooltip("当前动效名称文本。")]
    public Text EffectTitleLabel;

    [Tooltip("当前动效说明文本。")]
    public Text EffectGuideLabel;

    [Tooltip("参数行容器，运行时会自动清空并重建。")]
    public RectTransform RowContainer;

    [Header("样式配置")]
    [Tooltip("浮点参数行底色 A。")]
    public Color FloatRowColorA = new Color(0.16f, 0.18f, 0.24f, 0.92f);

    [Tooltip("浮点参数行底色 B。")]
    public Color FloatRowColorB = new Color(0.13f, 0.15f, 0.20f, 0.92f);

    [Tooltip("布尔参数行底色。")]
    public Color BoolRowColor = new Color(0.17f, 0.21f, 0.20f, 0.92f);

    [Tooltip("按钮背景色。")]
    public Color ButtonColor = new Color(0.31f, 0.40f, 0.65f, 0.95f);

    private ReplicaV3EffectBase mBoundEffect;
    private IReplicaV3ParameterSource mParameterSource;
    private readonly List<FloatBinding> mFloatBindings = new List<FloatBinding>();
    private readonly List<BoolBinding> mBoolBindings = new List<BoolBinding>();
    private bool mCollapsed;
    private int mRowCounter;

    private void Awake()
    {
        if (FoldButton != null)
        {
            FoldButton.onClick.AddListener(ToggleFold);
        }

        ApplyFoldState();
    }

    private void OnDestroy()
    {
        if (FoldButton != null)
        {
            FoldButton.onClick.RemoveListener(ToggleFold);
        }
    }

    private void Update()
    {
        RefreshBindingValues();
    }

    public void BindEffect(ReplicaV3EffectBase effect)
    {
        mBoundEffect = effect;
        mParameterSource = effect as IReplicaV3ParameterSource;
        RebuildRows();
    }

    private void RebuildRows()
    {
        mFloatBindings.Clear();
        mBoolBindings.Clear();
        mRowCounter = 0;

        if (RowContainer != null)
        {
            for (var i = RowContainer.childCount - 1; i >= 0; i--)
            {
                var child = RowContainer.GetChild(i);
                Destroy(child.gameObject);
            }
        }

        if (mBoundEffect == null)
        {
            SetPanelText("未选择动效", "点击左侧列表中的动效开始演示。");
            return;
        }

        SetPanelText(mBoundEffect.EffectDisplayName, mBoundEffect.UsageDescription);

        if (mParameterSource == null)
        {
            CreateSimpleHintRow("当前动效未实现参数接口。");
            return;
        }

        var definitions = mParameterSource.GetParameterDefinitions();
        if (definitions == null || definitions.Count == 0)
        {
            CreateSimpleHintRow("当前动效没有可调参数。");
            return;
        }

        for (var i = 0; i < definitions.Count; i++)
        {
            var definition = definitions[i];
            if (definition == null || string.IsNullOrWhiteSpace(definition.Id))
            {
                continue;
            }

            if (definition.Kind == ReplicaV3ParameterKind.Bool)
            {
                CreateBoolRow(definition);
            }
            else
            {
                CreateFloatRow(definition);
            }
        }

        if (RowContainer != null)
        {
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(RowContainer);
        }
    }

    private void CreateSimpleHintRow(string message)
    {
        if (RowContainer == null)
        {
            return;
        }

        var rowImage = ReplicaV3UIFactory.CreateImage("HintRow", RowContainer, FloatRowColorB, false);
        var rowRect = rowImage.rectTransform;
        var layoutElement = ReplicaV3UIFactory.EnsureComponent<LayoutElement>(rowImage.gameObject);
        layoutElement.preferredHeight = 54f;

        var text = ReplicaV3UIFactory.CreateText("HintLabel", rowRect, message, 20, TextAnchor.MiddleLeft, new Color(0.82f, 0.87f, 0.97f, 1f));
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = 14;
        text.resizeTextMaxSize = 20;
        var textRect = (RectTransform)text.transform;
        textRect.offsetMin = new Vector2(14f, 8f);
        textRect.offsetMax = new Vector2(-14f, -8f);
    }

    private void CreateFloatRow(ReplicaV3ParameterDefinition definition)
    {
        if (RowContainer == null)
        {
            return;
        }

        var rowImage = ReplicaV3UIFactory.CreateImage(
            $"Float_{definition.Id}",
            RowContainer,
            mRowCounter % 2 == 0 ? FloatRowColorA : FloatRowColorB,
            false);
        mRowCounter++;

        var rowRoot = rowImage.rectTransform;
        var layoutElement = ReplicaV3UIFactory.EnsureComponent<LayoutElement>(rowImage.gameObject);
        layoutElement.preferredHeight = 102f;

        var vertical = ReplicaV3UIFactory.EnsureComponent<VerticalLayoutGroup>(rowImage.gameObject);
        vertical.padding = new RectOffset(12, 12, 8, 8);
        vertical.spacing = 6f;
        vertical.childControlHeight = false;
        vertical.childControlWidth = true;
        vertical.childForceExpandHeight = false;

        var title = ReplicaV3UIFactory.CreateText(
            "Title",
            rowRoot,
            $"{definition.DisplayName} ({definition.Id})",
            18,
            TextAnchor.MiddleLeft,
            Color.white,
            FontStyle.Bold);
        var titleLayout = ReplicaV3UIFactory.EnsureComponent<LayoutElement>(title.gameObject);
        titleLayout.preferredHeight = 24f;

        var desc = ReplicaV3UIFactory.CreateText(
            "Description",
            rowRoot,
            string.IsNullOrWhiteSpace(definition.Description) ? "无描述" : definition.Description,
            14,
            TextAnchor.UpperLeft,
            new Color(0.76f, 0.83f, 0.98f, 0.95f));
        var descLayout = ReplicaV3UIFactory.EnsureComponent<LayoutElement>(desc.gameObject);
        descLayout.preferredHeight = 36f;

        var controls = ReplicaV3UIFactory.CreateRect("Controls", rowRoot);
        var controlsLayout = ReplicaV3UIFactory.EnsureComponent<HorizontalLayoutGroup>(controls.gameObject);
        controlsLayout.spacing = 6f;
        controlsLayout.childAlignment = TextAnchor.MiddleRight;
        controlsLayout.childControlWidth = false;
        controlsLayout.childControlHeight = true;
        controlsLayout.childForceExpandHeight = false;
        controlsLayout.childForceExpandWidth = false;

        var controlsElement = ReplicaV3UIFactory.EnsureComponent<LayoutElement>(controls.gameObject);
        controlsElement.preferredHeight = 28f;

        var minusButton = ReplicaV3UIFactory.CreateButton("Minus", controls, "-", ButtonColor, Color.white, out _);
        var minusLayout = ReplicaV3UIFactory.EnsureComponent<LayoutElement>(minusButton.gameObject);
        minusLayout.preferredWidth = 30f;

        var valueText = ReplicaV3UIFactory.CreateText("Value", controls, "--", 16, TextAnchor.MiddleCenter, Color.white, FontStyle.Bold);
        var valueLayout = ReplicaV3UIFactory.EnsureComponent<LayoutElement>(valueText.gameObject);
        valueLayout.preferredWidth = 86f;

        var plusButton = ReplicaV3UIFactory.CreateButton("Plus", controls, "+", ButtonColor, Color.white, out _);
        var plusLayout = ReplicaV3UIFactory.EnsureComponent<LayoutElement>(plusButton.gameObject);
        plusLayout.preferredWidth = 30f;

        var binding = new FloatBinding
        {
            Id = definition.Id,
            Min = definition.Min,
            Max = definition.Max,
            Step = EstimateStep(definition),
            ValueLabel = valueText
        };

        minusButton.onClick.AddListener(() => AdjustFloat(binding, -1f));
        plusButton.onClick.AddListener(() => AdjustFloat(binding, 1f));
        mFloatBindings.Add(binding);
    }

    private void CreateBoolRow(ReplicaV3ParameterDefinition definition)
    {
        if (RowContainer == null)
        {
            return;
        }

        var rowImage = ReplicaV3UIFactory.CreateImage($"Bool_{definition.Id}", RowContainer, BoolRowColor, false);
        var rowRoot = rowImage.rectTransform;

        var layoutElement = ReplicaV3UIFactory.EnsureComponent<LayoutElement>(rowImage.gameObject);
        layoutElement.preferredHeight = 86f;

        var vertical = ReplicaV3UIFactory.EnsureComponent<VerticalLayoutGroup>(rowImage.gameObject);
        vertical.padding = new RectOffset(12, 12, 8, 8);
        vertical.spacing = 6f;
        vertical.childControlHeight = false;
        vertical.childControlWidth = true;
        vertical.childForceExpandHeight = false;

        var title = ReplicaV3UIFactory.CreateText(
            "Title",
            rowRoot,
            $"{definition.DisplayName} ({definition.Id})",
            18,
            TextAnchor.MiddleLeft,
            Color.white,
            FontStyle.Bold);
        var titleLayout = ReplicaV3UIFactory.EnsureComponent<LayoutElement>(title.gameObject);
        titleLayout.preferredHeight = 24f;

        var toggleButton = ReplicaV3UIFactory.CreateButton("Toggle", rowRoot, "切换", ButtonColor, Color.white, out var toggleLabel);
        var toggleLayout = ReplicaV3UIFactory.EnsureComponent<LayoutElement>(toggleButton.gameObject);
        toggleLayout.preferredHeight = 30f;

        var binding = new BoolBinding
        {
            Id = definition.Id,
            ValueLabel = toggleLabel
        };

        toggleButton.onClick.AddListener(() => ToggleBool(binding));
        mBoolBindings.Add(binding);
    }

    private void RefreshBindingValues()
    {
        if (mParameterSource == null)
        {
            return;
        }

        for (var i = 0; i < mFloatBindings.Count; i++)
        {
            var binding = mFloatBindings[i];
            if (binding.ValueLabel == null)
            {
                continue;
            }

            if (mParameterSource.TryGetFloatParameter(binding.Id, out var value))
            {
                binding.ValueLabel.text = value.ToString("0.###");
            }
            else
            {
                binding.ValueLabel.text = "--";
            }
        }

        for (var i = 0; i < mBoolBindings.Count; i++)
        {
            var binding = mBoolBindings[i];
            if (binding.ValueLabel == null)
            {
                continue;
            }

            if (mParameterSource.TryGetBoolParameter(binding.Id, out var value))
            {
                binding.ValueLabel.text = value ? "当前：开" : "当前：关";
            }
            else
            {
                binding.ValueLabel.text = "当前：--";
            }
        }
    }

    private void AdjustFloat(FloatBinding binding, float direction)
    {
        if (mParameterSource == null)
        {
            return;
        }

        if (!mParameterSource.TryGetFloatParameter(binding.Id, out var current))
        {
            return;
        }

        var next = current + binding.Step * Mathf.Sign(direction);
        next = Mathf.Clamp(next, binding.Min, binding.Max);
        mParameterSource.TrySetFloatParameter(binding.Id, next);
    }

    private void ToggleBool(BoolBinding binding)
    {
        if (mParameterSource == null)
        {
            return;
        }

        if (!mParameterSource.TryGetBoolParameter(binding.Id, out var current))
        {
            return;
        }

        mParameterSource.TrySetBoolParameter(binding.Id, !current);
    }

    private float EstimateStep(ReplicaV3ParameterDefinition definition)
    {
        if (definition.Step > 0f)
        {
            return definition.Step;
        }

        var range = Mathf.Abs(definition.Max - definition.Min);
        if (range <= 0.001f)
        {
            return 0.1f;
        }

        return Mathf.Max(0.01f, range / 20f);
    }

    private void ToggleFold()
    {
        mCollapsed = !mCollapsed;
        ApplyFoldState();
    }

    private void ApplyFoldState()
    {
        if (PanelBodyRoot != null)
        {
            PanelBodyRoot.gameObject.SetActive(!mCollapsed);
        }

        if (FoldButtonLabel != null)
        {
            FoldButtonLabel.text = mCollapsed ? "参数面板 ︾" : "参数面板 ︽";
        }
    }

    private void SetPanelText(string title, string description)
    {
        if (EffectTitleLabel != null)
        {
            EffectTitleLabel.text = string.IsNullOrWhiteSpace(title) ? "未命名动效" : title;
        }

        if (EffectGuideLabel != null)
        {
            EffectGuideLabel.text = string.IsNullOrWhiteSpace(description) ? "暂无说明。": description;
        }
    }

    private sealed class FloatBinding
    {
        public string Id;
        public float Min;
        public float Max;
        public float Step;
        public Text ValueLabel;
    }

    private sealed class BoolBinding
    {
        public string Id;
        public Text ValueLabel;
    }
}
