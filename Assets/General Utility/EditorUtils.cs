using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
using System.IO;

namespace Dhs5.Utility.Editors
{
    public static class EditorUtils
    {
        #region Editor Events

        public static void SelectAndFocusAsset(UnityEngine.Object obj)
        {
            EditorUtility.FocusProjectWindow();
            SelectObject(obj);
        }

        public static void TriggerAssetRename(UnityEngine.Object obj, bool delay = false)
        {
            SelectAndFocusAsset(obj);
            if (delay)
            {
                EditorGUIUtility.PingObject(obj);
                DelayCall(0.2d, SendRenameEvent);
            }
            else
            {
                SendRenameEvent();
            }
        }
        private static void SendRenameEvent()
        {
            EditorWindow.focusedWindow.SendEvent(new Event() { keyCode = KeyCode.F2, type = EventType.KeyDown });
        }

        private static void SelectFocusAssetAndSendCommand(UnityEngine.Object obj, string command)
        {
            SelectAndFocusAsset(obj);
            EditorWindow.focusedWindow.SendEvent(EditorGUIUtility.CommandEvent(command));
        }

        public static void TriggerAssetDeletion(UnityEngine.Object obj)
        {
            SelectFocusAssetAndSendCommand(obj, "SoftDelete");
        }

        public static void CopyAsset(UnityEngine.Object obj)
        {
            SelectFocusAssetAndSendCommand(obj, "Copy");
        }
        public static void CutAsset(UnityEngine.Object obj)
        {
            SelectFocusAssetAndSendCommand(obj, "Cut");
        }
        public static void DuplicateAsset(UnityEngine.Object obj)
        {
            SelectFocusAssetAndSendCommand(obj, "Duplicate");
        }
        public static void PasteAsset()
        {
            EditorUtility.FocusProjectWindow();
            EditorWindow.focusedWindow.SendEvent(EditorGUIUtility.CommandEvent("Paste"));
        }

        #endregion

        #region Editor Update

        private static HashSet<EditorDelayedCall> _delayedCalls;
        private static bool _editorUpdateRegistered;

        private static void RegisterToUpdate()
        {
            if (!_editorUpdateRegistered)
            {
                _editorUpdateRegistered = true;
                EditorApplication.update += OnEditorUpdate;
            }
        }
        private static void UnregisterToUpdate()
        {
            if (_editorUpdateRegistered 
                && !_delayedCalls.IsValid())
            {
                _editorUpdateRegistered = false;
                EditorApplication.update -= OnEditorUpdate;
            }
        }

        private static void OnEditorUpdate()
        {
            CheckDelayedCalls();
        }

        #endregion

        #region Delayed Call

        private struct EditorDelayedCall
        {
            public EditorDelayedCall(double triggerTime, Action callback)
            {
                this.triggerTime = triggerTime;
                this.callback = callback;
            }

            public readonly double triggerTime;
            private Action callback;

            public bool TriggerIfValid(double editorTime)
            {
                if (editorTime >= triggerTime)
                {
                    callback?.Invoke();
                    return true;
                }
                return false;
            }
        }
        public static void DelayCall(double delay, Action callback)
        {
            if (_delayedCalls == null) _delayedCalls = new();

            _delayedCalls.Add(new EditorDelayedCall(EditorApplication.timeSinceStartup + delay, callback));
            RegisterToUpdate();
        }

        private static void CheckDelayedCalls()
        {
            if (_delayedCalls.IsValid())
            {
                List<EditorDelayedCall> toDelete = new();

                double editorTime = EditorApplication.timeSinceStartup;
                foreach (var call in _delayedCalls)
                {
                    if (call.TriggerIfValid(editorTime))
                    {
                        toDelete.Add(call);
                    }
                }

                if (toDelete.IsValid())
                {
                    foreach (var call in toDelete)
                    {
                        _delayedCalls.Remove(call);
                    }
                }

                if (!_delayedCalls.IsValid())
                {
                    UnregisterToUpdate();
                }
            }
        }

        #endregion

        #region Objects Utility

        public static void SelectObject(UnityEngine.Object obj)
        {
            if (obj is GameObject go)
            {
                Selection.activeGameObject = go;
            }
            else
            {
                Selection.activeObject = obj;
            }
        }
        public static void PingObject(UnityEngine.Object obj)
        {
            EditorGUIUtility.PingObject(obj);
        }
        public static void FocusProjectWindow()
        {
            EditorUtility.FocusProjectWindow();
        }

        public static void FullPingObject(UnityEngine.Object obj)
        {
            FocusProjectWindow();
            PingObject(obj);
        }

        public static string GetAssetContainingFolder(UnityEngine.Object obj)
        {
            return ProjectWindowUtil.GetContainingFolder(AssetDatabase.GetAssetPath(obj));
        }

        #endregion

        #region Directory Utility

        public static void OpenFolder(string path)
        {
            if (Directory.Exists(path))
            {
                FocusProjectWindow();
                var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
                
                AssetDatabase.OpenAsset(obj);
                DelayCall(0.05d, () => AssetDatabase.OpenAsset(obj));
            }
            else
            {
                Debug.LogWarning("Directory doesn't exists");
            }
        }

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
            var index = assetPath.LastIndexOf('/');
            if (index != -1)
            {
                string directoryPath = assetPath.Substring(0, index);
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
        }

        #endregion


        #region Menu Items

        #region Scenes

        [MenuItem("Assets/Add to build settings", priority = 101)]
        private static void AddToBuildSettings(MenuCommand menuCommand)
        {
            List<EditorBuildSettingsScene> newScenes = new();
            var currentScenes = EditorBuildSettings.scenes.ToList();
            var currentScenePaths = currentScenes.Select(s => s.path).ToList();

            string path;
            foreach (var obj in Selection.objects)
            {
                if (obj is SceneAsset asset)
                {
                    path = AssetDatabase.GetAssetPath(asset);
                    if (!currentScenePaths.Contains(path))
                        newScenes.Add(new EditorBuildSettingsScene(path, true));
                }
            }

            currentScenes.AddRange(newScenes);
            EditorBuildSettings.scenes = currentScenes.ToArray();
        }
        [MenuItem("Assets/Add to build settings", isValidateFunction:true)]
        private static bool AddToBuildSettings_Validation(MenuCommand menuCommand)
        {
            foreach (var obj in Selection.objects)
            {
                if (obj is SceneAsset) return true;
            }
            return false;
        }

        #endregion

        #endregion
    }
}

#endif
