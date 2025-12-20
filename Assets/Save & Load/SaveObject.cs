using System;
using System.Collections.Generic;
using UnityEngine;

namespace Dhs5.Utility.SaveLoad
{
    public sealed class SaveObject : ScriptableObject
    {
        #region STRUCT SubSaveWrapper

        [Serializable]
        private struct SubSaveWrapper
        {
            public SubSaveWrapper(ESaveCategory category, BaseSaveSubObject subObject)
            {
                this.category = category;
                typeName = subObject.GetType().AssemblyQualifiedName;
                content = JsonUtility.ToJson(subObject);
            }

            public ESaveCategory category;
            public string typeName;
            public string content;
        }

        #endregion


        #region Members

#if UNITY_EDITOR
        [SerializeField, ReadOnly] private BaseSaveSubObject[] m_subObjects;
#endif

        private Dictionary<ESaveCategory, BaseSaveSubObject> m_subObjectDictionary = new();

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
            List<SubSaveWrapper> wrappers = new();

            foreach (var (category, subObject) in m_subObjectDictionary)
            {
                wrappers.Add(new SubSaveWrapper(category, subObject));
            }

            return JsonUtility.ToJson(wrappers);
        }

        #endregion

        #region Load

        public void Load(string saveContent)
        {
            var wrappers = JsonUtility.FromJson<List<SubSaveWrapper>>(saveContent);
        }

        #endregion
    }
}
