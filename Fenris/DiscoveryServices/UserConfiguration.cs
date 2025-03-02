using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Fenris.DiscoveryServices
{
    public class UserConfiguration : IUserConfiguration
    {
        public List<string>? LoadBlockedWebsites()
        {
            string filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Fenris", "blockedWebsites.json");
            if (File.Exists(filePath))
            {
                string json = File.ReadAllText(filePath);
                var blockedWebsites = JsonSerializer.Deserialize<List<string>>(json);
                if (blockedWebsites != null)
                {
                    return blockedWebsites;
                }
            }
            return null;
        }

        public void StoreBlockedWebsites(List<string> websites)
        {
            string filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Fenris", "blockedWebsites.json");
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            var options = new JsonSerializerOptions
            {
                WriteIndented = true 
            };

            string json = JsonSerializer.Serialize(websites, options);
            File.WriteAllText(filePath, json);
        }

        public BlockSettings? LoadBlockSettings()
        {
            string filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Fenris", "blockSettings.json");
            if (File.Exists(filePath))
            {
                string json = File.ReadAllText(filePath);
                var options = new JsonSerializerOptions
                {
                    Converters = { new TimeSpanConverter(), new TimeSpanTupleConverter() }
                };
                return JsonSerializer.Deserialize<BlockSettings>(json) ?? new BlockSettings();
            }
            return new BlockSettings(); // Default if file doesn’t exist
        }
        public void StoreBlockSettings(BlockSettings settings)
        {
            // Save to JSON file
            string filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Fenris", "blockSettings.json");
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                Converters = { new TimeSpanConverter(), new TimeSpanTupleConverter() }
            };

            string json = JsonSerializer.Serialize(settings, options);
            File.WriteAllText(filePath, json);
        }
    }

    // Custom TimeSpan Converter
    public class TimeSpanConverter : JsonConverter<TimeSpan>
    {
        public override TimeSpan Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return TimeSpan.Parse(reader.GetString());
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
                    string propertyName = reader.GetString();
                    reader.Read();
                    if (propertyName == "BlockStart")
                    {
                        start = TimeSpan.Parse(reader.GetString());
                    }
                    else if (propertyName == "BlockEnd")
                    {
                        end = TimeSpan.Parse(reader.GetString());
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
