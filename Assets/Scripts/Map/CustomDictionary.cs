using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR
using Expand;
#endif

[Serializable]
public class Map<T>
{
    [SerializeField]
    private List<T> Vaule;

    public Map(List<T> vaule = null)
    {
        Vaule = new List<T>();
        if (vaule != null)
        {
            Vaule = vaule;
        }
    }

    public List<T> Get()
    {
        return Vaule;
    }
    public T Get(int index)
    {
        if (Vaule.Count > index && index >= 0)
        {
            return Vaule[index];
        }
        else
        {
            return default;
        }
    }
    public bool Set(int index, T t = default)
    {
        if (Vaule.Count > index && index >= 0)
        {
            Vaule[index] = t;
            return true;
        }
        else
        {
            return false;
        }
    }
    public bool Exist(T t)
    {
        return Vaule.Contains(t);
    }

    public void Add(T t = default)
    {
        Vaule.Add(t);
    }
    public void Remove(T t)
    {
        Vaule.Remove(t);
    }
    public void RemoveAt(int index)
    {
        Vaule.RemoveAt(index);
    }

    public int Length => Vaule.Count;

    public static Map<T> operator +(Map<T> map, T t)
    {
        map.Add(t);
        return map;
    }
    public static Map<T> operator -(Map<T> map, T t)
    {
        map.Remove(t);
        return map;
    }
    public static Map<T> operator -(Map<T> map, int index)
    {
        map.RemoveAt(index);
        return map;
    }

    public static int ArraySize(SerializedProperty property)
    {
        return property.FindPropertyRelative("Vaule").arraySize;
    }

}

[Serializable]
public class IntMap<T>
{
    [Serializable]
    public struct Vaule
    {
        public int key;
        public T vaule;
    }//int key , T vaule
    [SerializeField]
    private List<Vaule> Vaules;

    public IntMap(List<Vaule> vaules = null)
    {
        Vaules = new List<Vaule>();
        if (vaules != null)
        {
            Vaules = vaules;
        }
    }

    public List<Vaule> Get()
    {
        return Vaules;
    }
    public Vaule Get(int index)
    {
        if (Vaules.Count > index && index >= 0)
        {
            return Vaules[index];
        }
        else
        {
            return default;
        }
    }
    public Vaule GetKey(int Key)
    {
        if (Vaules.Exists(v => v.key == Key))
        {
            return Vaules.Find(v => v.key == Key);
        }
        else
        {
            return default;
        }
    }
    public T Find(int index)
    {
        if (Vaules.Count > index && index >= 0)
        {
            return Get(index).vaule;
        }
        else
        {
            return default;
        }
    }
    public T FindToKey(int Key)
    {
        return GetKey(Key).vaule;
    }
    public int FindIndex(int Key)
    {
        return Vaules.FindIndex(v => v.key == Key);
    }

    public bool Set(int index, T t = default)
    {
        if (Vaules.Count > index && index >= 0)
        {
            Vaules[index] = new Vaule { key = Vaules[index].key, vaule = t };
            return true;
        }
        else
        {
            return false;
        }
    }
    public bool SetToKey(int Key, T t = default)
    {
        int temp = FindIndex(Key);
        if (temp >= 0)
        {
            Vaules[temp] = new Vaule { key = Key, vaule = t };
            return true;
        }
        else
        {
            return false;
        }
    }

    public bool Exist(Vaule vaule)
    {
        return Vaules.Contains(vaule);
    }
    public bool Exist(int Key)
    {
        return Vaules.Exists(v => v.key == Key);
    }

    public void Add(Vaule vaule = default)
    {
        Vaules.Add(vaule);
    }
    public void Add(int key, T t = default)
    {
        Vaule Temp = new Vaule
        {
            key = key,
            vaule = t
        };

        Vaules.Add(Temp);
    }
    public void Remove(Vaule vaule)
    {
        Vaules.Remove(vaule);
    }
    public void Remove(int Key)
    {
        RemoveAt(FindIndex(Key));
    }
    public void RemoveAt(int index)
    {
        Vaules.RemoveAt(index);
    }

    public static IntMap<T> operator +(IntMap<T> intMap, Vaule vaule)
    {
        intMap.Add(vaule);
        return intMap;
    }
    public static IntMap<T> operator +(IntMap<T> intMap, int key)
    {
        intMap.Add(key);
        return intMap;
    }
    public static IntMap<T> operator -(IntMap<T> intMap, Vaule vaule)
    {
        intMap.Remove(vaule);
        return intMap;
    }
    public static IntMap<T> operator -(IntMap<T> intMap, int key)
    {
        intMap.RemoveAt(key);
        return intMap;
    }

    public int Count
    {
        get { return Vaules.Count; }
    }
    public void Clear()
    {
        Vaules.Clear();
    }
}

[Serializable]
public class Map<T, V>
{
    [Serializable]
    public struct MapSlot
    {
        public T Key;
        public V Vaule;
    }
    [SerializeField]
    private List<MapSlot> Slots = new List<MapSlot>();
    //[SerializeField]
    //List<T> Key = new List<T>();
    //[SerializeField]
    //List<V> Vaule = new List<V>();

    public Map()
    {
        //Key = new List<T>();
        //Vaule = new List<V>();
        Slots = new List<MapSlot>();
    }
    public void Clear()
    {
        Slots.Clear();
        //Key.Clear();
        //Vaule.Clear();
    }
    public void Add(T key, V vaule)
    {
        //Key.Add(key);
        //Vaule.Add(vaule);
        Slots.Add(new MapSlot { Key = key, Vaule = vaule});
    }
    public bool Remove(int i)
    {
        /*
        if (i >= 0 && i < Key.Count)
        {
            Key.RemoveAt(i);
            Vaule.RemoveAt(i);

            return true;
        }
        return false;*/
        if (i >= 0 && i < Slots.Count)
        {
            Slots.RemoveAt(i);

            return true;
        }
        return false;
    }

    public int Count
    {
        //get { return Key.Count; }
        get { return Slots.Count; }
    }
    public List<MapSlot> Get()
    {
        return Slots;
    }
    public List<T> GetKey()
    {
        //return Key;
        List<T> result = new List<T>();
        if (Slots != null)
        {
            for (int i = 0; i < Slots.Count; i++)
            {
                result.Add(Slots[i].Key);
            }
        }
        return result;
    }
    public T GetKey(int index)
    {
        //return Key[index];
        return Slots[index].Key;
    }
    public List<V> GetVaule()
    {
        //return Vaule;

        List<V> result = new List<V>();

        for (int i = 0; i < Slots.Count; i++)
        {
            result.Add(Slots[i].Vaule);
        }
        return result;
    }
    public V GetVaule(int index)
    {
        //return Vaule[index];
        return Slots[index].Vaule;
    }

    public bool SetKey(int index, T key)
    {
        /*
        if (index >= 0 && index < Key.Count)
        {
            Key[index] = key;
            return true;
        }else
        {
            return false;
        }*/

        if (index >= 0 && index < Slots.Count)
        {
            MapSlot temp = Slots[index];
            temp.Key = key;
            Slots[index] = temp;
            return true;
        }
        else
        {
            return false;
        }
    }
    public bool SetVaule(int index, V vaule)
    {
        /*
        if (index >= 0 && index < Vaule.Count)
        {
            Vaule[index] = vaule;
            return true;
        }else
        {
            return false;
        }*/

        if (index >= 0 && index < Slots.Count)
        {
            MapSlot temp = Slots[index];
            temp.Vaule = vaule;
            Slots[index] = temp;
            return true;
        }
        else
        {
            return false;
        }
    }

}

[Serializable]
public class GroupMap<T>
{
    [SerializeField]
    List<T> Key = new List<T>();
    [SerializeField]
    List<int> Vaule = new List<int>();

    public Map<int, Map<int>> SortList = new Map<int, Map<int>>();

    public GroupMap()
    {
        Key = new List<T>();
        Vaule = new List<int>();

        SortList = new Map<int, Map<int>>();
    }

    public List<T> GetKey()
    {
        return Key;
    }
    public T GetKey(int index)
    {
        return Key[index];
    }
    public List<int> GetVaule()
    {
        return Vaule;
    }
    public int GetVaule(int index)
    {
        return Vaule[index];
    }

    public void SortElement(int index)
    {
        int SortSlot = SortList.GetKey().FindIndex(t => t == Vaule[index]);
        if (SortSlot >= 0)
        {
            SortList.GetVaule(SortSlot).Add(index);
        }else
        {
            var temp = new Map<int>();
            temp.Add(index);

            SortList.Add(Vaule[index], temp);
        }
    }

    public void Add(T key, int vaule)
    {
        Key.Add(key);
        Vaule.Add(vaule);

        SortElement(Key.Count - 1);
    }

    public void Set(int index, T key)
    {
        Key[index] = key;
    }
    public void Set(int index, int vaule)
    {
        //이전값으로 SortList에서 저거 > 값변경 > SortElement
        RemoveSortList(index);

        Vaule[index] = vaule;

        SortElement(index);
    }
    public bool Remove(int index)
    {
        if (index >= 0 && index < Length)
        {
            RemoveSortList(index);

            Key.RemoveAt(index);
            Vaule.RemoveAt(index);

            return true;
        }else
        {
            return false;
        }
    }
    public void Clear()
    {
        Key.Clear();
        Vaule.Clear();
        SortList.Clear();
    }
    public int Length => Key.Count;

    void RemoveSortList(int index)
    {
        int SortIndex = SortList.GetKey().FindIndex(t => t == Vaule[index]);
        SortList.GetVaule(SortIndex).RemoveAt(index);
        if (SortList.GetVaule(SortIndex).Length == 0)
        {
            SortList.Remove(SortIndex);
        }
    }
}

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(Map<>))]
public class MapEditor : PropertyDrawer
{
    Rect DrawRect;
    //bool fold = false;
    UnityEditorInternal.ReorderableList list;

    SerializedProperty vaule;

    //float SlotOffset = 10f;
    //float SlotLineSize = 20;

    #region disable
    /*
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (property.isExpanded)
        {
            if (property.FindPropertyRelative("Vaule").arraySize > 0)
                return (property.FindPropertyRelative("Vaule").arraySize) * EditorGUI.GetPropertyHeight(property.FindPropertyRelative("Vaule").GetArrayElementAtIndex(0), true) + 40;
            else
                return 40;
        }
        else
        {
            return 20;
        }
    }
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        DrawRect = new Rect(position.x, position.y, position.width, 20);
        vaule = property.FindPropertyRelative("Vaule");

        if (property.FindPropertyRelative("Vaule").arraySize > 0)
        {
            SlotLineSize = EditorGUI.GetPropertyHeight(property.FindPropertyRelative("Vaule").GetArrayElementAtIndex(0), true);
        }

        {
            property.isExpanded = EditorGUI.Foldout(DrawRect, property.isExpanded, (property.displayName + " [" + vaule.arraySize + "]"), true);
            if (property.isExpanded)
            {
                Rect SlotSize = new Rect(position.x, position.y, position.width - 50, 20);
                for (int i = 0; i < vaule.arraySize; i++)
                {
                    DrawRect = EditorExpand.NextLine(SlotSize, DrawRect, 0, SlotLineSize);
                    DrawRect = new Rect(DrawRect.x + SlotOffset, DrawRect.y, DrawRect.width - SlotOffset, DrawRect.height);

                    EditorGUI.PropertyField(DrawRect, vaule.GetArrayElementAtIndex(i), new GUIContent { text = "" }, true);
                    //DrawRect = new Rect((position.width - position.x - 25), DrawRect.y, 50, DrawRect.height);
                    DrawRect = new Rect(DrawRect.x + DrawRect.width, DrawRect.y, 50, DrawRect.height);
                    if (GUI.Button(DrawRect, " - "))
                    {
                        vaule.DeleteArrayElementAtIndex(i);
                    }

                }
                {
                    DrawRect = EditorExpand.NextLine(position, DrawRect, SlotOffset, 20);
                    if (GUI.Button(DrawRect, "Add"))
                    {
                        vaule.arraySize++;
                    }
                }
            }
        }//Foldout

    }
    */
    #endregion

    public void DrawList()
    {
        list = new UnityEditorInternal.ReorderableList(vaule.serializedObject, vaule, true, true, true, true)
        {
            headerHeight = 0,
            elementHeightCallback = (int index) =>
            {
                return EditorGUI.GetPropertyHeight(vaule.GetArrayElementAtIndex(index));
            },
            drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                rect.y += 2;
                rect.x += 10;
                rect.width -= 10;
                EditorGUI.PropertyField(rect, vaule.GetArrayElementAtIndex(index), GUIContent.none, true);
            }
        };
    }
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        vaule = property.FindPropertyRelative("Vaule");

        if (list == null)
        {
            DrawList();        
        }

        {
            if (property.isExpanded)
            {
                float Lheight = 40;
                if (vaule.arraySize == 0 )
                {
                    Lheight += 20;
                }
                else
                {
                    for (int i = 0; i < vaule.arraySize; i++)
                    {
                        Lheight += EditorGUI.GetPropertyHeight(vaule.GetArrayElementAtIndex(i));
                    }
                }
                return Lheight;
            }
            else
            {
                return EditorGUIUtility.singleLineHeight;
            }
        }//Return Property Height
    }
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        DrawRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        property.isExpanded = EditorGUI.Foldout(DrawRect, property.isExpanded, new GUIContent(property.displayName), true);
        if (property.isExpanded)
        {
            list.DoList(new Rect(DrawRect.x, (DrawRect.y + DrawRect.height), DrawRect.width, DrawRect.height));
        }
    }
}

[CustomPropertyDrawer(typeof(IntMap<>))]
public class IntMapEditor : PropertyDrawer
{
    Rect DrawRect;
    //bool fold = false;

    UnityEditorInternal.ReorderableList list;
    SerializedProperty Vaule;

    //float SlotOffset = 10f;
    #region Disable
    /*
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (fold)
        {
            return (property.FindPropertyRelative("Vaules").arraySize + 2) * 20;
        }
        else
        {
            return 20;
        }
    }
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        DrawRect = new Rect(position.x, position.y, position.width, 20);
        //key = property.FindPropertyRelative("Key");
        //vaule = property.FindPropertyRelative("Vaule");
        list = property.FindPropertyRelative("Vaules");

        //fold = EditorGUI.Foldout(DrawRect, fold, label);
        if (GUI.Button(DrawRect, (label.text + (fold ? " (open) " : " (close) ") + " ( " + list.arraySize + " ) ")))
        {
            fold = !fold;
        }

        if (fold)
        {
            Rect SlotSize = new Rect(position.x, position.y, position.width - 50, 20);
            for (int i = 0; i < list.arraySize; i++)
            {
                key = list.GetArrayElementAtIndex(i).FindPropertyRelative("key");
                vaule = list.GetArrayElementAtIndex(i).FindPropertyRelative("vaule");

                DrawRect = EditorExpand.NextLine(SlotSize, DrawRect);
                DrawRect = EditorExpand.RateRect(SlotSize, DrawRect, 0, 2, SlotOffset, 20);
                EditorGUI.PropertyField(DrawRect, key, new GUIContent { text = "" });
                DrawRect = EditorExpand.RateRect(SlotSize, DrawRect, 1, 2, SlotOffset, 20);
                EditorGUI.PropertyField(DrawRect, vaule, new GUIContent { text = "" });
                DrawRect = new Rect(DrawRect.x + DrawRect.width, DrawRect.y, 50, DrawRect.height);
                if (GUI.Button(DrawRect, " - "))
                {
                    list.DeleteArrayElementAtIndex(i);
                }

            }
            {
                DrawRect = EditorExpand.NextLine(position, DrawRect);
                DrawRect = new Rect(DrawRect.x + SlotOffset, DrawRect.y, DrawRect.width - SlotOffset, DrawRect.height);

                if (GUI.Button(DrawRect, "Add"))
                {
                    list.arraySize++;
                }
            }

        }
    }
    */
    #endregion

    public void DrawList()
    {
        list = new UnityEditorInternal.ReorderableList(Vaule.serializedObject, Vaule, true, true, true, true)
        {
            headerHeight = 0,
            //elementHeight = EditorGUIUtility.singleLineHeight,
            elementHeightCallback = (int index) =>
            {
                return Mathf.Max(EditorGUI.GetPropertyHeight(Vaule.GetArrayElementAtIndex(index).FindPropertyRelative("key")),
                    EditorGUI.GetPropertyHeight(Vaule.GetArrayElementAtIndex(index).FindPropertyRelative("vaule")));
            },
            drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                rect.y += 2;
                rect.x += 10;
                rect.width -= 10;
                float KeyWidth = Mathf.Min(100, rect.width * 0.5f);

                EditorGUI.PropertyField(new Rect(rect.x, rect.y, KeyWidth, rect.height),
                    Vaule.GetArrayElementAtIndex(index).FindPropertyRelative("key"), GUIContent.none, true);

                EditorGUI.PropertyField(new Rect((rect.x + KeyWidth), rect.y, (rect.width - KeyWidth), rect.height),
                    Vaule.GetArrayElementAtIndex(index).FindPropertyRelative("vaule"), GUIContent.none, true);
            }
        };
    }
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        Vaule = property.FindPropertyRelative("Vaules");
        if (list == null)
        {
            DrawList();
        }


        {
            if (property.isExpanded)
            {
                float Lheight = 40;
                if (Vaule.arraySize == 0)
                {
                    Lheight += 20;
                }
                else
                {
                    for (int i = 0; i < Vaule.arraySize; i++)
                    {
                        Lheight += Mathf.Max(EditorGUI.GetPropertyHeight(Vaule.GetArrayElementAtIndex(i).FindPropertyRelative("key")),
                                EditorGUI.GetPropertyHeight(Vaule.GetArrayElementAtIndex(i).FindPropertyRelative("vaule")), 20);
                    }
                }
                return Lheight;
            }
            else
            {
                return EditorGUIUtility.singleLineHeight;
            }
        }//Return Property Height
    }
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        DrawRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        property.isExpanded = EditorGUI.Foldout(DrawRect, property.isExpanded, new GUIContent(property.displayName), true);
        if (property.isExpanded)
        {
            list.DoList(new Rect(DrawRect.x, (DrawRect.y + DrawRect.height), DrawRect.width, DrawRect.height));
        }
    }
}

//[CustomPropertyDrawer(typeof(IntMap<>.Vaule))]
public class IntMapSlotEditor : PropertyDrawer
{
    Rect DrawRect;

    SerializedProperty key;
    SerializedProperty vaule;

    float SlotOffset = 10f;

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        vaule = property.FindPropertyRelative("vaule");

        return EditorGUI.GetPropertyHeight(vaule, true);
    }
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        DrawRect = new Rect(position.x, position.y, position.width, 20);
        key = property.FindPropertyRelative("key");
        vaule = property.FindPropertyRelative("vaule");

        DrawRect = EditorExpand.RateRect(position, DrawRect, 0, 2, SlotOffset, 20);
        EditorGUI.PropertyField(DrawRect, key, new GUIContent { text = "" }, true);
        DrawRect = EditorExpand.RateRect(position, DrawRect, 1, 2, SlotOffset, 20);
        EditorGUI.PropertyField(DrawRect, vaule, new GUIContent { text = "" }, true);
    }
}

[CustomPropertyDrawer(typeof(Map<,>))]
public class MapEditor_KV : PropertyDrawer
{
    Rect DrawRect;
    //bool fold = false;

    //SerializedProperty key;
    //SerializedProperty vaule;
    SerializedProperty Slot;

    //float SlotOffset = 10f;
    UnityEditorInternal.ReorderableList list;

    #region Disable
    /*
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (fold)
        {
            return (property.FindPropertyRelative("Key").arraySize + 2) * 20;
        }
        else
        {
            return 20;
        }
    }
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        DrawRect = new Rect(position.x, position.y, position.width, 20);
        key = property.FindPropertyRelative("Key");
        vaule = property.FindPropertyRelative("Vaule");

        //fold = EditorGUI.Foldout(DrawRect, fold, label);
        if (GUI.Button(DrawRect, (label.text + (fold ? " (open) " : " (close) ") + " (" + key.arraySize + ") ")))
        {
            fold = !fold;
        }

        if (fold)
        {
            Rect SlotSize = new Rect(position.x, position.y, position.width - 50, 20);
            for (int i = 0; i < key.arraySize; i++)
            {
                DrawRect = EditorExpand.NextLine(SlotSize, DrawRect);
                DrawRect = EditorExpand.RateRect(SlotSize, DrawRect, 0, 2, SlotOffset, 20);
                EditorGUI.PropertyField(DrawRect, key.GetArrayElementAtIndex(i), new GUIContent { text = "" });
                DrawRect = EditorExpand.RateRect(SlotSize, DrawRect, 1, 2, SlotOffset, 20);
                EditorGUI.PropertyField(DrawRect, vaule.GetArrayElementAtIndex(i), new GUIContent { text = "" });
                //DrawRect = new Rect((position.width - position.x - 25), DrawRect.y, 50, DrawRect.height);
                DrawRect = new Rect(DrawRect.x + DrawRect.width, DrawRect.y, 50, DrawRect.height);
                if (GUI.Button(DrawRect, " - "))
                {
                    key.DeleteArrayElementAtIndex(i);
                    vaule.DeleteArrayElementAtIndex(i);
                }

            }
            {
                DrawRect = EditorExpand.NextLine(position, DrawRect);
                DrawRect = new Rect(DrawRect.x + SlotOffset, DrawRect.y, DrawRect.width - SlotOffset, DrawRect.height);
                if (GUI.Button(DrawRect, "Add"))
                {
                    key.arraySize++;
                    vaule.arraySize++;

                }
            }

        }
    }
    */
    #endregion
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        Slot = property.FindPropertyRelative("Slots");

        if (list == null)
        {
            list = new UnityEditorInternal.ReorderableList(Slot.serializedObject, Slot, true, true, true, true)
            {
                headerHeight = 0,
                //elementHeight = EditorGUIUtility.singleLineHeight,
                elementHeightCallback = (int index) =>
                {
                    return Mathf.Max(EditorGUI.GetPropertyHeight(Slot.GetArrayElementAtIndex(index).FindPropertyRelative("Key"), true),
                        EditorGUI.GetPropertyHeight(Slot.GetArrayElementAtIndex(index).FindPropertyRelative("Vaule"), true));
                },
                drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
                {
                    rect.y += 2;
                    rect.x += 10;
                    rect.width -= 10;

                    EditorGUI.PropertyField(new Rect(rect.x, rect.y, rect.width * 0.5f, rect.height),
                        Slot.GetArrayElementAtIndex(index).FindPropertyRelative("Key"), GUIContent.none, true);

                    EditorGUI.PropertyField(new Rect((rect.x + rect.width * 0.5f), rect.y, rect.width * 0.5f, rect.height),
                        Slot.GetArrayElementAtIndex(index).FindPropertyRelative("Vaule"), GUIContent.none, true);
                }
            };
        }

        return EditorGUIUtility.singleLineHeight + (property.isExpanded ? list.GetHeight() : 0);
    }
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        DrawRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        property.isExpanded = EditorGUI.Foldout(DrawRect, property.isExpanded, new GUIContent(property.displayName), true);
        if (property.isExpanded)
        {
            list.DoList(new Rect(DrawRect.x, (DrawRect.y + DrawRect.height), DrawRect.width, DrawRect.height));
        }
    }
}

//[CustomPropertyDrawer(typeof(Map<,>.MapSlot))]
public class MapSlotEditor : PropertyDrawer
{
    Rect DrawRect;

    SerializedProperty key;
    SerializedProperty vaule;

    float SlotOffset = 10f;

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        key = property.FindPropertyRelative("Key");
        vaule = property.FindPropertyRelative("Vaule");

        return Mathf.Max(EditorGUI.GetPropertyHeight(key, true), EditorGUI.GetPropertyHeight(vaule, true));
    }
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        DrawRect = new Rect(position.x, position.y, position.width, 20);
        key = property.FindPropertyRelative("Key");
        vaule = property.FindPropertyRelative("Vaule");

        DrawRect = EditorExpand.RateRect(position, DrawRect, 0, 2, SlotOffset, 20);
        EditorGUI.PropertyField(DrawRect, key, new GUIContent { text = "" }, true);
        DrawRect = EditorExpand.RateRect(position, DrawRect, 1, 2, SlotOffset, 20);
        EditorGUI.PropertyField(DrawRect, vaule, new GUIContent { text = "" }, true);
    }
}

[CustomPropertyDrawer(typeof(GroupMap<>))]
public class GroupMapEditor : PropertyDrawer
{
    Rect DrawRect;
    bool fold = false;
    List<bool> SlotFold = new List<bool>();

    SerializedProperty key;
    SerializedProperty vaule;
    SerializedProperty Sort;

    float SlotOffset = 10f;
    float RemoveButton = 50f;

    float LineSize = 20;

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {

        //return EditorGUI.GetPropertyHeight(property.FindPropertyRelative("SortList").FindPropertyRelative("Slots"), true) + 60;

        int Line = 0;
        var slots = property.FindPropertyRelative("SortList").FindPropertyRelative("Slots");

        if (property.FindPropertyRelative("Key").arraySize > 0)
        {
            LineSize = EditorGUI.GetPropertyHeight(property.FindPropertyRelative("Key").GetArrayElementAtIndex(0), true);
        }

        if (fold)
        {
            Line = 0;
            //Line += slots.arraySize;

            for (int i = 0; i < SlotFold.Count; i++)
            {
                if (i < slots.arraySize && SlotFold[i])
                {
                    Line += slots.GetArrayElementAtIndex(i).FindPropertyRelative("Vaule").FindPropertyRelative("Vaule").arraySize;
                }
                
            }
            //Sort.FindPropertyRelative("Slots").GetArrayElementAtIndex(i).FindPropertyRelative("Vaule").FindPropertyRelative("Vaule");//Map<int>.Vaule

            //SlotFold 적용
            return Line * LineSize + 60 + (slots.arraySize * 20);
        }
        else
        {
            return 20;
        }
    }
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        //base.OnGUI(position, property, label);
        DrawRect = new Rect(position.x, position.y, position.width, 20);
        key = property.FindPropertyRelative("Key");
        vaule = property.FindPropertyRelative("Vaule");
        Sort = property.FindPropertyRelative("SortList");

        for (int i = SlotFold.Count; i < MapSize(Sort); i++)
        {
            SlotFold.Add(false);
        }//SortList의 크기와 SlotFold 크기와 같게 + 항목이 제거시 같이 제거


        if (GUI.Button(DrawRect, (label.text + (fold ? " (open) " : " (close) ") + " (" + key.arraySize + ") ")))
        {
            fold = !fold;
        }

        if (property.FindPropertyRelative("Key").arraySize > 0)
        {
            LineSize = EditorGUI.GetPropertyHeight(property.FindPropertyRelative("Key").GetArrayElementAtIndex(0), true);
        }

        if (fold)
        {
            bool Changed = false;
            int RemoveGroup = -1;
            int RemoveSlot = -1;

            for (int i = 0; i < MapSize(Sort); i++)
            {
                SerializedProperty SortKey = Sort.FindPropertyRelative("Slots").GetArrayElementAtIndex(i).FindPropertyRelative("Key");

                var SortSlot = Sort.FindPropertyRelative("Slots").GetArrayElementAtIndex(i).FindPropertyRelative("Vaule").FindPropertyRelative("Vaule");//Map<int>.Vaule
                //Sort.FindPropertyRelative("Vaule").arraySize

                DrawRect = EditorExpand.NextLine(position, DrawRect, SlotOffset);
                if ((i < SlotFold.Count))
                {
                    if (GUI.Button(DrawRect, ("Group " + SortKey.intValue + (SlotFold[i] ? " (open) " : " (close) ") + (" [" + SortSlot.arraySize + "] "))))
                    {
                        SlotFold[i] = !SlotFold[i];
                    }

                    if (SlotFold[i])
                    {
                        
                        for (int j = 0; j < SortSlot.arraySize; j++)
                        {
                            DrawRect = EditorExpand.NextLine(position, DrawRect, (SlotOffset * 2), LineSize);//NextLineFix

                            DrawRect = EditorExpand.RateRect(position, DrawRect, 0, 2, (SlotOffset * 2 + 0), LineSize);
                            //DrawRect = new Rect(DrawRect.x - RemoveButton, DrawRect.y, DrawRect.width, DrawRect.height);

                            int Lindex = SortSlot.GetArrayElementAtIndex(j).intValue;

                            if (Lindex >= key.arraySize)
                            {
                                Changed = true;
                                break;
                            }

                            //DrawRect = EditorExpand.PropertyField(position, DrawRect, key.GetArrayElementAtIndex(Lindex), (i + " - " + j + " "), 2);
                            DrawRect = EditorExpand.ResizedLabel(position, DrawRect, (i + " - " + j + " "));
                            EditorGUI.PropertyField(DrawRect, key.GetArrayElementAtIndex(Lindex),GUIContent.none, true);

                            int temp = vaule.GetArrayElementAtIndex(Lindex).intValue;
                            DrawRect = EditorExpand.RateRect(position, DrawRect, 1, 2, (SlotOffset * 2), LineSize);

                            Rect TempRect = new Rect(position.x, position.y, position.width - RemoveButton * 2, DrawRect.height);
                            //DrawRect = EditorExpand.PropertyField(TempRect, DrawRect, vaule.GetArrayElementAtIndex(Lindex), "", 2);

                            DrawRect = new Rect(DrawRect.x, DrawRect.y, DrawRect.width - RemoveButton, DrawRect.height);
                            EditorGUI.PropertyField(DrawRect, vaule.GetArrayElementAtIndex(Lindex), GUIContent.none, true);

                            DrawRect = new Rect(position.width - RemoveButton * 0.5f, DrawRect.y, RemoveButton, LineSize);
                            if (GUI.Button(DrawRect, " - "))
                            {
                                RemoveGroup = i;
                                RemoveSlot = j;
                            }

                            if (temp != vaule.GetArrayElementAtIndex(Lindex).intValue)
                            {
                                Changed = true;
                            }
                        }
                        //DrawRect = EditorExpand.NextLineOverride(position, DrawRect, (SlotOffset * 2), 20);
                    }else
                    {
                        //DrawRect = EditorExpand.NextLine(position, DrawRect, (SlotOffset * 2), 20);
                    }
                }
            }

            if (RemoveSlot >= 0)
            {
                var SortSlot = Sort.FindPropertyRelative("Slots").GetArrayElementAtIndex(RemoveGroup).FindPropertyRelative("Vaule").FindPropertyRelative("Vaule");//Map<int>.Vaule

                key.DeleteArrayElementAtIndex(SortSlot.GetArrayElementAtIndex(RemoveSlot).intValue);
                vaule.DeleteArrayElementAtIndex(SortSlot.GetArrayElementAtIndex(RemoveSlot).intValue);

                SortMap(property);
            }

            if (Changed)
            {
                SortMap(property);
            }

            DrawRect = EditorExpand.NextLine(position, DrawRect, 0, 20);
            if (GUI.Button(DrawRect, "Add"))
            {
                key.arraySize++;
                vaule.arraySize++;

                SortMap(property);
                //키, 값 추가함
            }

            DrawRect = EditorExpand.NextLine(position, DrawRect, 0);
            if (GUI.Button(DrawRect, "Clear"))
            {
                property.FindPropertyRelative("Key").arraySize = 0;
                property.FindPropertyRelative("Vaule").arraySize = 0;

                property.FindPropertyRelative("SortList").FindPropertyRelative("Slots").arraySize = 0;
            }
        }
    }
    public int MapSize(SerializedProperty property)
    {
        if (property == null)
            return 0;
        else
            return property.FindPropertyRelative("Slots").arraySize;
    }

    public void SortMap(SerializedProperty property)
    {

        var LSlots = property.FindPropertyRelative("SortList").FindPropertyRelative("Slots");
        property.FindPropertyRelative("SortList").FindPropertyRelative("Slots").arraySize = 0;

        for (int i = 0; i < property.FindPropertyRelative("Vaule").arraySize; i++)
        {
            int Lvaule = property.FindPropertyRelative("Vaule").GetArrayElementAtIndex(i).intValue;

            int Find = -1;
            for (int f = 0; f < LSlots.arraySize; f++)
            {
                if (Lvaule == LSlots.GetArrayElementAtIndex(f).FindPropertyRelative("Key").intValue)
                {
                    Find = f;
                    break;
                }
            }

            if (Find >= 0)
            {
                //int Lsize = property.FindPropertyRelative("SortList").FindPropertyRelative("Slots").GetArrayElementAtIndex(Find).FindPropertyRelative("Vaule").arraySize;
                int Lsize = property.FindPropertyRelative("SortList").FindPropertyRelative("Slots").GetArrayElementAtIndex(Find).FindPropertyRelative("Vaule").FindPropertyRelative("Vaule").arraySize++;
                property.FindPropertyRelative("SortList").FindPropertyRelative("Slots").GetArrayElementAtIndex(Find).FindPropertyRelative("Vaule").FindPropertyRelative("Vaule").GetArrayElementAtIndex(Lsize - 1).intValue = i;
            }
            else
            {
                int Lkey = property.FindPropertyRelative("SortList").FindPropertyRelative("Slots").arraySize++;

                property.FindPropertyRelative("SortList").FindPropertyRelative("Slots").GetArrayElementAtIndex(Lkey).FindPropertyRelative("Key").intValue = Lvaule;
                property.FindPropertyRelative("SortList").FindPropertyRelative("Slots").GetArrayElementAtIndex(Lkey).FindPropertyRelative("Vaule").FindPropertyRelative("Vaule").arraySize = 1;
                property.FindPropertyRelative("SortList").FindPropertyRelative("Slots").GetArrayElementAtIndex(Lkey).FindPropertyRelative("Vaule").FindPropertyRelative("Vaule").GetArrayElementAtIndex(0).intValue = i;
            }
        }

        //값을 루프돌려서 i 번째 값을 임시저장 => Lvaule
        //정렬 리스트의 Key가 Lvaule인 인덱스를 찾음 => k
        //
        //k >= 0 이라면 정렬리스트 k 번째 값 마지막에 i를 추가
        //k < 0 이라면 정렬리스트에 새로운 Key 생성 , i 를 추가
    }
}
#endif

