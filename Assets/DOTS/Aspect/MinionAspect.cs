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
using Unity.Entities.UniversalDelegates;
using Unity.Transforms;

public readonly partial struct MinionAspect : IAspect
{
    public readonly Entity entity;

    //[NativeDisableUnsafePtrRestriction]
    private readonly RefRW<MinionData> minionData;
    //[NativeDisableUnsafePtrRestriction, ReadOnly]//--JobSystem에서 Aspect 쓰기위해서
    public readonly DynamicBuffer<MinionPart> minionParts;
    public readonly RefRW<MinionAnimation> minionAnimation;
    public readonly RefRO<LocalTransform> tranform;

    public bool IsSpawnedPart
    {
        get => minionData.ValueRO.isSpawnedPart;
        set => minionData.ValueRW.isSpawnedPart = value;
    }
    public bool IsEnablePart
    {
        get => minionData.ValueRO.isEnablePart;
        set => minionData.ValueRW.isEnablePart = value;
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

    public int PreviousClip
    {
        get => minionAnimation.ValueRO.PreviousAnimation;
        internal set => minionAnimation.ValueRW.PreviousAnimation = value;
    }
    public float StopedTime
    {
        get => minionAnimation.ValueRO.StopedTime;
        internal set => minionAnimation.ValueRW.StopedTime = value;
    }
    public int CurrectClip
    {
        get => minionAnimation.ValueRO.CurrectAnimation;
        internal set => minionAnimation.ValueRW.CurrectAnimation = value;
    }
    public float PlayTime
    {
        get => minionAnimation.ValueRO.PlayTime;
        internal set => minionAnimation.ValueRW.PlayTime = value;
    }

    public void ChangeAnimation(int newClipIndex)
    {
        PreviousClip = CurrectClip;
        StopedTime = PlayTime;

        CurrectClip = newClipIndex;
        PlayTime = 0;
    }
    /// <summary>
    /// false : End of animation
    /// </summary>
    /// <param name="deltaTime"></param>
    /// <returns></returns>
    public bool AnimationAddDelta(float deltaTime)
    {
        if (CurrectClip < 0)
            return false;

        float length = MinionAnimationDB.Instance.GetClipLength(CurrectClip);
        if (PlayTime + deltaTime < length)
        {
            PlayTime += deltaTime;
            return true;
        }
        else
        {
            PlayTime = length;
            return false;
        }
    }

    #region Disabled
    [System.Obsolete("Exception : This method should have been replaced by source gen. - Run Same Place")]
    public void SpawnMinonParts(NativeArray<MinionAspect> MinionAspect
        ,EntityManager entityManager, JobHandle handle)//===================== static 정적 영역에서 IJobEntity 사용XX
    {
        var ecb = new EntityCommandBuffer(Allocator.TempJob);

        var SpawnHandle = handle;
        foreach(var e in MinionAspect)
        {
            var localHandle = new PartSpawnJob()
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
    public PartSpawnJob SpawnToMinionPart(EntityCommandBuffer ecb)
    {
        return new PartSpawnJob()
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


    [BurstCompile, System.Obsolete("MinionAspect 를 중첩해서 사용 불가")]
    public partial struct PartSpawnJob_Aspect : IJobParallelFor
    {
        public EntityCommandBuffer.ParallelWriter ecb;

        public NativeArray<MinionAspect> minionAspect;

        public void Execute(int index)
        {
            var v = minionAspect[index];

            if (v.IsSpawnedPart == false)
            {
                foreach (var i in v.minionParts)
                {
                    var spawned = ecb.Instantiate(index, i.Part);
                    ecb.AddSharedComponent(index, spawned, new MinionPartParent { parent = v.entity });
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
    #endregion Disabled

    public static void GetSharedMutiplyFillter(EntityQuery query, NativeArray<Entity> FillterShared,
    ref NativeList<Entity> FilltedEntities)
    {
        foreach (var v in FillterShared)
        {
            var tempQuery = query;
            tempQuery.SetSharedComponentFilter(new MinionPartParent { parent = v});

            var temp = tempQuery.ToEntityArray(Allocator.Temp);

            FilltedEntities.AddRange(temp);

            temp.Dispose();
        }
    }
    public static void GetSharedMutiplyFillter<T>(EntityQuery query, NativeArray<T> FillterShared,
        ref NativeList<Entity> FilltedEntities) where T : unmanaged, ISharedComponentData
    {
        foreach(var v in FillterShared)
        {
            var tempQuery = query;
            tempQuery.SetSharedComponentFilter(v);

            var temp = tempQuery.ToEntityArray(Allocator.Temp);

            FilltedEntities.AddRange(temp);
        }
    }

    /// <summary>
    /// allPartsParent is {Aspect.IsEnablePart : true ==> Spawned Minion Part / false ==> Spawn source entity}
    /// </summary>
    [System.Obsolete("Perfomance Issue - Array.Fill")]
    public static void GetMinionsPart(NativeArray<MinionAspect> aspects , bool isNotSpawned,
        ref NativeList<MinionPart> allParts, ref NativeList<Entity> allPartsParent)
    {
        for (int v = 0; v < aspects.Length; v++)
        {
            if (aspects[v].IsEnablePart == isNotSpawned)
                continue;

            var buffer = aspects[v].minionParts.ToNativeArray(Allocator.Temp);
            var parents = new Entity[buffer.Length];
            Array.Fill(parents, aspects[v].entity);
            var parentNative = new NativeArray<Entity>(parents, Allocator.Temp);

            allParts.AddRange(buffer);
            allPartsParent.AddRange(parentNative);

            buffer.Dispose();
            parentNative.Dispose();
        }
    }

    #region JobSystem

    [BurstCompile]
    public partial struct PartSpawnJob : IJobEntity
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
    public partial struct PartSpawnJob_Parall : IJobParallelFor
    {
        public EntityCommandBuffer.ParallelWriter ecb;

        [ReadOnly] public NativeList<MinionPart> Parts;
        [ReadOnly] public NativeList<Entity> PartParents;
        public void Execute(int index)
        {
            var spawned = ecb.Instantiate(index, Parts[index].Part);
            ecb.AddSharedComponent(index ,spawned, new MinionPartParent { parent = PartParents[index] });
            ecb.AddComponent(index, spawned, new MinionPartIndex { Index = Parts[index].BodyIndex });
        }
    }

    [BurstCompile]
    public partial struct PartSpawnSetupJob_Ref : IJobEntity
    {
        //public EntityCommandBuffer.ParallelWriter ecb;
        [ReadOnly] public Entity targetParent;
        //[NativeDisableUnsafePtrRestriction] public MinionAspect targetAspect;
        [ReadOnly] public NativeArray<Entity> spawnedEntity;

        public void Execute(Entity entity, [EntityIndexInQuery] int index, ref MinionData minionData, ref DynamicBuffer<MinionPart> parts)
        {
            if (targetParent == entity)//(Equals(targetParent, entity))
            {
                for (int i = 0; i < spawnedEntity.Length; i++)
                {
                    parts[i] = new MinionPart
                    {
                        Part = spawnedEntity[i],
                        BodyIndex = i
                    };

                    //ecb.SetEnabled(index, spawnedEntity[startPartIndex + i], false);
                }

                minionData.isSpawnedPart = true;

            }
        }
    }

    [BurstCompile]
    public partial struct ToggleJob : IJobParallelFor
    {
        public EntityCommandBuffer.ParallelWriter ecb;
        [ReadOnly] public NativeList<Entity> spawnedEntity;
        public bool value;
        public void Execute(int index)
        {
            ecb.SetEnabled(index, spawnedEntity[index], value);
        }
    }
    #endregion
}
