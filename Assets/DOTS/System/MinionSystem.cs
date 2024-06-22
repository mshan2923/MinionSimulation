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
        // 모든 Minion 마다 Part들을 위치 업데이트...
            //한번에 적용하기 위해 , 데이터 준비한후 , 적용
            // 먼저 Minion 목록 에서 
                // 모든 부위
                // BoneIndex
                // 캐릭터 엔티티
            // HashMap<캐릭터 엔티티, MinionAnimation>

        {
            var MinionEntities = Minions.ToEntityArray(Allocator.TempJob);
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
                    }//=============== 임시
                }
                //===============Aspect으로 참조으로 다음 에니메이션 + PlayTime 수정
            }//======= 이것도 Job으로 해야됨

            MinionAspect.GetMinionsPart(Aspects, false, ref AllParts, ref AllPartsParent);//====== 이게 문제인듯
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
                
            }//=============== 크기조절 에 따른 위치 변경이 없음 , Job 말고도 주로 느려지게 하는이유가 따로 있음

            MinionEntities.Dispose();
            Aspects.Dispose();
            AllParts.Dispose();
            AllPartsParent.Dispose();
            AnimationData.Dispose();
            MinionTransform.Dispose();
        }
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

            //if (animations.TryGetValue(parent, out var anim))
            {
                transform.TryGetValue(parent, out var worldTrans);
                var localTrans = LocalTransform.Identity;
                localTrans.Position = new float3(0, part.BodyIndex, 0);
                localTrans.Scale = 1;
                    //MinionAnimationDB.Instance.GetPartTransform(anim.CurrectAnimation, part.BodyIndex, anim.PlayTime);
                        // -- 싱글톤에 접근하는게 성능에 영향 끼침 + NativeList 도 
                
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
