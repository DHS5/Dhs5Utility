using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[Serializable]
public class EnumFlagValues<TE, TF, U> : IEnumerable<KeyValuePair<TE, U>> where TE : Enum where TF : Enum
{
    #region Members

    [SerializeField] private TF m_flag;
    [SerializeField] private U[] m_enumValues;

    #endregion

    #region Properties

    public TF Flag { get => m_flag; set => m_flag = value; }
    public U this[TE enumValue]
    {
        get => Get(enumValue);
        set => Set(enumValue, value);
    }

    #endregion

    #region Accessors

    public bool TryGet(TE enumValue, out U value)
    {
        value = default;
        if (!m_enumValues.IsValid()) return false;

        int index = Convert.ToInt32(enumValue);
        if (index < m_enumValues.Length)
        {
            value = m_enumValues[index];
            return IsIndexValid(index);
        }
        else
        {
            Debug.LogError("Enum Values for type " + typeof(U) + " not complete, might need to serialize it in the inspector");
            return false;
        }
    }
    private U Get(TE enumValue)
    {
        if (!m_enumValues.IsValid()) return default;

        int index = Convert.ToInt32(enumValue);
        if (index < m_enumValues.Length)
            return m_enumValues[index];
        else
        {
            Debug.LogError("Enum Values for type " + typeof(U) + " not complete, might need to serialize it in the inspector");
            return default;
        }
    }
    private void Set(TE enumValue, U value)
    {
        if (!m_enumValues.IsValid()) return;

        int index = Convert.ToInt32(enumValue);
        if (index >= m_enumValues.Length)
        {
            var newEnumValues = new U[index + 1];
            for (int i = 0; i < m_enumValues.Length; i++)
            {
                newEnumValues[i] = m_enumValues[i];
            }
            m_enumValues = newEnumValues;
        }
        m_enumValues[index] = value;
    }

    #endregion

    #region Enumerators

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
    public IEnumerator<KeyValuePair<TE, U>> GetEnumerator()
    {
        for (int i = 0; i < m_enumValues.Length; i++)
        {
            if (Enum.IsDefined(typeof(TE), i) && IsIndexValid(i))
            {
                yield return new KeyValuePair<TE, U>((TE)Enum.ToObject(typeof(TE), i), m_enumValues[i]);
            }
        }
    }

    public IEnumerable<U> GetSimpleEnumerator()
    {
        foreach (var kvp in this)
        {
            yield return kvp.Value;
        }
    }

    #endregion

    #region Utility

    private bool IsIndexValid(int index)
    {
        return (Convert.ToInt32(m_flag) & (1 << index)) != 0;
    }

    #endregion
}

#region Drawer

#if UNITY_EDITOR

[CustomPropertyDrawer(typeof(EnumFlagValues<,,>))]
public class EnumFlagValuesDrawer : PropertyDrawer
{
    #region GUI

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var flagType = GetFlagType();
        var enumType = GetEnumType();
        var enumValues = Enum.GetValues(enumType);

        var p_flag = property.FindPropertyRelative("m_flag");
        var p_enumValues = property.FindPropertyRelative("m_enumValues");

        // Get array size
        int arraySize = 0;
        int intValue;
        for (int i = 0; i < enumValues.Length; i++)
        {
            intValue = (int)enumValues.GetValue(i);
            if (intValue >= arraySize) arraySize = intValue + 1;
        }
        p_enumValues.arraySize = arraySize;

        // --- GUI ---
        EditorGUI.BeginProperty(position, label, property);

        Rect rect = new(position.x, position.y, position.width, 20f);

        // Foldout
        Rect foldoutRect = new Rect(rect.x, rect.y, EditorGUIUtility.labelWidth, rect.height);
        // Click
        if (Event.current.type == EventType.MouseDown
            && Event.current.button == 0
            && foldoutRect.Contains(Event.current.mousePosition))
        {
            property.isExpanded = !property.isExpanded;
            Event.current.Use();
        }
        // Repaint
        if (Event.current.type == EventType.Repaint)
        {
            EditorStyles.foldout.Draw(foldoutRect, label, 0, property.isExpanded);
        }

        // Flag Field
        Rect flagRect = new Rect(rect.x + EditorGUIUtility.labelWidth + 2f, rect.y, rect.width - EditorGUIUtility.labelWidth - 2f, rect.height);
        EditorGUI.PropertyField(flagRect, p_flag, GUIContent.none);

        if (property.isExpanded)
        {
            EditorGUI.indentLevel++;

            rect.y += 25f;

            int index;
            foreach (var v in enumValues)
            {
                index = (int)v;
                if (index >= 0 && index < arraySize && (p_flag.enumValueFlag & 1 << index) != 0)
                {
                    EditorGUI.PropertyField(rect, p_enumValues.GetArrayElementAtIndex(index), new GUIContent(v.ToString()), true);
                    rect.y += Mathf.Max(20f, EditorGUI.GetPropertyHeight(p_enumValues.GetArrayElementAtIndex(index)));
                }
            }

            EditorGUI.indentLevel--;
        }

        EditorGUI.EndProperty();
    }

    #endregion

    #region GUI Height

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (property.isExpanded)
        {
            float height = 25f;

            var p_flag = property.FindPropertyRelative("m_flag");
            var p_enumValues = property.FindPropertyRelative("m_enumValues");
            int index;
            foreach (var v in Enum.GetValues(GetEnumType()))
            {
                index = (int)v;
                if (index >= 0 && index < p_enumValues.arraySize && (p_flag.enumValueFlag & 1 << index) != 0)
                {
                    height += Mathf.Max(20f, EditorGUI.GetPropertyHeight(p_enumValues.GetArrayElementAtIndex(index)));
                }
            }

            return height;
        }
        return 20f;
    }

    #endregion

    #region Utility

    private Type GetEnumType()
    {
        var enumType = fieldInfo.FieldType;
        while (enumType.IsGenericType) enumType = enumType.GetGenericArguments()[0];
        return enumType;
    }
    private Type GetFlagType()
    {
        var flagType = fieldInfo.FieldType;
        while (flagType.IsGenericType) flagType = flagType.GetGenericArguments()[1];
        return flagType;
    }

    #endregion
}

#endif

#endregion