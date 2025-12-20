using System;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[Serializable]
public class SerializableMonoScript
#if UNITY_EDITOR
        : ISerializationCallbackReceiver
#endif
{
    #region Members

#if UNITY_EDITOR
    [SerializeField] private MonoScript m_monoScript;
#endif

    [SerializeField, HideInInspector] private string m_typeName;

    private Type m_type;

    #endregion

    #region Properties

    public Type Type
    {
        get
        {
#if UNITY_EDITOR
            return m_monoScript != null ? m_monoScript.GetClass() : null;
#else
            if (m_type == null || m_type.AssemblyQualifiedName != m_typeName)
            {
                m_type = string.IsNullOrWhiteSpace(m_typeName) ? null : Type.GetType(m_typeName);
            }
            return m_type;
#endif
        }
    }

#if UNITY_EDITOR

    public MonoScript MonoScript
    {
        get => m_monoScript;
        set => m_monoScript = value;
    }

    public void SetMonoScriptFrom(MonoBehaviour monoBehaviour) => m_monoScript = MonoScript.FromMonoBehaviour(monoBehaviour);
    public void SetMonoScriptFrom(ScriptableObject scriptableObject) => m_monoScript = MonoScript.FromScriptableObject(scriptableObject);

#endif

    #endregion

    #region ISerializationCallbackReceiver

#if UNITY_EDITOR

    public void OnAfterDeserialize() { }

    public void OnBeforeSerialize()
    {
        m_typeName = m_monoScript != null ? m_monoScript.GetClass().AssemblyQualifiedName : "";
    }

#endif

    #endregion
}

[Serializable]
public class SerializableMonoScript<T> : SerializableMonoScript where T : class
{

}

#region Drawer

#if UNITY_EDITOR

[CustomPropertyDrawer(typeof(SerializableMonoScript), useForChildren:true)]
public class SerializableMonoScriptDrawer : PropertyDrawer
{
    #region GUI

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        Type baseType = null;
        if (fieldInfo.FieldType.IsGenericType)
        {
            baseType = fieldInfo.FieldType.GetGenericArguments()[0];
        }

        EditorGUI.BeginProperty(position, label, property);

        var p_monoScript = property.FindPropertyRelative("m_monoScript");
        if (baseType != null)
        {
            EditorGUI.BeginChangeCheck();
            var monoScript = (MonoScript)EditorGUI.ObjectField(position, label, p_monoScript.objectReferenceValue, typeof(MonoScript), false);
            if (EditorGUI.EndChangeCheck())
            {
                if (monoScript != null && baseType.IsAssignableFrom(monoScript.GetClass()))
                {
                    p_monoScript.objectReferenceValue = monoScript;
                }
                else
                {
                    if (monoScript != null) Debug.LogError(monoScript.name + " is not a child type of " + baseType.Name);
                    p_monoScript.objectReferenceValue = null;
                }
            }
        }
        else
        {
            EditorGUI.ObjectField(position, p_monoScript, label);
        }

        EditorGUI.EndProperty();
    }

    #endregion
}

#endif

#endregion
