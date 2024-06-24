using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;

[UpdateAfter(typeof(MinionSetUpSystem))]
public partial class MinionAnimationSystem : SystemBase
{
    protected override void OnStartRunning()
    {
        base.OnStartRunning();
    }
    protected override void OnUpdate()
    {
        {
            var ClipEntity = SystemAPI.GetSingleton<MinionClipEntities>();

            Debug.Log($"ClipEntity Length : {ClipEntity.clipsRef.Value.entity.Length}");
            Debug.Log($"ClipEntity index : {ClipEntity.clipsRef.Value.entity[0].Index}");//접근 불가

            /*
            var clipData = SystemAPI.GetComponent<MinionClipData>(ClipEntity.clipsRef.Value.entity[0]);

            Debug.Log($"Clip Data : {clipData.clipIndex}");
            Debug.Log($"Clip Data - Part Length : {clipData.assetReference.Value.parts.Length}");
            Debug.Log($"Clip Data - BodyIndex : {clipData.assetReference.Value.parts[0].BodyIndex}");*/

            var clipDataQuery = GetEntityQuery(typeof(MinionClipData));
            var clipData = clipDataQuery.ToComponentDataArray<MinionClipData>(Allocator.Temp);
            Debug.Log($"MinionClipData Query : {clipDataQuery.CalculateEntityCount()}");

            Debug.Log($"Clip Data : {clipData[0].clipIndex}");
            Debug.Log($"Clip Data - Part Length : {clipData[0].assetReference.Value.parts.Length}");
            Debug.Log($"Clip Data - BodyIndex : {clipData[0].assetReference.Value.parts[0].BodyIndex} , Frame Length : {clipData[0].assetReference.Value.parts[0].frames.Length}");

            //===== SharedComponent으로 편하게 찾게 해야 하나?

            clipData.Dispose();
        }//Debug for Test

        var AnimQuery = GetEntityQuery(typeof(MinionAnimation), typeof(MinionData));
        var Animations = AnimQuery.ToComponentDataArray<MinionAnimation>(Allocator.TempJob);

        new UpdateAnimation()
        {
            animations = Animations,
            delta = SystemAPI.Time.DeltaTime,

            TempAnimIndex = 0,
            TempAnimLength = 3.8f,
        }.ScheduleParallel(AnimQuery, Dependency).Complete();

        Animations.Dispose();
    }

    [BurstCompile]
    public partial struct UpdateAnimation : IJobEntity
    {
        [ReadOnly] public NativeArray<MinionAnimation> animations;
        public float delta;

        public float TempAnimLength;
        public int TempAnimIndex;

        public void Execute(Entity entity, [EntityIndexInQuery] int index, ref MinionAnimation animation, in MinionData minionData)
        {
            if (minionData.isEnablePart)
            {
                if (animations[index].PlayTime + delta < TempAnimLength)
                {
                    animation.PlayTime = animations[index].PlayTime + delta;
                }
                else
                {
                    animation.PlayTime = 0;                    
                }
            }
        }
    }
}
