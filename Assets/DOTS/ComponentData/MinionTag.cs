using Unity.Collections;
using Unity.Entities;

public struct MinionTag : IComponentData {}
public struct MinionData : IComponentData
{
    public FixedString32Bytes AvaterName;

    public bool isSpawnedPart;

    public Entity TestDefaultObj;

    public int Parts;
}
public struct MinionPart : IBufferElementData
{
    public Entity Part;
}
