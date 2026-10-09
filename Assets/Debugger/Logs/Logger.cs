using UnityEngine;
using Dhs5.Utility.Debugger;
using System.Text;

public static class Logger
{
    // Each log method is written out in full on purpose : calling a shared method would add a frame to the stack trace,
    // pushing the calling code further down in Unity's console

    // LogOnScreen is [Conditional] : on screen logs only exist in the editor and development builds, so in release builds
    // its calls (and the building of their arguments) are removed. The other log methods write to the player log in every build.

    #region Public Log Behaviour

    [HideInCallstack]
    public static void Log(EDebugCategory category, object message, LogType logType, int level = DebuggerAsset.MAX_DEBUGGER_LEVEL, bool onScreen = false, UnityEngine.Object context = null)
    {
        var categoryObj = DebuggerAsset.GetDebugCategoryObject(category);

        if (categoryObj == null)
        {
            Debug.unityLogger.Log(logType, (object)MessageToString(message), context);
            return;
        }

        var canLog = categoryObj.CanLog(logType, level);
        if (!canLog && !StoresLogs) return;

        DebuggerLog log = default;
        if (StoresLogs || onScreen)
        {
            log = new DebuggerLog(category, logType, level, MessageToString(message), context);
            StoreLog(log);
        }

        if (canLog)
        {
            var categorizedMessage = CategorizeMessage(categoryObj, level, message);
            Debug.unityLogger.Log(logType, (object)categorizedMessage, context);

            if (onScreen && Application.isPlaying)
            {
                OnScreenLogger.Log(log);
            }
        }
    }

    // --- LOG ---
    [HideInCallstack]
    public static void Log(EDebugCategory category, object message, int level = DebuggerAsset.MAX_DEBUGGER_LEVEL, bool onScreen = false, UnityEngine.Object context = null)
    {
        var categoryObj = DebuggerAsset.GetDebugCategoryObject(category);
        var logType = LogType.Log;

        if (categoryObj == null)
        {
            Debug.Log(MessageToString(message), context);
            return;
        }

        var canLog = categoryObj.CanLog(logType, level);
        if (!canLog && !StoresLogs) return;

        DebuggerLog log = default;
        if (StoresLogs || onScreen)
        {
            log = new DebuggerLog(category, logType, level, MessageToString(message), context);
            StoreLog(log);
        }

        if (canLog)
        {
            var categorizedMessage = CategorizeMessage(categoryObj, level, message);
            Debug.Log(categorizedMessage, context);

            if (onScreen && Application.isPlaying)
            {
                OnScreenLogger.Log(log);
            }
        }
    }

    // --- WARNING ---
    [HideInCallstack]
    public static void LogWarning(EDebugCategory category, object message, int level = DebuggerAsset.MAX_DEBUGGER_LEVEL, bool onScreen = false, UnityEngine.Object context = null)
    {
        var categoryObj = DebuggerAsset.GetDebugCategoryObject(category);
        var logType = LogType.Warning;

        if (categoryObj == null)
        {
            Debug.LogWarning(MessageToString(message), context);
            return;
        }

        var canLog = categoryObj.CanLog(logType, level);
        if (!canLog && !StoresLogs) return;

        DebuggerLog log = default;
        if (StoresLogs || onScreen)
        {
            log = new DebuggerLog(category, logType, level, MessageToString(message), context);
            StoreLog(log);
        }

        if (canLog)
        {
            var categorizedMessage = CategorizeMessage(categoryObj, level, message);
            Debug.LogWarning(categorizedMessage, context);

            if (onScreen && Application.isPlaying)
            {
                OnScreenLogger.Log(log);
            }
        }
    }

    // --- ERROR ---
    [HideInCallstack]
    public static void LogError(EDebugCategory category, object message, bool onScreen = true, UnityEngine.Object context = null)
    {
        var categoryObj = DebuggerAsset.GetDebugCategoryObject(category);
        var logType = LogType.Error;

        if (categoryObj == null)
        {
            Debug.LogError(MessageToString(message), context);
            return;
        }

        DebuggerLog log = default;
        if (StoresLogs || onScreen)
        {
            log = new DebuggerLog(category, logType, 0, MessageToString(message), context);
            StoreLog(log);
        }

        if (categoryObj.CanLog(logType, 0))
        {
            var categorizedMessage = CategorizeMessage(categoryObj, 0, message);
            Debug.LogError(categorizedMessage, context);

            if (onScreen && Application.isPlaying)
            {
                OnScreenLogger.Log(log);
            }
        }
    }

    // --- ALWAYS ---
    [HideInCallstack]
    public static void LogAlways(EDebugCategory category, object message, LogType logType = LogType.Error, bool onScreen = true, UnityEngine.Object context = null)
    {
        var categoryObj = DebuggerAsset.GetDebugCategoryObject(category);

        if (categoryObj == null)
        {
            Debug.unityLogger.Log(logType, (object)MessageToString(message), context);
            return;
        }

        DebuggerLog log = default;
        if (StoresLogs || onScreen)
        {
            log = new DebuggerLog(category, logType, 0, MessageToString(message), context);
            StoreLog(log);
        }

        var categorizedMessage = CategorizeMessage(categoryObj, 0, message);
        Debug.unityLogger.Log(logType, (object)categorizedMessage, context);

        if (onScreen && Application.isPlaying)
        {
            OnScreenLogger.Log(log);
        }
    }

    // --- ON SCREEN ---
    [HideInCallstack]
    [System.Diagnostics.Conditional("UNITY_EDITOR"), System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
    public static void LogOnScreen(EDebugCategory category, object message, LogType logType = LogType.Log, int level = DebuggerAsset.MAX_DEBUGGER_LEVEL, float duration = DebuggerAsset.DEFAULT_SCREEN_LOG_DURATION)
    {
        if (!Application.isPlaying) return;

        var categoryObj = DebuggerAsset.GetDebugCategoryObject(category);

        // Category not found (no DebuggerAsset, missing category...) : the message still reaches Unity's console, uncategorized
        if (categoryObj == null)
        {
            Debug.unityLogger.Log(logType, (object)MessageToString(message), null);
            return;
        }

        var log = new DebuggerLog(category, logType, level, MessageToString(message), null);
        StoreLog(log);

        // Like the other log methods : filtered by the category's level (errors always get through)
        if (categoryObj.CanLog(logType, level))
        {
            OnScreenLogger.Log(log, duration);
        }
    }

    #endregion

    #region Log Helpers

#if UNITY_EDITOR
    /// <summary>
    /// Logs are kept for the D5 Console in the editor only
    /// </summary>
    private const bool StoresLogs = true;
#else
    private const bool StoresLogs = false;
#endif

    /// <summary>
    /// Like Debug.Log : a null message is logged as "Null"
    /// </summary>
    private static string MessageToString(object message)
    {
        return message?.ToString() ?? "Null";
    }

    #endregion


    #region Log Storage

    /// <summary>
    /// Keeps the log for the D5 Console : editor only, logs are not kept in builds
    /// </summary>
    private static void StoreLog(DebuggerLog log)
    {
#if UNITY_EDITOR
        DebuggerLogsContainer.AddLog(log);
#endif
    }

    #endregion

    #region Log Permission

    private static bool CanLog(EDebugCategory category, LogType logType, int level)
    {
        var categoryObj = DebuggerAsset.GetDebugCategoryObject(category);

        return CanLog(categoryObj, logType, level);
    }
    private static bool CanLog(DebugCategoryObject categoryObject, LogType logType, int level)
    {
        if (categoryObject != null)
        {
            return categoryObject.CanLog(logType, level);
        }
        return false;
    }

    #endregion

    #region Message Categorization

    private static string CategorizeMessage(EDebugCategory category, int level, object message)
    {
        var categoryObj = DebuggerAsset.GetDebugCategoryObject(category);

        return CategorizeMessage(categoryObj, level, message);
    }
    private static string CategorizeMessage(DebugCategoryObject categoryObject, int level, object message)
    {
        if (categoryObject != null)
        {
            StringBuilder sb = new();

            sb.Append("<color=#");
            sb.Append(categoryObject.ColorString);
            sb.Append("><b>");
            sb.Append((EDebugCategory)categoryObject.EnumIndex);
            sb.Append(" ");
            for (int i = 0; i < DebuggerAsset.MAX_DEBUGGER_LEVEL + 1 - level; i++)
                sb.Append(">");
            sb.Append("</b></color> ");
            sb.Append(MessageToString(message));

            return sb.ToString();
        }
        return MessageToString(message);
    }

    #endregion
}
