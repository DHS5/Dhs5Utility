using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dhs5.Utility.Updates;
using UnityEditor;
using UnityEngine;

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


        #region SCRIPT Writer

        public class ScriptWriter
        {
            #region ENUM ScriptType

            public enum EScriptType
            {
                NONE = 0,
                CLASS = 1,
                STATIC_CLASS = 2,
                STRUCT = 3,
                ENUM = 4,
                INTERFACE = 5,
            }

            #endregion

            #region ENUM Protection

            public enum EProtection
            {
                PUBLIC = 0,
                PROTECTED = 1,
                PRIVATE = 2,
            }

            #endregion


            #region Members

            protected readonly StringBuilder baseStringBuilder;
            protected readonly StringBuilder usingStringBuilder;

            public readonly EProtection scriptProtection;
            public readonly EScriptType scriptType;
            public readonly string scriptName;
            public readonly string scriptNamespace;

            #endregion

            #region Constructor

            public ScriptWriter(EScriptType scriptType, string scriptName, string scriptNamespace, EProtection scriptProtection = EProtection.PUBLIC)
            {
                baseStringBuilder = new StringBuilder();
                usingStringBuilder = new StringBuilder();

                this.scriptProtection = scriptProtection;
                this.scriptType = scriptType;
                this.scriptName = scriptName;
                this.scriptNamespace = scriptNamespace;
            }

            #endregion
        }

        #region ENUM Writer

        public class EnumWriter : ScriptWriter
        {
            #region ENUM EnumType

            public enum EEnumType
            {
                INT32 = 0,
                INT64 = 1,
                INT8 = 2,
            }

            #endregion

            #region Members

            public readonly EEnumType enumType;
            public readonly string[] enumContent;

            #endregion

            #region Constructors

            public EnumWriter(string enumName, string[] enumContent, string enumNamespace, EEnumType enumType = EEnumType.INT32, EProtection enumProtection = EProtection.PUBLIC) 
                : base(EScriptType.ENUM, enumName, enumNamespace, enumProtection)
            {
                this.enumType = enumType;
                this.enumContent = enumContent;
            }

            #endregion


            // --- STATIC ---

            #region Static Utility

            public static string EnsureCorrectEnumName(string inputName)
            {
                var forbiddenCharacters = new char[] { ' ', '/', '\\', '<', '>', ':', ';', '*', '|', '"', '?', '!', '=', '+', '-', '.', ',', '\'', '{', '}', '(', ')', '[', ']',
                '#', '&', '~', '¨', '^', '`', '°', '€', '$', '£', '¤', '%', 'é', 'è', 'ç', 'à', 'ù', '@', '§', 'µ' };
                var result = inputName.Trim(forbiddenCharacters);
                foreach (var c in forbiddenCharacters)
                {
                    result = result.Replace(c, '_');
                }
                return result.ToUpper();
            }

            #endregion
        }

        #endregion

        #endregion
    }
}
