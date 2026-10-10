using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.IO;


#if UNITY_EDITOR
using UnityEditor;

namespace Dhs5.Utility.Editors
{
    public static class EditorDataUtility
    {
        #region Type Queries

        /// <summary>
        /// Every type deriving from <paramref name="type"/> (or implementing it, for an interface), <paramref name="type"/> excluded.<br></br>
        /// Uses Unity's TypeCache : instant, no scan of the assemblies.
        /// </summary>
        public static IEnumerable<Type> GetAllChildTypes(Type type)
        {
            return TypeCache.GetTypesDerivedFrom(type);
        }
        public static IEnumerable<Type> GetAllChildTypes(Type type, Func<Type, bool> predicate)
        {
            if (predicate == null) return GetAllChildTypes(type);

            return GetAllChildTypes(type)
                .Where(t => predicate(t));
        }

        #endregion

        #region Data Creation

        public static UnityEngine.Object CreateAssetOfType(Type type, string path, bool triggerRename = false)
        {
            if (type.IsSubclassOf(typeof(ScriptableObject)))
            {
                return CreateScriptableAsset(type, path, triggerRename);
            }
            else if (type.IsSubclassOf(typeof(Component)))
            {
                return CreatePrefabWithComponent(type, path, triggerRename);
            }
            else if (type == typeof(GameObject))
            {
                return CreateEmptyPrefab(path, triggerRename);
            }
            else if (type == typeof(TextAsset))
            {
                return CreateEmptyTextAsset(path);
            }
            return null;
        }

        // --- Scriptable ---
        public static ScriptableObject CreateScriptableAsset(Type type, string path, bool triggerRename = false)
        {
            if (!path.EndsWith(".asset")) path += ".asset";
            UtilityMethods.EnsureAssetParentDirectoryExistence(path);
            path = AssetDatabase.GenerateUniqueAssetPath(path);
            var obj = ScriptableObject.CreateInstance(type);
            if (obj != null)
            {
                AssetDatabase.CreateAsset(obj, path);
                AssetDatabase.SaveAssetIfDirty(obj);
                if (triggerRename) EditorUtils.TriggerAssetRename(obj);
            }
            return obj;
        }
        public static T CreateScriptableAsset<T>(string path, bool triggerRename = false) where T : ScriptableObject
        {
            if (!path.EndsWith(".asset")) path += ".asset";
            UtilityMethods.EnsureAssetParentDirectoryExistence(path);
            path = AssetDatabase.GenerateUniqueAssetPath(path);
            var obj = ScriptableObject.CreateInstance<T>();
            if (obj != null)
            {
                AssetDatabase.CreateAsset(obj, path);
                AssetDatabase.SaveAssetIfDirty(obj);
                if (triggerRename) EditorUtils.TriggerAssetRename(obj);
            }
            return obj;
        }
        public static ScriptableObject CreateScriptableAndAddToAsset(Type type, UnityEngine.Object asset)
        {
            var obj = ScriptableObject.CreateInstance(type);
            obj.name = "New";
            if (obj != null)
            {
                AssetDatabase.AddObjectToAsset(obj, AssetDatabase.GetAssetPath(asset));
                AssetDatabase.SaveAssets();
            }
            return obj;
        }
        public static T CreateScriptableAndAddToAsset<T>(UnityEngine.Object asset) where T : ScriptableObject
        {
            var obj = ScriptableObject.CreateInstance<T>();
            if (obj != null)
            {
                AssetDatabase.AddObjectToAsset(obj, AssetDatabase.GetAssetPath(asset));
                AssetDatabase.SaveAssets();
            }
            return obj;
        }

        // --- Prefab ---
        public static GameObject CreateEmptyPrefab(string path, bool triggerRename = false)
        {
            if (!path.EndsWith(".prefab")) path += ".prefab";
            UtilityMethods.EnsureAssetParentDirectoryExistence(path);
            path = AssetDatabase.GenerateUniqueAssetPath(path);
            var template = new GameObject();
            var obj = PrefabUtility.SaveAsPrefabAsset(template, path, out var success);
            UnityEngine.Object.DestroyImmediate(template);
            if (success)
            {
                if (triggerRename) EditorUtils.TriggerAssetRename(obj);
                return obj;
            }
            return null;
        }
        public static Component CreatePrefabWithComponent(Type behaviourType, string path, bool triggerRename = false)
        {
            var obj = CreateEmptyPrefab(path, triggerRename);
            if (obj != null && !behaviourType.IsAbstract)
            {
                var component = obj.AddComponent(behaviourType);
                PrefabUtility.SavePrefabAsset(obj);
                return component;
            }
            return null;
        }

        // --- Scripts ---
        public static TextAsset CreateOrOverwriteTextAsset(string path, string content)
        {
            UtilityMethods.EnsureAssetParentDirectoryExistence(path);
            File.WriteAllText(path, content);
            AssetDatabase.Refresh();
            AssetDatabase.SaveAssets();
            return AssetDatabase.LoadAssetAtPath<TextAsset>(path);
        }
        public static TextAsset CreateOrOverwriteTextAsset(TextAsset textAsset, string content)
        {
            return CreateOrOverwriteTextAsset(AssetDatabase.GetAssetPath(textAsset), content);
        }
        public static TextAsset CreateEmptyTextAsset(string path)
        {
            UtilityMethods.EnsureAssetParentDirectoryExistence(path);
            File.WriteAllText(path, "");
            AssetDatabase.Refresh();
            AssetDatabase.SaveAssets();
            return AssetDatabase.LoadAssetAtPath<TextAsset>(path);
        }
        public static TextAsset CreateOrLoadTextAsset(string path)
        {
            if (File.Exists(path))
            {
                return AssetDatabase.LoadAssetAtPath<TextAsset>(path);
            }

            UtilityMethods.EnsureAssetParentDirectoryExistence(path);
            File.WriteAllText(path, "");
            AssetDatabase.Refresh();
            AssetDatabase.SaveAssets();
            return AssetDatabase.LoadAssetAtPath<TextAsset>(path);
        }

        #endregion

        #region Asset Validity

        public static IEnumerable<UnityEngine.Object> GetSubAssets(UnityEngine.Object asset)
        {
            if (!AssetDatabase.IsMainAsset(asset))
            {
                Debug.LogError("Can't get sub asset from object inside asset");
            }
            else
            {
                var subAssets = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(asset));

                if (subAssets.IsValid())
                {
                    for (int i = 0; i < subAssets.Length; i++)
                    {
                        var subAsset = subAssets[i];
                        if (subAsset == asset) continue;

                        yield return subAsset;
                    }
                }
            }
        }

        /// <summary>
        /// Ensure no invalid objects are inside this asset
        /// </summary>
        /// <param name="asset">Asset to inspect</param>
        /// <param name="keepObject">If predicate returns TRUE, keep the object</param>
        public static void EnsureAssetValidity(UnityEngine.Object asset, Func<UnityEngine.Object, bool> keepObject)
        {
            if (!AssetDatabase.IsMainAsset(asset))
            {
                Debug.LogError("Can't ensure validity of object inside asset");
                return;
            }

            var subAssets = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(asset));

            if (subAssets.IsValid())
            {
                for (int i = subAssets.Length - 1; i >= 0; i--)
                {
                    var subAsset = subAssets[i];
                    if (subAsset == asset) continue;

                    if (!keepObject.Invoke(subAsset))
                    {
                        AssetDatabase.RemoveObjectFromAsset(subAsset);
                        GameObject.DestroyImmediate(subAsset);
                    }
                }

                AssetDatabase.SaveAssetIfDirty(asset);
            }
        }

        #endregion
    }
}

#endif