using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Fenris.DiscoveryServices
{
    public static class IconService
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct SHFILEINFO
        {
            public IntPtr hIcon;
            public int iIcon;
            public uint dwAttributes;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szDisplayName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
            public string szTypeName;
        }

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, ref SHFILEINFO psfi, uint cbFileInfo, uint uFlags);

        private const uint SHGFI_ICON = 0x000000100;
        private const uint SHGFI_LARGEICON = 0x000000000;

        public static string GetHighQualityIcon(string exePath)
        {
            if (!File.Exists(exePath))
            {
                return null;
            }

            string cachePath = Path.Combine(Path.GetTempPath(), $"{Path.GetFileNameWithoutExtension(exePath)}.png");

            try
            {
                var icon = GetLargeIcon(exePath);
                if (icon == null)
                {
                    return null;
                }

                using (var bitmap = icon.ToBitmap())
                {
                    bitmap.Save(cachePath, System.Drawing.Imaging.ImageFormat.Png); // PNG for better quality
                }

                return cachePath;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error extracting high-quality icon: {ex.Message}");
                return null;
            }
        }

        private static Icon GetLargeIcon(string filePath)
        {
            SHFILEINFO shinfo = new SHFILEINFO();
            IntPtr hImgLarge = SHGetFileInfo(filePath, 0, ref shinfo, (uint)Marshal.SizeOf(shinfo), SHGFI_ICON | SHGFI_LARGEICON);
            return hImgLarge != IntPtr.Zero ? Icon.FromHandle(shinfo.hIcon) : null;
        }

    }
}
