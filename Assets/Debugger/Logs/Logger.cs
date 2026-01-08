using UnityEngine;
using Dhs5.Utility.Debugger;
using System.Text;

public static class Logger
{
    #region Public Log Behaviour

    [HideInCallstack]
    public static void Log(EDebugCategory category, object message, LogType logType, int level = DebuggerAsset.MAX_DEBUGGER_LEVEL, bool onScreen = false, UnityEngine.Object context = null)
    {
        var categoryObj = DebuggerAsset.GetDebugCategoryObject(category);

        if (categoryObj != null)
        {
            var msgString = message.ToString();

            var logIndex = DebuggerLogsContainer.AddLog(new DebuggerLog(category, logType, level, msgString, context));

            if (CanLog(categoryObj, logType, level))
            {
                var categorizedMessage = CategorizeMessage(categoryObj, level, message);
                Debug.unityLogger.Log(logType, (object)categorizedMessage, context);

                if (onScreen && Application.isPlaying)
                {
                    OnScreenLogger.Log(logIndex);
                }
            }
        }
    }

    // --- LOG ---
    [HideInCallstack]
    public static void Log(EDebugCategory category, object message, int level = DebuggerAsset.MAX_DEBUGGER_LEVEL, bool onScreen = false, UnityEngine.Object context = null)
    {
        var categoryObj = DebuggerAsset.GetDebugCategoryObject(category);
        var logType = LogType.Log;

        if (categoryObj != null)
        {
            var msgString = message.ToString();

            var logIndex = DebuggerLogsContainer.AddLog(new DebuggerLog(category, logType, level, msgString, context));

            if (CanLog(categoryObj, logType, level))
            {
                var categorizedMessage = CategorizeMessage(categoryObj, level, message);
                Debug.Log(categorizedMessage, context);

                if (onScreen && Application.isPlaying)
                {
                    OnScreenLogger.Log(logIndex);
                }
            }
        }
    }

    // --- WARNING ---
    [HideInCallstack]
    public static void LogWarning(EDebugCategory category, object message, int level = DebuggerAsset.MAX_DEBUGGER_LEVEL, bool onScreen = false, UnityEngine.Object context = null)
    {
        var categoryObj = DebuggerAsset.GetDebugCategoryObject(category);
        var logType = LogType.Warning;

        if (categoryObj != null)
        {
            var msgString = message.ToString();

            var logIndex = DebuggerLogsContainer.AddLog(new DebuggerLog(category, logType, level, msgString, context));

            if (CanLog(categoryObj, logType, level))
            {
                var categorizedMessage = CategorizeMessage(categoryObj, level, message);
                Debug.LogWarning(categorizedMessage, context);

                if (onScreen && Application.isPlaying)
                {
                    OnScreenLogger.Log(logIndex);
                }
            }
        }
    }

    // --- ERROR ---
    [HideInCallstack]
    public static void LogError(EDebugCategory category, object message, bool onScreen = true, UnityEngine.Object context = null)
    {
        var categoryObj = DebuggerAsset.GetDebugCategoryObject(category);
        var logType = LogType.Error;

        if (categoryObj != null)
        {
            var msgString = message.ToString();

            var logIndex = DebuggerLogsContainer.AddLog(new DebuggerLog(category, logType, 0, msgString, context));

            if (CanLog(categoryObj, logType, 0))
            {
                var categorizedMessage = CategorizeMessage(categoryObj, 0, message);
                Debug.LogError(categorizedMessage, context);

                if (onScreen && Application.isPlaying)
                {
                    OnScreenLogger.Log(logIndex);
                }
            }
        }
    }

    // --- ALWAYS ---
    [HideInCallstack]
    public static void LogAlways(EDebugCategory category, object message, LogType logType = LogType.Error, bool onScreen = true, UnityEngine.Object context = null)
    {
        var categoryObj = DebuggerAsset.GetDebugCategoryObject(category);

        if (categoryObj != null)
        {
            var msgString = message.ToString();

            var logIndex = DebuggerLogsContainer.AddLog(new DebuggerLog(category, logType, 0, msgString, context));

            var categorizedMessage = CategorizeMessage(categoryObj, 0, message);
            Debug.Log(categorizedMessage, context);

            if (onScreen && Application.isPlaying)
            {
                OnScreenLogger.Log(logIndex);
            }
        }
    }

    // --- ON SCREEN ---
    [HideInCallstack]
    public static void LogOnScreen(EDebugCategory category, object message, LogType logType = LogType.Log, int level = DebuggerAsset.MAX_DEBUGGER_LEVEL, float duration = DebuggerAsset.DEFAULT_SCREEN_LOG_DURATION)
    {
        if (Application.isPlaying)
        {
            var logIndex = DebuggerLogsContainer.AddLog(new DebuggerLog(category, logType, level, message, null));

            OnScreenLogger.Log(logIndex, duration);
        }
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
            sb.Append(categoryObject.name);
            sb.Append(" ");
            for (int i = 0; i < DebuggerAsset.MAX_DEBUGGER_LEVEL + 1 - level; i++)
                sb.Append(">");
            sb.Append("</b></color> ");
            sb.Append(message);

            return sb.ToString();
        }
        return message.ToString();
    }

    #endregion
}
