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
