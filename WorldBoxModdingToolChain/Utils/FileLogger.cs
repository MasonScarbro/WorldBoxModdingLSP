using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorldBoxModdingToolChain.Utils
{
    public static class FileLogger
    {
        private static StreamWriter _logWriter;
        private static string _logPrefix = "[INFO]";  // Default log prefix

        public static void Initialize(string logFilePath, string prefix = "[INFO]")
        {
            _logPrefix = prefix;
            Refresh(logFilePath);
            // Open the file stream for logging
            _logWriter = new StreamWriter(logFilePath, append: true)
            {
                AutoFlush = true
            };
        }

        private static void Refresh(string logFilePath)
        {
            File.WriteAllText(logFilePath, string.Empty);
        }

        public static void Log(string message)
        {
            string logMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {_logPrefix} {message}";
            _logWriter.WriteLine(logMessage);


            //Console.WriteLine(logMessage);
        }

        public static void Close()
        {

            _logWriter?.Close();
        }
    }
}
