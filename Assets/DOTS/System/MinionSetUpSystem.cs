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

            }// IjobEntity 에서 MinionData.isSpawnedPart 확인후 생성한 다음 참조로 변경
            // ===== +  원래는 ClipIndex 필요없고 스폰 , 부모지정만 하고  비활성화 시킨후 / 다시 활성화 시킬때 위치 지정

            ecb.Playback(EntityManager);

            {
                for (int i = 0; i < MinionEntity.Length; i++)
                {
                    var spawnedPart = new NativeArray<Entity>(MinionDatas[i].Parts, Allocator.TempJob);

                    var selectHandle = new SelectNeedSetupJob()
                    {
                        ecb = ecb.AsParallelWriter(),
                        targetParent = MinionEntity[i],
                        spawnedEntity = spawnedPart

                    }.ScheduleParallel(Dependency);

                    new DespawnJob()
                    {
                        ecb = ecb.AsParallelWriter(),
                        targetParent = MinionEntity[i],
                        spawnedEntity = spawnedPart
                    }.ScheduleParallel(selectHandle).Complete();
                }
            }

            ecb.Dispose();
            MinionEntity.Dispose();
            MinionDatas.Dispose();
        }
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

        public void Execute(Entity entity, [EntityIndexInQuery] int index , ref MinionData minionData)
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

                    ecb.AddComponent(index, spawned, new MinionPartTag());
                    ecb.AddComponent(index, spawned, new Parent { Value = entity});
                }

                minionData.isSpawnedPart = true;
            }
        }
    }

    public partial struct SelectNeedSetupJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ecb;
        public Entity targetParent;
        public NativeArray<Entity> spawnedEntity;

        public void Execute(Entity entity, [EntityIndexInQuery] int index, MinionPartTag partTag, Parent parent)
        {
            if (Entity.Equals(targetParent, parent.Value))
            {
                spawnedEntity[index] = entity;
            }
        }
    }

    public partial struct DespawnJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ecb;
        [ReadOnly] public Entity targetParent;
        [ReadOnly] public NativeArray<Entity> spawnedEntity;

        public void Execute(Entity entity, [EntityIndexInQuery] int index , ref DynamicBuffer<MinionPart> parts)
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
            }
        }
    }
}
