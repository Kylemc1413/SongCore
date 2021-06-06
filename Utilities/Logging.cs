using IPA.Logging;

namespace SongCore.Utilities
{
    internal static class Logging
    {
        public static Logger logger;

        internal static void Log(string message)
        {
            logger.Info($"{message}");
        }

        internal static void Log(string message, Logger.Level level)
        {
            logger.Log(level, $"{message}");
        }
    }
}