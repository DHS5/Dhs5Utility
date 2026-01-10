using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Dhs5.Utility.Debugger
{
    public static class RuntimeDebugger
    {
        #region STRUCT TypeInformations

        private struct TypeInformations
        {
            public TypeInformations(List<FieldInfo> fieldInfos, List<PropertyInfo> propertyInfos)
            {
                this.fieldInfos = new(fieldInfos);
                this.propertyInfos = new(propertyInfos);
            }

            private readonly List<FieldInfo> fieldInfos;
            private readonly List<PropertyInfo> propertyInfos;

            public IEnumerable<KeyValuePair<string, object>> TempGetValues(object obj)
            {
                if (fieldInfos.IsValid())
                {
                    foreach (var fieldInfo in fieldInfos)
                    {
                        yield return new(fieldInfo.Name, fieldInfo.GetValue(obj));
                    }
                }

                if (propertyInfos.IsValid())
                {
                    foreach (var propertyInfo in propertyInfos)
                    {
                        yield return new(propertyInfo.Name, propertyInfo.GetValue(obj));
                    }
                }
            }
        }

        #endregion


        #region Members

        private static Dictionary<Type, TypeInformations> _typeInformations = new();
        private static Dictionary<EDebugCategory, HashSet<UnityEngine.Object>> _registeredObjects = new();

        #endregion

        #region Registration

        public static void Register(bool register, EDebugCategory category, MonoBehaviour monoBehaviour)
        {
            if (register)
            {
                var type = monoBehaviour.GetType();

                if (!_typeInformations.ContainsKey(type)
                    && !ComputeTypeInformations(type))
                {
                    Debug.LogError("Runtime debugger can't handle type " + type.Name);
                    return;
                }

                if (!_registeredObjects.ContainsKey(category))
                {
                    _registeredObjects.Add(category, new());
                }
                
                if (!_registeredObjects[category].Add(monoBehaviour))
                {
                    Debug.LogWarning("MonoBehaviour " + monoBehaviour + " already registered under category " + category);
                }
            }
            else
            {
                if (_registeredObjects.TryGetValue(category, out var set))
                {
                    set.Remove(monoBehaviour);
                }
            }
        }
        public static void Register(bool register, EDebugCategory category, ScriptableObject scriptableObject)
        {
            if (register)
            {
                var type = scriptableObject.GetType();

                if (!_typeInformations.ContainsKey(type)
                    && !ComputeTypeInformations(type))
                {
                    Debug.LogError("Runtime debugger can't handle type " + type.Name);
                    return;
                }

                if (!_registeredObjects.ContainsKey(category))
                {
                    _registeredObjects.Add(category, new());
                }

                if (!_registeredObjects[category].Add(scriptableObject))
                {
                    Debug.LogWarning("ScriptableObject " + scriptableObject + " already registered under category " + category);
                }
            }
            else
            {
                if (_registeredObjects.TryGetValue(category, out var set))
                {
                    set.Remove(scriptableObject);
                }
            }
        }

        #endregion

        #region Type Informations

        private static bool ComputeTypeInformations(Type type)
        {
            try
            {
                var bindingFlags = BindingFlags.Instance| BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
                RuntimeDebugAttribute attribute = null;

                List<FieldInfo> fieldInfos = new();
                foreach (var fieldInfo in type.GetFields(bindingFlags))
                {
                    attribute = fieldInfo.GetCustomAttribute<RuntimeDebugAttribute>();
                    if (attribute != null)
                    {
                        fieldInfos.Add(fieldInfo);
                    }
                }

                List<PropertyInfo> propertyInfos = new();
                foreach (var propertyInfo in type.GetProperties(bindingFlags))
                {
                    attribute = propertyInfo.GetCustomAttribute<RuntimeDebugAttribute>();
                    if (attribute != null)
                    {
                        propertyInfos.Add(propertyInfo);
                    }
                }

                _typeInformations.Add(type, new(fieldInfos, propertyInfos));
                return true;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return false;
            }
        }

        #endregion
    }
}
