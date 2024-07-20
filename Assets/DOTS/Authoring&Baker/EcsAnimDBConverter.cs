using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

class EcsAnimDBConverter : MonoBehaviour
{
    
}

class EcsAnimDBConverterBaker : Baker<EcsAnimDBConverter>
{
    public override void Bake(EcsAnimDBConverter authoring)
    {
        var db = MinionAnimationDB.Instance;


        var clipEntitiesBuilder = new BlobBuilder(Allocator.Temp);
        ref MinionClipEntity clipEntitySlot = ref clipEntitiesBuilder.ConstructRoot<MinionClipEntity>();
        var clipEntityBuilder = clipEntitiesBuilder.Allocate(ref clipEntitySlot.entity, db.ClipAmount);
        
        for (int c = 0; c < db.ClipAmount; c++)
        {
            var clipEntity = CreateAdditionalEntity(TransformUsageFlags.None, false, db.animationClips[c].Clip.name);
            clipEntityBuilder[c] = clipEntity;

            var clipDataBuilder = new BlobBuilder(Allocator.Temp);
            ref MinionClipPartData clipPart = ref clipDataBuilder.ConstructRoot<MinionClipPartData>();
            var clipPartBuilder = clipDataBuilder.Allocate(ref clipPart.parts, db.BoneAmount(c));

            for (int b = 0; b < db.BoneAmount(c); b++)
            {
                int clipSize = db.GetClipSize(c);
                clipPartBuilder[b].BodyIndex = b;
                clipPartBuilder[b].OffsetTransform = db.GetSpawnOffset(c, b);

                {
                    var obj = db.GetSpawnObj(c, b, true);

                    if (obj != null)
                    {
                        var clipFrameBuilder = clipDataBuilder.Allocate(ref clipPartBuilder[b].frames, clipSize);
                        for (int f = 0; f < clipSize; f++)
                        {
                            clipFrameBuilder[f] = db.GetPartTransform(c, b, f);
                        }
                    }
                    else
                    {
                        clipDataBuilder.Allocate(ref clipPartBuilder[b].frames, 0);
                    }
                }//Set Frames
            }//Set MinionClipFrameData

            var clipRef = clipDataBuilder.CreateBlobAssetReference<MinionClipPartData>(Allocator.Persistent);
            AddBlobAsset(ref clipRef, out var clipHash);
            AddComponent(clipEntity, new MinionClipData
            {
                clipIndex = c,
                ClipLength = db.GetClipLength(c),
                IsLooping = db.animationClips[c].isLooping,
                Cancellable = db.animationClips[c].Cancellable,

                interpolationTime = db.animationClips[c].interpolationTime,
                forceInterpolationTime = db.animationClips[c].forceInterpolationTime,

                assetReference = clipRef
                //MinionAnimationSystem이 제거될때  메모리 할당 해제 
            });
            clipDataBuilder.Dispose();
        }

        var clipEntityRef = clipEntitiesBuilder.CreateBlobAssetReference<MinionClipEntity>(Allocator.Persistent);
        AddBlobAsset(ref clipEntityRef , out var clipEntityHash);
        AddComponent(GetEntity(authoring.gameObject, TransformUsageFlags.None),
            new MinionClipEntities
            {
                clipsRef = clipEntityRef
            });
        clipEntitiesBuilder.Dispose();

        AddComponent
            (
                GetEntity(authoring.gameObject, TransformUsageFlags.None),
                new MinionAnimatorControllData
                {
                    Target = float3.zero,
                    MoveSpeed = 1.4f,
                    PressureSpeed = 1f,
                    RotationSpeed = 5f,
                    cellRadius = 0.5f,
                    MinionRadius = 0.5f,

                    IdleAnimationIndex = db.IdleAnimationIndex,
                    WalkAnimationIndex = db.WalkAnimationIndex,
                }
            );
    }
}
