using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using System.Text;
using System.Linq;
using Dhs5.Utility.Databases;
#endif

namespace Dhs5.Utility.Tags
{
    [Serializable]
    public class GameplayTagsList
    {
        #region Members

        [SerializeField] private List<int> m_tags;

        #endregion
    }

    #region Drawer

#if UNITY_EDITOR

    [CustomPropertyDrawer(typeof(GameplayTagsList))]
    public class GameplayTagsListDrawer : PropertyDrawer
    {
        #region GUI

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var id = GetTagsSelectorID(property);

            var p_tags = property.FindPropertyRelative("m_tags");

            // Events
            if (GameplayTagsSelector.TagsUpdated && GameplayTagsSelector.CurrentID == id)
            {
                var updatedTags = GameplayTagsSelector.GetUpdatedTags().ToList();
                p_tags.arraySize = updatedTags.Count;
                for (int i = 0; i < updatedTags.Count; i++)
                {
                    p_tags.GetArrayElementAtIndex(i).intValue = updatedTags[i];
                }
            }

            // Tags List computation
            var arraySize = p_tags.arraySize;
            List<int> tags = new();
            StringBuilder sb = new();
            var db = Database.Get<GameplayTagsDatabase>();
            if (arraySize > 0)
            {
                for (int i = 0; i < arraySize; i++)
                {
                    if (i > 0) sb.Append(", ");
                    tags.Add(p_tags.GetArrayElementAtIndex(i).intValue);
                    sb.Append(db.Editor_GetTagNameByUID(tags[i]));
                }
            }
            else
            {
                sb.Append("--- EMPTY ---");
            }

            EditorGUI.BeginProperty(position, label, property);

            // Label
            Rect labelRect = new Rect(position.x, position.y, EditorGUIUtility.labelWidth, 18f);
            EditorGUI.LabelField(labelRect, label);

            // Button
            Rect buttonRect = new Rect(position.x + EditorGUIUtility.labelWidth + 2f, position.y, position.width - EditorGUIUtility.labelWidth - 2f, 18f);
            if (GUI.Button(buttonRect, EditorGUIUtility.TrTempContent(sb.ToString()), EditorStyles.popup))
            {
                Vector2 mousePos = EditorGUIUtility.GUIToScreenPoint(Event.current.mousePosition);
                GameplayTagsSelector.OpenTagsSelector(id, mousePos, tags);
            }

            EditorGUI.EndProperty();
        }

        #endregion

        #region GUI Height

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return 20f;
        }

        #endregion

        #region Utility

        private int GetTagsSelectorID(SerializedProperty property)
        {
            return property.serializedObject.targetObject.GetInstanceID() + property.propertyPath.GetHashCode();
        }

        #endregion
    }

#endif

    #endregion
}
