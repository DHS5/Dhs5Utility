using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Dhs5.Utility.NewDatabase
{
    public class Container : ScriptableObject
    {
        #region STRUCT MappedObject

        protected readonly struct MappedObject
        {
            public MappedObject(UnityEngine.Object obj, IContainerElement elem)
            {
                this.obj = obj;
                this.elem = elem;
            }

            public readonly UnityEngine.Object obj;
            public readonly IContainerElement elem;
        }

        #endregion


        #region Members

        [SerializeField] protected List<UnityEngine.Object> m_objects = new();

        protected Dictionary<int, MappedObject> m_mappedObjects;

        #endregion

        #region Editor Members

#if UNITY_EDITOR

        [SerializeField] protected string m_lastCreateFolderPath;

#endif

        #endregion


        #region Mapped Objects

        protected virtual void InitMappedObjects()
        {
            m_mappedObjects = new();

            foreach (var obj in m_objects)
            {
                if (obj != null && obj is IContainerElement elem)
                {
                    if (!m_mappedObjects.TryAdd(elem.UID, new MappedObject(obj, elem)))
                    {
                        Debug.LogError("Could not add IContainerElement : " + obj + " with UID = " + elem.UID + " to mapped objects.", obj);
                    }
                }
                else
                {
                    Debug.LogWarning("Object is null or not a IContainerElement : " + obj, obj);
                }
            }
        }
        protected virtual bool TryGetObjectFromMappedObjects(int uid, out UnityEngine.Object obj)
        {
            if (m_mappedObjects == null)
            {
                InitMappedObjects();
            }

            if (m_mappedObjects.TryGetValue(uid, out var mappedObj))
            {
                obj = mappedObj.obj;
                return true;
            }

            obj = null;
            return false;
        }
        protected virtual bool TryGetElementFromMappedObjects(int uid, out IContainerElement element)
        {
            if (m_mappedObjects == null)
            {
                InitMappedObjects();
            }

            if (m_mappedObjects.TryGetValue(uid, out var mappedObj))
            {
                element = mappedObj.elem;
                return true;
            }

            element = null;
            return false;
        }
        protected virtual IEnumerable<(UnityEngine.Object, IContainerElement)> GetMappedObjects()
        {
            if (m_mappedObjects == null)
            {
                InitMappedObjects();
            }

            foreach (var (_, mappedObj) in m_mappedObjects)
            {
                yield return (mappedObj.obj, mappedObj.elem);
            }
        }

        #endregion

        #region Public Accessors

        // GETTERS
        public virtual UnityEngine.Object GetByUID(int uid)
        {
            if (TryGetByUID(uid, out var obj))
            {
                return obj;
            }
            return null;
        }
        public virtual bool TryGetByUID(int uid, out UnityEngine.Object obj)
        {
            return TryGetObjectFromMappedObjects(uid, out obj);
        }
        
        public virtual T GetByUID<T>(int uid) where T : UnityEngine.Object, IContainerElement
        {
            if (TryGetByUID<T>(uid, out var obj))
            {
                return obj;
            }
            return null;
        }
        public virtual bool TryGetByUID<T>(int uid, out T result) where T : UnityEngine.Object, IContainerElement
        {
            if (TryGetObjectFromMappedObjects(uid, out var obj) && obj is T t)
            {
                result = t;
                return true;
            }
            result = null;
            return false;
        }

        // ITERATORS
        public virtual IEnumerable<UnityEngine.Object> GetObjects()
        {
            return m_objects;
        }

        public virtual IEnumerable<T> GetObjectsOfType<T>() where T : UnityEngine.Object, IContainerElement
        {
            foreach (var obj in m_objects)
            {
                if (obj != null && obj is T t)
                {
                    yield return t;
                }
            }
        }

        // ALIAS
        public virtual bool TryFindObjectByAlias(string alias, out UnityEngine.Object result)
        {
            foreach (var (obj, elem) in GetMappedObjects())
            {
                if (!string.IsNullOrEmpty(elem.Alias)
                    && elem.Alias.Equals(alias, System.StringComparison.InvariantCultureIgnoreCase))
                {
                    result = obj;
                    return true;
                }
            }

            result = null;
            return false;
        }
        public virtual bool TryFindObjectByAlias<T>(string alias, out T result) where T : UnityEngine.Object, IContainerElement
        {
            foreach (var (obj, elem) in GetMappedObjects())
            {
                if (!string.IsNullOrEmpty(elem.Alias)
                    && elem.Alias.Equals(alias, System.StringComparison.InvariantCultureIgnoreCase)
                    && obj is T t)
                {
                    result = t;
                    return true;
                }
            }

            result = null;
            return false;
        }

        #endregion


        // --- EDITOR ---

#if UNITY_EDITOR

        #region List Callbacks

        public virtual void Editor_OnNewObjectInContainer(UnityEngine.Object newObj, IContainerElement newElem)
        {
            Editor_EnsureNewObjectValidUID(newObj, newElem);
        }

        #endregion

        #region Utility

        protected virtual void Editor_EnsureNewObjectValidUID(UnityEngine.Object newObj, IContainerElement newElem)
        {
            if (newElem.UID == 0)
            {
                newElem.Editor_SetUID(IContainerElement.ComputeContainerElementUID(newObj));
            }

            var conflict = false;
            var offset = 0;
            do
            {
                conflict = false;

                foreach (var obj in m_objects)
                {
                    if (obj != null && obj is IContainerElement elem)
                    {
                        if (elem.UID == newElem.UID)
                        {
                            conflict = true;
                            offset++;
                            newElem.Editor_SetUID(IContainerElement.ComputeContainerElementUID(newObj, offset));
                            break;
                        }
                    }
                }
            } while (conflict);
        }

        #endregion

#endif
    }

    #region Editor

#if UNITY_EDITOR

    [CustomEditor(typeof(Container), editorForChildClasses: true)]
    public class ContainerEditor : Editor
    {
        #region Database Core GUI

        public virtual void OnDatabaseGUI()
        {

        }

        #endregion
    }

#endif

    #endregion
}
