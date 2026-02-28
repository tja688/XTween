using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public sealed class ReplicaV3OrbitImagesEffect : ReplicaV3EffectBase
{
    [Header("组件绑定（OrbitImages）")]
    [Tooltip("主舞台节点。")]
    public RectTransform Stage;

    [Tooltip("轨道旋转根节点。")]
    public RectTransform OrbitRotationRoot;

    [Tooltip("中心标题。")]
    public Text CenterTitleText;

    [Tooltip("轨道项容器列表（顺序即排列顺序）。")]
    public List<RectTransform> ItemRoots = new List<RectTransform>();

    [Tooltip("轨道项图像列表（可与 ItemRoots 一一对应）。")]
    public List<Image> ItemImages = new List<Image>();

    [Header("参数（可在参数面板实时调）")]
    [Tooltip("一圈旋转时长。")]
    public float DurationPerLoop = 40f;

    [Tooltip("轨道 X 半径。")]
    public float RadiusX = 380f;

    [Tooltip("轨道 Y 半径。")]
    public float RadiusY = 140f;

    [Tooltip("轨道整体旋转角度（Z）。")]
    public float OrbitRotationZ = -8f;

    [Tooltip("是否反向旋转。")]
    public bool Reverse = false;

    [Tooltip("是否均匀铺满整个环。")]
    public bool Fill = true;

    [Tooltip("元素是否保持正立（抵消轨道旋转）。")]
    public bool KeepUpright = true;

    [Tooltip("是否暂停旋转。")]
    public bool Paused = false;

    [Header("文案/资源")]
    [Tooltip("中心标题文案。")]
    public string CenterTitle = "Orbit Images";

    [Tooltip("轨道项图片数据（为空时保持当前预制体图像）。")]
    public List<Sprite> Images = new List<Sprite>();

    [Tooltip("未设置图片时使用的回退色。")]
    public Color ItemFallbackColor = new Color(0.26f, 0.34f, 0.55f, 1f);

    [Header("进入/退出")]
    [Tooltip("进入偏移。")]
    public Vector2 EnterOffset = new Vector2(0f, -140f);

    [Tooltip("退出偏移。")]
    public Vector2 ExitOffset = new Vector2(0f, 140f);

    [Tooltip("进入/退出过渡时长。")]
    public float TransitionDuration = 0.55f;

    private readonly List<ReplicaV3ParameterDefinition> mParameters = new List<ReplicaV3ParameterDefinition>
    {
        new ReplicaV3ParameterDefinition { Id = "duration", DisplayName = "旋转周期", Description = "一圈旋转耗时。", Kind = ReplicaV3ParameterKind.Float, Min = 0.1f, Max = 80f, Step = 0.1f },
        new ReplicaV3ParameterDefinition { Id = "radius_x", DisplayName = "半径X", Description = "轨道 X 半径。", Kind = ReplicaV3ParameterKind.Float, Min = 10f, Max = 2000f, Step = 5f },
        new ReplicaV3ParameterDefinition { Id = "radius_y", DisplayName = "半径Y", Description = "轨道 Y 半径。", Kind = ReplicaV3ParameterKind.Float, Min = 10f, Max = 2000f, Step = 5f },
        new ReplicaV3ParameterDefinition { Id = "orbit_rotation_z", DisplayName = "轨道角度", Description = "轨道整体 Z 旋转。", Kind = ReplicaV3ParameterKind.Float, Min = -180f, Max = 180f, Step = 1f },
        new ReplicaV3ParameterDefinition { Id = "reverse", DisplayName = "反向旋转", Description = "反转旋转方向。", Kind = ReplicaV3ParameterKind.Bool },
        new ReplicaV3ParameterDefinition { Id = "keep_upright", DisplayName = "保持正立", Description = "元素不随轨道倾斜。", Kind = ReplicaV3ParameterKind.Bool },
        new ReplicaV3ParameterDefinition { Id = "paused", DisplayName = "暂停", Description = "暂停环绕动画。", Kind = ReplicaV3ParameterKind.Bool }
    };

    private float mProgress01;

    protected override void OnEffectInitialize()
    {
        EnsureBindings();
        ApplyContent();
        mProgress01 = 0f;
        LayoutItems();
        SetCanvasAlpha(1f);
        SetLifecycleLooping();
    }

    protected override void OnEffectTick(float deltaTime, float unscaledDeltaTime)
    {
        if (Paused)
        {
            return;
        }

        var count = ItemRoots.Count;
        if (count <= 0)
        {
            return;
        }

        var duration = Mathf.Max(0.1f, DurationPerLoop);
        var direction = Reverse ? -1f : 1f;
        mProgress01 = Mathf.Repeat(mProgress01 + (direction * (unscaledDeltaTime / duration)), 1f);
        LayoutItems();
    }

    protected override void OnPlayIn()
    {
        EnsureBindings();
        KillTrackedTweens(false);

        var duration = Mathf.Max(0.05f, TransitionDuration);
        if (Stage != null)
        {
            Stage.anchoredPosition = EnterOffset;
            TrackTween(Stage.DOAnchorPos(Vector2.zero, duration).SetEase(Ease.OutCubic));
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
        var duration = Mathf.Max(0.05f, TransitionDuration * 0.85f);
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

        if (Stage != null)
        {
            pending++;
            TrackTween(Stage.DOAnchorPos(ExitOffset, duration).SetEase(Ease.InCubic).OnComplete(TryComplete));
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
        mProgress01 = 0f;
        ApplyContent();
        LayoutItems();
        if (Stage != null)
        {
            Stage.anchoredPosition = Vector2.zero;
        }

        SetCanvasAlpha(1f);
        SetLifecycleLooping();
    }

    public override IReadOnlyList<ReplicaV3ParameterDefinition> GetParameterDefinitions() => mParameters;

    public override bool TryGetFloatParameter(string parameterId, out float value)
    {
        switch (parameterId)
        {
            case "duration": value = DurationPerLoop; return true;
            case "radius_x": value = RadiusX; return true;
            case "radius_y": value = RadiusY; return true;
            case "orbit_rotation_z": value = OrbitRotationZ; return true;
            default: value = 0f; return false;
        }
    }

    public override bool TrySetFloatParameter(string parameterId, float value)
    {
        switch (parameterId)
        {
            case "duration": DurationPerLoop = Mathf.Clamp(value, 0.1f, 80f); return true;
            case "radius_x": RadiusX = Mathf.Clamp(value, 10f, 2000f); LayoutItems(); return true;
            case "radius_y": RadiusY = Mathf.Clamp(value, 10f, 2000f); LayoutItems(); return true;
            case "orbit_rotation_z": OrbitRotationZ = Mathf.Clamp(value, -180f, 180f); LayoutItems(); return true;
            default: return false;
        }
    }

    public override bool TryGetBoolParameter(string parameterId, out bool value)
    {
        switch (parameterId)
        {
            case "reverse": value = Reverse; return true;
            case "keep_upright": value = KeepUpright; return true;
            case "paused": value = Paused; return true;
            default: value = false; return false;
        }
    }

    public override bool TrySetBoolParameter(string parameterId, bool value)
    {
        switch (parameterId)
        {
            case "reverse": Reverse = value; return true;
            case "keep_upright": KeepUpright = value; LayoutItems(); return true;
            case "paused": Paused = value; return true;
            default: return false;
        }
    }

    private void EnsureBindings()
    {
        if (Stage == null)
        {
            Stage = EffectRoot != null ? EffectRoot : transform as RectTransform;
        }

        if (OrbitRotationRoot == null)
        {
            OrbitRotationRoot = Stage;
        }
    }

    private void ApplyContent()
    {
        if (CenterTitleText != null)
        {
            CenterTitleText.text = string.IsNullOrWhiteSpace(CenterTitle) ? "Orbit Images" : CenterTitle;
        }

        var count = Mathf.Min(ItemImages.Count, ItemRoots.Count > 0 ? ItemRoots.Count : ItemImages.Count);
        for (var i = 0; i < count; i++)
        {
            var image = ItemImages[i];
            if (image == null)
            {
                continue;
            }

            var sprite = i < Images.Count ? Images[i] : null;
            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.color = sprite != null ? Color.white : ItemFallbackColor;
        }
    }

    private void LayoutItems()
    {
        if (OrbitRotationRoot != null)
        {
            OrbitRotationRoot.localRotation = Quaternion.Euler(0f, 0f, OrbitRotationZ);
        }

        var count = ItemRoots.Count;
        if (count <= 0)
        {
            return;
        }

        var uprightRotation = KeepUpright ? Quaternion.Euler(0f, 0f, -OrbitRotationZ) : Quaternion.identity;
        var radiusX = Mathf.Max(1f, RadiusX);
        var radiusY = Mathf.Max(1f, RadiusY);

        for (var i = 0; i < count; i++)
        {
            var item = ItemRoots[i];
            if (item == null)
            {
                continue;
            }

            var offset01 = Fill ? (i / (float)count) : 0f;
            var t = Mathf.Repeat(mProgress01 + offset01, 1f);
            var angle = t * Mathf.PI * 2f;
            item.anchoredPosition = new Vector2(Mathf.Cos(angle) * radiusX, Mathf.Sin(angle) * radiusY);
            item.localRotation = uprightRotation;
        }
    }
}

