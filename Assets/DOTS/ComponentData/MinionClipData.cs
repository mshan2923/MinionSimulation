using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public struct MinionClipEntities : IComponentData
{
    public BlobAssetReference<MinionClipEntity> clipsRef;
}//굳..이??
public struct MinionClipEntity
{
    public BlobArray<Entity> entity;
}

public struct MinionClipData : IComponentData
{
    public int clipIndex;
    public float ClipLength;
    [Tooltip("에니메이션 종료시 자동 반복 재생")]
    public bool IsLooping;
    [Tooltip("에니메이션 재생 취소 여부")]
    public bool Cancellable;

    [Tooltip("에니메이션 전환시 보간 시간")]
    public float interpolationTime;
    [Tooltip("에니메이션 강제 전환시 보간 시간")]
    public float forceInterpolationTime;

    public BlobAssetReference<MinionClipPartData> assetReference;

}
public struct MinionClipPartData
{
    public BlobArray<MinionClipFrameData> parts;
}
public struct MinionClipFrameData
{
    public int BodyIndex;
    public LocalTransform OffsetTransform;
    public BlobArray<LocalTransform> frames;
}


public struct MinionAnimatorControllData : IComponentData
{
    public float3 Target;
    public float MoveSpeed;// = 1.4f;
    public float PressureSpeed;// = 1.0f;
    public float RotationSpeed;// = 5f;
    public float cellRadius;// = 0.5f;
    public float MinionRadius;

    public int IdleAnimationIndex;
    public int WalkAnimationIndex;
}