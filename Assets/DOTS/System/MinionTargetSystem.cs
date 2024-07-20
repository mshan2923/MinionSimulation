using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

[UpdateAfter(typeof(MinionSystem))]
public partial class MinionTargetSystem : SystemBase
{
    protected override void OnUpdate()
    {
        var animController = SystemAPI.GetSingletonRW<MinionAnimatorControllData>();
        animController.ValueRW.Target = Camera.main.transform.position;
    }
}
