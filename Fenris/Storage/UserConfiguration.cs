using Fenris.Models;
using System.Drawing;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Fenris.Storage
{
    public static class UserConfiguration
    {
        private static readonly SemaphoreSlim _blockedWebsitesLock = new SemaphoreSlim(1, 1);
        private static readonly SemaphoreSlim _blockedAppsLock = new SemaphoreSlim(1, 1);
        private static readonly SemaphoreSlim _blockScheduleLock = new SemaphoreSlim(1, 1);

        public static async Task<string?> SaveIconToLocalFolder(Bitmap bitmap, string exePath)
        {
            if (bitmap == null)
                return null;

            string iconFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Fenris", "Icons");
            Directory.CreateDirectory(iconFolder);

            string safeFileName = Path.GetFileNameWithoutExtension(exePath) + ".png";
            string savePath = Path.Combine(iconFolder, safeFileName);

            await Task.Run(() => bitmap.Save(savePath, System.Drawing.Imaging.ImageFormat.Png));

            return savePath;
        }

        public static async Task<BlockSettingsUrl?> LoadBlockedWebsites()
        {
            if (!await _blockedWebsitesLock.WaitAsync(TimeSpan.FromSeconds(10)))
                throw new TimeoutException("Could not acquire lock for blocked websites.");

            try
            {
                string filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Fenris", "blockedWebsites.json");
                if (File.Exists(filePath))
                {
                    string json = await File.ReadAllTextAsync(filePath);
                    return JsonSerializer.Deserialize<BlockSettingsUrl>(json);
                }
                return null;
            }
            finally
            {
                _blockedWebsitesLock.Release();
            }
        }

        private static async Task<BlockSettingsUrl?> LoadBlockedWebsitesInternalUnlocked()
        {
            string filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Fenris", "blockedWebsites.json");
            if (File.Exists(filePath))
            {
                string json = await File.ReadAllTextAsync(filePath);
                return JsonSerializer.Deserialize<BlockSettingsUrl>(json);
            }
            return null;
        }

        public static async Task StoreBlockedWebsites(BlockSettingsUrl block, bool shouldCombine = true)
        {
            if (block == null || block.UrlBlock == null)
                throw new ArgumentNullException(nameof(block), "Block or UrlBlock cannot be null.");

            if (!await _blockedWebsitesLock.WaitAsync(TimeSpan.FromSeconds(10)))
                throw new TimeoutException("Could not acquire lock for blocked websites.");

            try
            {
                string filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Fenris", "blockedWebsites.json");
                string folderPath = Path.GetDirectoryName(filePath)!;
                Directory.CreateDirectory(folderPath);

                string json;
                var options = new JsonSerializerOptions { WriteIndented = true };

                if (shouldCombine)
                {
                    var blockCombined = await LoadBlockedWebsitesInternalUnlocked() ?? new BlockSettingsUrl();
                    foreach (var item in block.UrlBlock)
                    {
                        if (string.IsNullOrEmpty(item.Key))
                            continue;
                        blockCombined.UrlBlock[item.Key] = item.Value;
                    }
                    json = JsonSerializer.Serialize(blockCombined, options);
                }
                else
                {
                    json = JsonSerializer.Serialize(block, options);
                }

                await File.WriteAllTextAsync(filePath, json);
            }
            finally
            {
                _blockedWebsitesLock.Release();
            }
        }

        public static async Task UpdateWebsiteBlockType(string url, BlockType type)
        {
            if (string.IsNullOrEmpty(url))
                throw new ArgumentNullException(nameof(url), "URL cannot be null or empty.");

            if (!await _blockedWebsitesLock.WaitAsync(TimeSpan.FromSeconds(10)))
                throw new TimeoutException("Could not acquire lock for blocked websites.");

            try
            {
                var blockedWebsites = await LoadBlockedWebsitesInternalUnlocked() ?? new BlockSettingsUrl();
                if (blockedWebsites.UrlBlock == null)
                {
                    blockedWebsites.UrlBlock = new Dictionary<string, BlockData>();
                }

                if (!blockedWebsites.UrlBlock.ContainsKey(url))
                {
                    blockedWebsites.UrlBlock[url] = new BlockData(type);
                }
                else
                {
                    blockedWebsites.UrlBlock[url].Type = type;
                }

                string filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Fenris", "blockedWebsites.json");
                string folderPath = Path.GetDirectoryName(filePath)!;
                Directory.CreateDirectory(folderPath);

                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(blockedWebsites, options);
                await File.WriteAllTextAsync(filePath, json);
            }
            finally
            {
                _blockedWebsitesLock.Release();
            }
        }

        public static async Task RemoveWebsiteBlock(string url)
        {
            if (!await _blockedWebsitesLock.WaitAsync(TimeSpan.FromSeconds(10)))
                throw new TimeoutException("Could not acquire lock for blocked websites.");

            try
            {
                var blockedWebsites = await LoadBlockedWebsitesInternalUnlocked() ?? new BlockSettingsUrl();
                if (blockedWebsites.UrlBlock.ContainsKey(url))
                {
                    blockedWebsites.UrlBlock.Remove(url);

                    string filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Fenris", "blockedWebsites.json");
                    var options = new JsonSerializerOptions { WriteIndented = true };
                    string json = JsonSerializer.Serialize(blockedWebsites, options);
                    await File.WriteAllTextAsync(filePath, json);
                }
            }
            finally
            {
                _blockedWebsitesLock.Release();
            }
        }

        public static async Task<BlockSettings?> LoadBlockedApps()
        {
            if (!await _blockedAppsLock.WaitAsync(TimeSpan.FromSeconds(10)))
                throw new TimeoutException("Could not acquire lock for blocked apps.");

            try
            {
                string filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Fenris", "blockedApps.json");
                if (File.Exists(filePath))
                {
                    string json = await File.ReadAllTextAsync(filePath);
                    return JsonSerializer.Deserialize<BlockSettings>(json) ?? new BlockSettings();
                }
                return null;
            }
            finally
            {
                _blockedAppsLock.Release();
            }
        }

        public static async Task StoreBlockedApps(BlockSettings settings)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings), "Settings cannot be null.");

            if (!await _blockedAppsLock.WaitAsync(TimeSpan.FromSeconds(10)))
                throw new TimeoutException("Could not acquire lock for blocked apps.");

            try
            {
                string filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Fenris", "blockedApps.json");
                Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(settings, options);
                await File.WriteAllTextAsync(filePath, json);
            }
            finally
            {
                _blockedAppsLock.Release();
            }
        }

        public static async Task<BlockSchedule?> LoadBlockSchedule()
        {
            if (!await _blockScheduleLock.WaitAsync(TimeSpan.FromSeconds(10)))
                throw new TimeoutException("Could not acquire lock for block schedule.");

            try
            {
                string filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Fenris", "blockSchedule.json");
                if (File.Exists(filePath))
                {
                    string json = await File.ReadAllTextAsync(filePath);
                    var options = new JsonSerializerOptions
                    {
                        Converters = { new TimeSpanConverter(), new TimeSpanTupleConverter() }
                    };
                    return JsonSerializer.Deserialize<BlockSchedule>(json, options) ?? new BlockSchedule();
                }
                return null;
            }
            finally
            {
                _blockScheduleLock.Release();
            }
        }

        public static async Task StoreBlockSchedule(BlockSchedule settings)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings), "Settings cannot be null.");

            if (!await _blockScheduleLock.WaitAsync(TimeSpan.FromSeconds(10)))
                throw new TimeoutException("Could not acquire lock for block schedule.");

            try
            {
                string filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Fenris", "blockSchedule.json");
                Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Converters = { new TimeSpanConverter(), new TimeSpanTupleConverter() }
                };
                string json = JsonSerializer.Serialize(settings, options);
                await File.WriteAllTextAsync(filePath, json);
            }
            finally
            {
                _blockScheduleLock.Release();
            }
        }
    }



    // Custom TimeSpan Converter
    public class TimeSpanConverter : JsonConverter<TimeSpan>
    {
        public override TimeSpan Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return TimeSpan.Parse(reader.GetString()!);
        }

        public override void Write(Utf8JsonWriter writer, TimeSpan value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString(@"hh\:mm\:ss"));
        }
    }

    // Custom Tuple Converter for (TimeSpan, TimeSpan)
    public class TimeSpanTupleConverter : JsonConverter<(TimeSpan BlockStart, TimeSpan BlockEnd)>
    {
        public override (TimeSpan BlockStart, TimeSpan BlockEnd) Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException("Expected object for tuple");
            }

            TimeSpan start = default;
            TimeSpan end = default;

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    return (start, end);
                }

                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    string? propertyName = reader.GetString();
                    reader.Read();
                    if (propertyName == "BlockStart")
                    {
                        string? value = reader.GetString();
                        if (string.IsNullOrEmpty(value))
                        {
                            throw new JsonException("BlockStart cannot be null or empty");
                        }
                        start = TimeSpan.Parse(value);
                    }
                    else if (propertyName == "BlockEnd")
                    {
                        string? value = reader.GetString();
                        if (string.IsNullOrEmpty(value))
                        {
                            throw new JsonException("BlockEnd cannot be null or empty");
                        }
                        end = TimeSpan.Parse(value);
                    }
                }
            }
            throw new JsonException("Incomplete tuple data");
        }

        public override void Write(Utf8JsonWriter writer, (TimeSpan BlockStart, TimeSpan BlockEnd) value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteString("BlockStart", value.BlockStart.ToString(@"hh\:mm\:ss"));
            writer.WriteString("BlockEnd", value.BlockEnd.ToString(@"hh\:mm\:ss"));
            writer.WriteEndObject();
        }
    }
}
