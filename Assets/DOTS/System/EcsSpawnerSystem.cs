using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using UnityEngine;

/// <summary>
/// Sample Instance Spawer
/// </summary>
public partial class EcsSpawnerSystem : SystemBase
{
    protected override void OnStartRunning()
    {
        base.OnStartRunning();

        //var ecb = SystemAPI.GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>()
        //    .CreateCommandBuffer(EntityManager.WorldUnmanaged);
        var ecb = new EntityCommandBuffer(Allocator.TempJob);
        var spawnerQuery = GetEntityQuery(typeof(EcsSpawerComponent));
        var spawnerData = spawnerQuery.ToComponentDataArray<EcsSpawerComponent>(Allocator.TempJob);

        new SpawnJob()
        {
            ecb = ecb.AsParallelWriter(),
            data = spawnerData
        }.Schedule(spawnerData.Length, 8, Dependency).Complete();
        ecb.Playback(EntityManager);


        spawnerData.Dispose();
        ecb.Dispose();
    }
    protected override void OnUpdate()
    {
        Enabled = false;
    }

    //[BurstCompile]
    public struct SpawnJob : IJobParallelFor
    {
        public EntityCommandBuffer.ParallelWriter ecb;
        public NativeArray<EcsSpawerComponent> data;

        public void Execute(int index)
        {
            int width = 1;
            if (data[index].is3D)
            {
                width = Mathf.FloorToInt(Mathf.Pow(index, 0.3333f));
            }
            else
            {
                width = Mathf.FloorToInt(Mathf.Sqrt(data[index].amount));
            }

            for ( var i = 0; i < data[index].amount; i++ )
            {
                var spawned = ecb.Instantiate(index, data[index].Target);

                var trans = LocalTransform.Identity;
                trans.Scale = 1;
                if (data[index].is3D)
                {
                    trans.Position = new Unity.Mathematics.float3(i % width, i % width % width , i / width) * data[index].between;
                }
                else
                {
                    trans.Position = new Unity.Mathematics.float3(i % width, 0, i / width) * data[index].between;
                }
                trans.Position += data[index].Origin;

                ecb.SetComponent(index, spawned, trans);
            }
        }
    }
}
