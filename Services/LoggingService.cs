using System;
using System.IO;
using System.Threading.Tasks;

namespace TextLabClient.Services
{
    public static class LoggingService
    {
        private static readonly string LogFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "TextLabClient",
            "logs",
            $"textlab-client-{DateTime.Now:yyyy-MM-dd}.log"
        );

        static LoggingService()
        {
            // Créer le dossier de logs s'il n'existe pas
            var logDir = Path.GetDirectoryName(LogFilePath);
            if (!Directory.Exists(logDir))
            {
                Directory.CreateDirectory(logDir!);
            }
        }

        public static async Task LogAsync(string level, string message)
        {
            try
            {
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                var logEntry = $"[{timestamp}] [{level}] {message}{Environment.NewLine}";
                
                await File.AppendAllTextAsync(LogFilePath, logEntry);
                
                // Aussi afficher dans Debug pour Visual Studio
                System.Diagnostics.Debug.WriteLine($"[{level}] {message}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Erreur logging: {ex.Message}");
            }
        }

        public static async Task LogInfoAsync(string message)
        {
            await LogAsync("INFO", message);
        }

        public static async Task LogErrorAsync(string message)
        {
            await LogAsync("ERROR", message);
        }

        public static async Task LogDebugAsync(string message)
        {
            await LogAsync("DEBUG", message);
        }

        public static async Task LogWarningAsync(string message)
        {
            await LogAsync("WARNING", message);
        }

        public static string GetLogFilePath()
        {
            return LogFilePath;
        }

        public static async Task<string> GetLogsContentAsync(int lastLines = 100)
        {
            try
            {
                if (!File.Exists(LogFilePath))
                    return "Aucun fichier de log trouvé.";

                var lines = await File.ReadAllLinesAsync(LogFilePath);
                var startIndex = Math.Max(0, lines.Length - lastLines);
                var relevantLines = lines[startIndex..];
                
                return string.Join(Environment.NewLine, relevantLines);
            }
            catch (Exception ex)
            {
                return $"Erreur lecture logs: {ex.Message}";
            }
        }
    }
} 