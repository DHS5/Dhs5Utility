using System;
using UnityEngine;

namespace Dhs5.Utility.SaveLoad
{
    public static class SaveManager
    {
        #region DELEGATE LoadEvent

        public delegate void LoadEvent(ESaveCategory category, uint cycle, BaseSaveSubObject subObject);

        #endregion

        #region Properties

        public static bool IsSaveProcessActive { get; private set; }

        private static SaveObject CurrentSaveObject { get; set; }

        #endregion

        #region Events

        public static event LoadEvent OnLoadEvent;

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

        public static void StartSaveProcess()
        {
            CurrentSaveObject = SaveObject.CreateInstance<SaveObject>();
            IsSaveProcessActive = true;
        }

        public static void CompleteSaveProcess()
        {
            IsSaveProcessActive = false;
            var content = CurrentSaveObject.GetSaveContent();
            // TODO use a scriptable object to manage content saved to disk
        }

        #endregion
        
        #region LOAD Process

        public static void StartLoadProcess()
        {
            var content = "TODO"; // TODO use a scriptable object to manage the save file choice
            CurrentSaveObject = SaveObject.CreateInstance<SaveObject>();
            CurrentSaveObject.Load(content);

            foreach (var (category, cycle) in SaveAsset.GetCategoriesInLoadOrder())
            {
                if (CurrentSaveObject.TryGetSubObject(category, out var subObject))
                {
                    try
                    {
                        OnLoadEvent?.Invoke(category, cycle, subObject);
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                    }
                }
            }
        }

        #endregion
    }
}
