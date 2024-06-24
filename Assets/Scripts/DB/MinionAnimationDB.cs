using System.Collections;
using System.Collections.Generic;
using Unity.Transforms;
using UnityEditor;
using UnityEngine;
using System.Linq;
using System;
using UnityEditor.Experimental.GraphView;
using Unity.Mathematics;
using Unity.Entities.UniversalDelegates;

[CreateAssetMenu(fileName = "MinionAnimation", menuName = "DB/MinionAnimation")]
public class MinionAnimationDB : ScriptableObject
{
    public const string path = "Assets/Scripts/DB/MinionAnimation.asset";
    public const float AnimationFPS = 30f;
    public const float ClipDataInterval = 0.03333f;
    

    private static MinionAnimationDB instance;
    public static MinionAnimationDB Instance
    {
        get
        {
            if (instance == null)
            {
#if UNITY_EDITOR
                instance = UnityEditor.AssetDatabase.LoadAssetAtPath<MinionAnimationDB>(path);//Data
#endif
            }
            
            return instance;            
        }
    }


    [Serializable]
    public struct ClipData
    {
        public AnimationClip Clip;
        public Avatar OriginAvatar;

        public SkeletonBone[] GetSkeletons
        {
            get => OriginAvatar.humanDescription.skeleton;
        }
    }
    [Serializable]
    public struct ClipCurve
    {
        public TransformData[] Curve;

        public LocalTransform GetTransform(float time)
        {
            int timeIndex = Mathf.FloorToInt(time / ClipDataInterval);

            return Curve[timeIndex].transform;
        }
    }

    public ClipData[] animationClips;

    [SerializeField] private ClipCurve[] PartCurves;

    [SerializeField] private int[] ClipDataIndex;

    [SerializeField] private MinionSpawnObjDB[] SpawnObjDBs;

    public GameObject DefaultObject;

    //============ ( ClipIndex, BoneIndex, time ) 으로 LocalTransform 리턴 하는 함수
    //============  Clip 의 Bone 갯수 리턴
    //============ (ClipIndex, BoneIndex )으로 PartObject 리턴 

    private void OnEnable()
    {
        if (instance == null)
            instance = this;
    }

    public int ClipAmount
    {
        get => ClipDataIndex.Length;
    }

    public int BoneAmount(int ClipIndex)
    {
        if (ClipIndex < 0 || ClipIndex >= ClipDataIndex.Length)
            return -1;
        /*
        if (PartCurves == null)
            return -1;

        int startIndex = ClipDataIndex[ClipIndex];


        if (ClipIndex + 1 <= ClipDataIndex.Length)
        {
            return PartCurves.Length - startIndex;
        }else
        {
            return ClipDataIndex[ClipIndex + 1] - startIndex;
        }
        */

        return animationClips[ClipIndex].GetSkeletons.Length;
    }

    public int GetDatasIndex(int ClipIndex, int BoneIndex)
    {
        if (ClipIndex < 0 || ClipIndex >= ClipDataIndex.Length)
        {
            return -1;
        }

        int startIndex = ClipDataIndex[ClipIndex];
        if (BoneIndex < 0 || BoneIndex >= BoneAmount(ClipIndex))
        {
            return -1;
        }

        return BoneIndex + startIndex;
    }

    public LocalTransform GetPartTransform(int ClipIndex, int BoneIndex, float time)
    {
        return PartCurves[GetDatasIndex(ClipIndex, BoneIndex)].GetTransform(time);
    }
    public LocalTransform GetPartTransform(int ClipIndex, int BoneIndex, int index)
    {
        return PartCurves[GetDatasIndex(ClipIndex, BoneIndex)].Curve[index].transform;
    }

    public float GetClipLength(int ClipIndex)
    {
        return PartCurves[ClipDataIndex[ClipIndex]].Curve.Length * ClipDataInterval;
    }
    public int GetClipSize(int ClipIndex)
    {
        return PartCurves[ClipDataIndex[ClipIndex]].Curve.Length;
    }
    public void ResetData()
    {
        PartCurves = new ClipCurve[0];

        ClipDataIndex = new int[animationClips.Length];
        Array.Fill(ClipDataIndex, -1);
        Array.Resize(ref SpawnObjDBs, animationClips.Length);
    }

    public void AddClipDatas(int ClipIndex, ClipCurve[] newData)
    {
        ClipDataIndex[ClipIndex] = PartCurves.Length;

        var temp = PartCurves.ToList();
        temp.AddRange(newData);

        Array.Resize(ref PartCurves, temp.Count);
        PartCurves = temp.ToArray();
    }

    public GameObject GetSpawnObj(int ClipIndex, int BoneIndex, bool AllowNull)
    {
        GameObject obj = DefaultObject;

        try
        {
            obj = SpawnObjDBs[ClipIndex].Objects.GetVaule(BoneIndex);

            if (obj != null)
                return obj;
            if (obj == null && !AllowNull)
                return DefaultObject;
            else
                return null;
        }
        catch
        {
            return null;
        }
    }
}
