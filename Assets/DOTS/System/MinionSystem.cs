using Custom;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using static MinionAnimationDB;
using static UnityEngine.GraphicsBuffer;

[UpdateAfter(typeof(MinionSetUpSystem))]
public partial class MinionSystem : SystemBase
{
    EntityQuery Minions;

    protected override void OnStartRunning()
    {
        base.OnStartRunning();
        Minions = GetEntityQuery(typeof(MinionData));

        if (Minions.CalculateEntityCount() <= 0)
        {
            Debug.LogWarning("Can't find MinionData");
            Enabled = false;
            return;
        }
    }
    protected override void OnUpdate()
    {
        if (Enabled == false)//비활성화 되도 실행
            return;

        {
            var MinionEntities = Minions.ToEntityArray(Allocator.TempJob);
            var MinionDatas = Minions.ToComponentDataArray<MinionData>(Allocator.TempJob);

            var MinionsParall = new NativeParallelHashMap<Entity, MinionData>(MinionEntities.Length, Allocator.TempJob);
            var AnimationDataParall = new NativeParallelHashMap<Entity, MinionAnimation>(MinionEntities.Length, Allocator.TempJob);
            var MinionTransformParall = new NativeParallelHashMap<Entity, LocalTransform>(MinionEntities.Length, Allocator.TempJob);

            var clipData = GetEntityQuery(typeof(MinionClipData)).ToComponentDataArray<MinionClipData>(Allocator.TempJob);

            var PartsQuery = GetEntityQuery(typeof(MinionPartIndex), typeof(MinionPartParent), typeof(LocalTransform));
            var PartsTransform = PartsQuery.ToComponentDataArray<LocalTransform>(Allocator.TempJob);

            var ecb = SystemAPI.GetSingleton<BeginFixedStepSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(EntityManager.WorldUnmanaged);

            AnimationDataParall.Capacity = MinionEntities.Length;
            MinionTransformParall.Capacity = MinionEntities.Length;

            var setupHandle = new SetUpHashMap()
            {
                MinionsParall = MinionsParall.AsParallelWriter(),
                AnimationDataParall = AnimationDataParall.AsParallelWriter(),
                MinionTransformParall = MinionTransformParall.AsParallelWriter()
            }.ScheduleParallel(Dependency);

            var animHandle = new UpdateMinionAnimation_Ref()
            {
                animations = AnimationDataParall.AsReadOnly(),
                originTransform = MinionTransformParall.AsReadOnly(),
                ClipDatas = clipData,
                ClipDataInterval = MinionAnimationDB.ClipDataInterval,

                CameraPos = Camera.main.transform.position,
                CameraRot = Camera.main.transform.rotation,
                HorizonFov = Camera.VerticalToHorizontalFieldOfView(Camera.main.fieldOfView, Camera.main.aspect),
            }.ScheduleParallel(PartsQuery, setupHandle);
            //=============== 크기조절 에 따른 위치 변경이 없음 

            var seperateData = SystemAPI.GetSingleton<SeparationPartComponent>();

            new UpdateSeperteTrasform()
            {
                ecb = ecb.AsParallelWriter(),
                minions = MinionsParall.AsReadOnly(),
                originTransform = MinionTransformParall.AsReadOnly(),

                seperate = seperateData,
                PartsTransform = PartsTransform,
                delta = SystemAPI.Time.DeltaTime,

                CameraPos = Camera.main.transform.position,
                CameraRot = Camera.main.transform.rotation,
                HorizonFov = Camera.VerticalToHorizontalFieldOfView(Camera.main.fieldOfView, Camera.main.aspect),
            }.ScheduleParallel(PartsQuery, animHandle).Complete();

            MinionsParall.Dispose();
            AnimationDataParall.Dispose();
            MinionTransformParall.Dispose();
            PartsTransform.Dispose();
            clipData.Dispose();

            MinionEntities.Dispose();
            MinionDatas.Dispose();
        }
    }

    [BurstCompile]
    public partial struct SetUpHashMap : IJobEntity
    {
        public NativeParallelHashMap<Entity, MinionData>.ParallelWriter MinionsParall;
        public NativeParallelHashMap<Entity, MinionAnimation>.ParallelWriter AnimationDataParall;
        public NativeParallelHashMap<Entity, LocalTransform>.ParallelWriter MinionTransformParall;
        public void Execute(Entity entity, in MinionAnimation animation, in MinionData minionData, in LocalTransform transform)
        {
            MinionsParall.TryAdd(entity, minionData);
            MinionTransformParall.TryAdd(entity, transform);

            if (minionData.IsActive)
            {
                AnimationDataParall.TryAdd(entity, animation);
            }
        }
    }

    [BurstCompile]
    public partial struct UpdateMinionAnimation_Ref : IJobEntity
    {
        [ReadOnly] public NativeParallelHashMap<Entity, MinionAnimation>.ReadOnly animations;
        [ReadOnly] public NativeParallelHashMap<Entity, LocalTransform>.ReadOnly originTransform;
        [ReadOnly] public NativeArray<MinionClipData> ClipDatas;

        [ReadOnly] public float ClipDataInterval;

        [ReadOnly] public float3 CameraPos;
        [ReadOnly] public quaternion CameraRot;
        [ReadOnly] public float HorizonFov;

        public void Execute(Entity e, [EntityIndexInQuery] int index, in MinionPartIndex partIndex, in MinionPartParent parent,
            ref LocalTransform transform)
        {
            if (animations.TryGetValue(parent.parent, out var anim))
            {
                var BoriginTrans = originTransform.TryGetValue(parent.parent, out var worldTrans);

                MinionClipData clipData = default;
                {
                    if (ClipDatas[anim.CurrectAnimation].clipIndex == anim.CurrectAnimation)
                    {
                        clipData = ClipDatas[anim.CurrectAnimation];
                    }
                    else
                    {
                        foreach(var v in ClipDatas)
                        {
                            if (anim.CurrectAnimation == v.clipIndex)
                            {
                                clipData = v;
                                break;
                            }
                        }
                    }
                }// is Correct ClipIndex


                LocalTransform localTrans = LocalTransform.Identity;
                {

                    localTrans = GetClipTranform(clipData, partIndex, anim.PlayTime);

                    if (anim.ReserveAnimatiom >= 0)
                    {
                        if (clipData.Cancellable == false)
                        {
                            var startLerp = clipData.ClipLength - clipData.interpolationTime;

                            if (anim.PlayTime > startLerp)
                            {
                                var lerpRate = Mathf.Clamp01((anim.PlayTime - startLerp) / clipData.interpolationTime);

                                var reserveTrans = GetClipTranform(ClipDatas[anim.ReserveAnimatiom], partIndex, anim.PlayTime - startLerp);

                                localTrans = new LocalTransform
                                {
                                    Position = Vector3.Lerp(localTrans.Position, reserveTrans.Position, lerpRate),
                                    Rotation = Quaternion.Lerp(localTrans.Rotation, reserveTrans.Rotation, lerpRate),
                                    Scale = Mathf.Lerp(localTrans.Scale, reserveTrans.Scale, lerpRate)
                                };
                            }
                        }//Not Cancellable Transforming

                        if (ClipDatas[anim.PreviousAnimation].ClipLength - anim.StopedTime > ClipDataInterval
                            && ClipDatas[anim.PreviousAnimation].Cancellable == false)
                        {
                            var previous = ClipDatas[anim.PreviousAnimation];

                            var previousTrans = GetClipTranform(previous, partIndex, anim.StopedTime);
                            var lerpRate = Mathf.Clamp01(anim.PlayTime / clipData.forceInterpolationTime);

                            localTrans = new LocalTransform
                            {
                                Position = Vector3.Lerp(previousTrans.Position, localTrans.Position, lerpRate),
                                Rotation = Quaternion.Lerp(previousTrans.Rotation, localTrans.Rotation, lerpRate),
                                Scale = Mathf.Lerp(previousTrans.Scale, localTrans.Scale, lerpRate)
                            };
                        }//if ForceCancle To Not Cancellable Transforming

                        if (anim.PlayTime < clipData.interpolationTime && ClipDatas[anim.PreviousAnimation].Cancellable)
                        {
                            var previous = ClipDatas[anim.PreviousAnimation];

                            var previousTrans = GetClipTranform(previous, partIndex, anim.StopedTime);

                            var lerpRate = Mathf.Clamp01(anim.PlayTime / clipData.interpolationTime);

                            localTrans = new LocalTransform
                            {
                                Position = Vector3.Lerp(previousTrans.Position, localTrans.Position, lerpRate),
                                Rotation = Quaternion.Lerp(previousTrans.Rotation, localTrans.Rotation, lerpRate),
                                Scale = Mathf.Lerp(previousTrans.Scale, localTrans.Scale, lerpRate)
                            };
                        }//Cancellable Transforming
                    }

                }//Calculate Transforming Animation  
                 //     취소 가능 => 바로 전환 되므로 이전 정보랑 Lerp
                 //     취소 불가 => 끝나기 전에 Lerp (현)


                localTrans.Position = math.mul(worldTrans.Rotation, localTrans.Position);

                if (IsVisiable(worldTrans.Position + localTrans.Position, 0) == false)
                    return;

                var rot = math.mul(worldTrans.Rotation, localTrans.Rotation);
                rot = math.mul(rot, clipData.assetReference.Value.parts[partIndex.Index].OffsetTransform.Rotation);               

                transform.Position = worldTrans.Position + localTrans.Position;
                transform.Rotation = rot;
                transform.Scale = worldTrans.Scale * localTrans.Scale;
            }
        }

        public bool IsVisiable(float3 Target , float offset)
        {
            if (math.isfinite(Target).x == false)
                return true;
            if (math.distancesq(Target, CameraPos) < 0.01f)
                return true;
            
            //Debug.Log($"Dot({CameraRot} , {Quaternion.LookRotation(math.normalize(Target - CameraPos)).eulerAngles})" +
            //    $" => {Quaternion.Angle(CameraRot, Quaternion.LookRotation(math.normalize(Target - CameraPos)))}");

            return Quaternion.Angle(CameraRot, Quaternion.LookRotation(math.normalize(Target - CameraPos))) 
                <= HorizonFov + offset;
        }

        LocalTransform CancellableTransforming(MinionAnimation anim , MinionPartIndex partIndex, float clampLength, LocalTransform localTrans)
        {
            var previous = ClipDatas[anim.PreviousAnimation];
            var previousTrans = previous.assetReference.Value.parts[partIndex.Index]
                        .frames[Mathf.FloorToInt((anim.PlayTime / previous.ClipLength) * ClipDataInterval)];
            var lerpRate = anim.PlayTime / clampLength;

            return new LocalTransform
            {
                Position = Vector3.Lerp(previousTrans.Position, localTrans.Position, lerpRate),
                Rotation = Quaternion.Lerp(previousTrans.Rotation, localTrans.Rotation, lerpRate),
                Scale = Mathf.Lerp(previousTrans.Scale, localTrans.Scale, lerpRate)
            };
        }

        LocalTransform GetClipTranform(MinionClipData clipData, MinionPartIndex partIndex, float time)
        {
            int clipLength = clipData.assetReference.Value.parts[partIndex.Index].frames.Length;

            return clipData.assetReference.Value.parts[partIndex.Index]
                .frames[Mathf.Clamp(Mathf.FloorToInt(time / ClipDataInterval), 0, clipLength - 1)];
        }

    }//화면 밖에 나가면 위치 변경 X

    [BurstCompile]
    public partial struct UpdateSeperteTrasform : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ecb;
        [ReadOnly] public NativeParallelHashMap<Entity, MinionData>.ReadOnly minions;
        [ReadOnly] public NativeParallelHashMap<Entity, LocalTransform>.ReadOnly originTransform;

        [ReadOnly] public SeparationPartComponent seperate;
        [ReadOnly] public NativeArray<LocalTransform> PartsTransform;
        public float delta;

        [ReadOnly] public float3 CameraPos;
        [ReadOnly] public quaternion CameraRot;
        [ReadOnly] public float HorizonFov;

        public void Execute(Entity entity, [EntityIndexInQuery] int index, in MinionPartIndex partIndex, in MinionPartParent parent,
            ref LocalTransform transform)
        {

            if (minions.TryGetValue(parent.parent, out var minion))
            {
                originTransform.TryGetValue(parent.parent, out var origin);

                if (minion.isEnablePart == false)
                {
                    //수명 다함
                    ecb.SetEnabled(index, entity, false);
                    return;
                }
                if (minion.DisableCounter >= 0)
                {
                    if (PartsTransform[index].Position.y - origin.Position.y <= -seperate.DisableHeightUnderOrigin)
                    {
                        ecb.SetEnabled(index, entity, false);
                        return;
                    }

                    
                    //bool isRender = Custom.Math.IsRenderAreaLite(CameraPos, CameraRot, HorizonFov * 0.5f, HorizonFov, PartsTransform[index].Position);
                    if (IsVisiable(PartsTransform[index].Position, 0) == false)
                    {
                        ecb.SetEnabled(index, entity, false);
                        return;
                    }

                    if (minion.DisableCounter < seperate.SeparateTime + seperate.FalloffTime)
                    {
                        var offset = math.normalize(PartsTransform[index].Position - origin.Position);
                        var impactOffset = math.normalize(PartsTransform[index].Position - minion.ImpactLocation);

                        transform.Position = PartsTransform[index].Position
                            + math.normalize(offset + impactOffset) * (seperate.Speed * delta)
                            + seperate.Gravity * delta;
                        // 가상의 바닥 적용할려면 속도값 필요

                        if (seperate.SeparateTime <= minion.DisableCounter)
                        {
                            transform.Scale = 1 - ((minion.DisableCounter - seperate.SeparateTime) / seperate.FalloffTime);
                        }//FallOff - 크기 감소
                    }
                    else if (minion.DisableCounter >= seperate.SeparateTime + seperate.FalloffTime)
                    {
                        ecb.SetEnabled(index, entity, false);
                    }//수명 다함
                }

            }
        }

        public bool IsVisiable(float3 Target, float offset)
        {
            if (math.isfinite(Target).x == false)
                return true;
            if (math.distancesq(Target, CameraPos) < 0.01f)
                return true;

            return Quaternion.Angle(CameraRot, Quaternion.LookRotation(math.normalize(Target - CameraPos)))
                <= HorizonFov + offset;
        }
    }
}
