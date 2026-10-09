using System;
using System.IO;
using UnityEngine;

namespace Dhs5.Utility
{
    public static class UtilityMethods
    {
        #region Vectors

        public static float FlatDistance(Vector3 v1, Vector3 v2)
        {
            float num = v1.x - v2.x;
            float num2 = v1.z - v2.z;
            return (float)Mathf.Sqrt(num * num + num2 * num2);
        }

        #endregion

        #region Directory

        public static void EnsureDirectoryExistence(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
            {
                string[] pathMembers = directoryPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
                string currentPath = "";

                for (int i = 0; i < pathMembers.Length; i++)
                {
                    currentPath += pathMembers[i];
                    if (!Directory.Exists(currentPath))
                    {
                        Directory.CreateDirectory(currentPath);
                    }
                    currentPath += "/";
                }
            }
        }
        public static void EnsureAssetParentDirectoryExistence(string assetPath)
        {
            // Works with relative and absolute paths, with '/' or '\' separators
            // (creates every missing directory in the path)
            var directoryPath = Path.GetDirectoryName(assetPath);
            if (!string.IsNullOrEmpty(directoryPath) && !Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
        }

        #endregion
    }
}
