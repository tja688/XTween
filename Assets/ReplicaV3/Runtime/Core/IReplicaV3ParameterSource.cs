using System.Collections.Generic;

public interface IReplicaV3ParameterSource
{
    IReadOnlyList<ReplicaV3ParameterDefinition> GetParameterDefinitions();
    bool TryGetFloatParameter(string parameterId, out float value);
    bool TrySetFloatParameter(string parameterId, float value);
    bool TryGetBoolParameter(string parameterId, out bool value);
    bool TrySetBoolParameter(string parameterId, bool value);
}
