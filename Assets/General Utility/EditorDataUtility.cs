using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

namespace Dhs5.Utility.Editors
{
    public static class EditorDataUtility
    {
        #region Type Queries

        public static IEnumerable<Type> GetAllChildTypes(Type type)
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                        .SelectMany(a => a.GetTypes())
                        .Where(t => t.IsSubclassOf(type));
        }
        public static IEnumerable<Type> GetAllChildTypes(Type type, Func<Type, bool> predicate)
        {
            if (predicate == null) return GetAllChildTypes(type);

            return GetAllChildTypes(type)
                .Where(t => predicate(t));
        }

        #endregion

        #region Data Creation



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