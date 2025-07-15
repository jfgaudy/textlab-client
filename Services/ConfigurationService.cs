using System;
using System.IO;
using Newtonsoft.Json;
using TextLabClient.Models;

namespace TextLabClient.Services
{
    public static class ConfigurationService
    {
        private static readonly string ConfigDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TextLabClient");
        private static readonly string ConfigFilePath = Path.Combine(ConfigDirectory, "settings.json");

        public static void Initialize()
        {
            try
            {
                if (!Directory.Exists(ConfigDirectory))
                {
                    Directory.CreateDirectory(ConfigDirectory);
                }
            }
            catch (Exception ex)
            {
                // En cas d'erreur, on continue sans configuration persistante
                System.Diagnostics.Debug.WriteLine($"Erreur initialisation configuration: {ex.Message}");
            }
        }

        public static AppSettings LoadSettings()
        {
            try
            {
                if (File.Exists(ConfigFilePath))
                {
                    var json = File.ReadAllText(ConfigFilePath);
                    var settings = JsonConvert.DeserializeObject<AppSettings>(json);
                    return settings ?? new AppSettings();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur chargement settings: {ex.Message}");
            }

            return new AppSettings();
        }

        public static void SaveSettings(AppSettings settings)
        {
            try
            {
                var json = JsonConvert.SerializeObject(settings, Formatting.Indented);
                File.WriteAllText(ConfigFilePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur sauvegarde settings: {ex.Message}");
            }
        }

        public static string GetConfigDirectory()
        {
            return ConfigDirectory;
        }
    }
} 