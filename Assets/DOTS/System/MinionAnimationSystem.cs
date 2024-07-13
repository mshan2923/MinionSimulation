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
using Unity.Mathematics;

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
        /*
        Entities.WithAll<MinionClipData>().ForEach((in MinionClipData clipData) =>
        {
            clipData.assetReference.Dispose();
        }).ScheduleParallel(Dependency).Complete();
        *///굳이 안해도 종료시 되나?
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
        var Minions = AnimQuery.ToComponentDataArray<MinionData>(Allocator.TempJob);

        var clipDataQuery = GetEntityQuery(typeof(MinionClipData));
        var clipData = clipDataQuery.ToComponentDataArray<MinionClipData>(Allocator.TempJob);

        var updateHandle = new UpdateAnimation()
        {
            animations = Animations,
            clipDatas = clipData,
            delta = SystemAPI.Time.DeltaTime,
        }.ScheduleParallel(AnimQuery, Dependency);//데이터 수정만 , 위치 변경은 MinionSystem에서

        var seperateData = SystemAPI.GetSingleton<SeparationPartComponent>();
        new SeperateParts()
        {
            datas = Minions,
            Delta = SystemAPI.Time.DeltaTime,
            separateData = seperateData,
        }.ScheduleParallel(AnimQuery, updateHandle).Complete();

        Animations.Dispose();
        Minions.Dispose();
        clipData.Dispose();
    }

    [BurstCompile]
    public partial struct UpdateAnimation : IJobEntity
    {
        [ReadOnly] public NativeArray<MinionAnimation> animations;
        [ReadOnly] public NativeArray<MinionClipData> clipDatas;
        public float delta;

        public void Execute(Entity entity, [EntityIndexInQuery] int index, ref MinionAnimation animation, in MinionData minionData)
        {
            MinionClipData clipData = default;
            {
                if (animations[index].CurrectAnimation < 0)
                    return;

                if (clipDatas[animations[index].CurrectAnimation].clipIndex == animations[index].CurrectAnimation)
                {
                    clipData = clipDatas[animations[index].CurrectAnimation];
                }else
                {
                    foreach (var v in clipDatas)
                    {
                        if (animations[index].CurrectAnimation == v.clipIndex)
                        {
                            clipData = v;
                            break;
                        }
                    }
                }
            }// is Correct ClipIndex

            if (minionData.isEnablePart)
            {
                if (minionData.DisableCounter >= 0)
                    return;

                if (animations[index].ReserveAnimatiom >= 0 &&
                    (clipDatas[animations[index].CurrectAnimation].Cancellable || animations[index].ForceCancle))
                {
                    animation.PreviousAnimation = animations[index].CurrectAnimation;
                    animation.StopedTime = animations[index].PlayTime;
                    animation.CurrectAnimation = animations[index].ReserveAnimatiom;
                    animation.PlayTime = 0;
                    animation.ReserveAnimatiom = -1;
                    animation.ForceCancle = false;

                    return;
                }//에니메이션 캔슬

                float Ltime = animations[index].PlayTime + delta;
                if (Ltime < clipData.ClipLength)
                {
                    animation.PlayTime = animations[index].PlayTime + delta;
                }
                else
                {
                    if (!clipData.IsLooping)
                    {
                        animation.CurrectAnimation = -1;
                    }
                    if (animations[index].ReserveAnimatiom >= 0)
                    {
                        animation.CurrectAnimation = animations[index].ReserveAnimatiom;
                    }

                    animation.PreviousAnimation = animations[index].CurrectAnimation;
                    animation.ReserveAnimatiom = -1;
                    animation.StopedTime = animations[index].PlayTime + delta;
                    animation.PlayTime = 0;
                    animation.ForceCancle = false;
                }
            }
        }
    }
    public partial struct SeperateParts : IJobEntity
    {
        public NativeArray<MinionData> datas;
        public float Delta;
        public SeparationPartComponent separateData;

        public void Execute([EntityIndexInQuery] int index, ref MinionData minion)
        {
            if (datas[index].DisableCounter < 0)
                return;

            if (separateData.SeparateTime + separateData.FalloffTime > datas[index].DisableCounter + Delta)
            {
                minion.DisableCounter = datas[index].DisableCounter + Delta;
            }else
            {
                //minion.DisableCounter = separateData.SeparateTime + separateData.FalloffTime;
                minion.isEnablePart = false;
            }//Disable Minion -> 부위들만 비활성화 하는거라서
             //MinionSystem에서 부위 tranform 업데이트시 MinionData.isEnablePart값이 false면 비활성화

        }
    }
}
