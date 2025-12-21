using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using Dhs5.Utility.Editors;
#endif

namespace Dhs5.Utility.SaveLoad
{
    public sealed class SaveObject : ScriptableObject
    {
        #region STRUCT SaveWrapper

        [Serializable]
        private struct SaveWrapper
        {
            public SaveWrapper(ICollection<BaseSaveSubObject> saveSubObjects)
            {
                subWrappers = new();

                foreach (var subObject in saveSubObjects)
                {
                    subWrappers.Add(new SubSaveWrapper(subObject));
                }
            }

            public List<SubSaveWrapper> subWrappers;
        }

        #endregion

        #region STRUCT SubSaveWrapper

        [Serializable]
        private struct SubSaveWrapper
        {
            public SubSaveWrapper(BaseSaveSubObject subObject)
            {
                typeName = subObject.GetType().AssemblyQualifiedName;
                content = JsonUtility.ToJson(subObject);
            }

            public string typeName;
            public string content;
        }

        #endregion


        #region Members

#if UNITY_EDITOR
        [SerializeField] private BaseSaveSubObject[] m_editorSubObjects;
#endif

        private readonly Dictionary<ESaveCategory, BaseSaveSubObject> m_subObjectDictionary = new();

        #endregion


        #region Set Methods

        public void Add(BaseSaveSubObject subObject)
        {
            if (m_subObjectDictionary.ContainsKey(subObject.Category))
            {
                Debug.LogError("This save object already contains sub object for category " + subObject.Category);
                return;
            }

            m_subObjectDictionary.Add(subObject.Category, subObject);
        }
        public void Set(BaseSaveSubObject subObject)
        {
            m_subObjectDictionary[subObject.Category] = subObject;
        }
        public bool Remove(ESaveCategory category)
        {
            return m_subObjectDictionary.Remove(category);
        }
        public bool Remove(ESaveCategory category, out BaseSaveSubObject subObject)
        {
            return m_subObjectDictionary.Remove(category, out subObject);
        }

        #endregion

        #region Access Methods

        #endregion


        #region Save

        public string GetSaveContent()
        {
            SaveWrapper wrapper = new SaveWrapper(m_subObjectDictionary.Values);

            return JsonUtility.ToJson(wrapper);
        }

        #endregion

        #region Load

        public void Load(string saveContent)
        {
            Clear();

            var wrapper = JsonUtility.FromJson<SaveWrapper>(saveContent);

            foreach (var w in wrapper.subWrappers)
            {
                if (TryLoadSubObject(w.typeName, w.content, out var subObject))
                {
                    if (!m_subObjectDictionary.TryAdd(subObject.Category, subObject))
                    {
                        Debug.LogError("LOAD ERROR : Save object already contains a sub object with category " + subObject.Category);
                    }
                }
            }

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                m_editorSubObjects = new BaseSaveSubObject[m_subObjectDictionary.Count];
                int i = 0;
                foreach (var (_, subObject) in m_subObjectDictionary)
                {
                    AssetDatabase.AddObjectToAsset(subObject, this);
                    m_editorSubObjects[i] = subObject;
                    i++;
                }

                AssetDatabase.Refresh();
                AssetDatabase.SaveAssetIfDirty(this);
            }
#endif
        }

        private bool TryLoadSubObject(string typeName, string content, out BaseSaveSubObject subObject)
        {
            var type = Type.GetType(typeName, false);
            if (type != null)
            {
                return TryLoadSubObject(type, content, out subObject);
            }

            // TODO : enable handling of wrong type for cases where the type name changed etc...
            Debug.LogError("LOAD ERROR : Type " + typeName + " is not valid");
            subObject = null;
            return false;
        }
        private bool TryLoadSubObject(Type type, string content, out BaseSaveSubObject subObject)
        {
            try
            {
                subObject = ScriptableObject.CreateInstance(type) as BaseSaveSubObject;
                if (subObject == null)
                {
                    Debug.LogError("LOAD ERROR : Unable to create instance of " + type + " as BaseSaveSubObject");
                    return false;
                }

                JsonUtility.FromJsonOverwrite(content, subObject);
                if (subObject == null)
                {
                    Debug.LogError("LOAD ERROR : Json Overwrite nulled the object");
                    return false;
                }

                subObject.name = subObject.Category.ToString();
                return true;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                subObject = null;
                return false;
            }
        }

        #endregion

        #region Utility

        private void Clear()
        {
            m_subObjectDictionary.Clear();

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                m_editorSubObjects = null;

                EditorDataUtility.EnsureAssetValidity(this, (obj) => false);
            }
#endif
        }

        #endregion


        // --- EDITOR ---

        #region Editor Methods

        public void Editor_RefreshDictionaryFromArray()
        {
            m_subObjectDictionary.Clear();

            foreach (var subObject in m_editorSubObjects)
            {
                if (subObject != null)
                {
                    m_subObjectDictionary[subObject.Category] = subObject;
                }
            }
        }

        #endregion
    }
}
