using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dhs5.Utility.SaveLoad
{
    public static class SaveManager
    {
        #region Engine Callbacks

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            m_loadProcessObject = null;
            IsSaveProcessActive = false;
            IsLoadProcessActive = false;
            CurrentSaveObject = null;

            LoadCompleted = null;
            LoadCancelled = null;

            _loadables.Clear();
        }

        #endregion

        #region Members

        private static LoadProcessObject m_loadProcessObject;

        #endregion

        #region Properties

        public static bool IsSaveProcessActive { get; private set; }
        public static bool IsLoadProcessActive { get; private set; }

        private static SaveObject CurrentSaveObject { get; set; }

        public static SaveProcessModifier SaveProcessModifier
        {
            get => SaveAsset.HasModifier(out var modifier) ? modifier : null;
        }
        public static T GetSaveProcessModifierAs<T>() where T : SaveProcessModifier
        {
            if (SaveAsset.HasModifier(out var modifier) && modifier is T t)
            {
                return t;
            }
            return null;
        }

        #endregion

        #region Event

        public static event Action LoadCompleted;
        public static event Action<Exception> LoadCancelled;

        #endregion


        #region Loadable Registration

        private readonly static Dictionary<ESaveCategory, HashSet<ILoadable>> _loadables = new();

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

        /// <returns>Whether the save file was successfully written. If not, the previous save file is kept intact</returns>
        public static bool CompleteSaveProcess(ISaveParameter parameter = null)
        {
            IsSaveProcessActive = false;
            try
            {
                return SaveAsset.SaveContentToDisk(CurrentSaveObject, parameter);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                Debug.LogError("SAVE ERROR : Could not complete the save process");
                return false;
            }
        }

        #endregion
        
        #region LOAD Process

        public static bool StartLoadProcess()
        {
            if (IsLoadProcessActive || IsSaveProcessActive) return false;

            string content;
            try
            {
                content = SaveAsset.ReadContentFromSelectedSaveFile();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                Debug.LogError("LOAD ERROR : Could not read the selected save file");
                return false;
            }
            return StartLoadProcess(content);
        }
        public static bool StartLoadProcess(string path, System.Text.Encoding encoding)
        {
            if (IsLoadProcessActive || IsSaveProcessActive) return false;

            string content;
            try
            {
                content = SaveAsset.ReadContentAtPath(path, encoding);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                Debug.LogError("LOAD ERROR : Could not read the save file at " + path);
                return false;
            }
            return StartLoadProcess(content);
        }
        private static bool StartLoadProcess(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                Debug.LogError("LOAD ERROR : Save content is empty");
                return false;
            }

            // Parse the content before starting the process : if it's invalid, nothing starts and the current save object is kept
            var loadedSaveObject = SaveObject.CreateInstance<SaveObject>();
            try
            {
                loadedSaveObject.Load(content);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                Debug.LogError("LOAD ERROR : Save content is invalid");
                ScriptableObject.Destroy(loadedSaveObject);
                return false;
            }

            IsLoadProcessActive = true;

            m_loadProcessObject = new GameObject("LOAD PROCESS OBJECT").AddComponent<LoadProcessObject>();
            // Loadables may load scenes : the process must survive them, otherwise OnDisable would cancel it
            GameObject.DontDestroyOnLoad(m_loadProcessObject.gameObject);
            m_loadProcessObject.StartLoadProcessCoroutine(LoadCoroutine(loadedSaveObject), OnLoadProcessCancelled);

            return true;
        }
        private static void OnLoadProcessFinished()
        {
            if (m_loadProcessObject != null)
            {
                m_loadProcessObject.StopLoadProcessCoroutine();
                GameObject.Destroy(m_loadProcessObject.gameObject);
                m_loadProcessObject = null;
            }
            IsLoadProcessActive = false;
        }
        private static void OnLoadProcessCancelled() => OnLoadProcessCancelled(null);
        private static void OnLoadProcessCancelled(Exception e)
        {
            Debug.Log("CANCELLING LOAD PROCESS");
            OnLoadProcessFinished();
            LoadCancelled?.Invoke(e);
        }
        private static void OnLoadProcessComplete()
        {
            OnLoadProcessFinished();
            LoadCompleted?.Invoke();
        }

        private static IEnumerator LoadCoroutine(SaveObject loadedSaveObject)
        {
            CurrentSaveObject = loadedSaveObject;

            foreach (var (category, iteration) in SaveAsset.GetCategoriesInLoadOrder())
            {
                yield return null;

                if (CurrentSaveObject.TryGetSubObject(category, out var subObject))
                {
                    if (_loadables.TryGetValue(category, out var list))
                    {
                        // Iterate over snapshots of the set : loadables can register/unregister while loading (spawned/destroyed objects...),
                        // which would invalidate an enumerator of the set across the yields.
                        // Loadables registered during the load of this category are loaded too, in a following pass.
                        HashSet<ILoadable> processed = new();
                        List<ILoadable> toProcess = new(list);
                        while (toProcess.Count > 0)
                        {
                            foreach (var loadable in toProcess)
                            {
                                // Unregistered (e.g. destroyed) since the snapshot
                                if (!list.Contains(loadable)) continue;

                                processed.Add(loadable);

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

                                if (coroutine != null)
                                {
                                    bool running = true;
                                    while (running)
                                    {
                                        yield return coroutine.Current;

                                        try
                                        {
                                            running = coroutine.MoveNext();
                                        }
                                        catch (Exception e)
                                        {
                                            Debug.LogException(e);

                                            if (SaveAsset.HasModifier(out var modifier)
                                                && modifier.TryHandleLoadException(e))
                                            {
                                                Debug.Log("RESUMING LOAD PROCESS");
                                            }
                                            else
                                            {
                                                OnLoadProcessCancelled(e);
                                                yield break;
                                            }
                                        }
                                    }
                                }
                            }

                            toProcess.Clear();
                            foreach (var loadable in list)
                            {
                                if (!processed.Contains(loadable)) toProcess.Add(loadable);
                            }
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
