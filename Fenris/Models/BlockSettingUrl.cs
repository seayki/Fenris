

namespace Fenris.Models
{
    public class BlockSettingsUrl
    {
        public Dictionary<string, BlockData> UrlBlock { get; set; } = new();
        public BlockSettingsUrl(string url, BlockType type)
        {
            UrlBlock.Add(url, new BlockData(type));
        }
        public BlockSettingsUrl()
        {
            
        }
    }

    public class BlockData
    {
        public BlockType Type { get; set; }
        public string? IconBase64 { get; set; }

        public BlockData(BlockType type)
        {
            Type = type;
        }

        public BlockData()
        {
        }
    }

    public enum BlockType
    {
        Full,
        Schedule,
        None
    }
}
