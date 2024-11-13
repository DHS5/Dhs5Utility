using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;
using System;
using Dhs5.Utility.Editors;


#if UNITY_EDITOR
using UnityEditor;
using System.IO;
#endif

namespace Dhs5.Utility.Databases
{
    public class EnumDatabase<T> : ScriptableDatabase<T> where T : EnumDatabase<T>
    {
        #region Members

        // Enum Properties
        [SerializeField] private string m_enumName;
        [SerializeField] private string m_enumNamespace;
        [SerializeField, TextArea] private string m_usings;

        // Script Properties
        [SerializeField] private string m_scriptFolder;
        [SerializeField] private TextAsset m_textAsset;

        #endregion

        #region Accessors

        public U GetValueAtIndex<U>(int index) where U : ScriptableObject
        {
            if (TryGetElementAtIndex<U>(index, out var value))
            {
                return value;
            }
            return null;
        }

        #endregion


        #region Editor Utility

#if UNITY_EDITOR

        protected bool TryGetEnumScriptPath(out string path)
        {
            if (string.IsNullOrWhiteSpace(m_scriptFolder)
                || !m_scriptFolder.StartsWith("Assets"))
            {
                path = null;
                return false;
            }
            path = m_scriptFolder + "/" + m_enumName + ".cs";
            EditorUtils.AssureDirectoryExistence(m_scriptFolder);
            return true;
        }

        protected string GetEnumScriptContent()
        {
            string[] enumContent = new string[Count];
            for (int i = 0; i < enumContent.Length; i++)
            {
                enumContent[i] = GetElementAtIndex(i).name;
            }

            if (HasDataType(GetType(), out var dataType)
                && dataType.IsSubclassOf(typeof(ScriptableObject)))
            {
                return EnumDatabaseEditor.GenerateEnumScriptContent(
                    m_enumName, m_enumNamespace, m_usings, enumContent, dataType, GetType());
            }
            return null;
        }

        protected string GetCurrentScriptContent()
        {
            if (m_textAsset != null) return m_textAsset.text;
            return null;
        }

#endif

        #endregion

        #region Editor Content Management

#if UNITY_EDITOR

        internal override void Editor_ShouldRecomputeDatabaseContent()
        {
            base.Editor_ShouldRecomputeDatabaseContent();

            string content = GetEnumScriptContent();
            if (content != null
                && content != GetCurrentScriptContent()
                && TryGetEnumScriptPath(out string path))
            {
                var newTextAsset = BaseDatabase.CreateOrOverwriteScript(path, content);
                if (m_textAsset != null 
                    && newTextAsset != m_textAsset)
                {
                    BaseDatabase.DeleteAsset(m_textAsset, false);
                }
                m_textAsset = newTextAsset;

                AssetDatabase.Refresh();
                AssetDatabase.SaveAssets();
            }
        }

#endif

        #endregion
    }

#if UNITY_EDITOR

    [CustomEditor(typeof(EnumDatabase<>), editorForChildClasses:true)]
    public class EnumDatabaseEditor : ScriptableDatabaseEditor
    {
        #region Members

        protected SerializedProperty p_enumName;
        protected SerializedProperty p_enumNamespace;
        protected SerializedProperty p_usings;

        protected SerializedProperty p_scriptFolder;
        protected SerializedProperty p_textAsset;

        #endregion

        #region Core Behaviour

        protected override void OnEnable()
        {
            base.OnEnable();

            p_enumName = serializedObject.FindProperty("m_enumName");
            p_enumNamespace = serializedObject.FindProperty("m_enumNamespace");
            p_usings = serializedObject.FindProperty("m_usings");
            p_scriptFolder = serializedObject.FindProperty("m_scriptFolder");
            p_textAsset = serializedObject.FindProperty("m_textAsset");

            m_excludedProperties.Add(p_enumName.propertyPath);
            m_excludedProperties.Add(p_enumNamespace.propertyPath);
            m_excludedProperties.Add(p_usings.propertyPath);
            m_excludedProperties.Add(p_scriptFolder.propertyPath);
            m_excludedProperties.Add(p_textAsset.propertyPath);
        }

        #endregion

        #region GUI

        protected override void OnGUI()
        {
            DrawDefault();

            OnDatabaseInformationsGUI();

            EditorGUILayout.Space(10f);

            Rect dataListWindowRect = EditorGUILayout.GetControlRect(false, m_dataListWindowHeight);
            dataListWindowRect.x += 10f;
            dataListWindowRect.width -= 20f;
            OnDatabaseContentListWindowGUI(dataListWindowRect, refreshButton: true, addButton: true, deleteButtons: true);

            EditorGUILayout.Space(5f);
            Separator(2f, Color.white);
            EditorGUILayout.Space(10f);

            DisplayCurrentDatabaseContentListSelection();
        }

        #endregion

        #region Database Informations

        protected override void OnDatabaseInformationsContentGUI()
        {
            EditorGUILayout.PropertyField(p_enumName);
            EditorGUILayout.PropertyField(p_enumNamespace);
            EditorGUILayout.PropertyField(p_usings);

            EditorGUILayout.Space(8f);

            EditorGUIHelper.FolderPicker(p_scriptFolder, ForceDatabaseContentRefresh);
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.PropertyField(p_textAsset);
            EditorGUI.EndDisabledGroup();
        }

        #endregion

        #region Preview

        public override bool HasPreviewGUI()
        {
            return p_textAsset.objectReferenceValue != null;
        }
        public override void OnPreviewGUI(Rect r, GUIStyle background)
        {
            if (p_textAsset.objectReferenceValue != null
                && p_textAsset.objectReferenceValue is TextAsset textAsset)
            {
                EditorGUI.LabelField(r, textAsset.text);
            }
        }

        #endregion


        #region STATIC : Enum Script Generation

        public static string GenerateEnumScriptContent(
            string enumName, 
            string enumNamespace, 
            string usings,
            string[] enumContent,
            Type paramType,
            Type databaseType = null)
        {
            StringBuilder sb = new StringBuilder();

            if (string.IsNullOrWhiteSpace(enumName)) return null;

            // Parameters
            bool hasNamespace = !string.IsNullOrWhiteSpace(enumNamespace);
            bool hasUsings = !string.IsNullOrWhiteSpace(usings);
            string paramTypeName = paramType.Name;
            bool hasDatabase = databaseType != null;
            string databaseTypeName = hasDatabase ? databaseType.Name : null;

            string defaultUsings = "using UnityEngine;\nusing System;\n";

            // Utility Functions
            string prefix = "";
            void Increment()
            {
                prefix += "    ";
            }
            void Decrement()
            {
                prefix = prefix.Remove(0, 4);
            }
            void AppendPrefix()
            {
                sb.Append(prefix);
            }
            void OpenBracket()
            {
                AppendPrefix();
                sb.AppendLine("{");
            }
            void CloseBracket()
            {
                AppendPrefix();
                sb.AppendLine("}");
            }

            // USINGS
            sb.Append(defaultUsings);
            if (hasUsings)
            {
                sb.AppendLine(usings);
            }
            sb.AppendLine();

            // NAMESPACE
            if (hasNamespace)
            {
                sb.Append("namespace ");
                sb.AppendLine(enumNamespace);
                sb.AppendLine("{");
                Increment();
            }

            // ENUM
            AppendPrefix();
            sb.Append("public enum ");
            sb.AppendLine(enumName);
            OpenBracket();

            string value;
            Increment();
            int index = 0;
            for (int i = 0; i < enumContent.Length; i++)
            {
                value = enumContent[i];
                if (!string.IsNullOrWhiteSpace(value))
                {
                    AppendPrefix();
                    sb.Append(value);
                    sb.Append(" = ");
                    sb.Append(index.ToString());
                    sb.AppendLine(",");
                    index++;
                }
            }
            Decrement();
            CloseBracket();

            // FLAGS
            sb.AppendLine();
            AppendPrefix();
            sb.AppendLine("[Flags]");
            AppendPrefix();
            sb.Append("public enum ");
            sb.Append(enumName);
            sb.AppendLine("Flags");

            OpenBracket();
            Increment();
            index = 0;
            for (int i = 0; i < enumContent.Length; i++)
            {
                value = enumContent[i];
                if (!string.IsNullOrWhiteSpace(value))
                {
                    AppendPrefix();
                    sb.Append(value);
                    sb.Append(" = 1 << ");
                    sb.Append(index.ToString());
                    sb.AppendLine(",");
                    index++;
                }
            }
            Decrement();
            CloseBracket();

            // EXTENSION
            AppendPrefix();
            sb.AppendLine();

            AppendPrefix();
            sb.Append("public static class ");
            sb.Append(enumName);
            sb.AppendLine("Extension");

            OpenBracket();
            Increment();

            if (hasDatabase)
            {
                // Get Value
                AppendPrefix();
                sb.Append("public static ");
                sb.Append(paramTypeName);
                sb.Append(" GetValue(this ");
                sb.Append(enumName);
                sb.AppendLine(" e)");

                OpenBracket();
                Increment();
                AppendPrefix();
                sb.Append("return ");
                sb.Append(databaseTypeName);
                sb.AppendLine(".I");
                sb.Append(".GetValueAtIndex<");
                sb.Append(paramTypeName);
                sb.Append(">((int)e);");

                Decrement();
                CloseBracket();
            }

            // Flags Contains
            // Contains 1
            sb.AppendLine();

            AppendPrefix();
            sb.Append("public static bool Contains(this ");
            sb.Append(enumName);
            sb.Append("Flags flag, ");
            sb.Append(enumName);
            sb.AppendLine(" e)");

            OpenBracket();
            Increment();
            AppendPrefix();
            sb.Append("return (flag & ((");
            sb.Append(enumName);
            sb.AppendLine("Flags)(1 << (int)e))) != 0;");

            Decrement();
            CloseBracket();

            // Contains 2
            sb.AppendLine();

            AppendPrefix();
            sb.Append("public static bool Contains(this ");
            sb.Append(enumName);
            sb.Append("Flags flag, ");
            sb.Append(enumName);
            sb.AppendLine("Flags other)");

            OpenBracket();
            Increment();
            AppendPrefix();
            sb.AppendLine("return (flag & other) != 0;");
            Decrement();
            CloseBracket();

            Decrement();
            CloseBracket();

            // END NAMESPACE
            if (hasNamespace)
            {
                Decrement();
                CloseBracket();
            }

            // Result
            return sb.ToString();
        }

        #endregion
    }

#endif
}
