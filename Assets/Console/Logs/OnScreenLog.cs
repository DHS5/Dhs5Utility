using UnityEngine;

namespace Dhs5.Utility.Console
{
    public struct OnScreenLog
    {
        public OnScreenLog(EDebugCategory category, LogType type, int level, string message)
        {
            this.category = category;
            this.type = type;
            this.level = level;
            this.message = message;
        }
        public OnScreenLog(EDebugCategory category, LogType type, int level, object message)
        {
            this.category = category;
            this.type = type;
            this.level = level;
            this.message = message.ToString();
        }

        public readonly EDebugCategory category;
        public LogType type;
        public int level;
        public string message;
    }
}
