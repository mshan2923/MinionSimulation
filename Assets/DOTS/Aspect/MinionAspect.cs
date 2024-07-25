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
    public readonly RefRW<MinionData> minionData;
    //[NativeDisableUnsafePtrRestriction, ReadOnly]//--JobSystem���� Aspect �������ؼ�
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

    #region Disabled

    //-- ������� �ʴ� ����
    //Static ���� �����ּ� IJobEntity ��� �Ұ�
    //�Լ��� ���� �����ص� �ȵ� (Exception : This method should have been replaced by source gen. - Run Same Place)
    //
    //Aspect�� �迭�� ��� �Ұ� , GetAspect�� ���ɿ� �� ������ ����
    //Job ���ο��� Liqn ����  ,Job�� idle �ð��� �þ ���ɿ� ��������
    //Array.Fill ���ɿ� �ǿܷ� ū ����

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

    #region JobSystem

    //[BurstCompile] // ParentIndexData.Where ������
    public partial struct PartSpawnJob_Parall : IJobParallelFor
    {
        public EntityCommandBuffer.ParallelWriter ecb;

        [ReadOnly] public NativeArray<MinionPart> Parts;
        [ReadOnly] public NativeArray<KeyValuePair<int, Entity>> ParentIndexData;
        public bool isEnable;
        public void Execute(int index)
        {
            Entity Parent = ParentIndexData.AsParallel().Where(t => t.Key <= index).Last().Value;
            // ������ ĳ���͵��� �������� Parts �ϳ��� �迭���� ��Ƽ� �ް� 
            // ParentIndexData ���� ĳ���Ͱ� �ٲ�� index���� ����
            // ParentIndexData�� key���� index���� �۰ų� �������� ���� ������ ���� ���

            var spawned = ecb.Instantiate(index, Parts[index].Part);
            ecb.AddSharedComponent(index ,spawned, new MinionPartParent { parent = Parent});
            ecb.AddComponent(index, spawned, new MinionPartIndex { Index = Parts[index].SpawnBodyIndex });
        }
    }

    [BurstCompile]
    public partial struct PartSpawnSetupJob_Ref : IJobEntity
    {
        //public EntityCommandBuffer.ParallelWriter ecb;
        [ReadOnly] public Entity targetParent;
        //[NativeDisableUnsafePtrRestriction] public MinionAspect targetAspect;
        [ReadOnly] public NativeArray<Entity> spawnedEntity;

        /// <summary>
        /// Initial Spawn
        /// </summary>
        public bool isEnable;

        public void Execute(Entity entity, [EntityIndexInQuery] int index, ref MinionData minionData, ref DynamicBuffer<MinionPart> parts)
        {
            if (targetParent == entity)
            {
                for (int i = 0; i < spawnedEntity.Length; i++)
                {
                    parts[i] = new MinionPart
                    {
                        Part = spawnedEntity[i],
                        SpawnBodyIndex = -1
                    };

                    //ecb.SetEnabled(index, spawnedEntity[startPartIndex + i], false);
                }

                minionData.isSpawnedPart = true;
                minionData.isEnablePart = isEnable;
            }
        }
    }

    [BurstCompile]
    public partial struct ToggleJob : IJobParallelFor
    {
        public EntityCommandBuffer.ParallelWriter ecb;
        [ReadOnly] public NativeArray<Entity> spawnedEntity;
        public bool value;
        public void Execute(int index)
        {
            ecb.SetEnabled(index, spawnedEntity[index], value);
        }
    }
    #endregion
}
