using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using static Vanara.PInvoke.Shell32;

namespace Dawn.Common.Windows.Desktop.Extensions;

public static class IconEx
{
    extension(Icon icon)
    {
        public static Icon ExtractAssociatedIcon(string filePath, SHIL imageType = SHIL.SHIL_JUMBO)
        {
            var info = default(SHFILEINFO);
            SHGetFileInfo(filePath, FileAttributes.None, ref info, SHFILEINFO.Size,
                SHGFI.SHGFI_SYSICONINDEX);

            var imageList = SHGetImageList(imageType);

            using var hIcon = imageList.GetIcon(info.iIcon, ComCtl32.IMAGELISTDRAWFLAGS.ILD_TRANSPARENT);

            return hIcon.ToIcon();
        }
        
        public BitmapSource ToBitmapSource(Int32Rect? sourceRect = null, BitmapSizeOptions? sizeOptions = null)
        {
            return Imaging.CreateBitmapSourceFromHIcon(
                icon.Handle,
                sourceRect ?? Int32Rect.Empty,
                sizeOptions ?? BitmapSizeOptions.FromEmptyOptions());
        }
    }
    
    extension(SafeHICON icon)
    {
        public Icon ToIcon() => Icon.FromHandle(icon.ReleaseOwnership());
    }
}