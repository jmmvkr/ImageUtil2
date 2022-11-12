using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace jvk.util
{
    public class ImageUtil
    {

        public static BitmapImage loadImage(string pth)
        {
            var buf = File.ReadAllBytes(pth);
            int len = buf.Length;
            using (var ms = new MemoryStream(len))
            {
                ms.Write(buf, 0, len);
                ms.Seek(0, SeekOrigin.Begin);

                var bi = new BitmapImage();
                bi.BeginInit();
                bi.StreamSource = ms;
                bi.CacheOption = BitmapCacheOption.OnLoad;
                bi.EndInit();

                return bi;
            }
        }

        public static ImagePixels createPixels(BitmapSource src, int nBitsPerPixel)
        {
            return ImagePixels.fromImage(src, true, nBitsPerPixel);
        }

        public static ImagePixels createPixels(BitmapSource src)
        {
            return ImagePixels.fromImage(src, true, 0);
        }

        public static ImagePixels getPixelsCopy(BitmapSource src)
        {
            var st = ImagePixels.fromImage(src, true);
            src.CopyPixels(st.pixels, st.Stride, 0);
            return st;
        }

        public static WriteableBitmap createWriteableBitmap(BitmapSource src)
        {
            return createWriteableBitmap(src, PixelFormats.Bgra32);
        }

        public static WriteableBitmap createWriteableBitmap(BitmapSource src, PixelFormat fmt)
        {
            var st = ImagePixels.fromImage(src, false);
            WriteableBitmap wrImage = new WriteableBitmap(st.w, st.h, st.dpiX, st.dpiY, fmt, null);
            return wrImage;
        }

        public static void assignPixels(WriteableBitmap wrImage, ImagePixels st)
        {
            Int32Rect rect = Int32Rect.Parse("0,0,0,0");
            rect.Width = st.w;
            rect.Height = st.h;
            wrImage.WritePixels(rect, st.pixels, st.Stride, 0);
        }

        public static void saveToPng(BitmapSource src, string path)
        {
            byte[] buf;
            using (MemoryStream ms = new MemoryStream())
            {
                PngBitmapEncoder coder = new PngBitmapEncoder();
                coder.Frames.Add(BitmapFrame.Create(src));
                coder.Save(ms);
                buf = ms.ToArray();
            }
            File.WriteAllBytes(path, buf);
        }

    } // end - class ImageUtil
}
