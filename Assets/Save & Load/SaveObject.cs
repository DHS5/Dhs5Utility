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
            public SaveWrapper(int version, SerializableDate date, BaseSaveInfo saveInfo, ICollection<BaseSaveSubObject> saveSubObjects)
            {
                this.version = version;
                infoWrapper = new(date, saveInfo);

                subWrappers = new();
                foreach (var subObject in saveSubObjects)
                {
                    if (subObject != null)
                    {
                        subWrappers.Add(new SubSaveWrapper(subObject));
                    }
                }
            }

            /// <summary>
            /// 0 for saves made before versioning
            /// </summary>
            public int version;
            public SaveInfoWrapper infoWrapper;
            public List<SubSaveWrapper> subWrappers;
        }

        #endregion

        #region STRUCT SaveInfoWrapper

        [Serializable]
        private struct SaveInfoWrapper
        {
            public SaveInfoWrapper(SerializableDate date, BaseSaveInfo saveInfo)
            {
                this.date = date;

                if (saveInfo != null)
                {
                    typeName = saveInfo.GetType().AssemblyQualifiedName;
                    content = SerializeData(saveInfo);
                }
                else
                {
                    typeName = null;
                    content = null;
                    Debug.LogWarning("SAVE WARNING : No save info");
                }
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
                content = SerializeData(subObject);
            }

            public string categoryName;
            public string typeName;
            public string content;
        }

        #endregion


        #region Members

        [SerializeField, DateReadOnly] private SerializableDate m_date;
        [SerializeField, ReadOnly] private int m_version;
        [SerializeField] private BaseSaveInfo m_saveInfo;

#if UNITY_EDITOR
        [SerializeField] private BaseSaveSubObject[] m_editorSubObjects;
#endif

        private readonly Dictionary<ESaveCategory, BaseSaveSubObject> m_subObjectDictionary = new();
        /// <summary>
        /// Save info and sub objects created by this save object (when loading, or with the Create methods),
        /// destroyed with it
        /// </summary>
        private readonly HashSet<UnityEngine.Object> m_ownedObjects = new();

        #endregion


        #region Set Methods

        internal void SetInfo(BaseSaveInfo saveInfo)
        {
            m_saveInfo = saveInfo;
        }
        internal T CreateSaveInfo<T>() where T : BaseSaveInfo
        {
            var saveInfo = CreateInstance<T>();
            saveInfo.name = "SAVE INFO";
            m_ownedObjects.Add(saveInfo);
            SetInfo(saveInfo);
            return saveInfo;
        }
        internal T CreateCategoryData<T>(ESaveCategory category) where T : BaseSaveSubObject
        {
            var subObject = CreateInstance<T>();
            subObject.Category = category;
            subObject.name = category.ToString();
            m_ownedObjects.Add(subObject);
            Set(subObject);
            return subObject;
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
        /// <summary>
        /// Version of the save : when loaded, the version of the save file (0 for saves made before versioning),
        /// when saved, the current version of the SaveAsset
        /// </summary>
        internal int Version => m_version;
        /// <summary>
        /// Date of the save : when loaded, the date the save file was written, when saved, the date of the last write
        /// </summary>
        internal SerializableDate Date => m_date;
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
            m_version = SaveAsset.SaveVersion;
            m_date = SerializableDate.Now;
            SaveWrapper wrapper = new(m_version, m_date, m_saveInfo, m_subObjectDictionary.Values);

            return JsonUtility.ToJson(wrapper);
        }

        #endregion

        #region Load

        internal void Load(string saveContent)
        {
            Clear();

            var wrapper = JsonUtility.FromJson<SaveWrapper>(saveContent);

            // Version & Date
            m_version = wrapper.version;
            m_date = wrapper.infoWrapper.date;

            // Save Info
            // (the save info and sub objects are created here : this save object owns them)
            if (TryLoadSaveInfo(wrapper.infoWrapper.typeName, wrapper.infoWrapper.content, out m_saveInfo))
            {
                m_ownedObjects.Add(m_saveInfo);
                m_saveInfo.name = "SAVE INFO";
            }

            // Sub Objects
            foreach (var w in wrapper.subWrappers)
            {
                if (TryLoadSubObject(w.categoryName, w.typeName, w.content, out var subObject))
                {
                    m_ownedObjects.Add(subObject);
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
            if (string.IsNullOrWhiteSpace(typeName))
            {
                Debug.LogWarning("LOAD WARNING : No save info");
                saveInfo = null;
                return false;
            }

            var type = Type.GetType(typeName, false);
            if (type != null)
            {
                return TryLoadScriptableObject(type, content, out saveInfo);
            }

            if (SaveAsset.HasModifier(out var modifier) 
                && modifier.TryHandleTypeDeserializationError(typeName, out type))
            {
                return TryLoadScriptableObject(type, content, out saveInfo);
            }

            Debug.LogError("LOAD ERROR : Type " + typeName + " is not valid");
            saveInfo = null;
            return false;
        }

        private bool TryLoadSubObject(string categoryName, string typeName, string content, out BaseSaveSubObject subObject)
        {
            // Category is found by name, following the renames since the save was made :
            // the category number stored in the content can't be trusted, as categories may have been deleted or moved since
            if (!SaveAsset.TryGetCategoryFromSavedName(categoryName, out var category))
            {
                Debug.LogWarning("LOAD WARNING : Category " + categoryName + " doesn't exist anymore, its data is ignored");
                subObject = null;
                return false;
            }

            var type = Type.GetType(typeName, false);

            if (type == null
                && SaveAsset.HasModifier(out var modifier)
                && modifier.TryHandleTypeDeserializationError(typeName, out var backupType))
            {
                type = backupType;
            }

            if (type != null && TryLoadScriptableObject(type, content, out subObject))
            {
                subObject.Category = category;
                return true;
            }

            Debug.LogError("LOAD ERROR : Type " + typeName + " is not valid");
            subObject = null;
            return false;
        }
        private bool TryLoadScriptableObject<T>(Type type, string content, out T scriptableObject) where T : ScriptableObject
        {
            // Check the type BEFORE creating an instance : the type name comes from the save file,
            // which could make the game create (and run Awake/OnEnable of) any ScriptableObject type
            if (!typeof(T).IsAssignableFrom(type) || type.IsAbstract)
            {
                Debug.LogError("LOAD ERROR : Type " + type + " is not a valid " + typeof(T).Name);
                scriptableObject = null;
                return false;
            }

            scriptableObject = null;
            try
            {
                scriptableObject = (T)ScriptableObject.CreateInstance(type);
                DeserializeData(content, scriptableObject, m_version);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                if (scriptableObject != null) DestroyImmediate(scriptableObject);
                scriptableObject = null;
                return false;
            }
        }

        #endregion

        #endregion

        #region Data Serialization

        // The data of the save info and sub objects goes through the modifier (JsonUtility by default),
        // the save file structure (SaveWrapper) always uses JsonUtility
        private static string SerializeData(ScriptableObject data)
        {
            if (SaveAsset.HasModifier(out var modifier))
            {
                return modifier.SerializeData(data);
            }
            return JsonUtility.ToJson(data);
        }
        private static void DeserializeData(string content, ScriptableObject data, int saveVersion)
        {
            if (SaveAsset.HasModifier(out var modifier))
            {
                modifier.DeserializeData(content, data, saveVersion);
                return;
            }
            JsonUtility.FromJsonOverwrite(content, data);
        }

        #endregion

        #region Utility

        /// <summary>
        /// Destroys this save object, and the save info and sub objects it created (when loading, or with the Create methods).<br></br>
        /// Objects given to it during a save process (see <see cref="SaveManager.Set"/>) belong to the game and are never destroyed.
        /// </summary>
        internal void DestroyWithOwnedContent()
        {
            // Owned objects are destroyed even if they were replaced or removed from this save object since
            foreach (var ownedObject in m_ownedObjects)
            {
                SafeDestroyObject(ownedObject);
            }
            m_ownedObjects.Clear();
            m_saveInfo = null;
            m_subObjectDictionary.Clear();

            SafeDestroyObject(this);
        }
        private static void SafeDestroyObject(UnityEngine.Object obj)
        {
            if (obj == null) return;

            if (Application.isPlaying) Destroy(obj);
            else DestroyImmediate(obj);
        }

        private void Clear()
        {
            m_subObjectDictionary.Clear();
            m_ownedObjects.Clear();

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

#if UNITY_EDITOR
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
#endif

        #endregion
    }
}
