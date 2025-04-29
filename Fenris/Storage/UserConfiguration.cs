using Fenris.IconServices;
using Fenris.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Fenris.Storage
{
    public static class UserConfiguration 
    {
        public static async Task<string?> SaveIconToLocalFolder(Bitmap bitmap, string exePath)
        {
            if (bitmap == null)
                return null;

            string iconFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Fenris", "Icons");
            Directory.CreateDirectory(iconFolder);

            try
            {
                string safeFileName = Path.GetFileNameWithoutExtension(exePath) + ".png";
                string savePath = Path.Combine(iconFolder, safeFileName);

                await Task.Run(() => bitmap.Save(savePath, System.Drawing.Imaging.ImageFormat.Png));

                return savePath;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to save icon for {exePath}: {ex.Message}");
                return null;
            }
        }

        public static async Task<BlockSettingsUrl?> LoadBlockedWebsites()
        {
            string filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Fenris", "blockedWebsites.json");
            if (File.Exists(filePath))
            {
                string json = await File.ReadAllTextAsync(filePath);
                var blockedWebsites = JsonSerializer.Deserialize<BlockSettingsUrl>(json);
                if (blockedWebsites != null)
                {
                    return blockedWebsites;
                }
            }
            return null;
        }

        public static async Task StoreBlockedWebsites(BlockSettingsUrl block, bool shouldCombine = true)
        {
            string filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Fenris", "blockedWebsites.json");
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

            string json;
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            if (shouldCombine)
            {
                var blockCombined = await LoadBlockedWebsites() ?? new BlockSettingsUrl();
                foreach (var item in block.UrlBlock)
                {
                    blockCombined.UrlBlock.Add(item.Key, item.Value);
                }
                json = JsonSerializer.Serialize(blockCombined, options);
            }
            else
            {
                json = JsonSerializer.Serialize(block, options);
            }             
            await File.WriteAllTextAsync(filePath, json);
        }

        public static async Task UpdateWebsiteBlockType(string url, BlockType type)
        {
            string filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Fenris", "blockedWebsites.json");

            var blockedWebsites = await LoadBlockedWebsites();

            blockedWebsites!.UrlBlock[url].Type = type;
            await StoreBlockedWebsites(blockedWebsites, false);
        }

        public static async Task RemoveWebsiteBlock(string url)
        {
            string filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Fenris", "blockedWebsites.json");
            var blockedWebsites = await LoadBlockedWebsites() ?? new BlockSettingsUrl();
            if (blockedWebsites.UrlBlock.ContainsKey(url))
            {
                blockedWebsites.UrlBlock.Remove(url);
            }
            await StoreBlockedWebsites(blockedWebsites, false);
        }


        public static async Task<BlockSettings?> LoadBlockedApps()
        {
            string filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Fenris", "blockedApps.json");
            if (File.Exists(filePath))
            {
                string json = await File.ReadAllTextAsync(filePath);
                return JsonSerializer.Deserialize<BlockSettings>(json) ?? new BlockSettings();
            }
            return null;
        }

        public static async Task StoreBlockedApps(BlockSettings settings)
        {
            string filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Fenris", "blockedApps.json");

            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            string json = JsonSerializer.Serialize(settings, options);
            await File.WriteAllTextAsync(filePath, json);
        }

        public static async Task<BlockSchedule?> LoadBlockSchedule()
        {
            string filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Fenris", "blockSchedule.json");
            if (File.Exists(filePath))
            {
                string json = await File.ReadAllTextAsync(filePath); // Added async for consistency
                var options = new JsonSerializerOptions
                {
                    Converters = { new TimeSpanConverter(), new TimeSpanTupleConverter() }
                };
                return JsonSerializer.Deserialize<BlockSchedule>(json, options) ?? new BlockSchedule();
            }
            return null;
        }

        public static async Task StoreBlockSchedule(BlockSchedule settings)
        {
            // Save to JSON file
            string filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Fenris", "blockSchedule.json");
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                Converters = { new TimeSpanConverter(), new TimeSpanTupleConverter() }
            };
            string json = JsonSerializer.Serialize(settings, options);
            await File.WriteAllTextAsync(filePath, json);
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
