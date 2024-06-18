using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using System.Linq;
using Unity.Entities.UniversalDelegates;
using System;
using System.Reflection;
using Unity.Jobs;
using Unity.Transforms;
using Unity.Mathematics;

partial class MinionSetUpSystem : SystemBase
{
    /*
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        var MinionDatas = state.GetEntityQuery(typeof(MinionData))
            .ToComponentDataArray<MinionData>(Allocator.TempJob);

        //if (MinionDatas.Length > 0)


        MinionDatas.Dispose();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        
    }*/

    protected override void OnStartRunning()
    {
        base.OnStartRunning();
        /*
        {
            var MinionQuery = GetEntityQuery(typeof(MinionData));
            var MinionEntity = MinionQuery.ToEntityArray(Allocator.TempJob);
            var MinionDatas = MinionQuery.ToComponentDataArray<MinionData>(Allocator.TempJob);

            var ecb = new EntityCommandBuffer(Allocator.TempJob);

            for (int i = 0; i < MinionEntity.Length; i++)
            {
                int partLength = MinionDatas[i].Parts;

                var MinionBuffer = SystemAPI.GetBuffer<MinionPart>(MinionEntity[i]).ToNativeArray(Allocator.TempJob);

                new PartSpawnJob()
                {
                    ecb = ecb.AsParallelWriter(),
                    ClipIndex = i,//=================== (임시) 기본 에니메이션
                    MinionDatas = MinionDatas,
                    minionBuffer = MinionBuffer
                }.ScheduleParallel(MinionQuery, Dependency).Complete();

            }

            ecb.Playback(EntityManager);
            ecb.Dispose();
            
            {
                ecb = new EntityCommandBuffer(Allocator.TempJob);

                //var partQuery = GetEntityQuery(typeof(MinionPartParent));

                //Debug.Log($"part Query : {partQuery.CalculateEntityCount()}");

                //var partentities = partQuery.ToEntityArray(Allocator.TempJob);

                for (int i = 0; i < MinionEntity.Length; i++)
                {
                    var spawnedPart = Entities.WithSharedComponentFilter
                        (
                            new MinionPartParent { parent = MinionEntity[i] }
                        ).ToQuery().ToEntityArray(Allocator.TempJob);

                    new PartSpawnSetupJob()
                    {
                        ecb = ecb.AsParallelWriter(),
                        targetParent = MinionEntity[i],
                        spawnedEntity = spawnedPart
                    }.ScheduleParallel(Dependency).Complete();//DynamicBuffer<MinionPart> 에 스폰된 엔티티로 변경시키고 , 비활성화 시킴

                    spawnedPart.Dispose();
                }

                //partentities.Dispose();
                //partParents.Dispose();
                ecb.Playback(EntityManager);
            }

            // ------ ++ IjobEntity Aspect으로 옮기기

            ecb.Dispose();
            MinionEntity.Dispose();
            MinionDatas.Dispose();
        }
        */

        var MinionQuery = GetEntityQuery(typeof(MinionData));
        var MinionEntity = MinionQuery.ToEntityArray(Allocator.TempJob);
        var aspects = new NativeArray<MinionAspect>(MinionEntity.Length, Allocator.TempJob);
        for(int i = 0; i < MinionEntity.Length; i++)
        {
            aspects[i] = SystemAPI.GetAspect<MinionAspect>(MinionEntity[i]);
        }

        var ecb = new EntityCommandBuffer(Allocator.TempJob);

        var spawnHandle = Dependency;
        foreach (var v in aspects)
        {
            var lHandle = new MinionAspect.PartSpawnJob_Aspect()
            {
                ecb = ecb.AsParallelWriter(),
                minionBuffer = v.minionParts.AsNativeArray()
            }.ScheduleParallel(Dependency);
            spawnHandle = JobHandle.CombineDependencies(spawnHandle, lHandle);
        }
        spawnHandle.Complete();
        ecb.Playback(EntityManager);
        ecb.Dispose();


        ecb = new EntityCommandBuffer(Allocator.TempJob);
        spawnHandle = Dependency;
        foreach (var v in aspects)
        {
            var spawnedPart = Entities.WithSharedComponentFilter
                        (
                            new MinionPartParent { parent = v.entity }
                        ).ToQuery().ToEntityArray(Allocator.TempJob);

            var lHandle = new MinionAspect.PartSpawnSetupJob()
            {
                ecb = ecb.AsParallelWriter(),
                targetParent = v.entity,
                //targetAspect = v,
                spawnedEntity = spawnedPart
            }.ScheduleParallel(spawnHandle);
            spawnHandle = JobHandle.CombineDependencies(spawnHandle, lHandle);

            spawnedPart.Dispose(lHandle);//----job 끝난후 할당 해제
        }
        spawnHandle.Complete();
        ecb.Playback(EntityManager);
        ecb.Dispose();


        MinionEntity.Dispose();
        aspects.Dispose();
    }
    protected override void OnUpdate()
    {

    }
    /*
    //[BurstCompile]
    public partial struct PartSpawnJob : IJobParallelFor
    {
        public EntityCommandBuffer.ParallelWriter ecb;
        [ReadOnly] public int ClipIndex;
        [ReadOnly] public NativeArray<MinionData> MinionDatas;

        [ReadOnly] public NativeArray<MinionPart> minionBuffer;

        public void Execute(int index)
        {
            //ref var parts = ref MinionDatas[ClipIndex].MinionParts.Value.Parts;

            if (Entity.Equals(Entity.Null, minionBuffer[index].Part))
                return;

            var spawned = ecb.Instantiate(index, minionBuffer[index].Part);//MinionDatas[ClipIndex].TestDefaultObj

            var trans = MinionAnimationDB.Instance.GetPartTransform(ClipIndex, index, 0);
            trans.Scale = 0.2f;// ========= 임시

            // ++++++++  부모 엔티티 저장

            ecb.SetComponent(index, spawned, trans);
        }
    }*/

    //[BurstCompile]
    public partial struct PartSpawnJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ecb;
        [ReadOnly] public int ClipIndex;
        [ReadOnly] public NativeArray<MinionData> MinionDatas;

        [ReadOnly] public NativeArray<MinionPart> minionBuffer;

        public void Execute(Entity entity, [EntityIndexInQuery] int index , in MinionData minionData)
        {
            //if (Entity.Equals(Entity.Null, minionBuffer[index].Part))
            //    return;

            if (MinionDatas[index].isSpawnedPart == false)
            {
                for (int i = 0; i < minionBuffer.Length; i++)
                {
                    var spawned = ecb.Instantiate(index, minionBuffer[i].Part);

                    var trans = MinionAnimationDB.Instance.GetPartTransform(ClipIndex, i, 0);
                    trans.Scale = 0.2f;// ========= 임시
                    ecb.SetComponent(index, spawned, trans);//Work

                    //ecb.AddComponent(index, spawned, new MinionPartTag());
                    //ecb.AddComponent(index, spawned, new Parent { Value = entity});
                    ecb.AddSharedComponent(index, spawned, new MinionPartParent { parent = entity });
                }
            }
        }
    }

    public partial struct SelectNeedSetupJob : IJobParallelFor
    {
        public EntityCommandBuffer.ParallelWriter ecb;
        public NativeArray<Entity> entities;
        public NativeArray<Parent> parents;

        public Entity targetParent;
        public NativeArray<Entity> spawnedEntity;

        //public void Execute(Entity entity, [EntityIndexInQuery] int index, MinionPartTag partTag, Parent parent)
        public void Execute(int index)
        {
            if (Entity.Equals(targetParent, parents[index].Value))
            {
                spawnedEntity[index] = entities[index];
            }
        }
    }

    public partial struct PartSpawnSetupJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ecb;
        [ReadOnly] public Entity targetParent;
        [ReadOnly] public NativeArray<Entity> spawnedEntity;

        public void Execute(Entity entity, [EntityIndexInQuery] int index, ref MinionData minionData, ref DynamicBuffer<MinionPart> parts)
        {
            if (Entity.Equals(targetParent, entity))
            {
                for (int i = 0; i < spawnedEntity.Length; i++)
                {
                    parts[i] = new MinionPart
                    {
                        Part = spawnedEntity[i],
                        BodyIndex = i
                    };

                    ecb.SetEnabled(index, spawnedEntity[i], false);
                }

                minionData.isSpawnedPart = true;
            }
        }
    }
}
