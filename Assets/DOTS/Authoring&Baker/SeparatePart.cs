using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

class SeparatePart : MonoBehaviour
{
    public float Speed = 10f;
    public float SpeedOffset = 2f;
    public float SeparateTime = 2f;
    public float FalloffTime = 1f;
    public float3 Gravity = new float3(0, -9.8f, 0);

    [Min(0)]public float DisableHeightUnderOrigin = -5f;
}

class SeparatePartBaker : Baker<SeparatePart>
{
    public override void Bake(SeparatePart authoring)
    {
        AddComponent(GetEntity(authoring.gameObject, TransformUsageFlags.None),
            new SeparatePartComponent
            {
                Speed = authoring.Speed,
                SpeedOffset = authoring.SpeedOffset,
                SeparateTime = authoring.SeparateTime,
                FalloffTime =   authoring.FalloffTime,
                Gravity = authoring.Gravity,
                DisableHeightUnderOrigin = authoring.DisableHeightUnderOrigin,
            });
    }
}
