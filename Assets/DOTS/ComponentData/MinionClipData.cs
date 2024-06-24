using Unity.Entities;
using Unity.Transforms;

public struct MinionClipEntities : IComponentData
{
    public BlobAssetReference<MinionClipEntity> clipsRef;
}
public struct MinionClipEntity
{
    public BlobArray<Entity> entity;
}

public struct MinionClipData : IComponentData
{
    public int clipIndex;
    public BlobAssetReference<MinionClipPartData> assetReference;
}
public struct MinionClipPartData
{
    public BlobArray<MinionClipFrameData> parts;
}
public struct MinionClipFrameData
{
    public int BodyIndex;
    public BlobArray<LocalTransform> frames;
}
