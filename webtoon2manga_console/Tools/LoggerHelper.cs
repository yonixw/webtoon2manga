using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace webtoon2manga_console.Tools
{
    public class LoggerHelper
    {
        private static object _WriteSyncLock = new object();

        public enum LogLevel
        {
            INFO = ConsoleColor.White,
            WARNING = ConsoleColor.Yellow,
            ERROR = ConsoleColor.Red,
            CRITICAL = ConsoleColor.Blue
        }

        string lvlString(LogLevel lvl)
        {
            string result = "???";
            switch (lvl)
            {
                case LogLevel.INFO:
                    result = "info";
                    break;
                case LogLevel.WARNING:
                    result = "warn";
                    break;
                case LogLevel.ERROR:
                    result = "err";
                    break;
                case LogLevel.CRITICAL:
                    result = "crit";
                    break;
            }
            return result;
        }

        string _myTag;
        bool _writeToConsole = false;

        public LoggerHelper(string tag, bool writeToConsole = true)
        {
            _myTag = tag;
            _writeToConsole = writeToConsole;
        }

        public void log(object message, LogLevel level)
        {

            if (_writeToConsole)
            {
                lock (_WriteSyncLock)
                {
                    Console.Write("[{0}] {1}\t", DateTime.Now.ToString("HH:mm:ss"), _myTag);
                    Console.ForegroundColor = (ConsoleColor)level;
                    Console.Write(lvlString(level));
                    Console.ResetColor();
                    Console.WriteLine("\t{0}", message.ToString());
                    Console.ResetColor();
                } 
            }
        }

        public void log(object message, Exception ex, LogLevel level)
        {
            string ErrorMsg = "";
            string Stack = ex.StackTrace;
            while (ex != null)
            {
                ErrorMsg += "(*) " + ex.Message + "\r\n";
                ex = ex.InnerException;
            }

            log(message.ToString() + "\r\n" + ErrorMsg + Stack, level);
        }

        public void i(object msg) { log(msg, LogLevel.INFO); }
        public void w(object msg) { log(msg, LogLevel.WARNING); }
        public void e(object msg) { log(msg, LogLevel.ERROR); }
        public void e(object msg, Exception ex) { log(msg, ex, LogLevel.ERROR); }
        public void c(object msg) { log(msg, LogLevel.CRITICAL); }
        public void c(object msg, Exception ex) { log(msg, ex, LogLevel.CRITICAL); }

        public static string Stringify(string arrayName, object[] array)
        {
            return String.Format("'{0}': [{1}]", arrayName, string.Join(",", array.Select<object, string>((a) => a.ToString())));
        }
    }
}
