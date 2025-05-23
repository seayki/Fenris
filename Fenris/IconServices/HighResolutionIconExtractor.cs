using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace Fenris.IconServices
{
    public static class HighResolutionIconExtractor
    {
        [ComImport]
        [Guid("BCC18B79-BA16-442F-80C4-8A59C30C463B")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IShellItemImageFactory
        {
            void GetImage(SIZE size, SIIGBF flags, out IntPtr phbm);
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct SIZE
        {
            public int cx;
            public int cy;
        }

        [Flags]
        private enum SIIGBF
        {
            SIIGBF_RESIZETOFIT = 0x00,
            SIIGBF_BIGGERSIZEOK = 0x01,
            SIIGBF_MEMORYONLY = 0x02,
            SIIGBF_ICONONLY = 0x04,
            SIIGBF_THUMBNAILONLY = 0x08,
            SIIGBF_INCACHEONLY = 0x10,
        }

        [DllImport("shell32.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
        private static extern void SHCreateItemFromParsingName(
            [MarshalAs(UnmanagedType.LPWStr)] string pszPath,
            IntPtr pbc,
            ref Guid riid,
            [MarshalAs(UnmanagedType.Interface)] out IShellItemImageFactory ppv);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);

        [DllImport("gdi32.dll", SetLastError = true)]
        private static extern int GetObject(IntPtr hObject, int nCount, ref BITMAP lpObject);

        [StructLayout(LayoutKind.Sequential)]
        private struct BITMAP
        {
            public int bmType;
            public int bmWidth;
            public int bmHeight;
            public int bmWidthBytes;
            public ushort bmPlanes;
            public ushort bmBitsPixel;
            public IntPtr bmBits;
        }

        public static Bitmap? GetHighResIcon(string filePath, int size = 256)
        {
            var iid = new Guid("BCC18B79-BA16-442F-80C4-8A59C30C463B");
            SHCreateItemFromParsingName(filePath, IntPtr.Zero, ref iid, out var factory);

            var sz = new SIZE { cx = size, cy = size };
            factory.GetImage(sz, SIIGBF.SIIGBF_ICONONLY | SIIGBF.SIIGBF_RESIZETOFIT, out var hBitmap);

            if (hBitmap == IntPtr.Zero)
                return null;

            try
            {
                // Read bitmap info
                BITMAP bmp = new BITMAP();
                GetObject(hBitmap, Marshal.SizeOf(typeof(BITMAP)), ref bmp);

                // Create a new Bitmap with alpha channel
                var result = new Bitmap(bmp.bmWidth, bmp.bmHeight, PixelFormat.Format32bppArgb);

                // Copy pixels manually
                var bmpData = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
                int stride = bmpData.Stride;
                int srcStride = bmp.bmWidthBytes;

                unsafe
                {
                    Buffer.MemoryCopy(
                        (void*)bmp.bmBits,
                        (void*)bmpData.Scan0,
                        stride * bmp.bmHeight,
                        stride * bmp.bmHeight);
                }

                result.UnlockBits(bmpData);

                // Flip vertically because GDI bitmaps are upside down
                result.RotateFlip(RotateFlipType.RotateNoneFlipY);

                return result;
            }
            catch
            {
                return null;
            }
            finally
            {
                DeleteObject(hBitmap);
            }
        }
    }
}

