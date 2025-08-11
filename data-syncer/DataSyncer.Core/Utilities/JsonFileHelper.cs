using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace DataSyncer.Core.Utilities
{
    public static class JsonFileHelper
    {
        public static async Task SaveToFileAsync<T>(T data, string filePath)
        {
            try
            {
                var directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(filePath, json);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to save data to {filePath}: {ex.Message}", ex);
            }
        }

        public static async Task<T?> LoadFromFileAsync<T>(string filePath) where T : class
        {
            try
            {
                if (!File.Exists(filePath))
                    return null;

                var json = await File.ReadAllTextAsync(filePath);
                return JsonSerializer.Deserialize<T>(json);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to load data from {filePath}: {ex.Message}", ex);
            }
        }

        public static void SaveToFile<T>(T data, string filePath)
        {
            try
            {
                var directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(filePath, json);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to save data to {filePath}: {ex.Message}", ex);
            }
        }

        public static T? LoadFromFile<T>(string filePath) where T : class
        {
            try
            {
                if (!File.Exists(filePath))
                    return null;

                var json = File.ReadAllText(filePath);
                return JsonSerializer.Deserialize<T>(json);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to load data from {filePath}: {ex.Message}", ex);
            }
        }
    }
}
