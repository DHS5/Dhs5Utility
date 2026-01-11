using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using System.Reflection;
#endif

namespace Dhs5.Utility.Debugger
{
    public static class RuntimeDebugger
    {
#if UNITY_EDITOR

        #region STRUCT MemberInformations

        public struct DebugInformations
        {
            public DebugInformations(RuntimeDebugAttribute attribute, Type type)
            {
                this.attribute = attribute;
                this.propertyType = GetPropertyTypeFromType(type);
            }

            public readonly RuntimeDebugAttribute attribute;
            public readonly SerializedPropertyType propertyType;

            private static SerializedPropertyType GetPropertyTypeFromType(Type type)
            {
                if (typeof(UnityEngine.Object).IsAssignableFrom(type))
                {
                    return SerializedPropertyType.ObjectReference;
                }
                if (type == typeof(int) || type == typeof(long) || type == typeof(short) || type == typeof(byte)
                    || type == typeof(uint) || type == typeof(ulong) || type == typeof(ushort) || type == typeof(sbyte))
                {
                    return SerializedPropertyType.Integer;
                }
                if (type == typeof(float) || type == typeof(double))
                {
                    return SerializedPropertyType.Float;
                }
                if (type == typeof(bool))
                {
                    return SerializedPropertyType.Boolean;
                }
                if (type == typeof(string))
                {
                    return SerializedPropertyType.String;
                }
                if (type == typeof(Color))
                {
                    return SerializedPropertyType.Color;
                }
                if (type == typeof(LayerMask))
                {
                    return SerializedPropertyType.LayerMask;
                }
                if (type.IsEnum)
                {
                    return SerializedPropertyType.Enum;
                }
                if (type == typeof(Vector2))
                {
                    return SerializedPropertyType.Vector2;
                }
                if (type == typeof(Vector3))
                {
                    return SerializedPropertyType.Vector3;
                }
                if (type == typeof(Vector4))
                {
                    return SerializedPropertyType.Vector4;
                }
                if (type == typeof(char))
                {
                    return SerializedPropertyType.Character;
                }
                if (type == typeof(AnimationCurve))
                {
                    return SerializedPropertyType.AnimationCurve;
                }
                if (type == typeof(Bounds))
                {
                    return SerializedPropertyType.Bounds;
                }
                if (type == typeof(Gradient))
                {
                    return SerializedPropertyType.Gradient;
                }
                if (type == typeof(Quaternion))
                {
                    return SerializedPropertyType.Quaternion;
                }
                if (type == typeof(Vector2Int))
                {
                    return SerializedPropertyType.Vector2Int;
                }
                if (type == typeof(Vector3Int))
                {
                    return SerializedPropertyType.Vector3Int;
                }
                if (type == typeof(RectInt))
                {
                    return SerializedPropertyType.RectInt;
                }
                if (type == typeof(BoundsInt))
                {
                    return SerializedPropertyType.BoundsInt;
                }
                if (type == typeof(RenderingLayerMask))
                {
                    return SerializedPropertyType.RenderingLayerMask;
                }
                if (type == typeof(EntityId))
                {
                    return SerializedPropertyType.EntityId;
                }
                return SerializedPropertyType.Generic;
            }
        }

        #endregion

        #region STRUCT TypeInformations

        private struct TypeInformations
        {
            public TypeInformations(Dictionary<FieldInfo, DebugInformations> fieldInfos, Dictionary<PropertyInfo, DebugInformations> propertyInfos)
            {
                this.fieldInfos = new(fieldInfos);
                this.propertyInfos = new(propertyInfos);
            }

            public readonly Dictionary<FieldInfo, DebugInformations> fieldInfos;
            public readonly Dictionary<PropertyInfo, DebugInformations> propertyInfos;

            public IEnumerable<MemberInfo> GetAllMemberInfos()
            {
                foreach (var (fieldInfo, _) in fieldInfos)
                {
                    yield return fieldInfo;
                }
                foreach (var (propertyInfo, _) in propertyInfos)
                {
                    yield return propertyInfo;
                }
            }
        }

        #endregion

        #region STRUCT MemberSnapshot

        public struct MemberSnapshot
        {
            public MemberSnapshot(UnityEngine.Object obj, FieldInfo fieldInfo, DebugInformations informations)
            {
                this.name = ObjectNames.NicifyVariableName(fieldInfo.Name);
                this.value = fieldInfo.GetValue(obj);
                this.propertyType = informations.propertyType;
            }
            public MemberSnapshot(UnityEngine.Object obj, PropertyInfo propertyInfo, DebugInformations informations)
            {
                this.name = ObjectNames.NicifyVariableName(propertyInfo.Name);
                this.value = propertyInfo.GetValue(obj);
                this.propertyType = informations.propertyType;
            }

            public readonly string name;
            public readonly object value;
            public readonly SerializedPropertyType propertyType;
        }

        #endregion


        #region Members

        private static Dictionary<Type, TypeInformations> _typeInformations = new();
        private static Dictionary<EDebugCategory, HashSet<UnityEngine.Object>> _registeredObjects = new();

        #endregion

#endif

        #region Registration

        public static void Register(bool register, EDebugCategory category, MonoBehaviour monoBehaviour)
        {
#if UNITY_EDITOR
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
#endif
        }
        public static void Register(bool register, EDebugCategory category, ScriptableObject scriptableObject)
        {
#if UNITY_EDITOR
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
#endif
        }

        #endregion

#if UNITY_EDITOR

        #region Type Informations

        private static bool ComputeTypeInformations(Type type)
        {
            try
            {
                var bindingFlags = BindingFlags.Instance| BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
                RuntimeDebugAttribute attribute = null;

                Dictionary<FieldInfo, DebugInformations> fieldInfos = new();
                foreach (var fieldInfo in type.GetFields(bindingFlags))
                {
                    attribute = fieldInfo.GetCustomAttribute<RuntimeDebugAttribute>();
                    if (attribute != null)
                    {
                        fieldInfos.Add(fieldInfo, new DebugInformations(attribute, fieldInfo.FieldType));
                    }
                }

                Dictionary<PropertyInfo, DebugInformations> propertyInfos = new();
                foreach (var propertyInfo in type.GetProperties(bindingFlags))
                {
                    attribute = propertyInfo.GetCustomAttribute<RuntimeDebugAttribute>();
                    if (attribute != null)
                    {
                        propertyInfos.Add(propertyInfo, new DebugInformations(attribute, propertyInfo.PropertyType));
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

        #region Accessors

        public static IEnumerable<KeyValuePair<EDebugCategory, IEnumerable<UnityEngine.Object>>> GetRegisteredObjectsByCategory(EDebugCategoryFlags flags)
        {
            foreach (var value in Enum.GetValues(typeof(EDebugCategory)))
            {
                var category = (EDebugCategory)value;
                if (flags.HasCategory(category) 
                    && _registeredObjects.TryGetValue(category, out var objects) 
                    && objects.IsValid())
                {
                    yield return new KeyValuePair<EDebugCategory, IEnumerable<UnityEngine.Object>>(category, objects);
                }
            }
        }
        public static IEnumerable<UnityEngine.Object> GetRegisteredObjectsNameFiltered(EDebugCategoryFlags flags, string nameFilter)
        {
            foreach (var (category, objects) in _registeredObjects)
            {
                if (flags.HasCategory(category))
                {
                    foreach (var obj in objects)
                    {
                        if (obj.name.Contains(nameFilter, StringComparison.InvariantCultureIgnoreCase))
                        {
                            yield return obj;
                        }
                    }
                }
            }
        }
        public static IEnumerable<UnityEngine.Object> GetRegisteredObjectsMemberFiltered(EDebugCategoryFlags flags, string memberFilter)
        {
            foreach (var (category, objects) in _registeredObjects)
            {
                if (flags.HasCategory(category))
                {
                    foreach (var obj in objects)
                    {
                        if (_typeInformations.TryGetValue(obj.GetType(), out var typeInformations))
                        {
                            foreach (var memberInfo in typeInformations.GetAllMemberInfos())
                            {
                                if (ObjectNames.NicifyVariableName(memberInfo.Name).Contains(memberFilter, StringComparison.InvariantCultureIgnoreCase))
                                {
                                    yield return obj;
                                    break;
                                }
                            }
                        }
                    }
                }
            }
        }

        public static IEnumerable<MemberSnapshot> GetMemberSnapshotsOfObject(UnityEngine.Object obj)
        {
            if (_typeInformations.TryGetValue(obj.GetType(), out var typeInformations))
            {
                foreach (var (fieldInfo, debugInfo) in typeInformations.fieldInfos)
                {
                    yield return new MemberSnapshot(obj, fieldInfo, debugInfo);
                }
                
                foreach (var (propertyInfo, debugInfo) in typeInformations.propertyInfos)
                {
                    yield return new MemberSnapshot(obj, propertyInfo, debugInfo);
                }
            }
        }
        public static IEnumerable<MemberSnapshot> GetMemberSnapshotsOfObjectFiltered(UnityEngine.Object obj, string memberFilter)
        {
            if (_typeInformations.TryGetValue(obj.GetType(), out var typeInformations))
            {
                foreach (var (fieldInfo, debugInfo) in typeInformations.fieldInfos)
                {
                    var memberSnapshot = new MemberSnapshot(obj, fieldInfo, debugInfo);
                    if (memberSnapshot.name.Contains(memberFilter, StringComparison.InvariantCultureIgnoreCase))
                    { 
                        yield return memberSnapshot; 
                    }
                }
                
                foreach (var (propertyInfo, debugInfo) in typeInformations.propertyInfos)
                {
                    var memberSnapshot = new MemberSnapshot(obj, propertyInfo, debugInfo);
                    if (memberSnapshot.name.Contains(memberFilter, StringComparison.InvariantCultureIgnoreCase))
                    {
                        yield return memberSnapshot;
                    }
                }
            }
        }

        #endregion

#endif
    }
}
