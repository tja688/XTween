using System;
using UnityEngine;

[Serializable]
public sealed class ReplicaV3ShowcaseEffectEntry
{
    [Tooltip("动效 ID（跨场景共享，建议稳定命名）。")]
    public string EffectId;

    [Tooltip("动效列表展示名称。")]
    public string DisplayName;

    [TextArea(2, 5)]
    [Tooltip("动效说明，会显示在右侧展示区。")]
    public string Description;

    [Tooltip("动效预制体。必须挂载 ReplicaV3EffectBase 子类。")]
    public ReplicaV3EffectBase EffectPrefab;
}
