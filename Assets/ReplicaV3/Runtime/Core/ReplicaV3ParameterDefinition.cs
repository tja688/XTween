using System;
using UnityEngine;

public enum ReplicaV3ParameterKind
{
    Float = 0,
    Bool = 1
}

[Serializable]
public sealed class ReplicaV3ParameterDefinition
{
    [Tooltip("参数唯一 ID，用于参数面板与脚本通信。")]
    public string Id;

    [Tooltip("参数在演示面板上展示的中文名称。")]
    public string DisplayName;

    [TextArea(2, 5)]
    [Tooltip("参数用途说明，告诉使用者调节后会发生什么变化。")]
    public string Description;

    [Tooltip("参数类型：当前支持 Float / Bool。")]
    public ReplicaV3ParameterKind Kind = ReplicaV3ParameterKind.Float;

    [Tooltip("Float 参数最小值。")]
    public float Min = 0f;

    [Tooltip("Float 参数最大值。")]
    public float Max = 1f;

    [Tooltip("Float 参数步进值；<=0 时会自动估算。")]
    public float Step = 0.1f;

    [Tooltip("Bool 参数默认值。")]
    public bool DefaultBool = false;
}
