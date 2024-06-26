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

        var MinionEntities = Minions.ToEntityArray(Allocator.TempJob);
        var MinionDatas = Minions.ToComponentDataArray<MinionData>(Allocator.TempJob);

        {
            // 모든 Minion 마다 Part들을 위치 업데이트...
            //한번에 적용하기 위해 , 데이터 준비한후 , 적용
            // 먼저 Minion 목록 에서 
                // 모든 부위
                // BoneIndex
                // 캐릭터 엔티티
            // HashMap<캐릭터 엔티티, MinionAnimation>

            var AnimationDataParall = new NativeParallelHashMap<Entity, MinionAnimation>(MinionEntities.Length, Allocator.TempJob);
            var MinionTransformParall = new NativeParallelHashMap<Entity, LocalTransform>(MinionEntities.Length, Allocator.TempJob);

            var clipData = GetEntityQuery(typeof(MinionClipData)).ToComponentDataArray<MinionClipData>(Allocator.TempJob);

            //var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
            //    .CreateCommandBuffer(EntityManager.WorldUnmanaged);

            AnimationDataParall.Capacity = MinionEntities.Length;
            MinionTransformParall.Capacity = MinionEntities.Length;

            var setupHandle = new SetUpHashMap()
            {
                AnimationDataParall = AnimationDataParall.AsParallelWriter(),
                MinionTransformParall = MinionTransformParall.AsParallelWriter()
            }.ScheduleParallel(Dependency);

            new UpdateMinionAnimation_Ref()
            {
                animations = AnimationDataParall.AsReadOnly(),
                originTransform = MinionTransformParall.AsReadOnly(),
                ClipDatas = clipData,
                ClipDataInterval = MinionAnimationDB.ClipDataInterval
            }.ScheduleParallel(setupHandle).Complete();
            //=============== 크기조절 에 따른 위치 변경이 없음 -> 초기값을 덮어 쓸 수 밖에 없음

            //Aspects.Dispose();
            AnimationDataParall.Dispose();
            MinionTransformParall.Dispose();
            clipData.Dispose();
        }

        MinionEntities.Dispose();
        //Aspects.Dispose();
        MinionDatas.Dispose();
    }

    [BurstCompile]
    public partial struct SetUpHashMap : IJobEntity
    {
        public NativeParallelHashMap<Entity, MinionAnimation>.ParallelWriter AnimationDataParall;
        public NativeParallelHashMap<Entity, LocalTransform>.ParallelWriter MinionTransformParall;
        public void Execute(Entity entity, in MinionAnimation animation, in MinionData minionData, in LocalTransform transform)
        {
            if (minionData.isEnablePart && minionData.DisableCounter <= 0)
            {
                AnimationDataParall.TryAdd(entity, animation);
                MinionTransformParall.TryAdd(entity, transform);
            }
        }
    }

    //[BurstCompile]
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

                transform.Position = worldTrans.Position + localTrans.Position;
                transform.Rotation = math.mul(worldTrans.Rotation, localTrans.Rotation);
                transform.Scale = worldTrans.Scale * localTrans.Scale;
            }
        }
    }
    [BurstCompile]
    public partial struct UpdateMinionAnimation : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ecb;
        [ReadOnly] public NativeParallelHashMap<Entity, MinionAnimation>.ReadOnly animations;
        [ReadOnly] public NativeParallelHashMap<Entity, LocalTransform>.ReadOnly originTransform;

        public void Execute(Entity e, [EntityIndexInQuery] int index, in MinionPartIndex partIndex, in MinionPartParent parent,
            ref LocalTransform transform)
        {
            if (animations.TryGetValue(parent.parent, out var anim))
            {
                originTransform.TryGetValue(parent.parent, out var worldTrans);
                var localTrans = worldTrans;
                localTrans.Position = //MinionAnimationDB.Instance.GetPartTransform(anim.CurrectAnimation, partIndex.Index, anim.PlayTime).Position;
                    new float3(0, partIndex.Index, 0);


                ecb.SetComponent(index, e, new LocalTransform
                {
                    Position = worldTrans.Position + localTrans.Position,
                    Rotation = math.mul(worldTrans.Rotation, localTrans.Rotation),
                    Scale = worldTrans.Scale * localTrans.Scale
                });
            }
        }
    }
}
