using Unity.Collections;
using Unity.Entities;

public struct MinionTag : IComponentData {}

public struct MinionPartParent : ISharedComponentData
{
    public Entity parent;
}

public struct MinionData : IComponentData
{
    public FixedString32Bytes AvaterName;

    public bool isSpawnedPart;
    public bool isEnablePart;

    public int Parts;

    public Entity TestDefaultObj;

    public float DisableCounter;
}
public struct MinionPart : IBufferElementData
{
    public Entity Part;
    public int SpawnBodyIndex;
}
public struct MinionPartIndex : IComponentData
{
    public int Index;
}

public struct MinionAnimation : IComponentData
{
    public int PreviousAnimation;
    public float StopedTime;
    public int CurrectAnimation;
    public float PlayTime;
}
