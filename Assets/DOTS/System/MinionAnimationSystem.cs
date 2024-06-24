using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;

[UpdateAfter(typeof(MinionSetUpSystem))]
public partial class MinionAnimationSystem : SystemBase
{
    protected override void OnStartRunning()
    {
        base.OnStartRunning();
    }
    protected override void OnUpdate()
    {
        var AnimQuery = GetEntityQuery(typeof(MinionAnimation), typeof(MinionData));
        var Animations = AnimQuery.ToComponentDataArray<MinionAnimation>(Allocator.TempJob);

        new UpdateAnimation()
        {
            animations = Animations,
            delta = SystemAPI.Time.DeltaTime,

            TempAnimIndex = 0,
            TempAnimLength = 3.8f,
        }.ScheduleParallel(AnimQuery, Dependency).Complete();

        Animations.Dispose();
    }

    [BurstCompile]
    public partial struct UpdateAnimation : IJobEntity
    {
        [ReadOnly] public NativeArray<MinionAnimation> animations;
        public float delta;

        public float TempAnimLength;
        public int TempAnimIndex;

        public void Execute(Entity entity, [EntityIndexInQuery] int index, ref MinionAnimation animation, in MinionData minionData)
        {
            if (minionData.isEnablePart)
            {
                if (animations[index].PlayTime + delta < TempAnimLength)
                {
                    animation.PlayTime = animations[index].PlayTime + delta;
                }
                else
                {
                    animation.PlayTime = 0;                    
                }
            }
        }
    }
}
