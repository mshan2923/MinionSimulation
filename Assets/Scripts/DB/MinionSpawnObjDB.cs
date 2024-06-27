using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEditor;
using UnityEngine;
using static MinionSpawnObjDB;

[CreateAssetMenu(fileName = "MinionSpawnObj", menuName = "DB/MinionSpawnObj")]
public class MinionSpawnObjDB : ScriptableObject
{
    public Avatar OriginAvatar;


    [Serializable]
    public struct SpawnSlot
    {
        public GameObject Object;
        public TransformData offset;
    }
    public Map<string, SpawnSlot> SpawnParts = new();

    public void SetUp()
    {
        if (OriginAvatar != null && SpawnParts.Count == 0)
        {
            //Objects = new GameObject[OriginAvatar.humanDescription.skeleton.Length];

            foreach (var s in OriginAvatar.humanDescription.skeleton)
            {
                SpawnParts.Add(s.name, new SpawnSlot
                {
                    Object = null,
                    offset = new TransformData
                    {
                        position = float3.zero  ,
                        rotation = Quaternion.identity,
                        scale = Vector3.one
                    }
                });
            }
        }
    }
}


#if UNITY_EDITOR
[CustomEditor(typeof(MinionSpawnObjDB))]
public class MinionSpawnObjDBEditor : Editor
{
    MinionSpawnObjDB onwer;
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        if (onwer == null)
            onwer = target as MinionSpawnObjDB;

        if (GUILayout.Button("Set Up"))
        {
            onwer.SetUp();
        }
    }
}

[CustomPropertyDrawer(typeof(SpawnSlot))]
public class SpawnSlotEditor : PropertyDrawer
{
    Rect DrawRect;
    float offset = 10f;

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float height = 0;
        if (property.isExpanded)
        {
            height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("Object"));
            height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("offset"));
        }
        return EditorGUIUtility.singleLineHeight + (property.isExpanded ? height : 0);
    }
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        //base.OnGUI(position, property, label);

        DrawRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);

        var obj = property.FindPropertyRelative("Object").objectReferenceValue;
        property.isExpanded = EditorGUI.Foldout(DrawRect, property.isExpanded, (obj != null ? obj.name : "  "), true);

        if (property.isExpanded)
        {
            DrawRect = new Rect(position.x + offset, (DrawRect.y + DrawRect.height), position.width - offset, EditorGUIUtility.singleLineHeight);
            EditorGUI.PropertyField(DrawRect, property.FindPropertyRelative("Object"));

            DrawRect = new Rect(position.x + offset, (DrawRect.y + DrawRect.height), position.width - offset, EditorGUIUtility.singleLineHeight);
            EditorGUI.PropertyField(DrawRect, property.FindPropertyRelative("offset"));
        }
    }
}
#endif