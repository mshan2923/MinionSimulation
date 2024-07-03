using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using static MinionAnimationDB;

[UpdateAfter(typeof(MinionSetUpSystem))]
public partial class MinionSystem : SystemBase
{
    EntityQuery Minions;

    protected override void OnStartRunning()
    {
        base.OnStartRunning();
        Minions = GetEntityQuery(typeof(MinionData));
        var MinionEntities = Minions.ToEntityArray(Allocator.TempJob);

        foreach(var e in MinionEntities)
        {
            var aspect = SystemAPI.GetAspect<MinionAspect>(e);
            aspect.ChangeAnimation(0);
        }

        MinionEntities.Dispose();
    }
    protected override void OnUpdate()
    {
        {
            var MinionEntities = Minions.ToEntityArray(Allocator.TempJob);
            var MinionDatas = Minions.ToComponentDataArray<MinionData>(Allocator.TempJob);

            // 모든 Minion 마다 Part들을 위치 업데이트...
            //한번에 적용하기 위해 , 데이터 준비한후 , 적용
            // 먼저 Minion 목록 에서 
            // 모든 부위
            // BoneIndex
            // 캐릭터 엔티티
            // HashMap<캐릭터 엔티티, MinionAnimation>

            var MinionsParall = new NativeParallelHashMap<Entity, MinionData>(MinionEntities.Length, Allocator.TempJob);
            var AnimationDataParall = new NativeParallelHashMap<Entity, MinionAnimation>(MinionEntities.Length, Allocator.TempJob);
            var MinionTransformParall = new NativeParallelHashMap<Entity, LocalTransform>(MinionEntities.Length, Allocator.TempJob);

            var clipData = GetEntityQuery(typeof(MinionClipData)).ToComponentDataArray<MinionClipData>(Allocator.TempJob);

            var PartsQuery = GetEntityQuery(typeof(MinionPartIndex), typeof(MinionPartParent), typeof(LocalTransform));
            var PartsTransform = PartsQuery.ToComponentDataArray<LocalTransform>(Allocator.TempJob);

            var ecb = SystemAPI.GetSingleton<BeginFixedStepSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(EntityManager.WorldUnmanaged);

            AnimationDataParall.Capacity = MinionEntities.Length;
            MinionTransformParall.Capacity = MinionEntities.Length;

            var setupHandle = new SetUpHashMap()
            {
                MinionsParall = MinionsParall.AsParallelWriter(),
                AnimationDataParall = AnimationDataParall.AsParallelWriter(),
                MinionTransformParall = MinionTransformParall.AsParallelWriter()
            }.ScheduleParallel(Dependency);

            var animHandle = new UpdateMinionAnimation_Ref()
            {
                animations = AnimationDataParall.AsReadOnly(),
                originTransform = MinionTransformParall.AsReadOnly(),
                ClipDatas = clipData,
                ClipDataInterval = MinionAnimationDB.ClipDataInterval
            }.ScheduleParallel(PartsQuery, setupHandle);
            //=============== 크기조절 에 따른 위치 변경이 없음 

            var seperateData = SystemAPI.GetSingleton<SeparatePartComponent>();

            new UpdateSeperteTrasform()
            {
                ecb = ecb.AsParallelWriter(),
                minions = MinionsParall.AsReadOnly(),
                originTransform = MinionTransformParall.AsReadOnly(),

                seperate = seperateData,
                PartsTransform = PartsTransform,
                delta = SystemAPI.Time.DeltaTime
            }.ScheduleParallel(PartsQuery, animHandle).Complete();

            MinionsParall.Dispose();
            AnimationDataParall.Dispose();
            MinionTransformParall.Dispose();
            PartsTransform.Dispose();
            clipData.Dispose();

            MinionEntities.Dispose();
            MinionDatas.Dispose();
        }
    }

    [BurstCompile]
    public partial struct SetUpHashMap : IJobEntity
    {
        public NativeParallelHashMap<Entity, MinionData>.ParallelWriter MinionsParall;
        public NativeParallelHashMap<Entity, MinionAnimation>.ParallelWriter AnimationDataParall;
        public NativeParallelHashMap<Entity, LocalTransform>.ParallelWriter MinionTransformParall;
        public void Execute(Entity entity, in MinionAnimation animation, in MinionData minionData, in LocalTransform transform)
        {
            MinionsParall.TryAdd(entity, minionData);
            MinionTransformParall.TryAdd(entity, transform);

            if (minionData.isEnablePart && minionData.DisableCounter <= 0)
            {
                AnimationDataParall.TryAdd(entity, animation);
            }
        }
    }

    [BurstCompile]
    public partial struct UpdateMinionAnimation_Ref : IJobEntity
    {
        [ReadOnly] public NativeParallelHashMap<Entity, MinionAnimation>.ReadOnly animations;
        [ReadOnly] public NativeParallelHashMap<Entity, LocalTransform>.ReadOnly originTransform;
        [ReadOnly] public NativeArray<MinionClipData> ClipDatas;

        [ReadOnly] public float ClipDataInterval;

        public void Execute(Entity e, [EntityIndexInQuery] int index, in MinionPartIndex partIndex, in MinionPartParent parent,
            ref LocalTransform transform)
        {
            if (animations.TryGetValue(parent.parent, out var anim))
            {
                originTransform.TryGetValue(parent.parent, out var worldTrans);
                
                MinionClipData clipData = default;
                {
                    if (ClipDatas[anim.CurrectAnimation].clipIndex == anim.CurrectAnimation)
                    {
                        clipData = ClipDatas[anim.CurrectAnimation];
                    }
                    else
                    {
                        foreach(var v in ClipDatas)
                        {
                            if (anim.CurrectAnimation == v.clipIndex)
                            {
                                clipData = v;
                                break;
                            }
                        }
                    }
                }// is Correct ClipIndex


                var localTrans = clipData.assetReference.Value.parts[partIndex.Index]
                    .frames[Mathf.FloorToInt(anim.PlayTime / ClipDataInterval)];

                var rot = math.mul(worldTrans.Rotation, localTrans.Rotation);
                rot = math.mul(rot, clipData.assetReference.Value.parts[partIndex.Index].OffsetTransform.Rotation);               

                transform.Position = worldTrans.Position + localTrans.Position;
                transform.Rotation = rot;
                transform.Scale = worldTrans.Scale * localTrans.Scale;
            }
        }

    }

    public partial struct UpdateSeperteTrasform : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ecb;
        [ReadOnly] public NativeParallelHashMap<Entity, MinionData>.ReadOnly minions;
        [ReadOnly] public NativeParallelHashMap<Entity, LocalTransform>.ReadOnly originTransform;

        [ReadOnly] public SeparatePartComponent seperate;
        [ReadOnly] public NativeArray<LocalTransform> PartsTransform;
        public float delta;

        public void Execute(Entity entity, [EntityIndexInQuery] int index, in MinionPartIndex partIndex, in MinionPartParent parent,
            ref LocalTransform transform)
        {

            if (minions.TryGetValue(parent.parent, out var minion))
            {
                originTransform.TryGetValue(parent.parent, out var origin);

                if (minion.isEnablePart == false)
                {
                    //수명 다함
                    ecb.SetEnabled(index, entity, false);
                    return;
                }

                if (minion.DisableCounter >= 0 && minion.DisableCounter < seperate.SeparateTime + seperate.FalloffTime)
                {
                    var offset = math.normalize(PartsTransform[index].Position - origin.Position);
                    var impactOffset = math.normalize(PartsTransform[index].Position - minion.ImpactLocation);

                    transform.Position = PartsTransform[index].Position
                        + math.normalize(offset + impactOffset) * (seperate.Speed * delta)
                        + seperate.Gravity * delta;
                    // 가상의 바닥 적용할려면 속도값 필요

                    if (seperate.SeparateTime <= minion.DisableCounter)
                    {
                        transform.Scale = 1 - ((minion.DisableCounter - seperate.SeparateTime) / seperate.FalloffTime);
                    }//FallOff - 크기 감소
                }
                else if (minion.DisableCounter >= seperate.SeparateTime + seperate.FalloffTime)
                {
                    ecb.SetEnabled(index, entity, false);
                }//수명 다함
            }
        }
    }
}
