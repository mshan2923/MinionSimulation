using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

public partial class SelectMinionSystem : SystemBase , IEcsAddImpact
{
    public delegate void SelectedMinionsDelegate(NativeArray<Entity> entities);
    public SelectedMinionsDelegate SelectedMinions;

    List<LocalTransform> ImpactPoints = new List<LocalTransform>();

    public float DebugSelectRadius = 1.5f;

    protected override void OnUpdate()
    {
        if (Input.GetMouseButton(0))//For Debuging
        {
            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            PhysicsWorldSingleton physicsWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>();

            var tray = new RaycastInput()
            {
                Start = Camera.main.transform.position,//ray.origin,
                End = Camera.main.transform.position + ray.direction * 100f,//ray.direction * 100f,
                Filter = new CollisionFilter
                {
                    GroupIndex = 0,
                    BelongsTo = ~0u,//All Layer
                    CollidesWith = ~0u
                }
            };
            var Tresult = physicsWorld.CastRay(tray, out var Thit);//충돌 대상이 서브씬에 있지 않으면 가끔 충돌이 안됨

            if (Tresult)
            {
                ImpactPoints.Add(new LocalTransform
                {
                    Position = Thit.Position,
                    Rotation = Quaternion.LookRotation(tray.End - tray.Start),
                    Scale = DebugSelectRadius
                });
            }else
            {
                Debug.Log("Fail to Collision");
            }
        }//For Debuging


        if (ImpactPoints.Count > 0)
        {
            var points = ImpactPoints.ToNativeArray(Allocator.TempJob);
            ImpactPoints.Clear();

            var datas = GetEntityQuery(typeof(MinionData)).ToComponentDataArray<MinionData>(Allocator.TempJob);

            new SelectMinions()
            {
                datas = datas,
                points = points
            }.ScheduleParallel(Dependency).Complete();

            datas.Dispose();
            points.Dispose();

            ImpactPoints.Clear();
        }
    }

    public void AddImpact(LocalTransform transform)
    {
        ImpactPoints.Add(transform);
    }

    [BurstCompile]
    public partial struct SelectMinions : IJobEntity
    {
        public NativeArray<MinionData> datas;
        public NativeArray<LocalTransform> points;
        public void Execute([EntityIndexInQuery]int index, ref MinionData minion, in LocalTransform transform)
        {
            if (datas[index].isSpawnedPart && datas[index].isEnablePart)
            {
                if (datas[index].DisableCounter > 0)
                    return;

                foreach(var v in points)
                {
                    if (Unity.Mathematics.math.distance(transform.Position, v.Position) <= v.Scale * v.Scale)
                    {
                        if (datas[index].DisableCounter < 0)
                        {
                            minion.DisableCounter = 0;
                            minion.ImpactLocation = v.Position;
                        }
                    }
                }
            }
        }
    }
}
public interface IEcsAddImpact
{
    public void AddImpact(LocalTransform transform);
}
