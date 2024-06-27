using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Transforms;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using static MinionAnimationDB;

public class MinionAnimationBaker : MonoBehaviour
{
    MinionAnimationDB db;
    public Animator BakeCharacter;

    [SerializeField] private Transform[] Childs;

    [Space(20)]
    public int DebugClipIndex;
    public GameObject DebugObj;
    public int DebugPartIndex;

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Bake(int ClipIndex)
    {
        db = MinionAnimationDB.Instance;
        var clipData = db.animationClips[ClipIndex];

        Childs = GetAllChilds(clipData.GetSkeletons, BakeCharacter);


        int ClipFrames = Mathf.FloorToInt(clipData.Clip.length / MinionAnimationDB.ClipDataInterval);

        var result = new ClipCurve[Childs.Length];

        for (int p = 0;  p < Childs.Length; p++)
        {
            result[p] = new ClipCurve() { Curve = new TransformData[ClipFrames] };

            for (int i = 0; i < ClipFrames; i++)
            {
                float playTime = i * MinionAnimationDB.ClipDataInterval;

                clipData.Clip.SampleAnimation(BakeCharacter.gameObject, playTime);

                var data = new TransformData();
                data.position = Childs[p].transform.position;
                data.rotation = Childs[p].transform.rotation;
                data.scale = Childs[p].transform.localScale.y;

                result[p].Curve[i] = data;
            }
        }

        db.AddClipDatas(ClipIndex, result);
    }


    public Transform[] GetAllChilds(SkeletonBone[] skeletons, Animator baker)
    {
        var stack = new Stack<Transform>();
        var result = new Transform[skeletons.Length];

        stack.Push(baker.transform);
        result[0] = baker.transform;

        while (stack.Count > 0)
        {
            var temp = stack.Pop();

            int Lindex = Array.FindIndex(skeletons, t => Equals(t.name, temp.name));


            if (Lindex >= 0)
            {
                result[Lindex] = temp;
            }

            for (int i = 0; i < temp.childCount; i++)
            {
                stack.Push(temp.GetChild(i));
            }
        }

        return result;
    }

    /// <summary>
    /// For Debug
    /// </summary>
    public void OnSnappedObject(SkeletonBone[] skeletons, GameObject obj, int partIndex)
    {

        GetTransformFormBaker(skeletons, partIndex, out var result);

        if (result == null)
        {
            Debug.LogWarning($"Can't Find Bone Transfom");
            return;
        }

        if (obj != null)
            obj.transform.position = result.position;
    }

    public bool GetTransformFormBaker(SkeletonBone[] skeletons, int partIndex, out Transform result)
    {
        if (partIndex < skeletons.Length && Childs[partIndex] != null)
        {
            Transform trans = BakeCharacter.transform;

            if (partIndex > 0)
            {
                trans = Childs.First(t => Equals(t.name, skeletons[partIndex].name));
            }

            result = trans;
            return true;
        }else
        {
            result = null;
            return false;
        }
    }
}


#if UNITY_EDITOR
[CustomEditor(typeof(MinionAnimationBaker))]
public class MinionAnimationBakerEditor : Editor
{
    MinionAnimationDB db;
    MinionAnimationBaker onwer;

    bool ClipSlider = false;
    float playTime;

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        if (onwer == null)
            onwer = target as MinionAnimationBaker;
        if (db == null)
            db = MinionAnimationDB.Instance;


        if ( GUILayout.Button("Bake"))
        {
            db.ResetData();

            for (int i = 0; i < db.animationClips.Length; i++)
            {
                onwer.Bake(i);
            }
        }


        if (GUILayout.Button("Spnapped Obj"))
        {
            var skel = db.animationClips[onwer.DebugClipIndex].GetSkeletons;
            onwer.OnSnappedObject(skel, onwer.DebugObj, onwer.DebugPartIndex);
        }

        ClipSlider = EditorGUILayout.Toggle("Clip Play Slider", ClipSlider);
        if (ClipSlider)
        {
            if (onwer.DebugClipIndex >= 0 && onwer.DebugClipIndex < db.animationClips.Length)
            {
                playTime = EditorGUILayout.Slider(playTime, 0, db.animationClips[onwer.DebugClipIndex].Clip.length);
                db.animationClips[onwer.DebugClipIndex].Clip.SampleAnimation(onwer.BakeCharacter.gameObject, playTime);

                var skel = db.animationClips[onwer.DebugClipIndex].GetSkeletons;
                onwer.OnSnappedObject(skel, onwer.DebugObj, onwer.DebugPartIndex);
            }
        }

    }
}
#endif