using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Unity.Entities;
using Unity.Physics;
using Unity.Collections;
using Unity.Mathematics;

public readonly partial struct SwitchRigidAspect : IAspect
{
    internal readonly RefRO<PhysicsCollider> collider;

    /// <summary>
    /// --Must include PhysicsCollider in TargetQuery--
    /// </summary>
    /// <returns></returns>
    public static bool AddRigid(EntityQuery TargetQuery, EntityManager manager, Unity.Jobs.JobHandle handle,
        float3 LinearVelo, float3 AngularVelo, uint PhysicsWorldIndex = 0, float mass = 1,
        byte isKinematic = 0, float LinearDamping = 0.01f, float AngularDamping = 0.05f, float GravityFactor = 1)
    {

        var builder = new EntityQueryBuilder(Allocator.TempJob).WithAny<PhysicsCollider>();//.WithNone<PhysicsVelocity>();
        if (true)//(TargetQuery.CompareQuery(builder))
        {
            var ecb = new EntityCommandBuffer(Allocator.TempJob);
            
            var colliders = TargetQuery.ToComponentDataArray<PhysicsCollider>(Allocator.TempJob);

            var addRigidJob = new AddRigidJob()
            {
                ecb = ecb.AsParallelWriter(),
                collders = colliders,
                LinearVelocity = LinearVelo,
                AngularVelocity = AngularVelo,
                phsicsWorldIndex = PhysicsWorldIndex,
                mass = mass,
                isKinematic = isKinematic,
                LinearDamping = LinearDamping,
                AngularDamping = AngularDamping,
                GravityFactor = GravityFactor
            };
            var addRigidHandle = addRigidJob.ScheduleParallel(TargetQuery, handle);

            addRigidHandle.Complete();

            ecb.Playback(manager);
            ecb.Dispose();

            builder.Dispose();
            return true;
        }else
        {
            builder.Dispose();
            return false;
        }        
    }

    public partial struct AddRigidJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ecb;
        public NativeArray<PhysicsCollider> collders;

        public float3 LinearVelocity;
        public float3 AngularVelocity;
        public uint phsicsWorldIndex;
        public float mass;
        public byte isKinematic;
        public float LinearDamping;
        public float AngularDamping;
        public float GravityFactor;

        public void Execute(Entity entity, [EntityIndexInQuery] int i)
        {
            ecb.AddComponent(i, entity, new PhysicsVelocity() { Linear = LinearVelocity, Angular = AngularVelocity });
            ecb.AddSharedComponent(i, entity, new PhysicsWorldIndex() { Value = phsicsWorldIndex });
            ecb.AddComponent(i, entity, PhysicsMass.CreateDynamic(collders[i].MassProperties, mass));

            ecb.AddComponent(i, entity, new PhysicsMassOverride() { IsKinematic = isKinematic });
            ecb.AddComponent(i, entity, new PhysicsDamping() { Linear = LinearDamping, Angular = AngularDamping });
            ecb.AddComponent(i, entity, new PhysicsGravityFactor() { Value = GravityFactor });
        }
    }
}
