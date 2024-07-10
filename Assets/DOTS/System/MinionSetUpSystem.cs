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
using static UnityEngine.Rendering.VolumeComponent;
using Unity.VisualScripting;

[UpdateAfter(typeof(EcsSpawnerSystem))]
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


    bool isToggle = false;
    protected override void OnStartRunning()
    {
        base.OnStartRunning();

        var MinionQuery = GetEntityQuery(typeof(MinionData));
        var MinionEntity = MinionQuery.ToEntityArray(Allocator.TempJob);

        var aspects = new NativeArray<MinionAspect>(MinionEntity.Length, Allocator.TempJob);
        for (int v = 0; v < MinionEntity.Length; v++)
        {
            aspects[v] = SystemAPI.GetAspect<MinionAspect>(MinionEntity[v]);
        }

        var ecb = new EntityCommandBuffer(Allocator.TempJob);
        var spawnHandle = Dependency;

        {
            var allParts = new NativeList<MinionPart>(Allocator.Temp);
            var ParentIndexData = new NativeArray<KeyValuePair<int, Entity>>(MinionEntity.Length, Allocator.TempJob);

            for (int v = 0; v < MinionEntity.Length; v++)
            {
                if (aspects[v].IsEnablePart == true)
                    continue;

                var buffer = aspects[v].minionParts.ToNativeArray(Allocator.Temp);
                ParentIndexData[v] = new KeyValuePair<int, Entity>(allParts.Length, MinionEntity[v]);

                allParts.AddRange(buffer);

                buffer.Dispose();
            }
            var allPartArray = allParts.ToArray(Allocator.TempJob);

            spawnHandle = new MinionAspect.PartSpawnJob_Parall()
            {
                ecb = ecb.AsParallelWriter(),
                Parts = allPartArray,
                ParentIndexData = ParentIndexData,
            }.Schedule(allParts.Length, 32, Dependency);
            //Buffer들을 전부 합치고 , 같은 크기로 MinionPartParent 값 준비후 , 한번에 스폰 

            spawnHandle.Complete();
            ecb.Playback(EntityManager);

            ecb.Dispose();
            allParts.Dispose();
            allPartArray.Dispose();
            ParentIndexData.Dispose();
        }//Spawn



        var allPartQuery = Entities.WithSharedComponentFilter(new MinionPartParent { }).ToQuery();

        for (int i = 0; i < aspects.Length; i++)
        {
            var v = aspects[i];

            var spawnedQuery = allPartQuery;
            spawnedQuery.SetSharedComponentFilter(new MinionPartParent { parent = v.entity });

            var spawnedPart = spawnedQuery.ToEntityArray(Allocator.TempJob);

            var lHandle = new MinionAspect.PartSpawnSetupJob_Ref()
            {
                targetParent = v.entity,
                spawnedEntity = spawnedPart
            }.ScheduleParallel(spawnHandle);
            spawnHandle = JobHandle.CombineDependencies(spawnHandle, lHandle);

            spawnedPart.Dispose(lHandle);//----job 끝난후 할당 해제
        }//SetUp

        {
            ecb = new EntityCommandBuffer(Allocator.TempJob);

            allPartQuery.ResetFilter();

            var fillted = new NativeList<Entity>(Allocator.Temp);
            MinionAspect.GetSharedMutiplyFillter(allPartQuery, MinionEntity, ref fillted);
            var spawned = fillted.ToArray(Allocator.TempJob);

            var toggleHandle = new MinionAspect.ToggleJob()
            {
                ecb = ecb.AsParallelWriter(),
                spawnedEntity = spawned,
                value = false
            }.Schedule(fillted.Length, 32, spawnHandle);

            toggleHandle.Complete();
            ecb.Playback(EntityManager);

            ecb.Dispose();
            fillted.Dispose();
            spawned.Dispose(toggleHandle);
        }//Toggle

        MinionEntity.Dispose();
        aspects.Dispose();

        ToggleAllMinions();
    }
    protected override void OnUpdate()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            ToggleAllMinions();
        }//Toggle All Minions - Mouse Right Button
    }

    public void ToggleAllMinions()
    {
        var minions = Entities.WithAll<MinionData>().ToQuery().ToEntityArray(Allocator.TempJob);

        var Parts = GetEntityQuery(typeof(MinionPartIndex));
        var partArray = Parts.ToEntityArray(Allocator.TempJob);

        if (Parts.CalculateEntityCount() > 0)// true -> 분리 시작 , false -> 비활성화된 부위 활성화
        {
            foreach (var e in minions)
            {
                var aspect = SystemAPI.GetAspect<MinionAspect>(e);
                aspect.DisableCounter = 0;

                Unity.Mathematics.Random random = new((uint)(aspect.entity.Index + SystemAPI.Time.DeltaTime * 1000));
                float3 offst = random.NextFloat3();
                aspect.minionData.ValueRW.ImpactLocation = aspect.tranform.ValueRO.Position + offst;
            }//매 프레임 마다 하면 성능에 지장이 있지만 편해서
        }
        else
        {
            var ecb = SystemAPI.GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(EntityManager.WorldUnmanaged);
            //new EntityCommandBuffer(Allocator.TempJob);

            var partsList = new NativeList<Entity>(Allocator.Temp);
            {
                foreach (var e in minions)
                {
                    var aspect = SystemAPI.GetAspect<MinionAspect>(e);
                    var buffer = aspect.minionParts.ToNativeArray(Allocator.Temp);

                    var bufferArray = buffer.Select(t => t.Part).ToArray();

                    var bufferNative = new NativeArray<Entity>(bufferArray, Allocator.Temp);
                    partsList.AddRange(bufferNative);
                    bufferNative.Dispose();
                    buffer.Dispose();
                }
            }//Get All MinionPart - Disable Entity Can't find in Query
            var parts = partsList.ToArray(Allocator.TempJob);


            var toggleHandle = new MinionAspect.ToggleJob()
            {
                ecb = ecb.AsParallelWriter(),
                spawnedEntity = parts,
                value = true
            }.Schedule(parts.Length, 32, Dependency);
            toggleHandle.Complete();

            foreach (var e in minions)
            {
                var aspect = SystemAPI.GetAspect<MinionAspect>(e);
                aspect.IsEnablePart = true;
                aspect.DisableCounter = -1;
            }//매 프레임 마다 하면 성능에 지장이 있지만 편해서

            //ecb.Playback(EntityManager);
            //ecb.Dispose();
        }

        minions.Dispose();
    }
    public partial struct FillJob<T>: IJobParallelFor where T : struct
    {
        public NativeArray<T> partsList;
        public T data;

        public void Execute(int index)
        {
            partsList[index] = data;
        }
    }

}
