using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public sealed class ReplicaV3NoiseEffect : ReplicaV3EffectBase
{
    [Header("组件绑定（Noise）")]
    [Tooltip("噪点贴图显示组件。")]
    public RawImage NoiseImage;

    [Header("参数（可在参数面板实时调）")]
    [Tooltip("噪点纹理尺寸。")]
    public int TextureSize = 1024;

    [Tooltip("刷新间隔（按帧）。")]
    public int RefreshFrameInterval = 2;

    [Tooltip("基础噪点透明度（0-255）。")]
    public int NoiseAlphaByte = 15;

    [Tooltip("模型透明度乘子。")]
    public float AlphaMultiplier = 1f;

    [Tooltip("是否暂停刷新。")]
    public bool Paused = false;

    [Tooltip("是否像素化采样（Point）。")]
    public bool Pixelated = true;

    [Tooltip("UV 贴图缩放。")]
    public Vector2 PatternScale = Vector2.one;

    [Tooltip("噪点颜色。")]
    public Color Tint = Color.white;

    [Header("进入/退出")]
    [Tooltip("进入/退出透明度过渡时长。")]
    public float FadeDuration = 0.28f;

    private readonly List<ReplicaV3ParameterDefinition> mParameters = new List<ReplicaV3ParameterDefinition>
    {
        new ReplicaV3ParameterDefinition { Id = "texture_size", DisplayName = "纹理尺寸", Description = "噪点纹理边长。", Kind = ReplicaV3ParameterKind.Float, Min = 64f, Max = 2048f, Step = 64f },
        new ReplicaV3ParameterDefinition { Id = "refresh_interval", DisplayName = "刷新间隔", Description = "每隔多少帧刷新一次噪点。", Kind = ReplicaV3ParameterKind.Float, Min = 1f, Max = 30f, Step = 1f },
        new ReplicaV3ParameterDefinition { Id = "noise_alpha", DisplayName = "噪点透明度", Description = "噪点基础透明度（0-255）。", Kind = ReplicaV3ParameterKind.Float, Min = 0f, Max = 255f, Step = 1f },
        new ReplicaV3ParameterDefinition { Id = "alpha_multiplier", DisplayName = "透明度倍率", Description = "叠乘到基础透明度。", Kind = ReplicaV3ParameterKind.Float, Min = 0f, Max = 2f, Step = 0.05f },
        new ReplicaV3ParameterDefinition { Id = "pixelated", DisplayName = "像素化", Description = "Point/Bilinear 采样切换。", Kind = ReplicaV3ParameterKind.Bool },
        new ReplicaV3ParameterDefinition { Id = "paused", DisplayName = "暂停刷新", Description = "暂停后保持当前噪点帧。", Kind = ReplicaV3ParameterKind.Bool }
    };

    private Texture2D mNoiseTexture;
    private Color32[] mPixels;
    private int mTextureSize;
    private int mFrame;
    private System.Random mRandom;

    protected override void OnEffectInitialize()
    {
        if (NoiseImage == null)
        {
            NoiseImage = GetComponentInChildren<RawImage>(true);
        }

        mRandom = new System.Random(Environment.TickCount);
        EnsureTexture();
        RenderNoise();
        ApplyVisual();
        SetCanvasAlpha(1f);
        SetLifecycleLooping();
    }

    protected override void OnEffectTick(float deltaTime, float unscaledDeltaTime)
    {
        if (Paused)
        {
            return;
        }

        mFrame++;
        if (mFrame % Mathf.Max(1, RefreshFrameInterval) != 0)
        {
            return;
        }

        EnsureTexture();
        RenderNoise();
    }

    protected override void OnPlayIn()
    {
        KillTrackedTweens(false);
        SetCanvasAlpha(0f);
        if (EffectCanvasGroup != null)
        {
            TrackTween(EffectCanvasGroup
                .DOFade(1f, Mathf.Max(0.05f, FadeDuration))
                .SetEase(Ease.OutCubic));
        }

        SetLifecycleLooping();
    }

    protected override void OnPlayOut(Action onComplete)
    {
        KillTrackedTweens(false);
        if (EffectCanvasGroup != null)
        {
            TrackTween(EffectCanvasGroup
                .DOFade(0f, Mathf.Max(0.05f, FadeDuration * 0.75f))
                .SetEase(Ease.InCubic)
                .OnComplete(() => onComplete?.Invoke()));
            return;
        }

        onComplete?.Invoke();
    }

    protected override void OnEffectReset()
    {
        KillTrackedTweens(false);
        mFrame = 0;
        EnsureTexture();
        RenderNoise();
        ApplyVisual();
        SetCanvasAlpha(1f);
        SetLifecycleLooping();
    }

    protected override void OnEffectDispose()
    {
        if (mNoiseTexture != null)
        {
            Destroy(mNoiseTexture);
            mNoiseTexture = null;
        }

        mPixels = null;
    }

    public override IReadOnlyList<ReplicaV3ParameterDefinition> GetParameterDefinitions() => mParameters;

    public override bool TryGetFloatParameter(string parameterId, out float value)
    {
        switch (parameterId)
        {
            case "texture_size": value = TextureSize; return true;
            case "refresh_interval": value = RefreshFrameInterval; return true;
            case "noise_alpha": value = NoiseAlphaByte; return true;
            case "alpha_multiplier": value = AlphaMultiplier; return true;
            default: value = 0f; return false;
        }
    }

    public override bool TrySetFloatParameter(string parameterId, float value)
    {
        switch (parameterId)
        {
            case "texture_size":
                TextureSize = Mathf.RoundToInt(Mathf.Clamp(value, 64f, 2048f));
                EnsureTexture();
                RenderNoise();
                return true;
            case "refresh_interval":
                RefreshFrameInterval = Mathf.RoundToInt(Mathf.Clamp(value, 1f, 30f));
                return true;
            case "noise_alpha":
                NoiseAlphaByte = Mathf.RoundToInt(Mathf.Clamp(value, 0f, 255f));
                ApplyVisual();
                return true;
            case "alpha_multiplier":
                AlphaMultiplier = Mathf.Clamp(value, 0f, 2f);
                ApplyVisual();
                return true;
            default:
                return false;
        }
    }

    public override bool TryGetBoolParameter(string parameterId, out bool value)
    {
        switch (parameterId)
        {
            case "pixelated": value = Pixelated; return true;
            case "paused": value = Paused; return true;
            default: value = false; return false;
        }
    }

    public override bool TrySetBoolParameter(string parameterId, bool value)
    {
        switch (parameterId)
        {
            case "pixelated":
                Pixelated = value;
                EnsureTexture();
                return true;
            case "paused":
                Paused = value;
                return true;
            default:
                return false;
        }
    }

    private void EnsureTexture()
    {
        var size = Mathf.Clamp(TextureSize, 64, 2048);
        if (mNoiseTexture != null && mTextureSize == size)
        {
            mNoiseTexture.filterMode = Pixelated ? FilterMode.Point : FilterMode.Bilinear;
            return;
        }

        mTextureSize = size;
        if (mNoiseTexture != null)
        {
            Destroy(mNoiseTexture);
        }

        mNoiseTexture = new Texture2D(size, size, TextureFormat.ARGB32, false)
        {
            wrapMode = TextureWrapMode.Repeat,
            filterMode = Pixelated ? FilterMode.Point : FilterMode.Bilinear,
            anisoLevel = 0
        };

        mPixels = new Color32[size * size];
        if (NoiseImage != null)
        {
            NoiseImage.texture = mNoiseTexture;
        }
    }

    private void RenderNoise()
    {
        if (mNoiseTexture == null || mPixels == null)
        {
            return;
        }

        var total = mPixels.Length;
        for (var i = 0; i < total; i++)
        {
            var v = (byte)mRandom.Next(0, 256);
            mPixels[i] = new Color32(v, v, v, 255);
        }

        mNoiseTexture.SetPixels32(mPixels);
        mNoiseTexture.Apply(false, false);
    }

    private void ApplyVisual()
    {
        if (NoiseImage == null)
        {
            return;
        }

        var alpha = Mathf.Clamp01((NoiseAlphaByte / 255f) * AlphaMultiplier);
        var color = Tint;
        color.a = alpha;
        NoiseImage.color = color;
        NoiseImage.uvRect = new Rect(0f, 0f, Mathf.Max(0.01f, PatternScale.x), Mathf.Max(0.01f, PatternScale.y));
    }
}

