using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

#if UNITY_EDITOR
using UnityEditor;
using System.Reflection;
using System.IO;
#endif

namespace Dhs5.Utility.Databases
{
    public abstract class BaseDatabase : ScriptableObject
    {
        #region Instance

        private static Dictionary<Type, BaseDatabase> _instances = new();
        internal static BaseDatabase GetInstance(Type type)
        {
            if (!type.IsSubclassOf(typeof(BaseDatabase))) return null;

            if (!_instances.TryGetValue(type, out var instance)
                || instance == null)
            {
                var list = Resources.LoadAll("Databases", type);

                if (list != null && list.Length > 0)
                {
                    instance = list[0] as BaseDatabase;
                    _instances[type] = instance;
                }
#if UNITY_EDITOR
                else
                {
                    instance = CreateInstance(type) as BaseDatabase;

                    if (!Directory.Exists(Application.dataPath + "/Resources/Databases"))
                    {
                        if (!Directory.Exists(Application.dataPath + "/Resources"))
                        {
                            Directory.CreateDirectory(Application.dataPath + "/Resources");
                        }
                        Directory.CreateDirectory(Application.dataPath + "/Resources/Databases");
                    }

                    AssetDatabase.CreateAsset(instance, "Assets/Resources/Databases/" + type.Name + ".asset");
                    AssetDatabase.SaveAssets();
                }
#endif
            }

            return instance;
        }

        internal static BaseDatabase[] GetAllInstances() => GetAllInstances(GetAllChildTypes());
        internal static BaseDatabase[] GetAllInstances(Func<Type, bool> predicate) => GetAllInstances(GetAllChildTypes(t => predicate.Invoke(t)));
        private static BaseDatabase[] GetAllInstances(Type[] childTypes)
        {
            BaseDatabase[] settings = new BaseDatabase[childTypes.Length];

            for (int i = 0; i < settings.Length; i++)
            {
                settings[i] = GetInstance(childTypes[i]);
            }

            return settings;
        }

        #endregion

        #region Editor Utility

#if UNITY_EDITOR

        internal string Editor_GetPath()
        {
            return GetPath(GetType());
        }

        protected virtual bool Editor_IsElementValid(UnityEngine.Object element)
        {
            if (element == null) return false;

            if (HasDataType(GetType(), out var type))
            {
                Type elementType = element.GetType();
                return elementType == type || elementType.IsSubclassOf(type);
            }
            return true;
        }

        internal virtual IEnumerable<UnityEngine.Object> Editor_GetDatabaseContent()
        {
            yield return null;
        }

#endif

        #endregion

        #region Editor Functions

#if UNITY_EDITOR

        #region Statics

        private static Dictionary<Type, DatabaseAttribute> _attributes = new();
        protected static bool TryGetAttribute(Type type, out DatabaseAttribute attribute)
        {
            if (_attributes.TryGetValue(type, out attribute))
            {
                return true;
            }

            attribute = type.GetCustomAttribute<DatabaseAttribute>(inherit: true);

            if (attribute != null)
            {
                _attributes.Add(type, attribute);
                return true;
            }
            return false;
        }
        internal static string GetPath(Type type)
        {
            if (TryGetAttribute(type, out var attribute))
            {
                return attribute.path;
            }
            return "Null path";
        }
        internal static bool HasDataType(Type type, out Type dataType)
        {
            if (TryGetAttribute(type, out var attribute))
            {
                dataType = attribute.dataType;
                return !attribute.anyType;
            }
            dataType = null;
            return false;
        }

        private static Type[] GetAllChildTypes()
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t => t.IsSubclassOf(typeof(BaseDatabase)) && !t.IsAbstract && TryGetAttribute(t, out _))
                .ToArray();
        }
        private static Type[] GetAllChildTypes(Func<Type, bool> predicate)
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t => t.IsSubclassOf(typeof(BaseDatabase)) && !t.IsAbstract && TryGetAttribute(t, out _) && predicate.Invoke(t))
                .ToArray();
        }

        #endregion

        #region Callbacks

        internal event Action Editor_DatabaseContentChanged;

        internal virtual void Editor_ShouldRecomputeDatabaseContent()
        {
            Editor_DatabaseContentChanged?.Invoke();
        }

        #endregion

#endif

        #endregion
    }

    public abstract class Database<T> : BaseDatabase where T : Database<T>
    {
        #region Instance

        private static T _instance;
        public static T I
        {
            get
            {
                if (_instance == null)
                {
                    if (GetInstance(typeof(T)) is T t)
                    {
                        _instance = t;
                    }
                }

                return _instance;
            }
        }

        #endregion
    }

    #region Editor

#if UNITY_EDITOR

    [CustomEditor(typeof(BaseDatabase), editorForChildClasses:true)]
    public class BaseDatabaseEditor : Editor
    {
        #region Members

        protected BaseDatabase m_database;

        protected SerializedProperty p_script;

        protected List<string> m_excludedProperties;

        #endregion

        #region Properties

        // Database Informations
        protected bool DatabaseInformationsFoldoutOpen { get; set; }

        // Database Content List Window
        private List<UnityEngine.Object> m_databaseContentList;
        protected int DatabaseContentListCount => m_databaseContentList != null ? m_databaseContentList.Count : 0;
        protected int DatabaseContentListSelectionIndex { get; set; } = -1;
        protected Vector2 DatabaseContentListWindowScrollPos { get; set; }
        protected float DatabaseContentListElementHeight { get; set; } = 20f;

        // Database Element Display
        private Dictionary<UnityEngine.Object, Editor> m_editors = new();
        protected Vector2 DatabaseElementDisplayScrollPos { get; set; }

        #endregion

        #region Core Behaviour

        protected virtual void OnEnable()
        {
            m_database = (BaseDatabase)target;
            m_database.Editor_DatabaseContentChanged += OnDatabaseContentChanged;

            p_script = serializedObject.FindProperty("m_Script");

            m_excludedProperties = new()
            {
                p_script.propertyPath,
            };
        }
        protected virtual void OnDisable()
        {
            m_database.Editor_DatabaseContentChanged -= OnDatabaseContentChanged;
        }

        #endregion

        #region Event Callbacks

        protected virtual void OnDatabaseContentChanged()
        {
            m_databaseContentList = m_database.Editor_GetDatabaseContent().ToList();
        }

        #endregion

        #region Base GUI

        public sealed override void OnInspectorGUI()
        {
            serializedObject.Update();

            OnGUI();

            serializedObject.ApplyModifiedProperties();
        }

        protected virtual void OnGUI()
        {
            DrawDefault();
        }
        protected void DrawDefault()
        {
            DrawPropertiesExcluding(serializedObject, m_excludedProperties.ToArray());
        }

        #endregion

        #region Helper GUI

        #region Database Informations

        protected virtual void OnDatabaseInformationsGUI()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            DatabaseInformationsFoldoutOpen = EditorGUILayout.Foldout(DatabaseInformationsFoldoutOpen, "Database Informations", true, EditorGUIHelper.foldoutStyle);
            if (DatabaseInformationsFoldoutOpen)
            {
                EditorGUI.indentLevel++;

                EditorGUILayout.Space(5f);
                OnDatabaseInformationsContentGUI();

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
        }
        protected virtual void OnDatabaseInformationsContentGUI() { }

        #endregion

        #region Database Content List

        protected void ForceDatabaseContentRefresh() { m_database.Editor_ShouldRecomputeDatabaseContent(); }

        protected UnityEngine.Object GetDatabaseContentListAtIndex(int index)
        {
            if (index >= 0 && index < DatabaseContentListCount)
            {
                return m_databaseContentList[index];
            }
            return null;
        }

        protected virtual void OnDatabaseContentListWindowGUI(Rect rect)
        {
            float refreshButtonHeight = 20f;
            Rect listRect = new Rect(rect.x, rect.y, rect.width, rect.height - refreshButtonHeight);
            EditorGUI.DrawRect(listRect, EditorGUIHelper.transparentBlack01);

            bool needScrollRect = DatabaseContentListCount * DatabaseContentListElementHeight > listRect.height;
            if (needScrollRect)
            {
                Rect viewRect = new Rect(0, 0, listRect.width - 15f, DatabaseContentListCount * DatabaseContentListElementHeight);
                DatabaseContentListWindowScrollPos = GUI.BeginScrollView(listRect, DatabaseContentListWindowScrollPos, viewRect);

                Rect dataRect = new Rect(0, 0, viewRect.width, DatabaseContentListElementHeight);
                for (int i = 0; i < DatabaseContentListCount; i++)
                {
                    OnDatabaseContentListElementGUI(dataRect, i, GetDatabaseContentListAtIndex(i));
                    dataRect.y += DatabaseContentListElementHeight;
                }

                GUI.EndScrollView();
            }
            else
            {
                Rect dataRect = new Rect(listRect.x, listRect.y, listRect.width, DatabaseContentListElementHeight);
                for (int i = 0; i < DatabaseContentListCount; i++)
                {
                    OnDatabaseContentListElementGUI(dataRect, i, GetDatabaseContentListAtIndex(i));
                    dataRect.y += DatabaseContentListElementHeight;
                }
            }

            // Refresh Button
            Rect refreshButtonRect = new Rect(rect.x, rect.y + rect.height - refreshButtonHeight, rect.width, refreshButtonHeight);
            if (GUI.Button(refreshButtonRect, new GUIContent(EditorGUIHelper.RefreshIcon) { text = "Refresh" }))
            {
                ForceDatabaseContentRefresh();
            }
        }
        protected virtual void OnDatabaseContentListElementGUI(Rect rect, int index, UnityEngine.Object element)
        {
            bool selected = DatabaseContentListSelectionIndex == index;

            if (GUI.Button(rect, GUIContent.none, new GUIStyle()))
            {
                if (selected)
                {
                    DatabaseContentListSelectionIndex = -1;
                    OnDeselectDatabaseContentListElement(element);
                    selected = false;
                }
                else
                {
                    DatabaseContentListSelectionIndex = index;
                    OnSelectDatabaseContentListElement(element);
                    selected = true;
                }
            }
            
            OnDatabaseContentListElementBackgroundGUI(rect, index, selected, element);

            switch (element)
            {
                case null:
                    OnDatabaseContentListNullElementGUI(rect, index, element); break;
                case GameObject go:
                    OnDatabaseContentListGameObjectElementGUI(rect, index, selected, go); break;
                case ScriptableObject so:
                    OnDatabaseContentListScriptableObjectElementGUI(rect, index, selected, so); break;
                default:
                    OnDatabaseContentListOtherObjectElementGUI(rect, index, selected, element); break;
            }
        }
        protected virtual void OnDatabaseContentListElementBackgroundGUI(Rect rect, int index, bool selected, UnityEngine.Object element)
        {
            EditorGUI.DrawRect(rect, selected ? Color.grey : (index % 2 == 0 ? EditorGUIHelper.transparentBlack02 : EditorGUIHelper.transparentBlack04));
        }

        protected virtual void OnDatabaseContentListNullElementGUI(Rect rect, int index, bool selected)
        {
            Rect labelRect = new Rect(rect.x + 5f, rect.y, rect.width - 5f, rect.height);
            EditorGUI.LabelField(labelRect, "Null");
        }
        protected virtual void OnDatabaseContentListGameObjectElementGUI(Rect rect, int index, bool selected, GameObject element)
        {
            Rect labelRect = new Rect(rect.x + 5f, rect.y, rect.width - 5f, rect.height);
            EditorGUI.LabelField(labelRect, element.name);
        }
        protected virtual void OnDatabaseContentListScriptableObjectElementGUI(Rect rect, int index, bool selected, ScriptableObject element)
        {
            Rect labelRect = new Rect(rect.x + 5f, rect.y, rect.width - 5f, rect.height);
            EditorGUI.LabelField(labelRect, element.name);
        }
        protected virtual void OnDatabaseContentListOtherObjectElementGUI(Rect rect, int index, bool selected, UnityEngine.Object obj)
        {
            Rect labelRect = new Rect(rect.x + 5f, rect.y, rect.width - 5f, rect.height);
            EditorGUI.LabelField(labelRect, obj.name);
        }


        protected virtual void OnSelectDatabaseContentListElement(UnityEngine.Object element) { }
        protected virtual void OnDeselectDatabaseContentListElement(UnityEngine.Object element) { }

        #endregion

        #region Editor Handling

        protected Editor GetOrCreateEditorFor(UnityEngine.Object element)
        {
            if (m_editors.TryGetValue(element, out Editor editor)
                && editor != null)
            {
                return editor;
            }

            editor = CreateEditor(element);
            m_editors[element] = editor;
            return editor;
        }
        protected bool ShowElementEditorIfPossible(UnityEngine.Object element)
        {
            if (element == null) return false;

            var editor = GetOrCreateEditorFor(element);
            if (editor != null)
            {
                editor.OnInspectorGUI();
                return true;
            }
            return false;
        }

        #endregion

        #region Database Element Display

        protected bool DisplayCurrentDatabaseContentListSelection()
        {
            if (DatabaseContentListSelectionIndex != -1)
            {
                DisplayDatabaseElement(GetDatabaseContentListAtIndex(DatabaseContentListSelectionIndex));
                return true;
            }
            return false;
        }
        protected virtual void DisplayDatabaseElement(UnityEngine.Object element)
        {
            DatabaseElementDisplayScrollPos = EditorGUILayout.BeginScrollView(DatabaseElementDisplayScrollPos);

            switch (element)
            {
                case null:
                    OnDisplayDatabaseNullElement(); break;
                case GameObject go:
                    OnDisplayDatabaseGameObjectElement(go); break;
                case ScriptableObject so:
                    OnDisplayDatabaseScriptableObjectElement(so); break;
                default:
                    OnDisplayDatabaseOtherObjectElement(element); break;
            }

            EditorGUILayout.EndScrollView();
        }

        protected virtual void OnDisplayDatabaseNullElement()
        {

        }
        protected virtual void OnDisplayDatabaseGameObjectElement(GameObject element)
        {
            ShowElementEditorIfPossible(element);
        }
        protected virtual void OnDisplayDatabaseScriptableObjectElement(ScriptableObject element)
        {
            ShowElementEditorIfPossible(element);
        }
        protected virtual void OnDisplayDatabaseOtherObjectElement(UnityEngine.Object element)
        {
            ShowElementEditorIfPossible(element);
        }

        #endregion

        #region Data Creation

        protected virtual bool CreateNewData(string path)
        {
            if (BaseDatabase.HasDataType(m_database.GetType(), out Type dataType))
            {
                path = AssetDatabase.GenerateUniqueAssetPath(path);

                if (dataType.IsSubclassOf(typeof(ScriptableObject)))
                {
                    return OnCreateNewScriptableObject(path, dataType);
                }
                if (dataType.IsSubclassOf(typeof(MonoBehaviour)))
                {
                    return OnCreateNewMonoBehaviour(path, dataType);
                }
                if (dataType == typeof(GameObject))
                {
                    return OnCreateNewEmptyPrefab(path);
                }
            }
            return false;
        }

        protected virtual bool OnCreateNewEmptyPrefab(string path)
        {
            PrefabUtility.SaveAsPrefabAsset(null, path, out var success);
            return success;
        }
        protected virtual bool OnCreateNewMonoBehaviour(string path, Type behaviourType)
        {
            return false;
        }
        protected virtual bool OnCreateNewScriptableObject(string path, Type scriptableType)
        {
            var obj = ScriptableObject.CreateInstance(scriptableType);
            AssetDatabase.CreateAsset(obj, path);
            AssetDatabase.SaveAssets();
            return true;
        }

        #endregion

        #region Decoratives

        protected void Separator(float height, Color color)
        {
            EditorGUI.DrawRect(EditorGUILayout.GetControlRect(false, height), color);
        }

        #endregion

        #endregion
    }

#endif

    #endregion
}
