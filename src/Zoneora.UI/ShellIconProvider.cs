using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Windows.Media;

namespace Zoneora.UI;

internal static class ShellIconProvider
{
    private const uint ShgfiIcon = 0x000000100;
    private const uint ShgfiSmallIcon = 0x000000001;

    public static ImageSource GetIcon(string path)
    {
        SHFILEINFO info = new();
        uint flags = ShgfiIcon | ShgfiSmallIcon;
        if (SHGetFileInfo(path, 0, ref info, (uint)Marshal.SizeOf<SHFILEINFO>(), flags) == 0 || info.HIcon == 0)
        {
            info.HIcon = LoadIcon(0, 32512);
            return CreateImage(info.HIcon, destroyIcon: false);
        }

        return CreateImage(info.HIcon, destroyIcon: true);
    }

    private static ImageSource CreateImage(nint handle, bool destroyIcon)
    {
        try
        {
            ImageSource image = Imaging.CreateBitmapSourceFromHIcon(handle, System.Windows.Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            image.Freeze();
            return image;
        }
        finally
        {
            if (destroyIcon)
            {
                DestroyIcon(handle);
            }
        }
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern nint SHGetFileInfo(string pszPath, uint attributes, ref SHFILEINFO fileInfo, uint fileInfoSize, uint flags);
    [DllImport("user32.dll")]
    private static extern bool DestroyIcon(nint icon);
    [DllImport("user32.dll")]
    private static extern nint LoadIcon(nint instance, nint iconName);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct SHFILEINFO
    {
        public nint HIcon;
        public int IconIndex;
        public uint Attributes;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)] public string DisplayName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)] public string TypeName;
    }
}
