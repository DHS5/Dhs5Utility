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
                if (type == typeof(Rect))
                {
                    return SerializedPropertyType.Rect;
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

            public IEnumerable<MemberInfo> GetAllMemberInfos(bool staticOnly = false)
            {
                foreach (var (fieldInfo, _) in fieldInfos)
                {
                    if (!staticOnly || fieldInfo.IsStatic) yield return fieldInfo;
                }
                foreach (var (propertyInfo, _) in propertyInfos)
                {
                    if (!staticOnly || IsStatic(propertyInfo)) yield return propertyInfo;
                }
            }
        }

        /// <summary>
        /// Static classes are registered without an instance : only their static members can be read
        /// </summary>
        private static bool IsStatic(PropertyInfo propertyInfo)
        {
            var getter = propertyInfo.GetGetMethod(true);
            return getter != null && getter.IsStatic;
        }

        #endregion

        #region STRUCT MemberSnapshot

        public struct MemberSnapshot
        {
            public MemberSnapshot(UnityEngine.Object obj, FieldInfo fieldInfo, MemberDebugInformations informations)
            {
                this.name = ObjectNames.NicifyVariableName(fieldInfo.Name);
                this.valueType = fieldInfo.FieldType;
                this.propertyType = informations.propertyType;
                this.value = null;
                this.error = null;
                try
                {
                    this.value = fieldInfo.GetValue(obj);
                }
                catch (Exception e)
                {
                    this.error = GetErrorMessage(e);
                }
            }
            public MemberSnapshot(UnityEngine.Object obj, PropertyInfo propertyInfo, MemberDebugInformations informations)
            {
                this.name = ObjectNames.NicifyVariableName(propertyInfo.Name);
                this.valueType = propertyInfo.PropertyType;
                this.propertyType = informations.propertyType;
                this.value = null;
                this.error = null;
                // The getter can throw (e.g. accessing a destroyed component) : it must not break the debugger GUI
                try
                {
                    this.value = propertyInfo.GetValue(obj);
                }
                catch (Exception e)
                {
                    this.error = GetErrorMessage(e);
                }
            }

            public readonly string name;
            public readonly object value;
            /// <summary>
            /// Declared type of the member (the value can be null)
            /// </summary>
            public readonly Type valueType;
            public readonly SerializedPropertyType propertyType;
            /// <summary>
            /// Not null if the value couldn't be read
            /// </summary>
            public readonly string error;

            private static string GetErrorMessage(Exception e)
            {
                if (e is TargetInvocationException && e.InnerException != null) e = e.InnerException;
                return e.GetType().Name + " : " + e.Message;
            }
        }

        #endregion


        #region Members

        private static Dictionary<Type, TypeInformations> _typeInformations = new();
        private static Dictionary<EDebugCategory, HashSet<UnityEngine.Object>> _registeredObjects = new();
        private static Dictionary<EDebugCategory, HashSet<Type>> _registeredStaticClasses = new();

        #endregion

        #region Engine Callbacks

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            // Keep live objects (e.g. ScriptableObject assets registered in OnEnable, which won't re-register)
            foreach (var set in _registeredObjects.Values)
            {
                set.RemoveWhere(obj => obj == null);
            }
            _registeredStaticClasses.Clear();
        }

        #endregion

#endif

        #region Registration

        /// <summary>
        /// Registers <paramref name="obj"/> (MonoBehaviour, ScriptableObject...) to show its [RuntimeDebug] members in the Debugger window's Runtime tab.<br></br>
        /// Editor only : does nothing in builds.
        /// </summary>
        public static void Register(bool register, EDebugCategory category, UnityEngine.Object obj)
        {
#if UNITY_EDITOR
            if (obj == null) return;

            if (register)
            {
                var type = obj.GetType();

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
                
                if (!_registeredObjects[category].Add(obj))
                {
                    Debug.LogWarning(type.Name + " " + obj + " already registered under category " + category);
                }
            }
            else
            {
                if (_registeredObjects.TryGetValue(category, out var set))
                {
                    set.Remove(obj);
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
                // DeclaredOnly on each type of the hierarchy : GetFields/GetProperties/GetMethods on the type itself
                // don't return the private members of its base classes
                var bindingFlags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
                RuntimeDebugAttribute attribute = null;

                Dictionary<FieldInfo, MemberDebugInformations> fieldInfos = new();
                Dictionary<PropertyInfo, MemberDebugInformations> propertyInfos = new();
                Dictionary<MethodInfo, MethodDebugInformations> methodInfos = new();
                // Overridden properties/methods are declared again in derived types : only the most derived one is kept
                HashSet<string> propertyNames = new();
                HashSet<string> methodNames = new();

                for (var currentType = type; currentType != null && currentType != typeof(object); currentType = currentType.BaseType)
                {
                    foreach (var fieldInfo in currentType.GetFields(bindingFlags))
                    {
                        attribute = fieldInfo.GetCustomAttribute<RuntimeDebugAttribute>();
                        if (attribute != null)
                        {
                            fieldInfos.Add(fieldInfo, new MemberDebugInformations(attribute, fieldInfo.FieldType));
                        }
                    }

                    foreach (var propertyInfo in currentType.GetProperties(bindingFlags))
                    {
                        attribute = propertyInfo.GetCustomAttribute<RuntimeDebugAttribute>();
                        if (attribute != null && propertyNames.Add(propertyInfo.Name))
                        {
                            if (propertyInfo.GetIndexParameters().Length > 0)
                            {
                                Debug.LogWarning("Can't register RuntimeDebug indexer " + propertyInfo.Name + " on type " + currentType.Name);
                                continue;
                            }
                            propertyInfos.Add(propertyInfo, new MemberDebugInformations(attribute, propertyInfo.PropertyType));
                        }
                    }

                    foreach (var methodInfo in currentType.GetMethods(bindingFlags))
                    {
                        attribute = methodInfo.GetCustomAttribute<RuntimeDebugAttribute>();
                        if (attribute != null && methodNames.Add(methodInfo.Name))
                        {
                            if (methodInfo.ReturnType == typeof(void)
                                && !methodInfo.GetParameters().IsValid())
                            {
                                methodInfos.Add(methodInfo, new MethodDebugInformations(attribute));
                            }
                            else
                            {
                                Debug.LogWarning("Can't register RuntimeDebugMethod " + methodInfo.Name + " on type " + currentType.Name + " that doesn't return void and/or takes parameters");
                            }
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
                    // Destroyed without being unregistered
                    if (obj == null) continue;

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
                    // Destroyed without being unregistered
                    if (obj == null) continue;

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
                        foreach (var memberInfo in typeInformations.GetAllMemberInfos(staticOnly: true))
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
            => GetMemberSnapshots(obj.GetType(), obj, staticOnly: false, memberFilter: null);
        internal static IEnumerable<MemberSnapshot> GetMemberSnapshotsOfStaticClass(Type type)
            => GetMemberSnapshots(type, null, staticOnly: true, memberFilter: null);
        internal static IEnumerable<MemberSnapshot> GetMemberSnapshotsOfObjectFiltered(UnityEngine.Object obj, string memberFilter)
            => GetMemberSnapshots(obj.GetType(), obj, staticOnly: false, memberFilter);
        internal static IEnumerable<MemberSnapshot> GetMemberSnapshotsOfStaticClassFiltered(Type type, string memberFilter)
            => GetMemberSnapshots(type, null, staticOnly: true, memberFilter);

        /// <param name="obj">Instance to read the members from, null for a static class</param>
        /// <param name="staticOnly">Static classes are registered without an instance : only their static members can be read</param>
        /// <param name="memberFilter">Checked on the name before reading the value : getters of filtered out members are not called</param>
        private static IEnumerable<MemberSnapshot> GetMemberSnapshots(Type type, UnityEngine.Object obj, bool staticOnly, string memberFilter)
        {
            if (!_typeInformations.TryGetValue(type, out var typeInformations)) yield break;

            foreach (var (fieldInfo, debugInfo) in typeInformations.fieldInfos)
            {
                if (staticOnly && !fieldInfo.IsStatic) continue;
                if (!IsMemberNameMatching(fieldInfo, memberFilter)) continue;

                yield return new MemberSnapshot(obj, fieldInfo, debugInfo);
            }

            foreach (var (propertyInfo, debugInfo) in typeInformations.propertyInfos)
            {
                if (staticOnly && !IsStatic(propertyInfo)) continue;
                if (!IsMemberNameMatching(propertyInfo, memberFilter)) continue;

                yield return new MemberSnapshot(obj, propertyInfo, debugInfo);
            }
        }
        private static bool IsMemberNameMatching(MemberInfo memberInfo, string memberFilter)
        {
            return string.IsNullOrEmpty(memberFilter)
                || ObjectNames.NicifyVariableName(memberInfo.Name).Contains(memberFilter, StringComparison.InvariantCultureIgnoreCase);
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
                    // Registered without an instance : only static methods can be called
                    if (!methodInfo.IsStatic) continue;

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
