using UnityEngine;
using Dhs5.Utility.GUIs;
using Dhs5.Utility.Databases;
using System.Collections.Generic;
using System;

#if UNITY_EDITOR
using UnityEditor;
using Dhs5.Utility.Editors;

namespace Dhs5.Utility.Debugger
{
    public class DebuggerWindow : EditorWindow
    {
        #region Static Constructor

        [MenuItem("Window/Dhs5 Utility/Debugger", priority = 100)]
        public static void OpenWindow()
        {
            DebuggerWindow window = GetWindow<DebuggerWindow>();
            window.titleContent = new GUIContent(EditorGUIHelper.DebugIcon) { text = "Debugger" };
        }

        #endregion

        #region Asset Management

        private DebuggerAsset m_debuggerAsset;
        private DebuggerAsset Asset
        {
            get
            {
                if (m_debuggerAsset == null)
                {
                    m_debuggerAsset = DebuggerAsset.Instance;
                }
                return m_debuggerAsset;
            }
        }

        #endregion

        #region Asset Editor

        private DebuggerAssetEditor m_debuggerAssetEditor;
        private DebuggerAssetEditor AssetEditor
        {
            get
            {
                if (m_debuggerAssetEditor == null && Asset != null)
                {
                    m_debuggerAssetEditor = Editor.CreateEditor(Asset, typeof(DebuggerAssetEditor)) as DebuggerAssetEditor;
                }
                return m_debuggerAssetEditor;
            }
        }

        #endregion


        #region Members

        private int m_currentWindow;

        // RUNTIME
        private Vector2 m_runtimeDebugScrollPosition;
        private EDebugCategoryFlags m_runtimeDebugCategoryFlags = (EDebugCategoryFlags)(-1);
        private string m_runtimeDebugSearchString;
        private bool[] m_runtimeDebugOpenCategories;
        private Dictionary<UnityEngine.Object, bool> m_runtimeDebugOpenObjects = new();

        #endregion

        #region GUI Content

        private GUIContent g_title = new GUIContent("Debugger");
        private GUIContent[] g_windowOptions = new GUIContent[] { new GUIContent("Categories"), new GUIContent("Runtime"), new GUIContent("Settings") };

        #endregion


        #region Core Behaviour

        private void OnEnable()
        {
            m_runtimeDebugOpenCategories = new bool[Enum.GetValues(typeof(EDebugCategory)).Length];
            m_runtimeDebugOpenObjects.Clear();
        }
        private void OnDisable()
        {
            if (m_debuggerAssetEditor != null)
            {
                DestroyImmediate(m_debuggerAssetEditor);
            }
        }
        private void OnInspectorUpdate()
        {
            if (m_currentWindow == 1) // Runtime
            {
                Repaint();
            }
        }

        #endregion

        #region Core GUI

        private void OnGUI()
        {
            EditorGUILayout.Space(5f);
            EditorGUILayout.LabelField(g_title, GUIHelper.bigTitleLabel);

            EditorGUILayout.Space(10f);
            m_currentWindow = GUILayout.Toolbar(m_currentWindow, g_windowOptions);

            var rect = EditorGUILayout.GetControlRect(false, 2f);
            rect.x = 0f; rect.width = position.width;
            EditorGUI.DrawRect(rect, Color.white);

            if (AssetEditor != null)
            {
                AssetEditor.serializedObject.Update();
            }
            switch (m_currentWindow)
            {
                // CATEGORIES
                case 0:
                    if (Asset != null && AssetEditor != null)
                    {
                        AssetEditor.DrawCategoriesGUI();
                    }
                    else
                    {
                        EditorGUILayout.HelpBox("No active asset found", MessageType.Warning);
                    }
                    break;

                // RUNTIME
                case 1:
                    if (Application.isPlaying || true)
                    {
                        DrawRuntimeGUI();
                    }
                    else
                    {
                        EditorGUILayout.HelpBox("Usable only in play mode", MessageType.Info);
                    }
                    break;

                // SETTINGS
                case 2:
                    DrawSettingsGUI();
                    break;
            }

            if (AssetEditor != null)
            {
                AssetEditor.serializedObject.ApplyModifiedProperties();
            }
        }

        #endregion

        #region RUNTIME GUI

        private void DrawRuntimeGUI()
        {
            // TOOLBAR
            var rect = EditorGUILayout.GetControlRect(false, 20f);
            rect.x = 0f; rect.width = position.width;
            DrawRuntimeDebugToolbarGUI(rect);

            // CATEGORIES
            m_runtimeDebugScrollPosition = EditorGUILayout.BeginScrollView(m_runtimeDebugScrollPosition); 

            DrawRuntimeDebugCategoriesGUI();

            EditorGUILayout.EndScrollView();
        }

        private void DrawRuntimeDebugToolbarGUI(Rect rect)
        {
            // Background
            GUI.Box(rect, GUIContent.none, EditorStyles.toolbar);

            // Search Field
            var r_searchField = new Rect(rect.x + 4f, rect.y + 2f, rect.width * 0.6f, rect.height);
            m_runtimeDebugSearchString = EditorGUI.TextField(r_searchField, m_runtimeDebugSearchString, EditorStyles.toolbarSearchField);

            // Filters
            var r_filters = new Rect(rect.x + 6f + rect.width * 0.6f, rect.y, rect.width * 0.4f - 6f, rect.height);
            m_runtimeDebugCategoryFlags = (EDebugCategoryFlags)EditorGUI.EnumFlagsField(r_filters, m_runtimeDebugCategoryFlags, EditorStyles.toolbarDropDown);
        }

        private void DrawRuntimeDebugCategoriesGUI()
        {
            // No search
            if (string.IsNullOrWhiteSpace(m_runtimeDebugSearchString))
            {
                foreach (var (category, objects) in RuntimeDebugger.GetRegisteredObjects(m_runtimeDebugCategoryFlags))
                {
                    DrawRuntimeDebugCategoryGUI(category, objects);
                }
            }
        }
        private void DrawRuntimeDebugCategoryGUI(EDebugCategory category, IEnumerable<UnityEngine.Object> objects)
        {
            var categoryIndex = (int)category;
            var rect = EditorGUILayout.GetControlRect(false, 25f);

            // Background
            rect.x = 0f; rect.width = position.width;
            var color = DebuggerAsset.GetCategoryColor(category);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 2f), color);

            // Foldout
            var r_foldout = new Rect(rect.x + 4f, rect.y + 2f, rect.width - 10f, 20f);
            EditorStyles.foldout.fontStyle = FontStyle.Bold;
            using (new GUIHelper.GUIContentColorScope(color))
            {
                var open = EditorGUI.Foldout(r_foldout, m_runtimeDebugOpenCategories[categoryIndex], category.ToString(), true);
                if (open != m_runtimeDebugOpenCategories[categoryIndex])
                {
                    m_runtimeDebugOpenCategories[categoryIndex] = open;
                    GUI.FocusControl(null);
                }
            }
            EditorStyles.foldout.fontStyle = FontStyle.Normal;
            if (m_runtimeDebugOpenCategories[categoryIndex])
            {
                foreach (var obj in objects)
                {
                    if (obj != null)
                    {
                        DrawRuntimeDebugObjectGUI(obj);
                    }
                }

                rect = EditorGUILayout.GetControlRect(false, 2f);
                rect.x = 0f; rect.width = position.width;
                EditorGUI.DrawRect(rect, color);
            }
            else
            {
                EditorGUI.DrawRect(new Rect(rect.x, rect.y + 22f, rect.width, 2f), color);
            }
        }
        private void DrawRuntimeDebugObjectGUI(UnityEngine.Object obj)
        {
            // Init if necessary
            if (!m_runtimeDebugOpenObjects.ContainsKey(obj))
            {
                m_runtimeDebugOpenObjects[obj] = false;
            }

            EditorGUI.indentLevel++;
            m_runtimeDebugOpenObjects[obj] = EditorGUILayout.Foldout(m_runtimeDebugOpenObjects[obj], obj.name, true);
            if (m_runtimeDebugOpenObjects[obj])
            {
                EditorGUI.BeginDisabledGroup(true);

                EditorGUI.indentLevel++;
                foreach (var memberSnapshot in RuntimeDebugger.GetMemberSnapshotsOfObject(obj))
                {
                    switch (memberSnapshot.propertyType)
                    {
                        case SerializedPropertyType.Integer:
                            EditorGUILayout.IntField(memberSnapshot.name, (int)memberSnapshot.value);
                            break;

                        case SerializedPropertyType.Float:
                            EditorGUILayout.FloatField(memberSnapshot.name, (float)memberSnapshot.value);
                            break;

                        case SerializedPropertyType.Boolean:
                            EditorGUILayout.Toggle(memberSnapshot.name, (bool)memberSnapshot.value);
                            break;

                        case SerializedPropertyType.String:
                            EditorGUILayout.TextField(memberSnapshot.name, (string)memberSnapshot.value);
                            break;

                        case SerializedPropertyType.Color:
                            EditorGUILayout.ColorField(memberSnapshot.name, (Color)memberSnapshot.value);
                            break;

                        case SerializedPropertyType.ObjectReference:
                            var unityObj = (UnityEngine.Object)memberSnapshot.value;
                            EditorGUILayout.ObjectField(memberSnapshot.name, unityObj, unityObj.GetType(), true);
                            break;

                        case SerializedPropertyType.LayerMask:
                            EditorGUILayout.LayerField(memberSnapshot.name, (LayerMask)memberSnapshot.value);
                            break;

                        case SerializedPropertyType.Enum:
                            EditorGUILayout.EnumPopup(memberSnapshot.name, (Enum)memberSnapshot.value);
                            break;

                        case SerializedPropertyType.Vector2:
                            EditorGUILayout.Vector2Field(memberSnapshot.name, (Vector2)memberSnapshot.value);
                            break;

                        case SerializedPropertyType.Vector3:
                            EditorGUILayout.Vector3Field(memberSnapshot.name, (Vector3)memberSnapshot.value);
                            break;

                        case SerializedPropertyType.Vector4:
                            EditorGUILayout.Vector4Field(memberSnapshot.name, (Vector4)memberSnapshot.value);
                            break;

                        case SerializedPropertyType.Rect:
                            EditorGUILayout.RectField(memberSnapshot.name, (Rect)memberSnapshot.value);
                            break;

                        case SerializedPropertyType.Character:
                            EditorGUILayout.TextField(memberSnapshot.name, ((char)memberSnapshot.value).ToString());
                            break;

                        case SerializedPropertyType.AnimationCurve:
                            EditorGUILayout.CurveField(memberSnapshot.name, (AnimationCurve)memberSnapshot.value);
                            break;

                        case SerializedPropertyType.Bounds:
                            EditorGUILayout.BoundsField(memberSnapshot.name, (Bounds)memberSnapshot.value);
                            break;

                        case SerializedPropertyType.Gradient:
                            EditorGUILayout.GradientField(memberSnapshot.name, (Gradient)memberSnapshot.value);
                            break;

                        case SerializedPropertyType.Quaternion:
                            var quaternion = (Quaternion)memberSnapshot.value;
                            EditorGUILayout.Vector4Field(memberSnapshot.name, new Vector4(quaternion.x, quaternion.y, quaternion.z, quaternion.w));
                            break;

                        case SerializedPropertyType.Vector2Int:
                            EditorGUILayout.Vector2IntField(memberSnapshot.name, (Vector2Int)memberSnapshot.value);
                            break;

                        case SerializedPropertyType.Vector3Int:
                            EditorGUILayout.Vector3IntField(memberSnapshot.name, (Vector3Int)memberSnapshot.value);
                            break;

                        case SerializedPropertyType.RectInt:
                            EditorGUILayout.RectIntField(memberSnapshot.name, (RectInt)memberSnapshot.value);
                            break;

                        case SerializedPropertyType.BoundsInt:
                            EditorGUILayout.BoundsIntField(memberSnapshot.name, (BoundsInt)memberSnapshot.value);
                            break;

                        case SerializedPropertyType.RenderingLayerMask:
                            EditorGUILayout.RenderingLayerMaskField(memberSnapshot.name, (RenderingLayerMask)memberSnapshot.value);
                            break;

                        case SerializedPropertyType.EntityId:
                            EditorGUILayout.IntField(memberSnapshot.name, (EntityId)memberSnapshot.value);
                            break;
                    }
                }
                EditorGUI.indentLevel--;

                EditorGUI.EndDisabledGroup();
            }
            EditorGUI.indentLevel--;
        }

        #endregion

        #region SETTINGS GUI

        private void DrawSettingsGUI()
        {
            EditorGUILayout.Space(10f);
            EditorGUILayout.LabelField("Asset", EditorStyles.boldLabel);

            // Assets
            var array = Resources.LoadAll<DebuggerAsset>("Debugger");
            if (array != null && array.Length > 0)
            {
                if (array.Length == 1)
                {
                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.ObjectField(array[0], typeof(DebuggerAsset), false);
                    EditorGUI.EndDisabledGroup();
                }
                else
                {
                    EditorGUILayout.HelpBox("Too much Debugger Assets", MessageType.Error);
                    for (int i = 0; i < array.Length; i++)
                    {
                        var rect = EditorGUILayout.GetControlRect(false, 22f);

                        EditorGUI.BeginDisabledGroup(true);
                        var objectRect = new Rect(rect.x, rect.y, rect.width - 32f, rect.height - 2f);
                        EditorGUI.ObjectField(objectRect, array[i], typeof(DebuggerAsset), false);
                        EditorGUI.EndDisabledGroup();

                        var deleteButtonRect = new Rect(rect.x + rect.width - 30f, rect.y + 1f, 30f, rect.height - 2f);
                        using (new GUIHelper.GUIBackgroundColorScope(Color.red))
                        {
                            if (GUI.Button(deleteButtonRect, EditorGUIHelper.DeleteIcon))
                            {
                                Database.DeleteAsset(array[i], true);
                                AssetDatabase.SaveAssets();
                            }
                        }
                    }
                }
            }
            else
            {
                EditorGUILayout.HelpBox("No assets found", MessageType.Warning);
                EditorGUILayout.Space(5f);

                // Create asset button
                if (GUILayout.Button("Create new asset"))
                {
                    Database.CreateAssetOfType(typeof(DebuggerAsset), "Assets/Resources/Debugger/Debugger.asset");
                    AssetDatabase.SaveAssets();
                }
            }

            // Assets Settings
            if (AssetEditor != null)
            {
                EditorGUILayout.Space(5f);
                AssetEditor.DrawSettingsGUI();
            }
        }

        #endregion
    }
}

#endif