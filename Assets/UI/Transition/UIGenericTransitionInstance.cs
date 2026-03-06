using System;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Dhs5.Utility.UI
{
    [Serializable]
    public class UIGenericTransitionInstance
    {
        #region Members

        [SerializeField] private UIGenericTransitionAsset m_asset;
        [SerializeField] private int m_presetIndex;

        private Dictionary<Graphic, object> m_initialValues = new();
        private IUIGenericTransitionPayload m_payload;

        #endregion

        #region Properties

        public int PresetIndex => m_presetIndex;
        public IUIGenericTransitionPayload Payload => m_payload;
        public T GetInitialValue<T>(Graphic graphic)
        {
            return (T)m_initialValues[graphic];
        }

        #endregion

        #region Methods

        public void UpdateState(IEnumerable<Graphic> graphics, FUIState oldStates, FUIState newStates, bool instant, IUITransitionParam param)
        {
            if (m_asset == null) return;

            foreach (var g in graphics)
            {
                if (!m_initialValues.ContainsKey(g))
                {
                    m_initialValues[g] = m_asset.GetGraphicInitialValue(g);
                }
            }

            m_payload = m_asset.UpdateState(this, graphics, oldStates, newStates, instant, param);
        }

        #endregion
    }

    #region Drawer

#if UNITY_EDITOR

    [CustomPropertyDrawer(typeof(UIGenericTransitionInstance))]
    public class UIGenericTransitionInstanceDrawer : PropertyDrawer
    {
        #region Members

        private string[] presetNames;
        private int[] presetIndexes;

        GUIContent g_preset = new GUIContent("Preset", "You can create new presets in the asset");

        #endregion

        #region GUI

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var p_asset = property.FindPropertyRelative("m_asset");
            var p_presetIndex = property.FindPropertyRelative("m_presetIndex");

            EditorGUI.BeginProperty(position, label, property);

            var formerAsset = p_asset.objectReferenceValue as UIGenericTransitionAsset;

            EditorGUI.BeginChangeCheck();
            EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, 18f), p_asset, true);
            var hasData = p_asset.objectReferenceValue != null;
            if (EditorGUI.EndChangeCheck())
            {
                p_presetIndex.intValue = hasData ? 0 : -1;

                if (!hasData)
                {
                    p_presetIndex.isExpanded = false;

                    if (formerAsset != null)
                        DestroySerializedObject(formerAsset);
                }
            }

            if (hasData && p_asset.objectReferenceValue is UIGenericTransitionAsset asset)
            {
                var so = GetOrCreateSerializedObject(asset);

                var p_presets = so.FindProperty("m_presets");
                if (presetNames == null || presetNames.Length != p_presets.arraySize)
                {
                    presetNames = new string[p_presets.arraySize];
                }

                for (int i = 0; i < presetNames.Length; i++)
                {
                    presetNames[i] = p_presets.GetArrayElementAtIndex(i).FindPropertyRelative("m_name").stringValue;
                }

                if (presetIndexes == null || presetIndexes.Length != presetNames.Length)
                {
                    presetIndexes = new int[presetNames.Length];
                    for (int i = 0; i < presetIndexes.Length; i++)
                    {
                        presetIndexes[i] = i;
                    }
                }

                var r_foldout = new Rect(position.x, position.y + 20f, EditorGUIUtility.labelWidth, 18f);
                p_presetIndex.isExpanded = EditorGUI.Foldout(r_foldout, p_presetIndex.isExpanded, g_preset, true);

                var r_presetIndex = new Rect(position.x + EditorGUIUtility.labelWidth + 2f, position.y + 20f, position.width - EditorGUIUtility.labelWidth - 2f, 18f);
                p_presetIndex.intValue = EditorGUI.IntPopup(r_presetIndex, p_presetIndex.intValue, presetNames, presetIndexes);

                if (p_presetIndex.isExpanded && p_presetIndex.intValue >= 0)
                {
                    EditorGUI.indentLevel++;

                    var p_preset = p_presets.GetArrayElementAtIndex(p_presetIndex.intValue);
                    var r_preset = new Rect(position.x, position.y + 40f, position.width, EditorGUI.GetPropertyHeight(p_preset));

                    EditorGUI.BeginDisabledGroup(true);

                    var p_stateOrder = p_preset.FindPropertyRelative("m_stateOrder");
                    EditorGUI.PropertyField(r_preset, p_stateOrder, true); r_preset.y += EditorGUI.GetPropertyHeight(p_stateOrder) + 2f;
                    EditorGUI.PropertyField(r_preset, p_preset.FindPropertyRelative("m_normalState"), true); r_preset.y += 20f;
                    EditorGUI.PropertyField(r_preset, p_preset.FindPropertyRelative("m_highlightedState"), true); r_preset.y += 20f;
                    EditorGUI.PropertyField(r_preset, p_preset.FindPropertyRelative("m_pressedState"), true); r_preset.y += 20f;
                    EditorGUI.PropertyField(r_preset, p_preset.FindPropertyRelative("m_selectedState"), true); r_preset.y += 20f;
                    EditorGUI.PropertyField(r_preset, p_preset.FindPropertyRelative("m_disabledState"), true); r_preset.y += 20f;

                    EditorGUI.EndDisabledGroup();

                    EditorGUI.indentLevel--;
                }
            }

            EditorGUI.EndProperty();
        }
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (property.FindPropertyRelative("m_asset").objectReferenceValue is UIGenericTransitionAsset asset)
            {
                var p_presetIndex = property.FindPropertyRelative("m_presetIndex");
                if (p_presetIndex.isExpanded
                    && TryGetSerializedObject(asset, out var so)
                    && so != null)
                {
                    return 40f 
                        + EditorGUI.GetPropertyHeight(
                            so.FindProperty("m_presets").GetArrayElementAtIndex(p_presetIndex.intValue).FindPropertyRelative("m_stateOrder"))
                        + 102f;
                }
                return 40f;
            }
            return 20f;
        }

        #endregion

        #region Serialized Objects

        private static Dictionary<UIGenericTransitionAsset, SerializedObject> _assetSerializedObjects = new();

        private static SerializedObject GetOrCreateSerializedObject(UIGenericTransitionAsset asset)
        {
            if (_assetSerializedObjects.TryGetValue(asset, out var so) && so != null)
            {
                so.Update();
                return so;
            }

            so = new SerializedObject(asset);
            _assetSerializedObjects[asset] = so;
            return so;
        }
        private static bool TryGetSerializedObject(UIGenericTransitionAsset asset, out SerializedObject so)
        {
            return _assetSerializedObjects.TryGetValue(asset, out so);
        }
        private static void DestroySerializedObject(UIGenericTransitionAsset asset)
        {
            if (_assetSerializedObjects.TryGetValue(asset, out var so) && so != null)
            {
                so.Dispose();
                _assetSerializedObjects.Remove(asset);
            }
        }

        #endregion
    }

#endif

    #endregion

    #region Payload

    public interface IUIGenericTransitionPayload { }

    public struct UITransitionTweenPayload : IUIGenericTransitionPayload
    {
        public UITransitionTweenPayload(List<UITransitionTween> tweens) { m_tweens = tweens; }

        private readonly List<UITransitionTween> m_tweens;

        public readonly IEnumerable<UITransitionTween> Tweens
        {
            get
            {
                if (m_tweens != null)
                {
                    foreach (var t in m_tweens)
                    {
                        yield return t;
                    }
                }
            }
        }
    }

    #endregion
}
