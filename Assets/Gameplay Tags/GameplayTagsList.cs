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
    public class GameplayTagsList : IEnumerable<int>
    {
        #region Constructor

        public GameplayTagsList(HashSet<int> tags)
        {
            m_tags = new();
            foreach (var tag in tags)
            {
                m_tags.Add(tag);
            }
        }

        #endregion

        #region Members

        [SerializeField] private List<int> m_tags;

        #endregion

        #region Properties

        public int Count => m_tags != null ? m_tags.Count : 0;

        #endregion

        #region Setters

        public void Set(HashSet<int> tags)
        {
            m_tags.Clear();
            foreach (var tag in tags)
            {
                m_tags.Add(tag);
            }
        }

        #endregion

        #region IEnumerable<int>

        public IEnumerator<int> GetEnumerator()
        {
            foreach (var tag in m_tags)
            {
                yield return tag; 
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        #region Utility

        public bool Contains(int uid)
        {
            return m_tags.Contains(uid);
        }

        #endregion
    }

    #region Extensions

    public static class GameplayTagsListExtension
    {
        public static bool IsValid(this GameplayTagsList gameplayTagsList)
        {
            return gameplayTagsList != null && gameplayTagsList.Count > 0;
        }
    }

    #endregion

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
            if (Event.current.type == EventType.ExecuteCommand 
                && Event.current.commandName == GameplayTagsSelector.CommandName 
                && GameplayTagsSelector.CurrentID == id)
            {
                Event.current.Use();

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
                GameplayTagsSelector.OpenTagsSelector(id, tags);
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
