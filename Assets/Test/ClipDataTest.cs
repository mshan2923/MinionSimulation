using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using System.Linq;
using Unity.Entities.UniversalDelegates;
using System;
using System.Reflection;

//[ExecuteAlways]
public class ClipDataTest : MonoBehaviour
{
    public Animator animator;
    public GameObject FirstBone;

    //public Avatar avatar;
    public AnimationClip clip;

    public int partIndex = 2;

    public float playTime = 0;

    public GameObject TestObj;

    public float PlayTime = 0;

    public Transform[] Childs;
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        //avatar.humanDescription.skeleton[0]
        foreach (var v in animator.avatar.humanDescription.skeleton)
        {
            //Debug.Log($"{v.name}");
        }

        //AnimationUtility.GetAllCurves(clip, true);
        var part = AnimationUtility.GetCurveBindings(clip);
        

        AnimationUtility.GetCurveBindings(clip);
        AnimationUtility.GetEditorCurve(clip, part[partIndex]);
        //var partCurve = AnimationUtility.GetObjectReferenceCurve(clip, part[partIndex])[0].value;


        Debug.Log($"{part[partIndex].path} , {part[partIndex].propertyName} , {animator.avatar.humanDescription.skeleton[partIndex].name} : {AnimationUtility.GetEditorCurve(clip, part[partIndex])[0].value}");

        //ECS으로 저장 하지 말고 어쩌피 변경되지 않으니 스크립트에이블오브젝트 으로 
        //부위마다 일정간격으로 위치, 회전값을 저장


        //part[partIndex].path 값을 avatar.humanDescription.skeleton[partIndex].name 에서 찾아 순번 지정

        //var temp = AnimationUtility.GetAnimatableBindings();
        //Debug.Log ($"{temp.Length} , ");


        //clip.SampleAnimation(animator.gameObject, playTime);//Working


    }

    [CustomEditor(typeof(ClipDataTest))]
    public class ClipDataTestEditor : Editor
    {
        ClipDataTest onwer;
        bool AutoUpdata = false;
        Transform snapped;
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (onwer == null)
                onwer = target as ClipDataTest;

            //clip.SampleAnimation(animator.gameObject, playTime);

            EditorGUILayout.BeginHorizontal();
            AutoUpdata = EditorGUILayout.Toggle("Auto Update", AutoUpdata);
            if (AutoUpdata)
            {
                onwer.clip.SampleAnimation(onwer.animator.gameObject, serializedObject.FindProperty("PlayTime").floatValue);

                snapped = OnSnappedTestObj();
            }
            EditorGUILayout.LabelField($"Last Spap : {(snapped != null ? snapped.name : "Null")}");
            //serializedObject.ApplyModifiedProperties();
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Bake All"))
            {
                //MinionAnimationScriptableObj.Instance.BakeAll();

                var skeleton = onwer.animator.avatar.humanDescription.skeleton;

                {
                    var sb = new StringBuilder();
                    var stack = new Stack<Transform>();
                    onwer.Childs = new Transform[skeleton.Length];

                    stack.Push(onwer.animator.transform);
                    onwer.Childs[0] = onwer.animator.transform;

                    while (stack.Count > 0)
                    {
                        var temp = stack.Pop();
                        //onwer.Childs.Add(temp);

                        int Lindex = Array.FindIndex(skeleton, t => Equals(t.name, temp.name));

                        if (Lindex >= 0)
                        {
                            onwer.Childs[Lindex] = temp;
                            sb.Append(temp.name);
                            sb.Append(" ,");
                        }

                        for (int i = 0; i < temp.childCount; i++)
                        {
                            stack.Push(temp.GetChild(i));
                        }
                    }

                    
                    Debug.Log($"Childs List : {sb}");
                }

                bool isBone = false;
                for (int i = 0; i < skeleton.Length; i++)
                {
                    if (Equals(skeleton[i].name, onwer.FirstBone.name))
                        isBone = true;
                    if (isBone)
                    {

                    }
                }
                if (isBone == false)
                {
                    Debug.LogWarning("Need equal Name to Bone");
                    return;
                }

                snapped = OnSnappedTestObj();
            }
        }

        public Transform OnSnappedTestObj()
        {
            var skeleton = onwer.animator.avatar.humanDescription.skeleton;

            if (onwer.partIndex >= skeleton.Length)
                return null;
            if (onwer.Childs[onwer.partIndex] == null)
                return null;

            Transform trans = onwer.animator.transform;

            if (onwer.partIndex > 0)
            {
                trans = onwer.Childs.First(t => Equals(t.name, skeleton[onwer.partIndex].name));
                //if 0 : Child -> name ,  skeleton -> name(Clone)
            }

            if (onwer.TestObj != null)
                onwer.TestObj.transform.position = trans.position;

            return trans;
        }
    }
}
