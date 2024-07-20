using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public struct MinionClipEntities : IComponentData
{
    public BlobAssetReference<MinionClipEntity> clipsRef;
}//��..��??
public struct MinionClipEntity
{
    public BlobArray<Entity> entity;
}

public struct MinionClipData : IComponentData
{
    public int clipIndex;
    public float ClipLength;
    [Tooltip("���ϸ��̼� ����� �ڵ� �ݺ� ���")]
    public bool IsLooping;
    [Tooltip("���ϸ��̼� ��� ��� ����")]
    public bool Cancellable;

    [Tooltip("���ϸ��̼� ��ȯ�� ���� �ð�")]
    public float interpolationTime;
    [Tooltip("���ϸ��̼� ���� ��ȯ�� ���� �ð�")]
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