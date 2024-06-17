using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[Serializable]
public struct TransformData
{
    public float3 position;
    public quaternion rotation;
    public float3 scale;

    public TransformData(LocalTransform transform)
    {
        position = transform.Position;
        rotation = transform.Rotation;
        scale = transform.Scale;
    }

    public LocalTransform transform
    {
        get => new() { Position = position, Rotation = rotation, Scale = scale.y };
    }
}
