using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dhs5.Utility.SaveLoad
{
    public static class SaveManager
    {
        #region Members

        private static LoadProcessObject m_loadProcessObject;

        #endregion

        #region Properties

        public static bool IsSaveProcessActive { get; private set; }
        public static bool IsLoadProcessActive { get; private set; }

        private static SaveObject CurrentSaveObject { get; set; }

        #endregion

        #region Event

        public static event Action LoadCompleted;

        #endregion


        #region Loadable Registration

        private static Dictionary<ESaveCategory, HashSet<ILoadable>> _loadables = new();

        public static void Register(bool register, ILoadable loadable, ESaveCategory category)
        {
            if (loadable == null) return;

            if (register)
            {
                if (_loadables.TryGetValue(category, out var list) && list != null)
                {
                    list.Add(loadable);
                }
                else
                {
                    _loadables[category] = new()
                    {
                        loadable
                    };
                }
            }
            else
            {
                if (_loadables.TryGetValue(category, out var list) && list.IsValid())
                {
                    list.Remove(loadable);
                }
            }
        }

        #endregion


        #region Save Set Methods

        public static void SetInfo(BaseSaveInfo saveInfo)
        {
            if (CurrentSaveObject != null && IsSaveProcessActive)
            {
                CurrentSaveObject.SetInfo(saveInfo);
            }
            else
            {
                if (CurrentSaveObject == null) Debug.LogError("SAVE SET ERROR : Current SaveObject is null");
                else Debug.LogError("SAVE SET ERROR : Save Process is not active");
            }
        }
        public static void Add(BaseSaveSubObject subObject)
        {
            if (CurrentSaveObject != null && IsSaveProcessActive)
            {
                CurrentSaveObject.Add(subObject);
            }
            else
            {
                if (CurrentSaveObject == null) Debug.LogError("SAVE SET ERROR : Current SaveObject is null");
                else Debug.LogError("SAVE SET ERROR : Save Process is not active");
            }
        }
        public static void Set(BaseSaveSubObject subObject)
        {
            if (CurrentSaveObject != null)
            {
                CurrentSaveObject.Set(subObject);
            }
            else
            {
                if (CurrentSaveObject == null) Debug.LogError("SAVE SET ERROR : Current SaveObject is null");
                else Debug.LogError("SAVE SET ERROR : Save Process is not active");
            }
        }
        public static bool Remove(ESaveCategory category)
        {
            if (CurrentSaveObject != null)
            {
                return CurrentSaveObject.Remove(category);
            }
            else
            {
                if (CurrentSaveObject == null) Debug.LogError("SAVE SET ERROR : Current SaveObject is null");
                else Debug.LogError("SAVE SET ERROR : Save Process is not active");
                return false;
            }
        }
        public static bool Remove(ESaveCategory category, out BaseSaveSubObject subObject)
        {
            if (CurrentSaveObject != null)
            {
                return CurrentSaveObject.Remove(category, out subObject);
            }
            else
            {
                if (CurrentSaveObject == null) Debug.LogError("SAVE SET ERROR : Current SaveObject is null");
                else Debug.LogError("SAVE SET ERROR : Save Process is not active");
                subObject = null;
                return false;
            }
        }

        #endregion

        #region Save Access Methods

        public static BaseSaveInfo GetSaveInfo()
        {
            if (CurrentSaveObject != null)
            {
                return CurrentSaveObject.GetSaveInfo();
            }
            else
            {
                Debug.LogError("SAVE GET ERROR : Current SaveObject is null");
                return null;
            }
        }
        public static bool TryGetSubObject(ESaveCategory category, out BaseSaveSubObject subObject)
        {
            if (CurrentSaveObject != null)
            {
                return CurrentSaveObject.TryGetSubObject(category, out subObject);
            }
            else
            {
                Debug.LogError("SAVE GET ERROR : Current SaveObject is null");
                subObject = null;
                return false;
            }
        }
        public static bool TryGetSubObject<T>(ESaveCategory category, out T subObject) where T : BaseSaveSubObject
        {
            if (CurrentSaveObject != null)
            {
                return CurrentSaveObject.TryGetSubObject(category, out subObject);
            }
            else
            {
                Debug.LogError("SAVE GET ERROR : Current SaveObject is null");
                subObject = null;
                return false;
            }
        }

        #endregion


        #region SAVE Process

        public static bool StartSaveProcess()
        {
            if (IsLoadProcessActive || IsSaveProcessActive) return false;

            CurrentSaveObject = SaveObject.CreateInstance<SaveObject>();
            IsSaveProcessActive = true;
            return true;
        }

        public static void CompleteSaveProcess(ISaveParameter parameter = null)
        {
            IsSaveProcessActive = false;
            SaveAsset.SaveContentToDisk(CurrentSaveObject, parameter);
        }

        #endregion
        
        #region LOAD Process

        public static bool StartLoadProcess()
        {
            if (IsLoadProcessActive || IsSaveProcessActive) return false;

            var content = SaveAsset.ReadContentFromSelectedSaveFile();
            return StartLoadProcess(content);
        }
        public static bool StartLoadProcess(string path, System.Text.Encoding encoding)
        {
            if (IsLoadProcessActive || IsSaveProcessActive) return false;

            var content = SaveAsset.ReadContentAtPath(path, encoding);
            return StartLoadProcess(content);
        }
        private static bool StartLoadProcess(string content)
        {
            if (!string.IsNullOrWhiteSpace(content))
            {
                IsLoadProcessActive = true;

                m_loadProcessObject = new GameObject("LOAD PROCESS OBJECT").AddComponent<LoadProcessObject>();
                m_loadProcessObject.StartLoadProcessCoroutine(LoadCoroutine(content));

                return true;
            }
            return false;
        }
        private static void OnLoadProcessComplete()
        {
            if (m_loadProcessObject != null)
            {
                GameObject.Destroy(m_loadProcessObject.gameObject);
                m_loadProcessObject = null;
            }
            IsLoadProcessActive = false;
            LoadCompleted?.Invoke();
        }

        private static IEnumerator LoadCoroutine(string content)
        {
            CurrentSaveObject = SaveObject.CreateInstance<SaveObject>();
            CurrentSaveObject.Load(content);

            foreach (var (category, iteration) in SaveAsset.GetCategoriesInLoadOrder())
            {
                yield return null;

                if (CurrentSaveObject.TryGetSubObject(category, out var subObject))
                {
                    if (_loadables.TryGetValue(category, out var list))
                    {
                        foreach (var loadable in list)
                        {
                            IEnumerator coroutine = null;
                            try
                            {
                                if (loadable.CanLoad(category, iteration))
                                {
                                    coroutine = loadable.LoadCoroutine(category, iteration, subObject);
                                }
                            }
                            catch (Exception e)
                            {
                                Debug.LogException(e);
                            }

                            if (coroutine != null) yield return coroutine;
                        }
                    }
                }
            }

            yield return null;

            OnLoadProcessComplete();
        }

        #endregion
    }
}
