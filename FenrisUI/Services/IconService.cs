using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;

namespace FenrisUI.Services
{
    public static class IconService
    {
        public static async Task<SoftwareBitmapSource?> IconToImageSourceAsync(Icon icon)
        {
            if (icon == null) return null;

            using (var stream = new MemoryStream())
            {
                icon.ToBitmap().Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                stream.Seek(0, SeekOrigin.Begin);

                var decoder = await BitmapDecoder.CreateAsync(stream.AsRandomAccessStream());
                var softwareBitmap = await decoder.GetSoftwareBitmapAsync();
                softwareBitmap = SoftwareBitmap.Convert(softwareBitmap,
                                                   BitmapPixelFormat.Bgra8,
                                                   BitmapAlphaMode.Premultiplied);
                var imageSource = new SoftwareBitmapSource();
                await imageSource.SetBitmapAsync(softwareBitmap);
                return imageSource;
            }
        }

        public static async Task<SoftwareBitmapSource?> GetIcon(string? iconString)
        {
            if (string.IsNullOrEmpty(iconString) || iconString == "Empty")
                return null;

            try
            {
                byte[] bytes = Convert.FromBase64String(iconString);
                using MemoryStream ms = new(bytes);
                using Icon icon = new(ms);

                return await IconToImageSourceAsync(icon);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error decoding icon: {ex.Message}");
                return null;
            }
        }
    }
}
