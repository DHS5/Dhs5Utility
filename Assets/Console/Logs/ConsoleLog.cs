using UnityEngine;

namespace Dhs5.Utility.Console
{
    public struct ConsoleLog
    {
        public ConsoleLog(EDebugCategory category, LogType type, int level, string message, UnityEngine.Object context)
        {
            this.category = category;
            this.type = type;
            this.level = level;
            this.message = message;
            this.context = context;
            this.timestamp = Time.timeAsDouble;
        }

        public readonly EDebugCategory category;
        public readonly LogType type;
        public readonly int level;
        public readonly string message;
        public readonly UnityEngine.Object context;
        public readonly double timestamp;
    }
}
