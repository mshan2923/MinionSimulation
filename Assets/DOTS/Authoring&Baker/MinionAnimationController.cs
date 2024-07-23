using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

class MinionAnimationController : MonoBehaviour
{
    public float MoveSpeed = 2.5f;

    public float PressureSpeed = 1.0f;
    public float RotationSpeed = 5f;
    public float cellRadius = 0.5f;
    public float MinionRadius = 0.5f;
}

class MinionAnimationControllerBaker : Baker<MinionAnimationController>
{
    public override void Bake(MinionAnimationController authoring)
    {
        var db = MinionAnimationDB.Instance;
        AddComponent
        (
            GetEntity(authoring.gameObject, TransformUsageFlags.None),
            new MinionAnimatorControllData
            {
                Target = float3.zero,
                MoveSpeed = authoring.MoveSpeed,
                PressureSpeed = authoring.PressureSpeed,
                RotationSpeed = authoring.RotationSpeed,
                cellRadius = authoring.cellRadius,
                MinionRadius = authoring.MinionRadius,

                IdleAnimationIndex = db.IdleAnimationIndex,
                WalkAnimationIndex = db.WalkAnimationIndex,
            }
        );
    }
}
