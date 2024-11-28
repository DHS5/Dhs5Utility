using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Dhs5.Utility.Console
{
    [Serializable]
    public class ConsoleCommandPiece
    {
        #region ENUM : Type

        private enum Type
        {
            SINGLE = 0,
            MULTI = 1,
            PARAMETER = 2
        }

        #endregion

        #region Members

        // TYPE
        [SerializeField] private Type m_type;

        // INPUTS
        [SerializeField] private string m_singleInput;
        [SerializeField] private string[] m_multiInputs;
        [SerializeField] private ConsoleCommand.ParamType m_paramType;

        // PARAMETERS
        [SerializeField] private bool m_optional;

        #endregion

        #region Constructors

        public ConsoleCommandPiece(bool optional, string singleInput)
        {
            m_type = Type.SINGLE;

            m_singleInput = singleInput;
            m_multiInputs = null;
            m_paramType = ConsoleCommand.ParamType.BOOL;

            m_optional = optional;
        }
        public ConsoleCommandPiece(bool optional, params string[] multiInputs)
        {
            m_type = Type.MULTI;

            m_singleInput = null;
            m_multiInputs = multiInputs;
            m_paramType = ConsoleCommand.ParamType.BOOL;

            m_optional = optional;
        }
        public ConsoleCommandPiece(ConsoleCommand.ParamType paramType)
        {
            m_type = Type.PARAMETER;

            m_singleInput = null;
            m_multiInputs = null;
            m_paramType = paramType;

            m_optional = false;
        }

        #endregion


        #region Accessors

        public IEnumerable<string> GetOptions()
        {
            switch (m_type)
            {
                case Type.SINGLE:
                    yield return m_singleInput;
                    if (m_optional)
                    {
                        yield return string.Empty;
                    }
                    break;

                case Type.MULTI:
                    for (int i = 0; i < m_multiInputs.Length; i++)
                    {
                        yield return m_multiInputs[i];
                    }
                    if (m_optional)
                    {
                        yield return string.Empty;
                    }
                    break;

                case Type.PARAMETER:
                    yield return ConsoleCommand.GetParameterString(m_paramType);
                    break;
            }
        }

        #endregion

        #region Command Validation

        public bool IsCommandValid(string rawCommandPiece, out object parameter, out string rawCommandLeft)
        {
            parameter = null;
            rawCommandLeft = null;

            if (string.IsNullOrWhiteSpace(rawCommandPiece))
            {
                return m_optional;
            }

            switch (m_type)
            {
                case Type.SINGLE:
                    if (rawCommandPiece.StartsWith(m_singleInput, StringComparison.OrdinalIgnoreCase) &&
                        (rawCommandPiece.Length == m_singleInput.Length || rawCommandPiece[m_singleInput.Length] == ' '))
                    {
                        parameter = null;
                        rawCommandLeft = rawCommandPiece.Substring(m_singleInput.Length).Trim();
                        return true;
                    }
                    break;

                case Type.MULTI:
                    for (int i = 0; i < m_multiInputs.Length; i++)
                    {
                        if (rawCommandPiece.StartsWith(m_multiInputs[i], StringComparison.OrdinalIgnoreCase) &&
                            (rawCommandPiece.Length == m_multiInputs[i].Length || rawCommandPiece[m_multiInputs[i].Length] == ' '))
                        {
                            parameter = i;
                            rawCommandLeft = rawCommandPiece.Substring(m_multiInputs[i].Length).Trim();
                            return true;
                        }
                    }
                    break;

                case Type.PARAMETER:
                    int spaceIndex = rawCommandPiece.IndexOf(' ');
                    string paramStr;
                    if (spaceIndex != -1)
                    {
                        paramStr = rawCommandPiece.Substring(0, spaceIndex).Trim();
                        rawCommandLeft = rawCommandPiece.Substring(spaceIndex).Trim();
                    }
                    else
                    {
                        paramStr = rawCommandPiece;
                        rawCommandLeft = string.Empty;
                    }

                    return ConsoleCommand.IsParameterValid(paramStr, m_paramType, out parameter);
            }

            if (m_optional)
            {
                parameter = null;
                rawCommandLeft = rawCommandPiece;
                return true;
            }

            return false;
        }

        #endregion
    }

#if UNITY_EDITOR

    [CustomPropertyDrawer(typeof(ConsoleCommandPiece))]
    public class ConsoleCommandPieceDrawer : PropertyDrawer
    {
        #region Members

        SerializedProperty p_type;
        SerializedProperty p_singleInput;
        SerializedProperty p_multiInputs;
        SerializedProperty p_paramType;
        SerializedProperty p_optional;

        const float SPACE = 2f;

        #endregion

        #region GUI Draw

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            p_type = property.FindPropertyRelative("m_type");
            p_singleInput = property.FindPropertyRelative("m_singleInput");
            p_multiInputs = property.FindPropertyRelative("m_multiInputs");
            p_paramType = property.FindPropertyRelative("m_paramType");
            p_optional = property.FindPropertyRelative("m_optional");

            var rect = new Rect(position.x, position.y, position.width, 20f);

            EditorGUI.BeginProperty(position, label, property);

            EditorGUI.PropertyField(rect, p_type);
            rect.y += EditorGUI.GetPropertyHeight(p_type) + SPACE;

            switch (p_type.enumValueIndex)
            {
                // SINGLE
                case 0:
                    EditorGUI.PropertyField(rect, p_singleInput);
                    rect.y += EditorGUI.GetPropertyHeight(p_singleInput) + SPACE;
                    break;

                // MULTI
                case 1:
                    EditorGUI.PropertyField(rect, p_multiInputs);
                    rect.y += EditorGUI.GetPropertyHeight(p_multiInputs) + SPACE;
                    break;
                    
                // PARAMETER
                case 2:
                    EditorGUI.PropertyField(rect, p_paramType);
                    rect.y += EditorGUI.GetPropertyHeight(p_paramType);
                    p_optional.boolValue = false;
                    break;
            }

            if (p_type.enumValueIndex != 2)
            {
                EditorGUI.PropertyField(rect, p_optional);
                rect.y += EditorGUI.GetPropertyHeight(p_optional);
            }

            EditorGUI.EndProperty();
        }

        #endregion

        #region GUI Height

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            p_type = property.FindPropertyRelative("m_type");
            p_singleInput = property.FindPropertyRelative("m_singleInput");
            p_multiInputs = property.FindPropertyRelative("m_multiInputs");
            p_paramType = property.FindPropertyRelative("m_paramType");
            p_optional = property.FindPropertyRelative("m_optional");

            float height = EditorGUI.GetPropertyHeight(p_type) + SPACE;

            switch (p_type.enumValueIndex)
            {
                // SINGLE
                case 0:
                    height += EditorGUI.GetPropertyHeight(p_singleInput) + SPACE;
                    height += EditorGUI.GetPropertyHeight(p_optional);
                    break;

                // MULTI
                case 1:
                    height += EditorGUI.GetPropertyHeight(p_multiInputs) + SPACE;
                    height += EditorGUI.GetPropertyHeight(p_optional);
                    break;

                // PARAMETER
                case 2:
                    height += EditorGUI.GetPropertyHeight(p_paramType);
                    break;
            }

            return height;
        }

        #endregion
    }

#endif
}
