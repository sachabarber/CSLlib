using System;

namespace CSLib
{
    /// <summary>
    /// Simple LogService interface. In reality this would likely not exist as a proper logging framework such as
    /// <list type="bullet">
    /// <item>
    /// <description>Serilog</description>
    /// </item>
    /// <item>
    /// <description>Log4Net</description>
    /// </item>
    /// </list>
    /// Where this interface abstraction already exists in some form such as <c>ILog</c> for Serilog for example
    /// </summary>
    public interface ILoggerService
    {
        /// <summary>
        /// Logs a Info level log
        /// </summary>
        /// <param name="msg">The message to log</param>
        void Info(String msg);

        /// <summary>
        /// Logs a Warning level log
        /// </summary>
        /// <param name="msg">The message to log</param>
        void Warning(String msg);

        /// <summary>
        /// Logs a Debug level log
        /// </summary>
        /// <param name="msg">The message to log</param>
        void Debug(String msg);

        /// <summary>
        /// Logs a Trace level log
        /// </summary>
        /// <param name="msg">The message to log</param>
        void Trace(String msg);

        /// <summary>
        /// Logs a param level log
        /// </summary>
        /// <param name="msg">The message to log</param>
        /// <param name="ex">The Exception to log</param>
        void Error(String msg, Exception ex);
    }
}
