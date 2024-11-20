using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;
using System;
using System.Linq;


#if UNITY_EDITOR
using UnityEditor;
using System.IO;
using Dhs5.Utility.Editors;
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

        #region Enum Database Element (Editor Only)

#if UNITY_EDITOR

        private struct EnumDatabaseElement
        {
            public EnumDatabaseElement(ScriptableObject obj, int index) 
            { 
                this.obj = obj;
                this.index = index;
            }

            public readonly ScriptableObject obj;
            public readonly int index;
        }

        private List<EnumDatabaseElement> m_enumElements;

#endif

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
                enumContent[i] = GetElementAtIndex(i).name;
            }

            if (HasDataType(GetType(), out var dataType)
                && dataType.IsSubclassOf(typeof(ScriptableObject)))
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

        internal override void Editor_ShouldRecomputeDatabaseContent()
        {
            SaveCurrentContentOrder();

            base.Editor_ShouldRecomputeDatabaseContent();

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

        protected virtual void SaveCurrentContentOrder()
        {
            bool objectsAreEnumDatabaseElements = 
                HasDataType(GetType(), out var dataType)
                && typeof(IEnumDatabaseElement).IsAssignableFrom(dataType);

            if (m_enumElements != null) m_enumElements.Clear();
            else m_enumElements = new();

            int i = 0;
            foreach (var elem in Editor_GetDatabaseContent())
            {
                if (elem is ScriptableObject so)
                {
                    if (objectsAreEnumDatabaseElements
                        && so is IEnumDatabaseElement enumElement)
                    {
                        enumElement.Editor_SetIndex(i);
                    }
                    else
                    {
                        m_enumElements.Add(new EnumDatabaseElement(so, i));
                    }
                    i++;
                }
            }
        }
        protected override void SortContent()
        {
            if (HasDataType(GetType(), out var dataType)
                && typeof(IEnumDatabaseElement).IsAssignableFrom(dataType))
            {
                var content = Editor_GetDatabaseContent().ToList().ConvertAll(o => o as ScriptableObject);
                content.Sort((e1,e2) => (e1 as IEnumDatabaseElement).EnumIndex.CompareTo((e2 as IEnumDatabaseElement).EnumIndex));

                // Set new content
                Editor_SetContent(content);
            }
            else if (m_enumElements != null)
            {
                // Sort enum elements
                m_enumElements.Sort((e1,e2) => e1.index.CompareTo(e2.index));

                // Get content
                List<ScriptableObject> newContent = new();
                var currentContent = Editor_GetDatabaseContent().ToList().ConvertAll(o => o as ScriptableObject);

                // Add saved sorted content first (if still exists)
                foreach (var elem in m_enumElements)
                {
                    if (currentContent.Contains(elem.obj))
                    {
                        newContent.Add(elem.obj);
                    }
                }
                // Add new content then
                foreach (var elem in currentContent)
                {
                    if (!newContent.Contains(elem))
                    {
                        newContent.Add(elem);
                    }
                }

                // Set new content
                Editor_SetContent(newContent);
            }
            else
            {
                base.SortContent();
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
            OnDatabaseContentListWindowGUI(dataListWindowRect, refreshButton: true, addButton: true, contextButtons: true);

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

            if (ShowExtraUsings)
                EditorGUILayout.PropertyField(p_usings);

            EditorGUILayout.Space(8f);

            EditorGUIHelper.FolderPicker(p_scriptFolder, ForceDatabaseContentRefresh);
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.PropertyField(p_textAsset);
            EditorGUI.EndDisabledGroup();
        }

        #endregion

        #region Database Content List

        protected override void OnDatabaseContentListElementNameGUI(Rect rect, int index, bool selected, UnityEngine.Object obj, string name)
        {
            base.OnDatabaseContentListElementNameGUI(rect, index, selected, obj, index + ": " + name);
        }

        #endregion

        #region Context Menu

        protected override void PopulateDatabaseContentListElementContextMenu(int index, GenericMenu menu)
        {
            base.PopulateDatabaseContentListElementContextMenu(index, menu);

            if (index > 0)
            {
                menu.AddItem(new GUIContent("Move Up"), false, () => MoveElement(index, index - 1));
            }
            else
            {
                menu.AddDisabledItem(new GUIContent("Move Up"));
            }
            
            if (index < DatabaseContentListCount - 1)
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
            if (index >= 0 && index < DatabaseContentListCount && 
                newIndex >= 0 && newIndex < DatabaseContentListCount)
            {
                p_content.MoveArrayElement(index, newIndex);
                serializedObject.ApplyModifiedProperties();
                OnReorderElements();
            }
        }
        protected virtual void OnReorderElements()
        {
            ForceDatabaseContentRefresh();
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
                sb.Append("return ");
                sb.Append(databaseTypeName);
                sb.Append(".I");
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
