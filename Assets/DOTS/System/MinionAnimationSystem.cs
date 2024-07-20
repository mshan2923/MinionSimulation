using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using UnityEditor.Experimental.GraphView;
using System.Linq;
using static MinionAnimationDB;
using Unity.Mathematics;
using Unity.Transforms;

[UpdateAfter(typeof(MinionSetUpSystem))]
public partial class MinionAnimationSystem : SystemBase
{
    protected override void OnStartRunning()
    {
        base.OnStartRunning();
    }
    protected override void OnDestroy()
    {
        base.OnDestroy();
        /*
        Entities.WithAll<MinionClipData>().ForEach((in MinionClipData clipData) =>
        {
            clipData.assetReference.Dispose();
        }).ScheduleParallel(Dependency).Complete();
        *///���� ���ص� ����� �ǳ�?
    }
    protected override void OnUpdate()
    {
        {
            /*
            var ClipEntity = SystemAPI.GetSingleton<MinionClipEntities>();

            Debug.Log($"ClipEntity Length : {ClipEntity.clipsRef.Value.entity.Length}");
            Debug.Log($"ClipEntity index : {ClipEntity.clipsRef.Value.entity[0].Index}");//���� �Ұ�


            var clipDataQuery = GetEntityQuery(typeof(MinionClipData));
            var clipData = clipDataQuery.ToComponentDataArray<MinionClipData>(Allocator.Temp);
            Debug.Log($"MinionClipData Query : {clipDataQuery.CalculateEntityCount()}");

            Debug.Log($"Clip Data : {clipData[0].clipIndex}");

            ref var clipParts = ref clipData[0].assetReference.Value.parts;//<---------- �̷��� Job���� �ټ� �ֳ�?

            Debug.Log($"Clip Data - Part Length : {clipData[0].assetReference.Value.parts.Length}");
            Debug.Log($"Clip Data - BodyIndex : {clipParts[0].BodyIndex} , Frame Length : {clipParts[0].frames.Length}");

            for (int i = 0; i < clipParts.Length; i++)
            {
                if (clipParts[i].frames.Length > 0)
                {
                    Debug.Log($"Body index : {clipParts[i].BodyIndex} -> {clipParts[i].frames[0]}");
                    break;
                }
            }

            //===== SharedComponent���� ���ϰ� ã�� �ؾ� �ϳ�?  ,  ���� �����ΰ� Job ���ο��� ���� ���ϸ��̼� ����...�����? ������?

            clipData.Dispose();
            */
        }//Debug for Test

        var AnimQuery = GetEntityQuery(typeof(MinionAnimation), typeof(MinionData), typeof(MinionNaviData));
        var animControllData = SystemAPI.GetSingleton<MinionAnimatorControllData>();

        var minionNavi = AnimQuery.ToComponentDataArray<MinionNaviData>(Allocator.TempJob);

        if (AnimQuery.CalculateEntityCount() <= 0)
        {
            Debug.LogWarning("Can't find MinionData");
            Enabled = false;
            return;
        }

        var Animations = AnimQuery.ToComponentDataArray<MinionAnimation>(Allocator.TempJob);
        var Minions = AnimQuery.ToComponentDataArray<MinionData>(Allocator.TempJob);

        var clipDataQuery = GetEntityQuery(typeof(MinionClipData));
        var clipData = clipDataQuery.ToComponentDataArray<MinionClipData>(Allocator.TempJob);

        var updateHandle = new UpdateAnimation()
        {
            animations = Animations,
            clipDatas = clipData,
            NaviDatas = minionNavi,
            controllData = animControllData,
            delta = SystemAPI.Time.DeltaTime,
        }.ScheduleParallel(AnimQuery, Dependency);//������ ������ , ��ġ ������ MinionSystem����

        var seperateData = SystemAPI.GetSingleton<SeparationPartComponent>();
        new SeperateParts()
        {
            datas = Minions,
            Delta = SystemAPI.Time.DeltaTime,
            separateData = seperateData,
        }.ScheduleParallel(AnimQuery, updateHandle).Complete();

        Animations.Dispose();
        Minions.Dispose();
        clipData.Dispose();
        minionNavi.Dispose();
    }

    [BurstCompile]
    public partial struct UpdateAnimation : IJobEntity
    {
        [ReadOnly] public NativeArray<MinionAnimation> animations;
        [ReadOnly] public NativeArray<MinionClipData> clipDatas;
        [ReadOnly] public MinionAnimatorControllData controllData;

        [ReadOnly] public NativeArray<MinionNaviData> NaviDatas;
        public float delta;

        public void Execute(Entity entity, [EntityIndexInQuery] int index, ref MinionAnimation animation, in MinionData minionData)
        {
            MinionClipData clipData = default;
            {
                if (animations[index].CurrectAnimation < 0)
                    return;

                if (clipDatas[animations[index].CurrectAnimation].clipIndex == animations[index].CurrectAnimation)
                {
                    clipData = clipDatas[animations[index].CurrectAnimation];
                }else
                {
                    foreach (var v in clipDatas)
                    {
                        if (animations[index].CurrectAnimation == v.clipIndex)
                        {
                            clipData = v;
                            break;
                        }
                    }
                }
            }// is Correct ClipIndex

            if (minionData.isEnablePart)
            {
                if (minionData.DisableCounter >= 0)
                    return;

                if (animations[index].ReserveAnimatiom >= 0 &&
                    (clipDatas[animations[index].CurrectAnimation].Cancellable || animations[index].ForceCancle))
                {
                    animation.PreviousAnimation = animations[index].CurrectAnimation;
                    animation.StopedTime = animations[index].PlayTime;
                    animation.CurrectAnimation = animations[index].ReserveAnimatiom;
                    animation.PlayTime = 0;
                    animation.ReserveAnimatiom = -1;
                    animation.ForceCancle = false;

                    return;
                }//���ϸ��̼� ĵ��

                float Ltime = animations[index].PlayTime + delta;
                if (Ltime < clipData.ClipLength)
                {
                    animation.PlayTime = animations[index].PlayTime + delta;
                }
                else
                {
                    if (!clipData.IsLooping)
                    {
                        animation.CurrectAnimation = -1;
                    }
                    if (animations[index].ReserveAnimatiom >= 0)
                    {
                        animation.CurrectAnimation = animations[index].ReserveAnimatiom;
                    }

                    animation.PreviousAnimation = animations[index].CurrectAnimation;
                    animation.ReserveAnimatiom = -1;
                    animation.StopedTime = animations[index].PlayTime + delta;
                    animation.PlayTime = 0;
                    animation.ForceCancle = false;
                }

                {
                    if (animations[index].CurrectAnimation == controllData.IdleAnimationIndex ||
                        animations[index].CurrectAnimation == controllData.WalkAnimationIndex)
                    {
                        //bool isStoped = (math.distance(transform.Position, NaviDatas[index].PreviousPosition) < controllData.MoveSpeed * delta);

                        // �������� �ϸ� ���� �ݺ��Ǹ鼭 ������ ���ϸ��̼� ��ȯ
                        // PlayTime�� �����ð� �̳����� ������ ���ϸ��̼� ����� ����

                        float interpolationTime = clipDatas[animations[index].CurrectAnimation].interpolationTime;

                        if ((NaviDatas[index].isStoped ) 
                            && animations[index].CurrectAnimation == controllData.WalkAnimationIndex)
                        {
                            //animation.ReserveAnimatiom = controllData.IdleAnimationIndex;//Legacy

                            animation.PreviousAnimation = controllData.WalkAnimationIndex;
                            animation.ReserveAnimatiom = -1;
                            animation.StopedTime = animations[index].PlayTime + delta;
                            animation.CurrectAnimation = controllData.IdleAnimationIndex;
                            animation.PlayTime = math.max(interpolationTime - animations[index].PlayTime, 0);
                            animation.ForceCancle = false;
                        }
                        if ((NaviDatas[index].isStoped == false) 
                            && animations[index].CurrectAnimation == controllData.IdleAnimationIndex)
                        {
                            //animation.ReserveAnimatiom = controllData.WalkAnimationIndex;//Legacy

                            animation.PreviousAnimation = controllData.IdleAnimationIndex;
                            animation.ReserveAnimatiom = -1;
                            animation.StopedTime = animations[index].PlayTime + delta;
                            animation.CurrectAnimation = controllData.WalkAnimationIndex;
                            animation.PlayTime = math.max(interpolationTime - animations[index].PlayTime, 0);
                            animation.ForceCancle = false;
                        }
                        /*
                        if (NaviDatas[index].isStoped && animations[index].CurrectAnimation == controllData.WalkAnimationIndex)
                        {
                            animation.PreviousAnimation = animations[index].CurrectAnimation;
                            animation.ReserveAnimatiom = -1;
                            animation.StopedTime = animations[index].PlayTime + delta;
                            animation.CurrectAnimation = controllData.IdleAnimationIndex;
                            animation.PlayTime = 0;
                            animation.ForceCancle = false;
                        }
                        if (NaviDatas[index].isStoped == false && animations[index].CurrectAnimation == controllData.IdleAnimationIndex)
                        {
                            animation.PreviousAnimation = animations[index].CurrectAnimation;
                            animation.ReserveAnimatiom = -1;
                            animation.StopedTime = animations[index].PlayTime + delta;
                            animation.CurrectAnimation = controllData.WalkAnimationIndex;
                            animation.PlayTime = 0;
                            animation.ForceCancle = false;
                        }*/

                        //���ϸ��̼� ��ȯ�� Ư���ϰ� ����... ������ �ƴ϶�
                    }//================
                }
            }
        }
    }
    public partial struct SeperateParts : IJobEntity
    {
        public NativeArray<MinionData> datas;
        public float Delta;
        public SeparationPartComponent separateData;

        public void Execute([EntityIndexInQuery] int index, ref MinionData minion)
        {
            if (datas[index].DisableCounter < 0)
                return;

            if (separateData.SeparateTime + separateData.FalloffTime > datas[index].DisableCounter + Delta)
            {
                minion.DisableCounter = datas[index].DisableCounter + Delta;
            }else
            {
                //minion.DisableCounter = separateData.SeparateTime + separateData.FalloffTime;
                minion.isEnablePart = false;
            }//Disable Minion -> �����鸸 ��Ȱ��ȭ �ϴ°Ŷ�
             //MinionSystem���� ���� tranform ������Ʈ�� MinionData.isEnablePart���� false�� ��Ȱ��ȭ

        }
    }
}
