using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Collections;
using Unity.Burst;
using Unity.Jobs;
using System.Linq;
using System;
using Unity.Collections.LowLevel.Unsafe;

public readonly partial struct MinionAspect : IAspect
{
    public readonly Entity entity;

    internal readonly RefRW<MinionData> minionData;
    public readonly DynamicBuffer<MinionPart> minionParts;

    public bool IsSpawnedPart
    {
        get => minionData.ValueRO.isSpawnedPart;
        set => minionData.ValueRW.isSpawnedPart = value;
    }
    public int PartAmount
    {
        get => minionData.ValueRO.Parts;
    }
    public bool IsDisabling
    {
        get => minionData.ValueRO.DisableCounter >= 0;
    }
    public float DisableCounter
    {
        get => minionData.ValueRO.DisableCounter;
        set => minionData.ValueRW.DisableCounter = value; 
    }

    [System.Obsolete("Exception : This method should have been replaced by source gen. - Run Same Place")]
    public void SpawnMinonParts(NativeArray<MinionAspect> MinionAspect
        ,EntityManager entityManager, JobHandle handle)//===================== static 정적 영역에서 IJobEntity 사용XX
    {
        var ecb = new EntityCommandBuffer(Allocator.TempJob);

        var SpawnHandle = handle;
        foreach(var e in MinionAspect)
        {
            var localHandle = new PartSpawnJob_Aspect()
            {
                ecb = ecb.AsParallelWriter(),
                minionBuffer = e.minionParts.AsNativeArray(),
            }.ScheduleParallel(handle);
            SpawnHandle = JobHandle.CombineDependencies(SpawnHandle, localHandle);
        }
        //================= PartSpawnJob 을 사용하고 , For문으로 MinionDatas , minionBuffer 데이터 준비
        SpawnHandle.Complete();

        ecb.Playback(entityManager);
        ecb.Dispose();



        ecb = new EntityCommandBuffer(Allocator.TempJob);
        entityManager.GetAllUniqueSharedComponents<MinionPartParent>(out var spawnedPart, Allocator.TempJob);
        
        foreach (var e in MinionAspect)
        {
            var temp = from v in spawnedPart
                       where Entity.Equals(e.entity, v.parent)
                       select v.parent;
            var spawned = new NativeArray<Entity>(temp.ToArray(), Allocator.TempJob);

            new PartSpawnSetupJob()
            {
                ecb = ecb.AsParallelWriter(),
                targetParent = e.entity,
                spawnedEntity = spawned
            }.ScheduleParallel(SpawnHandle).Complete();

            spawned.Dispose();
        }

        //============== Entities.WithSharedComponentFilter 대체를...어떻게 
        // ==== 아니면 모든 MinionPartParent를 가진 엔티티 대상으로 Aspect 접근해서?
        //===== 스폰 전과 후에 모든 MinionPartParent를 가진 엔티티 대상을 임시 저장후 빼면 될듯?

        ecb.Playback(entityManager);
        ecb.Dispose();
        spawnedPart.Dispose();

    }
    [System.Obsolete("Exception : This method should have been replaced by source gen. - Run Same Place")]
    public PartSpawnJob_Aspect SpawnToMinionPart(EntityCommandBuffer ecb)
    {
        return new PartSpawnJob_Aspect()
        {
            ecb = ecb.AsParallelWriter(),
            minionBuffer = minionParts.AsNativeArray(),
        };
    }
    [System.Obsolete("Exception : This method should have been replaced by source gen. - Run Same Place")]
    public PartSpawnSetupJob SetupSpawnedPart(EntityCommandBuffer ecb, NativeArray<Entity> spawnedPart)
    {
        return new PartSpawnSetupJob()
        {
            ecb = ecb.AsParallelWriter(),
            targetParent = entity,
            spawnedEntity = spawnedPart
        };
    }

    #region JobSystem

    [BurstCompile]
    public partial struct PartSpawnJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ecb;
        [ReadOnly] public NativeArray<MinionData> MinionDatas;

        [ReadOnly] public NativeArray<MinionPart> minionBuffer;

        public void Execute(Entity entity, [EntityIndexInQuery] int index, in MinionData minionData)
        {
            if (MinionDatas[index].isSpawnedPart == false)
            {
                for (int i = 0; i < minionBuffer.Length; i++)
                {
                    var spawned = ecb.Instantiate(index, minionBuffer[i].Part);

                    //ecb.SetComponent(index, spawned, trans);//Work
                    //ecb.AddComponent(index, spawned, new Parent { Value = entity});
                    ecb.AddSharedComponent(index, spawned, new MinionPartParent { parent = entity });
                }
            }
        }
    }
    [BurstCompile]
    public partial struct PartSpawnJob_Aspect : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ecb;

        //[NativeDisableUnsafePtrRestriction] public MinionAspect minionAspect;
        [ReadOnly] public NativeArray<MinionPart> minionBuffer;

        public void Execute(Entity entity, [EntityIndexInQuery] int index, in MinionData minionData)
        {
            if (minionData.isSpawnedPart == false)
            {
                for (int i = 0; i < minionData.Parts; i++)
                {
                    var spawned = ecb.Instantiate(index, minionBuffer[i].Part);
                    //ecb.SetComponent(index, spawned, trans);//Work
                    //ecb.AddComponent(index, spawned, new Parent { Value = entity});
                    ecb.AddSharedComponent(index, spawned, new MinionPartParent { parent = entity });
                }
            }
        }
    }

    [BurstCompile]
    public partial struct PartSpawnSetupJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ecb;
        [ReadOnly] public Entity targetParent;
        //[NativeDisableUnsafePtrRestriction] public MinionAspect targetAspect;
        [ReadOnly] public NativeArray<Entity> spawnedEntity;

        public void Execute(Entity entity, [EntityIndexInQuery] int index, in MinionData minionData, in DynamicBuffer<MinionPart> parts)
        {
            if (targetParent == entity)//(Equals(targetParent, entity))
            {
                //targetAspect.minionParts.Clear();
                var buffer = ecb.SetBuffer<MinionPart>(index, entity);

                for (int i = 0; i < spawnedEntity.Length; i++)
                {
                    /*
                    parts[i] = new MinionPart
                    {
                        Part = spawnedEntity[i],
                        BodyIndex = i
                    };*/
                    buffer.Add(new MinionPart
                    {
                        Part = spawnedEntity[i],
                        BodyIndex = i
                    });

                    ecb.SetEnabled(index, spawnedEntity[i], false);
                }

                //targetAspect.IsSpawnedPart = true;
                var lData = minionData;
                lData.isSpawnedPart = true;

                ecb.SetComponent(index, entity, lData);
            }
        }
    }
    #endregion
}
