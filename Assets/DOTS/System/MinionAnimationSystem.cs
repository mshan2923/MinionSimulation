using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using UnityEditor.Experimental.GraphView;
using System.Linq;
using static MinionAnimationDB;

[UpdateAfter(typeof(MinionSetUpSystem))]
public partial class MinionAnimationSystem : SystemBase
{
    protected override void OnStartRunning()
    {
        base.OnStartRunning();
    }
    protected override void OnDestroy()
    {
        base.OnDestroy();

        Entities.WithAll<MinionClipData>().ForEach((in MinionClipData clipData) =>
        {
            clipData.assetReference.Dispose();
        }).ScheduleParallel(Dependency);
    }
    protected override void OnUpdate()
    {
        {
            /*
            var ClipEntity = SystemAPI.GetSingleton<MinionClipEntities>();

            Debug.Log($"ClipEntity Length : {ClipEntity.clipsRef.Value.entity.Length}");
            Debug.Log($"ClipEntity index : {ClipEntity.clipsRef.Value.entity[0].Index}");//접근 불가


            var clipDataQuery = GetEntityQuery(typeof(MinionClipData));
            var clipData = clipDataQuery.ToComponentDataArray<MinionClipData>(Allocator.Temp);
            Debug.Log($"MinionClipData Query : {clipDataQuery.CalculateEntityCount()}");

            Debug.Log($"Clip Data : {clipData[0].clipIndex}");

            ref var clipParts = ref clipData[0].assetReference.Value.parts;//<---------- 이렇게 Job에게 줄수 있나?

            Debug.Log($"Clip Data - Part Length : {clipData[0].assetReference.Value.parts.Length}");
            Debug.Log($"Clip Data - BodyIndex : {clipParts[0].BodyIndex} , Frame Length : {clipParts[0].frames.Length}");

            for (int i = 0; i < clipParts.Length; i++)
            {
                if (clipParts[i].frames.Length > 0)
                {
                    Debug.Log($"Body index : {clipParts[i].BodyIndex} -> {clipParts[i].frames[0]}");
                    break;
                }
            }

            //===== SharedComponent으로 편하게 찾게 해야 하나?  ,  제일 문제인건 Job 내부에서 여러 에니메이션 접근...어떻하지? 쿼리로?

            clipData.Dispose();
            */
        }//Debug for Test

        var AnimQuery = GetEntityQuery(typeof(MinionAnimation), typeof(MinionData));
        var Animations = AnimQuery.ToComponentDataArray<MinionAnimation>(Allocator.TempJob);

        var clipDataQuery = GetEntityQuery(typeof(MinionClipData));
        var clipData = clipDataQuery.ToComponentDataArray<MinionClipData>(Allocator.TempJob);

        new UpdateAnimation()
        {
            animations = Animations,
            clipDatas = clipData,
            delta = SystemAPI.Time.DeltaTime,

            TempAnimIndex = 0,
        }.ScheduleParallel(AnimQuery, Dependency).Complete();

        Animations.Dispose();
        clipData.Dispose();
    }

    [BurstCompile]
    public partial struct UpdateAnimation : IJobEntity
    {
        [ReadOnly] public NativeArray<MinionAnimation> animations;
        [ReadOnly] public NativeArray<MinionClipData> clipDatas;
        public float delta;

        public int TempAnimIndex;

        public void Execute(Entity entity, [EntityIndexInQuery] int index, ref MinionAnimation animation, in MinionData minionData)
        {
            MinionClipData clipData = default;
            {
                if (clipDatas[TempAnimIndex].clipIndex == TempAnimIndex)
                {
                    clipData = clipDatas[TempAnimIndex];
                }else
                {
                    foreach (var v in clipDatas)
                    {
                        if (TempAnimIndex == v.clipIndex)
                        {
                            clipData = v;
                            break;
                        }
                    }
                }
            }// is Correct ClipIndex

            if (minionData.isEnablePart)
            {
                if (animations[index].PlayTime + delta < clipData.ClipLength)
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
