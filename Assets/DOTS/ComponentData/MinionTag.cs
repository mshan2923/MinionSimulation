using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEditor.EditorTools;
using UnityEngine;

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

    public float DisableCounter;
    public float3 ImpactLocation;

    /// <summary>
    /// 미니언이 스폰되어 파괴되지 않은 조작 가능한 상태
    /// </summary>
    public bool IsActive
    {
        get => isSpawnedPart && isEnablePart && DisableCounter < 0;
    }
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
    [Tooltip("다음 에니메이션 예약\n 캔슬 가능하면 바로 전환")]
    public int ReserveAnimatiom;//==== 다음 에니메이션 예약 (취고 가능하면 바로 전환)
    [Tooltip("캔슬 불가 이여도 강제로 스킵")]
    public bool ForceCancle;
}
