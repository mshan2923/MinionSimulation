using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Collections;
using Unity.Physics;
using UnityEditor;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Rendering;
using Unity.Physics.Aspects;
using Collider = Unity.Physics.Collider;
using Unity.Jobs;

public partial class EcsSelectSphere : SystemBase
{
    RenderMeshDescription renderMeshDescription;
    RenderMeshArray renderMeshArray;
    MaterialMeshInfo materialMeshInfo;

    Entity SelectSphere;
    public bool IsSelecting = false;

    public float SelectRadius = 2;

    protected override void OnStartRunning()
    {

        var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        var renderer = sphere.GetComponent<MeshRenderer>();
        var mesh = sphere.GetComponent<MeshFilter>().mesh;

        renderMeshDescription = new RenderMeshDescription(renderer);
        renderMeshArray = new RenderMeshArray(new[] { renderer.material }, new[] { mesh });
        materialMeshInfo = MaterialMeshInfo.FromRenderMeshArrayIndices(0, 0);
        Object.DestroyImmediate(sphere);

    }
    protected override void OnUpdate()
    {
        //마우스 위치 > 월드 위치 변환 
        //Camera.main.ScreenPointToRay

        //Camera.Screen - 게임뷰 / Camera.currect - 에디터뷰

        Enabled = false;
        Debug.Log($"Disabled EcsSelectSphere");
        return;

        if (Input.GetMouseButton(0))
        {
            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            //Debug.DrawRay(ray.origin, ray.direction * 100f, Color.red, 1f);

            //var hitResult = Physics.Raycast(ray, out var hit, 100f);

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
            var Tresult = physicsWorld.CastRay(tray, out var Thit);
            //Debug.DrawLine(tray.Start, tray.End, Color.blue, 1f);

            if (Tresult)
            {
                if (!IsSelecting)
                {
                    if (SelectSphere == Entity.Null)
                    {
                        SelectSphere = CreateSphere(EntityManager, renderMeshArray, materialMeshInfo, renderMeshDescription,
                        1, SelectRadius, Thit.Position, Quaternion.identity);
                    }
                    else
                    {
                        EntityManager.SetEnabled(SelectSphere, true);
                    }

                    IsSelecting = true;
                }


                EntityManager.SetComponentData(SelectSphere,
                                        new LocalTransform
                                        {
                                            Position = Thit.Position,
                                            Rotation = quaternion.identity,
                                            Scale = 1
                                        });

                
                var onHit = new NativeList<ColliderCastHit>(10, Allocator.TempJob);
                /*
                EntityManager.GetAspect<ColliderAspect>(SelectSphere).SphereCastAll(Thit.Position, 1f, new float3(0,-1,0), 0,
                    ref onHit, new CollisionFilter
                    {
                        GroupIndex = 0,
                        BelongsTo = ~0u,//All Layer
                        CollidesWith = ~0u
                    });//안되는데?
                Debug.Log(EntityManager.GetAspect<ColliderAspect>(SelectSphere).Collider.ToString());*/

                physicsWorld.SphereCastAll(Thit.Position, SelectRadius, float3.zero, 0, ref onHit, new CollisionFilter
                {
                    GroupIndex = 0,
                    BelongsTo = ~0u,//All Layer
                    CollidesWith = ~0u
                });//이건 작동함

                //onHit을 IJobParallel 으로


                
                if (onHit.Length > 0)
                    Debug.Log($"Length : {onHit.Length} /  First : {onHit[0].Position} <- {onHit[0].Entity}");
                else
                    Debug.Log("No Collision");
                


                onHit.Dispose();
                //Debug.Log("Select Pawn" + ((SelectedPawn.Length > 0) ? SelectedPawn[0] : "Empty"));

            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            if (SelectSphere == Entity.Null)
            {

            }
            else
            {
                EntityManager.SetEnabled(SelectSphere, false);
            }
            IsSelecting = false;
        }
    }

    Entity CreateSphere(EntityManager entityManager, RenderMeshArray renderMeshArray, MaterialMeshInfo materialMeshInfo, RenderMeshDescription renderMeshDescription,
        int uniformScale, float radius, float3 position, quaternion orientation)
    {
        SphereGeometry sphereGeometry = new SphereGeometry
        {
            Center = float3.zero,
                        Radius = radius
        };
        // Sphere with default filter and material. Add to Create() call if you want non default:
        BlobAssetReference<Unity.Physics.Collider> sphereCollider 
            = Unity.Physics.SphereCollider.Create(sphereGeometry, CollisionFilter.Default);
        //sphereCollider.Value.MassProperties 이거 쓰면 되지 않을까? / (Collider*)collider.GetUnsafePtr(); 대신에?


        return CreateBody(entityManager, renderMeshArray, materialMeshInfo, renderMeshDescription,
            position, orientation, uniformScale, sphereCollider, float3.zero , float3.zero, 1.0f, false);
    }
    Entity CreateBody(EntityManager entityManager, RenderMeshArray renderMeshArray, MaterialMeshInfo materialMeshInfo, RenderMeshDescription renderMeshDescription, float3 position,
        quaternion orientation, float uniformScale, BlobAssetReference<Collider> collider, float3 linearVelocity,
        float3 angularVelocity, float mass, bool isDynamic)
    {


        var archeType =
            isDynamic ?
            entityManager.CreateArchetype
            (
            typeof(LocalTransform), typeof(LocalToWorld),
            typeof(PhysicsCollider), typeof(PhysicsWorldIndex),
            typeof(PhysicsVelocity), typeof(PhysicsMass),
            typeof(PhysicsDamping), typeof(PhysicsGravityFactor)
            ) :
            entityManager.CreateArchetype
            (
                typeof(LocalTransform), typeof(LocalToWorld),
                typeof(PhysicsCollider), typeof(PhysicsWorldIndex)
            );


        var ecb = new EntityCommandBuffer(Allocator.TempJob);
        //var Spawned = new NativeArray<Entity>(1, Allocator.TempJob);

        var e = EntityManager.CreateEntity(archeType);

        RenderMeshUtility.AddComponents(e, EntityManager, renderMeshDescription, renderMeshArray, materialMeshInfo);

        var AddRigidJob = new AddRigidComponent
        {
            ecb = ecb,
            archetype = archeType,
            Spawned = e,

            position = position,
            rotation = orientation,
            uniformScale = uniformScale,

            collider = collider,
            linearVelocity = linearVelocity,
            angularVelocity = angularVelocity,
            Mass = mass,

            isDynamic = false,
            IsTrigger = true
        };

        var AddRigidHandle = AddRigidJob.Schedule(Dependency);
        AddRigidHandle.Complete();

        ecb.Playback(EntityManager);
        ecb.Dispose();

        return e;

        // ECB를 스폰후 , 컴포넌트 추가/설정 해야함

    }

    public partial struct AddRigidComponent : IJob
    {
        public EntityCommandBuffer ecb;
        public EntityArchetype archetype;
        public Entity Spawned;

        public float3 position;
        public quaternion rotation;

        public float uniformScale;
        public BlobAssetReference<Collider> collider;
        public bool isDynamic;

        public float3 linearVelocity;
        public float3 angularVelocity;
        public float Mass;
        public bool IsTrigger;

        public void Execute()
        {

            ecb.AddComponent(Spawned, new RenderBounds
            {
                Value = new AABB
                {
                    Center = new float3(0, 0, 0),
                    Extents = new float3(0.5f, 0.5f, 0.5f)// * uniformScale                    
                }
            });

            ecb.SetComponent(Spawned, new LocalTransform
            {
                Position = position,
                Rotation = rotation,
                Scale = uniformScale
            });

            if (Mathf.Approximately(uniformScale, 1) == false)
            {
                ecb.AddComponent(Spawned, new PostTransformMatrix
                {
                    Value = new float4x4
                    {
                        c0 = new float4(1, 1, 1, 0) * uniformScale,
                        c1 = new float4(1, 1, 1, 0) * uniformScale,
                        c2 = new float4(1, 1, 1, 0) * uniformScale,
                        c3 = new float4(0, 0, 0, 1)
                    }
                });
            }

            if (!IsTrigger)
            {
                ecb.SetComponent(Spawned, new PhysicsCollider
                {
                    Value = collider
                });
            }

            /*
            ecb.SetComponent(e, new PhysicsGravityFactor
            {
                Value = 1
            });*///------ 여기서 문제 발생
        }
    }
    public partial struct AddRigidComponent_Parallel : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ecb;
        public EntityArchetype archetype;
        [WriteOnly] public NativeArray<Entity> Spawned;

        public float3 position;
        public quaternion rotation;
        public float Scale;

        public float uniformScale;
        public BlobAssetReference<Collider> collider;
        public bool isDynamic;

        public float3 linearVelocity;
        public float3 angularVelocity;

        public void Execute([EntityIndexInQuery] int i)
        {
            var e = ecb.CreateEntity(i, archetype);
            Spawned[i] = e;

            ecb.AddComponent(i, e, new RenderBounds
            {
                Value = new AABB
                {
                    Center = new float3(0, 0, 0),
                    Extents = new float3(0.5f, 0.5f, 0.5f)// * uniformScale                    
                }
            });

            ecb.SetComponent(i, e, new LocalTransform 
            { Position = position, 
              Rotation = rotation, 
              Scale = Scale
            });

            if (Mathf.Approximately(uniformScale, 1) == false)
            {
                ecb.AddComponent(i, e, new PostTransformMatrix
                {
                    Value = new float4x4
                    {
                        c0 = new float4(1, 1, 1, 0) * uniformScale,
                        c1 = new float4(1, 1, 1, 0) * uniformScale,
                        c2 = new float4(1, 1, 1, 0) * uniformScale,
                        c3 = new float4(0, 0, 0, 1)
                    }
                });
            }

            ecb.SetComponent(i, e, new PhysicsCollider
            {
                Value = collider
            });

            if (isDynamic) return;

            ecb.SetComponent(i, e, PhysicsMass.CreateDynamic(collider.Value.MassProperties, 1));//======

            ecb.SetComponent(i, e, new PhysicsVelocity()
            {
                Linear = linearVelocity,
                Angular = math.mul(math.inverse(collider.Value.MassProperties.MassDistribution.Transform.rot), angularVelocity)
            });

            ecb.SetComponent(i, e, new PhysicsDamping()
            {
                Linear = 0.01f,
                Angular = 0.05f
            });

            ecb.SetComponent(i, e, new PhysicsGravityFactor
            {
                Value = 1
            });
        }
    }//갯수를 주고 병렬 실행


}
