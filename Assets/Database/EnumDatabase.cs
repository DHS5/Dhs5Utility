using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;
using System;


#if UNITY_EDITOR
using UnityEditor;
using System.IO;
using Dhs5.Utility.Editors;
using Dhs5.Utility.GUIs;
#endif

namespace Dhs5.Utility.Databases
{
    /// <summary>
    /// Base class for a database managed by an auto-generated ENUM
    /// </summary>
    /// <remarks>
    /// EnumDatabases should always use the <see cref="DatabaseAttribute"/> and never the <see cref="DataContainerAttribute"/>
    /// </remarks>
    public abstract class EnumDatabase : ScriptableDataContainer
    {
        #region Members

        // Enum Properties
        [SerializeField] private string m_enumName;
        [SerializeField] private string m_enumNamespace;
        [SerializeField, TextArea] private string m_usings;

        // Script Properties
        [SerializeField, FolderPicker] private string m_scriptFolder;
        [SerializeField] private TextAsset m_textAsset;

        #endregion


        #region Editor Data Type

#if UNITY_EDITOR

        internal override bool Editor_ContainerHasValidDataType(out Type dataType)
        {
            if (base.Editor_ContainerHasValidDataType(out dataType))
            {
                return typeof(IEnumDatabaseElement).IsAssignableFrom(dataType);
            }
            return false;
        }

#endif

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
            EditorUtils.EnsureDirectoryExistence(m_scriptFolder);
            return true;
        }

        protected virtual string GetEnumScriptContentFor(
            string enumName, string enumNamespace, string usings, string[] enumContent, Type dataType, Type databaseType)
        {
            return EnumDatabaseEditor.GenerateEnumScriptContent
                (enumName, enumContent, enumNamespace, usings, dataType, databaseType);
        }
        protected string GetEnumScriptContent()
        {
            string[] enumContent = new string[Count];
            for (int i = 0; i < enumContent.Length; i++)
            {
                enumContent[i] = GetDataAtIndex(i).name;
            }

            if (Editor_ContainerHasValidDataType(out var dataType))
            {
                return GetEnumScriptContentFor(m_enumName, m_enumNamespace, m_usings, enumContent, dataType, GetType());
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

        internal override void Editor_ShouldRecomputeContainerContent()
        {
            SaveCurrentContentOrder();

            base.Editor_ShouldRecomputeContainerContent();
        }
        internal void Editor_UpdateEnumScript()
        {
            string content = GetEnumScriptContent();
            bool differentContent = content != null && content != GetCurrentScriptContent();
            bool differentPath = false;
            bool pathValid = TryGetEnumScriptPath(out string path);
            if (pathValid)
            {
                differentPath = m_textAsset == null || path != AssetDatabase.GetAssetPath(m_textAsset);
            }
            if (pathValid && (differentContent || differentPath))
            {
                var newTextAsset = Database.CreateOrOverwriteScript(path, content);
                if (m_textAsset != null
                    && newTextAsset != m_textAsset)
                {
                    Database.DeleteAsset(m_textAsset, false);
                }

                m_textAsset = newTextAsset;

                AssetDatabase.Refresh();
                AssetDatabase.SaveAssets();
            }
        }

        protected virtual void SaveCurrentContentOrder()
        {
            int i = 0;
            foreach (var elem in Editor_GetContainerElements<IEnumDatabaseElement>())
            {
                elem.Editor_SetIndex(i);
                i++;
            }
        }
        protected override Comparison<ScriptableObject> Editor_SortComparison()
        {
            return (e1, e2) => (e1 as IEnumDatabaseElement).EnumIndex.CompareTo((e2 as IEnumDatabaseElement).EnumIndex);
        }

        internal override string Editor_GetDataPrefixedName(UnityEngine.Object obj)
        {
            return obj != null ? obj.name : null; 
        }

#endif

        #endregion
    }

#if UNITY_EDITOR

    [CustomEditor(typeof(EnumDatabase), editorForChildClasses:true)]
    public class EnumDatabaseEditor : ScriptableDataContainerEditor
    {
        #region Members

        protected EnumDatabase m_enumDatabase;

        protected SerializedProperty p_enumName;
        protected SerializedProperty p_enumNamespace;
        protected SerializedProperty p_usings;

        protected SerializedProperty p_scriptFolder;
        protected SerializedProperty p_textAsset;

        protected Vector2 PreviewScrollPos { get; set; }

        #endregion

        #region Properties

        // Database Informations
        protected bool ShowExtraUsings { get; set; } = true;

        #endregion

        #region Core Behaviour

        protected override void OnEnable()
        {
            base.OnEnable();

            m_enumDatabase = target as EnumDatabase;

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
            OnContainerInformationsGUI("Enum Informations");

            EditorGUILayout.Space(10f);

            Rect dataListWindowRect = EditorGUILayout.GetControlRect(false, m_dataListWindowHeight);
            dataListWindowRect.x += 10f;
            dataListWindowRect.width -= 20f;
            OnContainerContentListWindowGUI(dataListWindowRect, refreshButton: true, addButton: true, contextButtons: true);

            EditorGUILayout.Space(5f);
            Separator(2f, Color.white);
            EditorGUILayout.Space(10f);

            DisplayContainerCurrentSelection();
        }

        protected override string ContainerInvalidDataTypeMessage()
        {
            return "The data type of this Database is not valid.\n\n" +
                    "- Add the DatabaseAttribute to the top of your script.\n" +
                    "- Make sure the dataType parameter inherits from ScriptableObject and implements at least the IEnumDatabaseElement interface.";
        }

        #endregion

        #region Container Informations

        protected override void OnContainerInformationsContentGUI()
        {
            EditorGUILayout.PropertyField(p_enumName);
            EditorGUILayout.PropertyField(p_enumNamespace);

            if (ShowExtraUsings)
                EditorGUILayout.PropertyField(p_usings);

            EditorGUILayout.Space(8f);

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(p_scriptFolder, true);
            if (EditorGUI.EndChangeCheck())
            {
                ForceContainerContentRefresh();
            }

            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.PropertyField(p_textAsset);
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space(5f);

            if (GUILayout.Button("Update Script"))
            {
                m_enumDatabase.Editor_UpdateEnumScript();
            }

            base.OnContainerInformationsContentGUI();
        }

        #endregion

        #region Database Content List

        protected override EContentListDisplayType GetContentListDisplayType()
        {
            return EContentListDisplayType.INDEX;
        }

        protected override void OnContentListElementNameGUI(Rect rect, int index, bool selected, UnityEngine.Object obj, string name)
        {
            base.OnContentListElementNameGUI(rect, index, selected, obj, index + ": " + name);
        }

        #endregion

        #region Data Creation

        protected override void OnAddNewDataToContainer(UnityEngine.Object obj)
        {
            if (obj is IEnumDatabaseElement elem)
            {
                elem.Editor_SetIndex(int.MaxValue);
            }

            base.OnAddNewDataToContainer(obj);
        }

        #endregion

        #region Data Renaming

        protected override bool OnCompleteRenaming(UnityEngine.Object obj, int index)
        {
            if (base.OnCompleteRenaming(obj, index))
            {
                ForceContainerContentRefresh();
                return true;
            }
            return false;
        }

        #endregion

        #region Database Element

        protected override void DisplayContainerElement(UnityEngine.Object element)
        {
            if (element != null)
            {
                EditorGUILayout.LabelField(element.name, GUIHelper.centeredBoldLabel);
            }
            base.DisplayContainerElement(element);
        }

        #endregion

        #region Context Menu

        protected override void PopulateContainerDataContextMenu(UnityEngine.Object obj, int index, GenericMenu menu)
        {
            base.PopulateContainerDataContextMenu(obj, index, menu);

            if (index > 0)
            {
                menu.AddItem(new GUIContent("Move Up"), false, () => MoveElement(index, index - 1));
            }
            else
            {
                menu.AddDisabledItem(new GUIContent("Move Up"));
            }
            
            if (index < ContentListCount - 1)
            {
                menu.AddItem(new GUIContent("Move Down"), false, () => MoveElement(index, index + 1));
            }
            else
            {
                menu.AddDisabledItem(new GUIContent("Move Down"));
            }
        }

        #endregion

        #region List Reorder

        protected void MoveElement(int index, int newIndex)
        {
            if (index >= 0 && index < ContentListCount && 
                newIndex >= 0 && newIndex < ContentListCount)
            {
                p_content.MoveArrayElement(index, newIndex);
                serializedObject.ApplyModifiedProperties();
                OnReorderElements();
            }
        }
        protected virtual void OnReorderElements()
        {
            ForceContainerContentRefresh();
        }

        #endregion

        #region Preview

        public override bool HasPreviewGUI()
        {
            return p_textAsset.objectReferenceValue != null;
        }
        public override void OnPreviewGUI(Rect r, GUIStyle background)
        {
            base.OnPreviewGUI(r, background);

            if (p_textAsset.objectReferenceValue != null
                && p_textAsset.objectReferenceValue is TextAsset textAsset)
            {
                GUIContent textAssetContent = new GUIContent(textAsset.text);
                Vector2 contentSize = EditorStyles.label.CalcSize(textAssetContent);
                Rect contentViewRect = new Rect(0, 0, contentSize.x + 10f, contentSize.y + 4f);
                Rect contentRect = new Rect(5f, 2f, contentSize.x, contentSize.y);

                PreviewScrollPos = GUI.BeginScrollView(r, PreviewScrollPos, contentViewRect);

                EditorGUI.LabelField(contentRect, textAsset.text);

                GUI.EndScrollView();
            }
        }

        #endregion


        #region STATIC : Enum Script Generation

        public static string GenerateEnumScriptContent(
            string enumName, 
            string[] enumContent,
            string enumNamespace = null,
            string usings = null,
            Type paramType = null,
            Type databaseType = null,
            string[] extensions = null)
        {
            StringBuilder sb = new StringBuilder();

            if (string.IsNullOrWhiteSpace(enumName)) return null;

            // Parameters
            bool hasNamespace = !string.IsNullOrWhiteSpace(enumNamespace);
            bool hasUsings = !string.IsNullOrWhiteSpace(usings);
            bool hasParam = paramType != null;
            string paramTypeName = paramType.Name;
            bool hasDatabase = databaseType != null;
            string databaseTypeName = hasDatabase ? databaseType.Name : null;
            bool hasExtensions = extensions != null;

            string defaultUsings = "using UnityEngine;\nusing System;\nusing Dhs5.Utility.Databases;\n";

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

            if (hasDatabase && hasParam)
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
                sb.Append("return Database.Get<");
                sb.Append(databaseTypeName);
                sb.Append(">().GetDataAtIndex<");
                sb.Append(paramTypeName);
                sb.AppendLine(">((int)e);");

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

            // OTHER EXTENSIONS
            if (hasExtensions)
            {
                foreach (var extension in extensions)
                {
                    AppendPrefix();
                    sb.AppendLine(extension);
                }
            }

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
