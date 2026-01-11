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

        #region STRUCT MemberDebugInformations

        public struct MemberDebugInformations
        {
            public MemberDebugInformations(RuntimeDebugAttribute attribute, Type type)
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

        #region STRUCT MethodDebugInformations

        public struct MethodDebugInformations
        {
            public MethodDebugInformations(RuntimeDebugAttribute attribute)
            {
                this.attribute = attribute;
            }

            public readonly RuntimeDebugAttribute attribute;
        }

        #endregion

        #region STRUCT TypeInformations

        private struct TypeInformations
        {
            public TypeInformations(
                Dictionary<FieldInfo, MemberDebugInformations> fieldInfos, 
                Dictionary<PropertyInfo, MemberDebugInformations> propertyInfos,
                Dictionary<MethodInfo, MethodDebugInformations> methodInfos)
            {
                this.fieldInfos = new(fieldInfos);
                this.propertyInfos = new(propertyInfos);
                this.methodInfos = new(methodInfos);
            }

            public readonly Dictionary<FieldInfo, MemberDebugInformations> fieldInfos;
            public readonly Dictionary<PropertyInfo, MemberDebugInformations> propertyInfos;
            public readonly Dictionary<MethodInfo, MethodDebugInformations> methodInfos;

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
            public MemberSnapshot(UnityEngine.Object obj, FieldInfo fieldInfo, MemberDebugInformations informations)
            {
                this.name = ObjectNames.NicifyVariableName(fieldInfo.Name);
                this.value = fieldInfo.GetValue(obj);
                this.propertyType = informations.propertyType;
            }
            public MemberSnapshot(UnityEngine.Object obj, PropertyInfo propertyInfo, MemberDebugInformations informations)
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
        private static Dictionary<EDebugCategory, HashSet<Type>> _registeredStaticClasses = new();

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

        public static void RegisterStaticClass(bool register, EDebugCategory category, Type type)
        {
#if UNITY_EDITOR
            if (register)
            {
                if (!_typeInformations.ContainsKey(type)
                    && !ComputeTypeInformations(type))
                {
                    Debug.LogError("Runtime debugger can't handle type " + type.Name);
                    return;
                }

                if (!_registeredStaticClasses.ContainsKey(category))
                {
                    _registeredStaticClasses.Add(category, new());
                }

                if (!_registeredStaticClasses[category].Add(type))
                {
                    Debug.LogWarning("Static Class of type " + type.Name + " already registered under category " + category);
                }
            }
            else
            {
                if (_registeredStaticClasses.TryGetValue(category, out var set))
                {
                    set.Remove(type);
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

                Dictionary<FieldInfo, MemberDebugInformations> fieldInfos = new();
                foreach (var fieldInfo in type.GetFields(bindingFlags))
                {
                    attribute = fieldInfo.GetCustomAttribute<RuntimeDebugAttribute>();
                    if (attribute != null)
                    {
                        fieldInfos.Add(fieldInfo, new MemberDebugInformations(attribute, fieldInfo.FieldType));
                    }
                }

                Dictionary<PropertyInfo, MemberDebugInformations> propertyInfos = new();
                foreach (var propertyInfo in type.GetProperties(bindingFlags))
                {
                    attribute = propertyInfo.GetCustomAttribute<RuntimeDebugAttribute>();
                    if (attribute != null)
                    {
                        propertyInfos.Add(propertyInfo, new MemberDebugInformations(attribute, propertyInfo.PropertyType));
                    }
                }

                Dictionary<MethodInfo, MethodDebugInformations> methodInfos = new();
                foreach (var methodInfo in type.GetMethods(bindingFlags))
                {
                    attribute = methodInfo.GetCustomAttribute<RuntimeDebugAttribute>();
                    if (attribute != null)
                    {
                        if (methodInfo.ReturnType == typeof(void)
                            && !methodInfo.GetParameters().IsValid())
                        {
                            methodInfos.Add(methodInfo, new MethodDebugInformations(attribute));
                        }
                        else
                        {
                            Debug.LogWarning("Can't register RuntimeDebugMethod " + methodInfo.Name + " on type " + type.Name + " that doesn't return void and/or takes parameters");
                        }
                    }
                }

                _typeInformations.Add(type, new(fieldInfos, propertyInfos, methodInfos));
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

        internal static IEnumerable<UnityEngine.Object> GetRegisteredObjectsOfCategory(EDebugCategory category)
        {
            if (_registeredObjects.TryGetValue(category, out var objects)
                && objects.IsValid())
            {
                foreach (var obj in objects)
                {
                    yield return obj;
                }
            }
        }
        internal static IEnumerable<Type> GetRegisteredStaticClassesOfCategory(EDebugCategory category)
        {
            if (_registeredStaticClasses.TryGetValue(category, out var types)
                && types.IsValid())
            {
                foreach (var type in types)
                {
                    yield return type;
                }
            }
        }
        internal static IEnumerable<UnityEngine.Object> GetRegisteredObjectsOfCategoryNameFiltered(EDebugCategory category, string nameFilter)
        {
            if (_registeredObjects.TryGetValue(category, out var objects)
                && objects.IsValid())
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
        internal static IEnumerable<Type> GetRegisteredStaticClassesOfCategoryNameFiltered(EDebugCategory category, string nameFilter)
        {
            if (_registeredStaticClasses.TryGetValue(category, out var staticClasses)
                && staticClasses.IsValid())
            {
                foreach (var type in staticClasses)
                {
                    if (type.Name.Contains(nameFilter, StringComparison.InvariantCultureIgnoreCase))
                    {
                        yield return type;
                    }
                }
            }
        }
        internal static IEnumerable<UnityEngine.Object> GetRegisteredObjectsOfCategoryMemberFiltered(EDebugCategory category, string memberFilter)
        {
            if (_registeredObjects.TryGetValue(category, out var objects)
                && objects.IsValid())
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
        internal static IEnumerable<Type> GetRegisteredStaticClassesOfCategoryMemberFiltered(EDebugCategory category, string memberFilter)
        {
            if (_registeredStaticClasses.TryGetValue(category, out var staticClasses)
                && staticClasses.IsValid())
            {
                foreach (var type in staticClasses)
                {
                    if (_typeInformations.TryGetValue(type, out var typeInformations))
                    {
                        foreach (var memberInfo in typeInformations.GetAllMemberInfos())
                        {
                            if (ObjectNames.NicifyVariableName(memberInfo.Name).Contains(memberFilter, StringComparison.InvariantCultureIgnoreCase))
                            {
                                yield return type;
                                break;
                            }
                        }
                    }
                }
            }
        }

        internal static IEnumerable<MemberSnapshot> GetMemberSnapshotsOfObject(UnityEngine.Object obj)
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
        internal static IEnumerable<MemberSnapshot> GetMemberSnapshotsOfStaticClass(Type type)
        {
            if (_typeInformations.TryGetValue(type, out var typeInformations))
            {
                foreach (var (fieldInfo, debugInfo) in typeInformations.fieldInfos)
                {
                    yield return new MemberSnapshot(null, fieldInfo, debugInfo);
                }
                
                foreach (var (propertyInfo, debugInfo) in typeInformations.propertyInfos)
                {
                    yield return new MemberSnapshot(null, propertyInfo, debugInfo);
                }
            }
        }
        internal static IEnumerable<MemberSnapshot> GetMemberSnapshotsOfObjectFiltered(UnityEngine.Object obj, string memberFilter)
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
        internal static IEnumerable<MemberSnapshot> GetMemberSnapshotsOfStaticClassFiltered(Type type, string memberFilter)
        {
            if (_typeInformations.TryGetValue(type, out var typeInformations))
            {
                foreach (var (fieldInfo, debugInfo) in typeInformations.fieldInfos)
                {
                    var memberSnapshot = new MemberSnapshot(null, fieldInfo, debugInfo);
                    if (memberSnapshot.name.Contains(memberFilter, StringComparison.InvariantCultureIgnoreCase))
                    { 
                        yield return memberSnapshot; 
                    }
                }
                
                foreach (var (propertyInfo, debugInfo) in typeInformations.propertyInfos)
                {
                    var memberSnapshot = new MemberSnapshot(null, propertyInfo, debugInfo);
                    if (memberSnapshot.name.Contains(memberFilter, StringComparison.InvariantCultureIgnoreCase))
                    {
                        yield return memberSnapshot;
                    }
                }
            }
        }

        internal static void InvokeRuntimeDebugMethodsOfObject(UnityEngine.Object obj)
        {
            if (_typeInformations.TryGetValue(obj.GetType(), out var typeInformations))
            {
                foreach (var (methodInfo, debugInfo) in typeInformations.methodInfos)
                {
                    try
                    {
                        methodInfo.Invoke(obj, null);
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                    }
                }
            }
        }
        internal static void InvokeRuntimeDebugMethodsOfStaticClass(Type type)
        {
            if (_typeInformations.TryGetValue(type, out var typeInformations))
            {
                foreach (var (methodInfo, debugInfo) in typeInformations.methodInfos)
                {
                    try
                    {
                        methodInfo.Invoke(null, null);
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                    }
                }
            }
        }

        #endregion

#endif
    }
}
