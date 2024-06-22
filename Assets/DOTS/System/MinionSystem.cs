using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[UpdateAfter(typeof(MinionSetUpSystem))]
public partial class MinionSystem : SystemBase
{
    EntityQuery Minions;
    protected override void OnStartRunning()
    {
        base.OnStartRunning();
        Minions = GetEntityQuery(typeof(MinionData));
        var MinionEntities = Minions.ToEntityArray(Allocator.TempJob);

        foreach(var e in MinionEntities)
        {
            var aspect = SystemAPI.GetAspect<MinionAspect>(e);
            aspect.ChangeAnimation(0);
        }

        MinionEntities.Dispose();
    }
    protected override void OnUpdate()
    {

        var MinionEntities = Minions.ToEntityArray(Allocator.TempJob);
        var MinionDatas = Minions.ToComponentDataArray<MinionData>(Allocator.TempJob);

        if (false)
        {
            // ��� Minion ���� Part���� ��ġ ������Ʈ...
            //�ѹ��� �����ϱ� ���� , ������ �غ����� , ����
            // ���� Minion ��� ���� 
                // ��� ����
                // BoneIndex
                // ĳ���� ��ƼƼ
            // HashMap<ĳ���� ��ƼƼ, MinionAnimation>

            var Aspects = new NativeArray<MinionAspect>(MinionEntities.Length, Allocator.TempJob);
            var AllParts = new NativeList<MinionPart>(Allocator.TempJob);
            var AllPartsParent = new NativeList<Entity>(Allocator.TempJob);

            var AnimationData = new NativeHashMap<Entity, MinionAnimation>(MinionEntities.Length, Allocator.TempJob);
            var MinionTransform = new NativeHashMap<Entity, LocalTransform>(MinionEntities.Length, Allocator.TempJob);


            for (int i = 0; i < Aspects.Length; i++)
            {
                Aspects[i] = SystemAPI.GetAspect<MinionAspect>(MinionEntities[i]);

                if (Aspects[i].IsEnablePart && Aspects[i].DisableCounter <= 0)
                {
                    AnimationData.Add(Aspects[i].entity, Aspects[i].minionAnimation.ValueRO);
                    MinionTransform.Add(Aspects[i].entity, Aspects[i].tranform.ValueRO);

                    if (Aspects[i].AnimationAddDelta(SystemAPI.Time.DeltaTime) == false)
                    {
                        Aspects[i].ChangeAnimation(0);
                    }//=============== �ӽ�
                }
                //===============Aspect���� �������� ���� ���ϸ��̼� + PlayTime ����
            }//======= �̰͵� Job���� �ؾߵ�

            MinionAspect.GetMinionsPart(Aspects, false, ref AllParts, ref AllPartsParent);//====== �̰� �����ε�
                        //EntityManager.GetSharedComponentManaged()

            Debug.Log($"e : {MinionEntities.Length} , part : {AllParts.Length} , parent : {AllPartsParent.Length}");

            if (AllParts.Length > 0)
            {
                
                var ecb = new EntityCommandBuffer(Allocator.TempJob);

                var AllPartArray = AllParts.ToArray(Allocator.TempJob);
                var AllPartsParentArray = AllPartsParent.ToArray(Allocator.TempJob);

                new UpdateMinionAnimation()
                {
                    ecb = ecb.AsParallelWriter(),
                    parts = AllPartArray,
                    partsParent = AllPartsParentArray,
                    animations = AnimationData.AsReadOnly(),
                    transform = MinionTransform.AsReadOnly()
                }.Schedule(AllParts.Length, 32, Dependency).Complete();
                ecb.Playback(EntityManager);
                ecb.Dispose();

                AllPartArray.Dispose();
                AllPartsParentArray.Dispose();
                
            }//=============== ũ������ �� ���� ��ġ ������ ���� , Job ���� �ַ� �������� �ϴ������� ���� ����

            MinionEntities.Dispose();
            Aspects.Dispose();
            AllParts.Dispose();
            AllPartsParent.Dispose();
            AnimationData.Dispose();
            MinionTransform.Dispose();
        }//First Try

        {
            //var ecb = SystemAPI.GetSingleton<BeginFixedStepSimulationEntityCommandBufferSystem.Singleton>()
            //    .CreateCommandBuffer(EntityManager.WorldUnmanaged).AsParallelWriter();

            var origins = new NativeArray<LocalTransform>(MinionEntities.Length, Allocator.TempJob);

            var updateHandle = Dependency;
            //for (int i = 0; i < MinionEntities.Length; i++)
            {
                //Aspects[i] = SystemAPI.GetAspect<MinionAspect>(MinionEntities[i]);//====== �̰�...������?
                //origins[i] = Aspects[i].tranform.ValueRO;
            }

            for (int i = 0; i < MinionEntities.Length; i++)
            {
                //var aspect = Aspects[i];
                var parent = MinionEntities[i];
                var data = MinionDatas[i];

                if (data.isEnablePart)
                {
                    var origin = origins[i];

                    Entities.WithSharedComponentFilter(new MinionPartParent { parent = parent })
                        .ForEach((Entity entity, int entityInQueryIndex, ref LocalTransform transform) =>
                        {
                            /*
                            ecb.SetComponent(entityInQueryIndex, entity, new LocalTransform
                            {
                                Position = origin.Position + new float3(0, entityInQueryIndex, 0),
                                Rotation = quaternion.identity,
                                Scale = 1
                            });*/
                            transform.Position = origin.Position + new float3(0, entityInQueryIndex, 0);
                        }).ScheduleParallel();// ���� �Ұ��� �ص� 500�� ���� MinionSystem�� 30ms ���� ����
                }
            }

            //updateHandle.Complete();
            origins.Dispose();
        }//=== ������ �������̰� ���ɵ� ������ , ���� �̻��� Job �Ϸ� ��� �ð�
            // ó���� BeginIntiECB > BeginFixECB > ������� ������ > ������ ���� > ���� , ���ݾ� ���� ���������� ������ �������

        MinionEntities.Dispose();
        //Aspects.Dispose();
        MinionDatas.Dispose();
    }

    public partial struct UpdateMinionAnimation : IJobParallelFor
    {
        public EntityCommandBuffer.ParallelWriter ecb;

        [ReadOnly] public NativeArray<MinionPart> parts;
        [ReadOnly] public NativeArray<Entity> partsParent;
        [ReadOnly] public NativeHashMap<Entity, MinionAnimation>.ReadOnly animations;
        [ReadOnly] public NativeHashMap<Entity, LocalTransform>.ReadOnly transform;

        public void Execute(int index)
        {
            //var e = entities[index];
            var part = parts[index];
            var parent = partsParent[index];

            if (Equals(part, Entity.Null))
                return;

            if (animations.TryGetValue(parent, out var anim))
            {
                transform.TryGetValue(parent, out var worldTrans);
                var localTrans = LocalTransform.Identity;
                localTrans.Position = new float3(0, part.BodyIndex, 0);
                localTrans.Scale = 1;
                    //MinionAnimationDB.Instance.GetPartTransform(anim.CurrectAnimation, part.BodyIndex, anim.PlayTime);
                        // -- �̱��濡 �����ϴ°� ���ɿ� ���� ��ħ + NativeList �� 
                
                ecb.SetComponent(index, part.Part, new LocalTransform 
                { 
                    Position = worldTrans.Position + localTrans.Position,
                    Rotation = math.mul(worldTrans.Rotation, localTrans.Rotation),
                    Scale = worldTrans.Scale * localTrans.Scale,
                });

            }

        }
    }
}
