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

[UpdateAfter(typeof(MinionSystem))]
public partial class MinionAnimationSystem : SystemBase
{
    protected override void OnStartRunning()
    {
        base.OnStartRunning();
    }
    protected override void OnDestroy()
    {
        base.OnDestroy();
    }
    protected override void OnUpdate()
    {

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
        }.ScheduleParallel(AnimQuery, Dependency);//데이터 수정만 , 위치 변경은 MinionSystem에서

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
                }//에니메이션 캔슬

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
                        // 예약으로 하면 값이 반복되면서 강제로 에니메이션 전환
                        // PlayTime을 보간시간 이내에서 이전의 에니메이션 영향력 조절

                        float interpolationTime = clipDatas[animations[index].CurrectAnimation].interpolationTime;
                        float playMultiply = controllData.MoveSpeed / MinionAnimatorControllData.DefaultSpeed;

                        if ((NaviDatas[index].isStoped ) 
                            && animations[index].CurrectAnimation == controllData.WalkAnimationIndex)
                        {
                            //animation.ReserveAnimatiom = controllData.IdleAnimationIndex;//Legacy

                            animation.PreviousAnimation = controllData.WalkAnimationIndex;
                            animation.ReserveAnimatiom = -1;
                            animation.StopedTime = animations[index].PlayTime;// + delta;
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
                            animation.StopedTime = animations[index].PlayTime;// + delta;
                            animation.CurrectAnimation = controllData.WalkAnimationIndex;
                            animation.PlayTime = math.max(interpolationTime - animations[index].PlayTime, 0);
                            animation.ForceCancle = false;
                        }

                        if (animations[index].PlayTime + delta * playMultiply < clipData.ClipLength)
                            animation.PlayTime = animations[index].PlayTime + delta * playMultiply;
                        //에니메이션 전환을 특수하게 관리... 예약이 아니라
                    }
                }// idle , walk 상태 일때 보간 - 떨림 방지
            }
        }
    }

    //[BurstCompile]
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
                minion.DisableCounter = -1;
            }//Disable Minion -> 부위들만 비활성화 하는거라서
             //MinionSystem에서 부위 tranform 업데이트시 MinionData.isEnablePart값이 false면 비활성화

        }
    }
}
