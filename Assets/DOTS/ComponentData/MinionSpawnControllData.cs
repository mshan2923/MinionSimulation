using Unity.Entities;
using Unity.Mathematics;

public struct MinionSpawnControllData : IComponentData
{
    public float SpawnInterval;// = 0.5f;
    public int SpawnAmount;// = 10;
    public float3 SpawnTarget;// = new float3(0, 0, 50);
    public float SpawnRadius;// = 10f;
}
