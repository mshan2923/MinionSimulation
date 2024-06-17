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
            var MinionEntity = GetEntityQuery(typeof(MinionData)).ToEntityArray(Allocator.TempJob);
            var MinionDatas = GetEntityQuery(typeof(MinionData))
                .ToComponentDataArray<MinionData>(Allocator.TempJob);

           

            var ecb = new EntityCommandBuffer(Allocator.TempJob);

            int ClipIndex = 0;
            {
                int partLength = MinionDatas[ClipIndex].Parts;

                //var temp = EntityManager.Instantiate(MinionDatas[ClipIndex].TestDefaultObj);//Work

                var MinionBuffer = SystemAPI.GetBuffer<MinionPart>(MinionEntity[ClipIndex]).ToNativeArray(Allocator.TempJob);

                new PartSpawnJob()
                {
                    ecb = ecb.AsParallelWriter(),
                    MinionDatas = MinionDatas,
                    minionBuffer = MinionBuffer
                }.Schedule(partLength, 32, Dependency).Complete();


                ecb.Playback(EntityManager);
            }

            ecb.Dispose();
            MinionEntity.Dispose();
            MinionDatas.Dispose();
        }
    }
    protected override void OnUpdate()
    {

    }

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

            var spawned = ecb.Instantiate(index, minionBuffer[index].Part);//MinionDatas[ClipIndex].TestDefaultObj

            var trans = MinionAnimationDB.Instance.GetPartTransform(ClipIndex, index, 0);
            trans.Scale = 0.2f;

            ecb.SetComponent(index, spawned, trans);
        }
    }
}
