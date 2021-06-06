using IPALogger = IPA.Logging.Logger;

namespace SongCore.Utilities
{
    internal static class Logging
    {
        public static IPALogger logger;

        internal static void Log(string message)
        {
            logger.Info($"{message}");
        }
        internal static void Log(string message, IPALogger.Level level)
        {
            logger.Log(level, $"{message}");
        }
    }
}
