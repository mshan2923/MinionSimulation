using Unity.Entities;
using Unity.Mathematics;

public struct SeparationPartComponent : IComponentData
{
    public float Speed;
    public float SpeedOffset;
    public float SeparateTime;
    public float FalloffTime;
    public float3 Gravity;
    public float DisableHeightUnderOrigin;
}
