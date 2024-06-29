using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[ExecuteAlways]
public class MinionImportTester : MonoBehaviour
{
    public int ClipIndex = 0;
    [HideInInspector] public float Time = 0;
    public Map<int, GameObject> SpawnObject = new Map<int, GameObject>();
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        for (int i = 0; i < SpawnObject.Count; i++)
        {
            var Ltransform = MinionAnimationDB.Instance.GetPartTransform(ClipIndex, SpawnObject.GetKey(i), Time);
            var spawnSlot = MinionAnimationDB.Instance.GetSpawnSlotData(ClipIndex, SpawnObject.GetKey(i));

            SpawnObject.GetVaule(i).transform.localPosition = Ltransform.Position + spawnSlot.Vaule.offset.position;
            SpawnObject.GetVaule(i).transform.localRotation = Ltransform.Rotation * spawnSlot.Vaule.offset.rotation;
            SpawnObject.GetVaule(i).transform.localScale = Ltransform.Scale * spawnSlot.Vaule.offset.scale;
        }
    }

    public void Spawn()
    {
        if (SpawnObject.Count > 0)
            return;

        for(int i = 0; i < MinionAnimationDB.Instance.BoneAmount(ClipIndex); i++)
        {
            var spawnTarget = MinionAnimationDB.Instance.GetSpawnObj(ClipIndex, i, true);

            if (spawnTarget == null)
                continue;

            var spawnSlot = MinionAnimationDB.Instance.GetSpawnSlotData(ClipIndex, i);

            var spawned = GameObject.Instantiate(spawnTarget, transform);
            spawned.name = spawnSlot.Key;
            SpawnObject.Add(i, spawned);

            var Ltransform = MinionAnimationDB.Instance.GetPartTransform(ClipIndex, i, Time);

            spawned.transform.localPosition = Ltransform.Position + spawnSlot.Vaule.offset.position;
            spawned.transform.localRotation = Ltransform.Rotation * spawnSlot.Vaule.offset.rotation;
            spawned.transform.localScale = Ltransform.Scale * spawnSlot.Vaule.offset.scale;
        }
    }
    public void Clear()
    {
        foreach(var e in SpawnObject.GetVaule())
        {
            DestroyImmediate(e);
        }
        SpawnObject.Clear();
    }
}

[CustomEditor(typeof(MinionImportTester))]
public class MinionImportTesterEditor : Editor
{
    MinionImportTester onwer;
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        if (onwer == null)
            onwer = target as MinionImportTester;

        EditorGUILayout.Slider(serializedObject.FindProperty("Time"), 0, MinionAnimationDB.Instance.GetClipLength(onwer.ClipIndex));

        if (GUILayout.Button("Initial"))
        {
            onwer.Spawn();
        }

        if (GUILayout.Button("Clear"))
        {
            onwer.Clear();
        }

        serializedObject.ApplyModifiedProperties();
    }
}
