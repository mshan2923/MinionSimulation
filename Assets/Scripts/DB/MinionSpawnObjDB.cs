using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu(fileName = "MinionSpawnObj", menuName = "DB/MinionSpawnObj")]
public class MinionSpawnObjDB : ScriptableObject
{
    public Avatar OriginAvatar;

    public Map<string, GameObject> Objects = new();


    public void SetUp()
    {
        if (OriginAvatar != null && Objects.Count == 0)
        {
            //Objects = new GameObject[OriginAvatar.humanDescription.skeleton.Length];

            foreach (var s in OriginAvatar.humanDescription.skeleton)
            {
                Objects.Add(s.name, null);
            }
        }
    }
}

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
