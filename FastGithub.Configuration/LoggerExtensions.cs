using Microsoft.Extensions.Logging;
using System;

namespace FastGithub
{
    /// <summary>
    /// 日志插值字符串扩展
    /// </summary>
    public static class LoggerExtensions
    {
        /// <summary>
        /// 输出日志
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="level"></param>
        /// <param name="formattableString"></param>
        public static void Log(this ILogger logger, LogLevel level, FormattableString formattableString)
            => logger.Log(level, formattableString.Format, formattableString.GetArguments());

        /// <summary>
        /// 输出日志
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="level"></param>
        /// <param name="error"></param>
        /// <param name="formattableString"></param>
        public static void Log(this ILogger logger, LogLevel level, Exception? error, FormattableString formattableString)
            => logger.Log(level, error, formattableString.Format, formattableString.GetArguments());

        /// <summary>
        /// 输出Trace日志
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="formattableString"></param>
        public static void LogTrace(this ILogger logger, FormattableString formattableString)
            => logger.Log(LogLevel.Trace, formattableString);

        /// <summary>
        /// 输出Debug日志
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="formattableString"></param>
        public static void LogDebug(this ILogger logger, FormattableString formattableString)
            => logger.Log(LogLevel.Debug, formattableString);

        /// <summary>
        /// 输出Information日志
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="formattableString"></param>
        public static void LogInformation(this ILogger logger, FormattableString formattableString)
            => logger.Log(LogLevel.Information, formattableString);

        /// <summary>
        /// 输出Warning日志
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="formattableString"></param>
        public static void LogWarning(this ILogger logger, FormattableString formattableString)
            => logger.Log(LogLevel.Warning, formattableString);

        /// <summary>
        /// 输出日志
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="formattableString"></param>
        public static void LogError(this ILogger logger, FormattableString formattableString)
            => logger.Log(LogLevel.Error, formattableString);

        /// <summary>
        /// 输出日志
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="formattableString"></param>
        public static void LogError(this ILogger logger, Exception error, FormattableString formattableString)
            => logger.Log(LogLevel.Error, error, formattableString);

        /// <summary>
        /// 输出Critical日志
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="formattableString"></param>
        public static void LogCritical(this ILogger logger, FormattableString formattableString)
            => logger.Log(LogLevel.Critical, formattableString);
    }
}
