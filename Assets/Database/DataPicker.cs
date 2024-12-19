using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Dhs5.Utility.Databases
{
    [Serializable]
    public class DataPicker
    {
        #region Members

        [SerializeField] private BaseDataContainer m_container;
        [SerializeField] private int m_currentSelectionUID = 0;

        #endregion

        #region Methods

        public bool TryGetObject(out IDataContainerElement obj)
        {
            if (m_container != null && m_currentSelectionUID > 0)
            {
                return m_container.TryGetObjectByUID(m_currentSelectionUID, out obj);
            }
            obj = null;
            return false;
        }
        public bool TryGetObject<T>(out T objOfTypeT) where T : UnityEngine.Object, IDataContainerElement
        {
            if (m_container != null && m_currentSelectionUID > 0)
            {
                if (m_container.TryGetObjectByUID(m_currentSelectionUID, out var obj)
                    && obj is T t)
                {
                    objOfTypeT = t;
                    return true;
                }
            }
            objOfTypeT = null;
            return false;
        }

        #endregion
    }

    [Serializable]
    public class DataPicker<DatabaseType> where DatabaseType : BaseDataContainer
    {
        #region Members

        [SerializeField] private DatabaseType m_container;
        [SerializeField] private int m_currentSelectionUID = 0;

        #endregion

        #region Methods

        public bool TryGetObject(out IDataContainerElement obj)
        {
            if (m_container != null && m_currentSelectionUID > 0)
            {
                return m_container.TryGetObjectByUID(m_currentSelectionUID, out obj);
            }
            obj = null;
            return false;
        }
        public bool TryGetObject<T>(out T objOfTypeT) where T : UnityEngine.Object, IDataContainerElement
        {
            if (m_container != null && m_currentSelectionUID > 0)
            {
                if (m_container.TryGetObjectByUID(m_currentSelectionUID, out var obj)
                    && obj is T t)
                {
                    objOfTypeT = t;
                    return true;
                }
            }
            objOfTypeT = null;
            return false;
        }

        #endregion
    }

    #region Drawer

#if UNITY_EDITOR

    [CustomPropertyDrawer(typeof(DataPicker), useForChildren:true)]
    [CustomPropertyDrawer(typeof(DataPicker<>), useForChildren:true)]
    public class DataPickerDrawer : PropertyDrawer
    {
        #region Serialized Properties

        protected SerializedProperty p_container;
        protected SerializedProperty p_currentSelectionUID;

        protected virtual void GetSerializedProperties(SerializedProperty property)
        {
            p_container = property.FindPropertyRelative("m_container");
            p_currentSelectionUID = property.FindPropertyRelative("m_currentSelectionUID");
        }

        #endregion

        #region DataPicker Type

        protected bool IsDatabaseDataPicker(out Type databaseType)
        {
            Type fieldType = fieldInfo.FieldType;
            if (fieldType.IsGenericType)
            {
                databaseType = fieldType.GetGenericArguments()[0];
                return true;
            }
            if (fieldType.BaseType.IsGenericType)
            {
                databaseType = fieldType.BaseType.GetGenericArguments()[0];
                return true;
            }

            databaseType = null;
            return false;
        }

        #endregion

        #region GUI

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            Rect totalRect = new Rect(position.x, position.y, position.width, GetPropertyHeight(property, label));

            GetSerializedProperties(property);

            EditorGUI.BeginProperty(totalRect, label, property);

            Rect rect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);

            bool isDatabaseDataPicker = IsDatabaseDataPicker(out var databaseType);

            // DATABASE VERSION
            if (isDatabaseDataPicker)
            {
                // CONTAINER
                p_container.objectReferenceValue = Database.Get(databaseType);
                // PICKER
                if (p_container.objectReferenceValue is BaseDataContainer database)
                {
                    (var names, var uids) = database.Editor_GetContainerDisplayContent();
                    p_currentSelectionUID.intValue = EditorGUI.IntPopup(rect, label.text, p_currentSelectionUID.intValue, names, uids);
                }
                else
                {
                    EditorGUI.LabelField(rect, label, new GUIContent("Can't find instance of type " + databaseType));
                }
            }
            // GENERIC VERSION
            else
            {
                // CONTAINER
                EditorGUI.PropertyField(rect, p_container, label, true);
                rect.y += rect.height;
                // PICKER
                if (p_container.objectReferenceValue is BaseDataContainer dataContainer)
                {
                    (var names, var uids) = dataContainer.Editor_GetContainerDisplayContent();
                    var pickerRect = new Rect(rect.x + EditorGUIUtility.labelWidth, rect.y, rect.width - EditorGUIUtility.labelWidth, rect.height);
                    p_currentSelectionUID.intValue = EditorGUI.IntPopup(pickerRect, p_currentSelectionUID.intValue, names, uids);
                }
            }

            EditorGUI.EndProperty();
        }

        #endregion

        #region GUI Height

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            GetSerializedProperties(property);

            if (IsDatabaseDataPicker(out _))
            {
                return 22f;
            }
            else
            {
                if (p_container.objectReferenceValue != null) return 42f;
                return 22f;
            }
        }

        #endregion

    }

#endif

    #endregion
}
