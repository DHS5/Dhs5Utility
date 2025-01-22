using System;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[AttributeUsage(AttributeTargets.Field)]
public class HelpBoxAttribute : PropertyAttribute
{
    #region ENUM Type

    public enum EType
    {
        NONE,
        INFO,
        WARNING,
        ERROR
    }

    #endregion

    #region Members

    public readonly string content = null;
    public readonly EType type = EType.NONE;

    #endregion

    #region Constructors

    public HelpBoxAttribute(string content, EType type)
    {
        this.content = content;
        this.type = type;
    }

    #endregion
}

#region Drawer

#if UNITY_EDITOR

[CustomPropertyDrawer(typeof(HelpBoxAttribute))]
public class HelpBoxAttributeDrawer : PropertyDrawer
{
    #region Members

    HelpBoxAttribute m_helpBoxAttribute;
    const float margin = 5f;

    static GUIStyle helpBoxLabel
    {
        get
        {
            GUIStyle m_HelpBoxLabel = new GUIStyle(EditorStyles.helpBox);
            m_HelpBoxLabel.name = "HelpBoxLabel";
            m_HelpBoxLabel.normal.background = null;
            m_HelpBoxLabel.hover.background = null;
            m_HelpBoxLabel.active.background = null;
            m_HelpBoxLabel.focused.background = null;
            m_HelpBoxLabel.onNormal.background = null;
            m_HelpBoxLabel.onHover.background = null;
            m_HelpBoxLabel.onActive.background = null;
            m_HelpBoxLabel.onFocused.background = null;
            return m_HelpBoxLabel;
        }
    }

    #endregion

    #region GUI

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        m_helpBoxAttribute = attribute as HelpBoxAttribute;

        EditorGUI.BeginProperty(position, label, property);

        if (string.IsNullOrWhiteSpace(m_helpBoxAttribute.content) 
            || m_helpBoxAttribute.type == HelpBoxAttribute.EType.NONE)
        {
            EditorGUI.PropertyField(position, property, label, true);
        }
        else
        {
            GUI.Box(position, GUIContent.none, EditorStyles.helpBox);

            var style = helpBoxLabel;
            GUIContent content = EditorGUIUtility.TrTextContentWithIcon(m_helpBoxAttribute.content, (MessageType)m_helpBoxAttribute.type);

            var rect = new Rect(position.x + 2f, position.y + 2f, position.width - 4f, style.CalcHeight(content, position.width - 4f));
            EditorGUI.LabelField(rect, content, style);

            rect.y += rect.height + margin;
            rect.height = EditorGUI.GetPropertyHeight(property);
            EditorGUI.PropertyField(rect, property, label, true);
        }

        EditorGUI.EndProperty();
    }

    #endregion

    #region GUI Height

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        m_helpBoxAttribute = attribute as HelpBoxAttribute;

        if (string.IsNullOrWhiteSpace(m_helpBoxAttribute.content)
            || m_helpBoxAttribute.type == HelpBoxAttribute.EType.NONE)
        {
            return EditorGUI.GetPropertyHeight(property, label);
        }
        else
        {
            var style = helpBoxLabel;
            GUIContent content = EditorGUIUtility.TrTextContentWithIcon(m_helpBoxAttribute.content, (MessageType)m_helpBoxAttribute.type);

            return EditorGUI.GetPropertyHeight(property, label) + 4f + margin
                + style.CalcHeight(content, EditorGUIUtility.currentViewWidth - 20f);
        }
    }

    #endregion
}

#endif

#endregion
