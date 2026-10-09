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
            // Without domain reload, the save object of the previous play session is still alive
            ReplaceCurrentSaveObject(null);

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

        /// <summary>
        /// Destroys the current save object when it's replaced, with the save info and sub objects it created when loading
        /// (loaded data must be copied by loadables, not kept, see <see cref="BaseSaveSubObject"/>)
        /// </summary>
        private static void ReplaceCurrentSaveObject(SaveObject saveObject)
        {
            if (CurrentSaveObject != null && CurrentSaveObject != saveObject)
            {
                CurrentSaveObject.DestroyWithOwnedContent();
            }
            CurrentSaveObject = saveObject;
        }

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

        /// <summary>
        /// Creates a save info of type <typeparamref name="T"/> and sets it as the save info of the current save.<br></br>
        /// The save info is owned by the save : it's destroyed when the current save is replaced (next save or load).<br></br>
        /// Use this in runtime code, instead of creating the save info yourself and passing it to <see cref="SetInfo"/>.
        /// </summary>
        /// <returns>The created save info to fill, or null outside of the Save Process</returns>
        public static T CreateSaveInfo<T>() where T : BaseSaveInfo
        {
            if (CanModifyCurrentSaveObject())
            {
                return CurrentSaveObject.CreateSaveInfo<T>();
            }
            return null;
        }
        /// <summary>
        /// Creates the data of <paramref name="category"/>, of type <typeparamref name="T"/>, and sets it in the current save
        /// (replacing the current data of <paramref name="category"/> if any).<br></br>
        /// The data is owned by the save : it's destroyed when the current save is replaced (next save or load).<br></br>
        /// Use this in runtime code, instead of creating the data yourself and passing it to <see cref="Set"/> or <see cref="Add"/>.
        /// </summary>
        /// <returns>The created data to fill, or null outside of the Save Process</returns>
        public static T CreateCategoryData<T>(ESaveCategory category) where T : BaseSaveSubObject
        {
            if (CanModifyCurrentSaveObject())
            {
                return CurrentSaveObject.CreateCategoryData<T>(category);
            }
            return null;
        }

        /// <summary>
        /// Sets <paramref name="saveInfo"/> as the save info of the current save.<br></br>
        /// Use it only with an asset created in the editor : the save never destroys it.
        /// In runtime code, use <see cref="CreateSaveInfo"/> instead,
        /// otherwise the save infos you create would never be destroyed.
        /// </summary>
        public static void SetInfo(BaseSaveInfo saveInfo)
        {
            if (CanModifyCurrentSaveObject())
            {
                CurrentSaveObject.SetInfo(saveInfo);
            }
        }
        /// <summary>
        /// Adds <paramref name="subObject"/> to the current save, logs an error if its category already has data.<br></br>
        /// Use it only with an asset created in the editor : the save never destroys it.
        /// In runtime code, use <see cref="CreateCategoryData"/> instead,
        /// otherwise the sub objects you create would never be destroyed.
        /// </summary>
        public static void Add(BaseSaveSubObject subObject)
        {
            if (CanModifyCurrentSaveObject())
            {
                CurrentSaveObject.Add(subObject);
            }
        }
        /// <summary>
        /// Sets <paramref name="subObject"/> in the current save, replacing the current data of its category if any.<br></br>
        /// Use it only with an asset created in the editor : the save never destroys it.
        /// In runtime code, use <see cref="CreateCategoryData"/> instead,
        /// otherwise the sub objects you create would never be destroyed.
        /// </summary>
        public static void Set(BaseSaveSubObject subObject)
        {
            if (CanModifyCurrentSaveObject())
            {
                CurrentSaveObject.Set(subObject);
            }
        }
        /// <summary>
        /// Removes the data of <paramref name="category"/> from the current save
        /// </summary>
        /// <returns>Whether the current save had data for <paramref name="category"/></returns>
        public static bool Remove(ESaveCategory category)
        {
            if (CanModifyCurrentSaveObject())
            {
                return CurrentSaveObject.Remove(category);
            }
            return false;
        }
        /// <summary>
        /// Removes the data of <paramref name="category"/> from the current save and outs it.<br></br>
        /// If that data was created by the save (with <see cref="CreateCategoryData"/>), it's still owned by the save :
        /// it will be destroyed when the current save is replaced (next save or load), so don't keep it.
        /// </summary>
        /// <returns>Whether the current save had data for <paramref name="category"/></returns>
        public static bool Remove(ESaveCategory category, out BaseSaveSubObject subObject)
        {
            if (CanModifyCurrentSaveObject())
            {
                return CurrentSaveObject.Remove(category, out subObject);
            }
            subObject = null;
            return false;
        }

        /// <summary>
        /// The save can only be modified during the Save Process (between <see cref="StartSaveProcess"/> and <see cref="CompleteSaveProcess"/>) :
        /// each save process starts with an empty save, so modifications made outside of it would never be written to a save file
        /// </summary>
        private static bool CanModifyCurrentSaveObject()
        {
            if (!IsSaveProcessActive)
            {
                Debug.LogError("SAVE SET ERROR : The save can only be modified during the Save Process (between StartSaveProcess and CompleteSaveProcess)");
                return false;
            }
            if (CurrentSaveObject == null)
            {
                Debug.LogError("SAVE SET ERROR : Current SaveObject is null");
                return false;
            }
            return true;
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
        /// <summary>
        /// Version of the current save : during and after a load, the version the save file was made with (0 for saves made before versioning).<br></br>
        /// Use it in loadables to handle data saved by older versions of the game.
        /// </summary>
        public static int GetSaveVersion()
        {
            if (CurrentSaveObject != null)
            {
                return CurrentSaveObject.Version;
            }
            else
            {
                Debug.LogError("SAVE GET ERROR : Current SaveObject is null");
                return 0;
            }
        }
        /// <summary>
        /// Date of the current save : during and after a load, the date the save file was written
        /// </summary>
        public static SerializableDate GetSaveDate()
        {
            if (CurrentSaveObject != null)
            {
                return CurrentSaveObject.Date;
            }
            else
            {
                Debug.LogError("SAVE GET ERROR : Current SaveObject is null");
                return default;
            }
        }
        /// <summary>
        /// Version written in new save files, set in the SaveAsset
        /// </summary>
        public static int CurrentVersion => SaveAsset.SaveVersion;

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

            ReplaceCurrentSaveObject(SaveObject.CreateInstance<SaveObject>());
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

        /// <summary>
        /// Loads the save file selected by the SaveProcessModifier, or the default save file if there is none
        /// </summary>
        /// <param name="parameter">Without a SaveProcessModifier, must match the parameter used to save (extension and encoding)</param>
        public static bool StartLoadProcess(ISaveParameter parameter = null)
        {
            if (!CanStartLoadProcess()) return false;

            string content;
            try
            {
                content = SaveAsset.ReadContentFromSelectedSaveFile(parameter);
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
            if (!CanStartLoadProcess()) return false;

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
        private static bool CanStartLoadProcess()
        {
            // Loadables are scene objects and the load process runs as a coroutine : it only makes sense in play mode
            if (!Application.isPlaying)
            {
                Debug.LogError("LOAD ERROR : The Load Process can only be started in play mode");
                return false;
            }
            return !IsLoadProcessActive && !IsSaveProcessActive;
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
                loadedSaveObject.DestroyWithOwnedContent();
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
            ReplaceCurrentSaveObject(loadedSaveObject);

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
                                    // Step the loadable's coroutine first, then yield what it returned :
                                    // the loadable starts right away instead of waiting a frame
                                    bool running = true;
                                    while (running)
                                    {
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
                                                // Skip the rest of this loadable's coroutine and resume with the next one
                                                Debug.Log("RESUMING LOAD PROCESS");
                                                running = false;
                                            }
                                            else
                                            {
                                                OnLoadProcessCancelled(e);
                                                yield break;
                                            }
                                        }

                                        if (running)
                                        {
                                            yield return coroutine.Current;
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
                // No data for this category in the save : loadables reset to their default state, once per load
                else if (iteration == 1 && _loadables.TryGetValue(category, out var defaultLoadables))
                {
                    foreach (var loadable in new List<ILoadable>(defaultLoadables))
                    {
                        // Unregistered (e.g. destroyed) by a previous loadable
                        if (!defaultLoadables.Contains(loadable)) continue;

                        try
                        {
                            loadable.LoadDefault(category);
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

            yield return null;

            OnLoadProcessComplete();
        }

        #endregion
    }
}
