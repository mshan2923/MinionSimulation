using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEditor;
using UnityEngine;

[Serializable]
public struct TransformData
{
    public float3 position;
    public Quaternion rotation;
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

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(TransformData))]
public class TransformDataEditor : PropertyDrawer
{
    Rect DrawRect;
    float offset = 10f;

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUIUtility.singleLineHeight * (property.isExpanded ? 4 : 1);
    }
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        //base.OnGUI(position, property, label);

        DrawRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);

        property.isExpanded = EditorGUI.Foldout(DrawRect, property.isExpanded, property.name, true);

        if (property.isExpanded)
        {
            DrawRect = new Rect(position.x + offset, (DrawRect.y + DrawRect.height), position.width - offset, EditorGUIUtility.singleLineHeight);
            EditorGUIUtility.labelWidth = 50;
            EditorGUI.PropertyField(DrawRect, property.FindPropertyRelative("position"));

            DrawRect = new Rect(position.x + offset, (DrawRect.y + DrawRect.height), position.width - offset, EditorGUIUtility.singleLineHeight);
            EditorGUI.PropertyField(DrawRect, property.FindPropertyRelative("rotation"));

            DrawRect = new Rect(position.x + offset, (DrawRect.y + DrawRect.height), position.width - offset, EditorGUIUtility.singleLineHeight);
            EditorGUI.PropertyField(DrawRect, property.FindPropertyRelative("scale"));
        }
    }

}
#endif