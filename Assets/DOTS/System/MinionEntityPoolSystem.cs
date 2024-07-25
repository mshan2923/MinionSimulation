using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using System.Linq;
using Unity.Jobs;
using Unity.Transforms;
using Unity.Burst;

[UpdateAfter(typeof(MinionSystem))]
public partial class MinionEntityPoolSystem : SystemBase
{
    float Ltime = 0;
    MinionSpawnControllData spawnData;

    protected override void OnStartRunning()
    {
        spawnData = SystemAPI.GetSingleton<MinionSpawnControllData>();
    }
    protected override void OnUpdate()
    {
        if (Ltime < spawnData.SpawnInterval)
        {
            Ltime += SystemAPI.Time.DeltaTime;
        }
        else
        {
            var minionQuery = Entities.WithAll<MinionData, LocalTransform>().ToQuery();
            var Disabled = new NativeParallelHashMap<int, Entity>(minionQuery.CalculateEntityCount(), Allocator.TempJob);

            var GetDisabledHandle = new GetDisableMinionJob
            {
                Disabled = Disabled.AsParallelWriter(),
                SpawnAmount = spawnData.SpawnAmount,
            }.ScheduleParallel(minionQuery, Dependency);
            GetDisabledHandle.Complete();
            //Debug.Log($"{minionList.Length} / {minionList.Capacity} != {minionQuery.CalculateEntityCount()}");

            if (Disabled.Count() <= 0)
            {
                Disabled.Dispose();
                return;
            }

            Ltime = 0;

            var ecb = SystemAPI.GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(EntityManager.WorldUnmanaged);

            var partsList = new NativeList<Entity>(Allocator.TempJob);

            var disableEntity = Disabled.GetValueArray(Allocator.TempJob);
            
            for (int i = 0; i < Mathf.Min(Disabled.Count(), spawnData.SpawnAmount); i++)
            {
                var aspect = SystemAPI.GetAspect<MinionAspect>(disableEntity[i]);
                var buffer = aspect.minionParts.ToNativeArray(Allocator.Temp);

                var bufferArray = buffer.Select(t => t.Part).ToArray();

                var bufferNative = new NativeArray<Entity>(bufferArray, Allocator.Temp);
                partsList.AddRange(bufferNative);
                bufferNative.Dispose();
                buffer.Dispose();

                {
                    aspect.IsEnablePart = true;
                    aspect.DisableCounter = -1;
                    var trans = aspect.tranform.ValueRO;
                    trans.Position = spawnData.SpawnTarget + Vec2ToFloat3(UnityEngine.Random.insideUnitCircle) * spawnData.SpawnRadius;
                    ecb.SetComponent(aspect.entity, trans);
                }//Setup
            }
            disableEntity.Dispose();

            var parts = partsList.ToArray(Allocator.TempJob);

            var toggleHandle = new MinionAspect.ToggleJob()
            {
                ecb = ecb.AsParallelWriter(),
                spawnedEntity = parts,
                value = true
            }.Schedule(parts.Length, 32, GetDisabledHandle);
            toggleHandle.Complete();

            partsList.Dispose(toggleHandle);
            parts.Dispose(toggleHandle);

            Disabled.Dispose();
        }
    }

    public float3 Vec2ToFloat3(Vector2 vec2)
    {
        return new float3(vec2.x, 0, vec2.y);
    }

    [BurstCompile]
    public partial struct GetDisableMinionJob : IJobEntity
    {
        public NativeParallelHashMap<int, Entity>.ParallelWriter Disabled;
        public int SpawnAmount;

        public void Execute([EntityIndexInQuery] int index, Entity entity, MinionData minion)
        {
            if (minion.IsActive == false && minion.DisableCounter < 0)
            {
                Disabled.TryAdd(index, entity);
            }
        }
    }
}
