using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class MinionSpawnController : MonoBehaviour
{
    public float SpawnInterval = 0.5f;
    public int SpawnAmount = 10;
    public GameObject SpawnTarget;
    public float SpawnRadius = 10f;
}

public class MinionSpawnControllerBaker : Baker<MinionSpawnController>
{
    public override void Bake(MinionSpawnController authoring)
    {
        AddComponent(GetEntity(authoring.gameObject, TransformUsageFlags.None),
            new MinionSpawnControllData()
            {
                SpawnInterval = authoring.SpawnInterval,
                SpawnAmount = authoring.SpawnAmount,
                SpawnTarget = authoring.SpawnTarget != null? authoring.SpawnTarget.transform.position : float3.zero,
                SpawnRadius = authoring.SpawnRadius,
            });
    }
}