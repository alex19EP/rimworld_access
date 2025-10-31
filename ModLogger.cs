using MelonLoader;

namespace RimWorldAccess
{
    /// <summary>
    /// Static logger for accessing MelonLoader logging from anywhere in the mod.
    /// </summary>
    public static class ModLogger
    {
        private static MelonLogger.Instance logger;

        /// <summary>
        /// Initialize the logger with the MelonMod's logger instance.
        /// </summary>
        public static void Initialize(MelonLogger.Instance loggerInstance)
        {
            logger = loggerInstance;
        }

        /// <summary>
        /// Log a message to MelonLoader console.
        /// </summary>
        public static void Msg(string message)
        {
            logger?.Msg(message);
        }

        /// <summary>
        /// Log a warning to MelonLoader console.
        /// </summary>
        public static void Warning(string message)
        {
            logger?.Warning(message);
        }

        /// <summary>
        /// Log an error to MelonLoader console.
        /// </summary>
        public static void Error(string message)
        {
            logger?.Error(message);
        }
    }
}
