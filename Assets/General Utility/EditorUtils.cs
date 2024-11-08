using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;


#if UNITY_EDITOR
using UnityEditor;

namespace Dhs5.Utility.Editors
{
    public static class EditorUtils
    {
        #region Editor Events

        public static void SelectAndFocusAsset(UnityEngine.Object obj)
        {
            EditorUtility.FocusProjectWindow();
            if (obj is GameObject go)
            {
                Selection.activeGameObject = go;
            }
            else
            {
                Selection.activeObject = obj;
            }
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
        /// <summary>
        /// Be aware that it will be immediate and can't be undone
        /// </summary>
        public static void TriggerAssetHardDeletion(UnityEngine.Object obj)
        {
            SelectFocusAssetAndSendCommand(obj, "Delete");
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
    }
}

#endif
