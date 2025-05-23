using Fenris.Models;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;

namespace FenrisUI.Models
{
    public class UrlBlock
    {
        public SoftwareBitmapSource? Icon { get; set; }
        public string Url { get; set; }       
        public BlockType BlockType { get; set; }

        public SolidColorBrush BackgroundColor
        {
            get
            {
              return BlockType == BlockType.Full
               ? new SolidColorBrush(Windows.UI.Color.FromArgb(255, 0xf4, 0x97, 0x3e)) 
               : new SolidColorBrush(Windows.UI.Color.FromArgb(255, 0x62, 0xB7, 0x56)); 
            }
        }
        public UrlBlock(SoftwareBitmapSource? icon, string url, BlockType blockType)
        {
            Icon = icon;
            Url = url;
            BlockType = blockType;
        }
    }
}
