using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

class EcsSpawner : MonoBehaviour
{
    public int Amount = 1000;
    public float Between = 0.5f;
    public bool Is3D = false;
    public GameObject Target;
}
public struct EcsSpawerComponent : IComponentData
{
    public float3 Origin;
    public int amount;
    public float between;
    public bool is3D;
    public Entity Target;
}

class EcsSpawnerBaker : Baker<EcsSpawner>
{
    public override void Bake(EcsSpawner authoring)
    {
        AddComponent(GetEntity(authoring, TransformUsageFlags.None), new EcsSpawerComponent()
        {
            Origin = authoring.transform.position,
            amount = authoring.Amount,
            between = authoring.Between,
            is3D = authoring.Is3D,
            Target = GetEntity(authoring.Target, TransformUsageFlags.Renderable)
        });
    }
}
