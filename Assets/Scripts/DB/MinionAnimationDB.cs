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
using System.Reflection;

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
    public class ClipData
    {
        public AnimationClip Clip;
        public Avatar OriginAvatar;
        [Tooltip("에니메이션 종료시 자동 반복 재생")]
        public bool isLooping;
        [Tooltip("에니메이션 재생중 전환 가능 여부")]
        public bool Cancellable;

        [Tooltip("에니메이션 전환시 보간 시간")]
        public float interpolationTime = 0.2f;
        [Tooltip("에니메이션 강제 전환시 보간 시간")]
        public float forceInterpolationTime = 0.3f;

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

    [NonReorderable]
    public ClipData[] animationClips;

    [SerializeField] private ClipCurve[] PartCurves;

    [SerializeField] private int[] ClipDataIndex;

    [SerializeField] private MinionSpawnObjDB[] SpawnObjDBs;

    [Space(10)]
    public int IdleAnimationIndex;
    public int WalkAnimationIndex;

    [Space(20)]
    public GameObject DefaultObject;
 

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


    public Map<string, MinionSpawnObjDB.SpawnSlot>.MapSlot GetSpawnSlotData(int ClipIndex, int BodyIndex)
    {
        if (ClipIndex < SpawnObjDBs.Length)
        {
            if (SpawnObjDBs[ClipIndex] == null)
            {
                Debug.LogError("Need Setup SpawnObjDB");
            }
        }

        return SpawnObjDBs[ClipIndex].SpawnParts.Get()[BodyIndex];
    }
    public GameObject GetSpawnObj(int ClipIndex, int BoneIndex, bool AllowNull)
    {
        GameObject obj = DefaultObject;

        try
        {
            obj = SpawnObjDBs[ClipIndex].SpawnParts.GetVaule(BoneIndex).Object;

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
    public LocalTransform GetSpawnOffset(int ClipIndex, int BoneIndex)
    {
        return SpawnObjDBs[ClipIndex].SpawnParts.GetVaule(BoneIndex).offset.transform;
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(MinionAnimationDB))]
public class MinionAnimDBEditor : Editor
{
    public override void OnInspectorGUI()
    {
        string value = "Must Equal Clip And OriginAvatar \n" +
            "Not work  Avatar Definition : 'Copy form Other Avatar'";
        EditorGUILayout.HelpBox(value, MessageType.Warning, true);
        base.OnInspectorGUI();
    }
}

[CustomPropertyDrawer(typeof(MinionAnimationDB.ClipData))]
public class MinionAnimationClipEdtor : PropertyDrawer
{
    Rect DrawRect;
    float FadeIn = 10f;

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        var members = typeof(MinionAnimationDB.ClipData).GetMembers
                (BindingFlags.Instance | BindingFlags.Public);
        int fieldCount = members.Count(t => t.MemberType == MemberTypes.Field);

        return EditorGUIUtility.singleLineHeight * (property.isExpanded ? fieldCount + 1 : 1);
    }
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        DrawRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);

        var clip = property.FindPropertyRelative("Clip").objectReferenceValue;
        property.isExpanded = EditorGUI.Foldout(DrawRect, property.isExpanded, clip == null? "": clip.name, true);

        if (property.isExpanded)
        {
            var members = typeof(MinionAnimationDB.ClipData).GetMembers
                (BindingFlags.Instance | BindingFlags.Public);

            foreach ( var member in members )
            {
                if (member.MemberType == MemberTypes.Field)
                {
                    DrawRect = NextLine(position, DrawRect, FadeIn);
                    EditorGUI.PropertyField(DrawRect, property.FindPropertyRelative(member.Name));
                }
            }
        }
    }
    public Rect NextLine(Rect position, Rect drawRect, float FadeIn = 0)
    {
        return new Rect(position.x + FadeIn, drawRect.y + EditorGUIUtility.singleLineHeight, position.width - FadeIn, EditorGUIUtility.singleLineHeight);
    }
}
#endif
