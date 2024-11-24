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

        // PARAMETERS
        [SerializeField] private bool m_optional;

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
                    yield return ConsoleCommand.PARAMETER;
                    if (m_optional)
                    {
                        yield return string.Empty;
                    }
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
                        if (rawCommandPiece.StartsWith(m_multiInputs[i], StringComparison.OrdinalIgnoreCase))
                        {
                            parameter = i;
                            rawCommandLeft = rawCommandPiece.Substring(m_multiInputs[i].Length).Trim();
                            return true;
                        }
                    }
                    break;

                case Type.PARAMETER:
                    int spaceIndex = rawCommandPiece.IndexOf(' ');
                    if (spaceIndex != -1)
                    {
                        parameter = rawCommandPiece.Substring(0, spaceIndex).Trim();
                        rawCommandLeft = rawCommandPiece.Substring(spaceIndex).Trim();
                    }
                    else
                    {
                        parameter = rawCommandPiece;
                        rawCommandLeft = string.Empty;
                    }
                    return true;
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
        SerializedProperty p_optional;

        const float SPACE = 2f;

        #endregion

        #region GUI Draw

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            p_type = property.FindPropertyRelative("m_type");
            p_singleInput = property.FindPropertyRelative("m_singleInput");
            p_multiInputs = property.FindPropertyRelative("m_multiInputs");
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
                    break;
            }

            if (p_type.enumValueIndex != 2)
            {
                EditorGUI.PropertyField(rect, p_optional);
                rect.y += EditorGUI.GetPropertyHeight(p_optional);
            }
            else
            {
                p_optional.boolValue = false;
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
            p_optional = property.FindPropertyRelative("m_optional");

            float height = EditorGUI.GetPropertyHeight(p_type) + SPACE;

            switch (p_type.enumValueIndex)
            {
                // SINGLE
                case 0:
                    height += EditorGUI.GetPropertyHeight(p_singleInput) + SPACE;
                    break;

                // MULTI
                case 1:
                    height += EditorGUI.GetPropertyHeight(p_multiInputs) + SPACE;
                    break;

                // PARAMETER
                case 2:
                    break;
            }

            if (p_type.enumValueIndex != 2)
            {
                height += EditorGUI.GetPropertyHeight(p_optional);
            }

            return height;
        }

        #endregion
    }

#endif
}
