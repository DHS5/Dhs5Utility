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
            public SaveWrapper(BaseSaveInfo saveInfo, ICollection<BaseSaveSubObject> saveSubObjects)
            {
                infoWrapper = new(saveInfo);

                subWrappers = new();
                foreach (var subObject in saveSubObjects)
                {
                    subWrappers.Add(new SubSaveWrapper(subObject));
                }
            }

            public SaveInfoWrapper infoWrapper;
            public List<SubSaveWrapper> subWrappers;
        }

        #endregion

        #region STRUCT SaveInfoWrapper

        [Serializable]
        private struct SaveInfoWrapper
        {
            public SaveInfoWrapper(BaseSaveInfo saveInfo)
            {
                date = SerializableDate.Now;
                typeName = saveInfo.GetType().AssemblyQualifiedName;
                content = JsonUtility.ToJson(saveInfo);
            }

            public SerializableDate date;
            public string typeName;
            public string content;
        }

        #endregion

        #region STRUCT SubSaveWrapper

        [Serializable]
        private struct SubSaveWrapper
        {
            public SubSaveWrapper(BaseSaveSubObject subObject)
            {
                categoryName = subObject.Category.ToString();
                typeName = subObject.GetType().AssemblyQualifiedName;
                content = JsonUtility.ToJson(subObject);
            }

            public string categoryName;
            public string typeName;
            public string content;
        }

        #endregion


        #region Members

        [SerializeField, DateReadOnly] private SerializableDate m_date;
        [SerializeField] private BaseSaveInfo m_saveInfo;

#if UNITY_EDITOR
        [SerializeField] private BaseSaveSubObject[] m_editorSubObjects;
#endif

        private readonly Dictionary<ESaveCategory, BaseSaveSubObject> m_subObjectDictionary = new();

        #endregion


        #region Set Methods

        internal void SetInfo(BaseSaveInfo saveInfo)
        {
            m_saveInfo = saveInfo;
        }
        internal void Add(BaseSaveSubObject subObject)
        {
            if (!m_subObjectDictionary.TryAdd(subObject.Category, subObject))
            {
                Debug.LogError("This save object already contains sub object for category " + subObject.Category);
            }
        }
        internal void Set(BaseSaveSubObject subObject)
        {
            m_subObjectDictionary[subObject.Category] = subObject;
        }
        internal bool Remove(ESaveCategory category)
        {
            return m_subObjectDictionary.Remove(category);
        }
        internal bool Remove(ESaveCategory category, out BaseSaveSubObject subObject)
        {
            return m_subObjectDictionary.Remove(category, out subObject);
        }

        #endregion

        #region Access Methods

        internal BaseSaveInfo GetSaveInfo() => m_saveInfo;
        internal bool TryGetSubObject(ESaveCategory category, out BaseSaveSubObject subObject)
        {
            return m_subObjectDictionary.TryGetValue(category, out subObject);
        }
        internal bool TryGetSubObject<T>(ESaveCategory category, out T subObject) where T : BaseSaveSubObject
        {
            if (m_subObjectDictionary.TryGetValue(category, out var obj)
                && obj is T t)
            {
                subObject = t;
                return true;
            }
            subObject = null;
            return false;
        }

        #endregion


        #region Save

        internal string GetSaveContent()
        {
            SaveWrapper wrapper = new(m_saveInfo, m_subObjectDictionary.Values);

            return JsonUtility.ToJson(wrapper);
        }

        #endregion

        #region Load

        internal void Load(string saveContent)
        {
            Clear();

            var wrapper = JsonUtility.FromJson<SaveWrapper>(saveContent);

            // Date
            m_date = wrapper.infoWrapper.date;

            // Save Info
            TryLoadSaveInfo(wrapper.infoWrapper.typeName, wrapper.infoWrapper.content, out m_saveInfo);
            m_saveInfo.name = "SAVE INFO";

            // Sub Objects
            foreach (var w in wrapper.subWrappers)
            {
                if (TryLoadSubObject(w.categoryName, w.typeName, w.content, out var subObject))
                {
                    subObject.name = subObject.Category.ToString();
                    if (!m_subObjectDictionary.TryAdd(subObject.Category, subObject))
                    {
                        Debug.LogError("LOAD ERROR : Save object already contains a sub object with category " + subObject.Category);
                    }
                }
            }

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                if (m_saveInfo != null)
                {
                    AssetDatabase.AddObjectToAsset(m_saveInfo, this);
                }

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

        #region SubObject & SaveInfo Loading

        private bool TryLoadSaveInfo(string typeName, string content, out BaseSaveInfo saveInfo)
        {
            var type = Type.GetType(typeName, false);
            if (type != null)
            {
                return TryLoadScriptableObject(type, content, out saveInfo);
            }

            if (SaveAsset.HasModifier(out var modifier) 
                && modifier.TryHandleTypeDeserializingError(typeName, out type))
            {
                return TryLoadScriptableObject(type, content, out saveInfo);
            }

            Debug.LogError("LOAD ERROR : Type " + typeName + " is not valid");
            saveInfo = null;
            return false;
        }

        private bool TryLoadSubObject(string categoryName, string typeName, string content, out BaseSaveSubObject subObject)
        {
            var type = Type.GetType(typeName, false);

            if (type == null
                && SaveAsset.HasModifier(out var modifier)
                && modifier.TryHandleTypeDeserializingError(typeName, out var backupType))
            {
                type = backupType;
            }

            if (type != null && TryLoadScriptableObject(type, content, out subObject))
            {
                // Category double check
                if (Enum.TryParse(typeof(ESaveCategory), categoryName, out var result))
                {
                    ESaveCategory category = (ESaveCategory)result;
                    if (category != subObject.Category)
                    {
                        Debug.LogWarning("Sub Object category is " + subObject.Category + " but category name was " + categoryName + " when serializing\n" +
                            "The category will be changed to " + category);
                        subObject.Category = category;
                    }
                }
                else
                {
                    Debug.LogWarning("Sub Object category is " + subObject.Category + " but category name was " + categoryName + " when serializing\n" +
                            "Can't find Category with name " + categoryName + " so Sub Object category will stay " + subObject.Category);
                }

                return true;
            }

            Debug.LogError("LOAD ERROR : Type " + typeName + " is not valid");
            subObject = null;
            return false;
        }
        private bool TryLoadScriptableObject<T>(Type type, string content, out T scriptableObject) where T : ScriptableObject
        {
            try
            {
                scriptableObject = ScriptableObject.CreateInstance(type) as T;
                if (scriptableObject == null)
                {
                    Debug.LogError("LOAD ERROR : Unable to create instance of " + type + " as " + typeof(T).Name);
                    return false;
                }

                JsonUtility.FromJsonOverwrite(content, scriptableObject);
                if (scriptableObject == null)
                {
                    Debug.LogError("LOAD ERROR : Json Overwrite nulled the object");
                    return false;
                }

                return true;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                scriptableObject = null;
                return false;
            }
        }

        #endregion

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

        internal void Editor_RefreshDictionaryFromArray()
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
