using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public struct MinionClipEntities : IComponentData
{
    public BlobAssetReference<MinionClipEntity> clipsRef;
}//±ª..¿Ã??
public struct MinionClipEntity
{
    public BlobArray<Entity> entity;
}

public struct MinionClipData : IComponentData
{
    public int clipIndex;
    public float ClipLength;
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
