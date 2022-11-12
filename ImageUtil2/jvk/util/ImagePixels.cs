using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace jvk.util
{
    public struct ImagePixels
    {
        public int w;
        public int h;
        public int bpp;
        public byte[] pixels;
        private int _nBits;
        public double dpiX;
        public double dpiY;

        public int BitsPerPixel { get { return _nBits; } set { setBitsPerPixel(value); }  }
        public int Stride { get { return (w * bpp); } }
        public int PixelCount { get { return (w * h); } }

        void setBitsPerPixel(int numberOfBits)
        {
            int nBytes = (numberOfBits + 7) / 8;
            _nBits = numberOfBits;
            bpp = nBytes;
        }

        public static ImagePixels fromImage(BitmapSource src, bool bAllocPixel, int nBitsPerPixel = 0)
        {
            ImagePixels st;
            st.w = src.PixelWidth;
            st.h = src.PixelHeight;
            st.pixels = null;
            st.bpp = 0;
            st._nBits = 0;
            st.dpiX = src.DpiX;
            st.dpiY = src.DpiY;

            if (0 == nBitsPerPixel)
            {
                st.setBitsPerPixel(src.Format.BitsPerPixel);
            }
            else
            {
                st.setBitsPerPixel(nBitsPerPixel);
            }
            if (bAllocPixel)
            {
                byte[] arr = new byte[st.w * st.h * st.bpp];
                st.pixels = arr;
            }
            return st;
        }

        public static ImagePixels headerOnly(ImagePixels p)
        {
            ImagePixels st = p;
            st.pixels = null;
            return st;
        }

        void ensureSameFormat(ImagePixels src, int nExpectedBpp)
        {
            ImagePixels h1 = headerOnly(this);
            ImagePixels h2 = headerOnly(src);
            if (!h1.Equals(h2))
            {
                throw new InvalidOperationException("Format not match");
            }
            if (h1.bpp != nExpectedBpp)
            {
                throw new InvalidOperationException(String.Format("Not a {0}-bit image", nExpectedBpp * 8));
            }
        }

        public void copyAlphaAsRgb(ImagePixels src)
        {
            int nExpectedBpp = 4;
            ensureSameFormat(src, nExpectedBpp);

            int nPixel = src.w * src.h;
            byte[] srcPixels = src.pixels;
            byte[] dstPixels = pixels;
            int off = 0;
            for (int i = 0; i < nPixel; i++, off += nExpectedBpp)
            {
                byte a = srcPixels[off + 3];
                dstPixels[off] = a;
                dstPixels[off + 1] = a;
                dstPixels[off + 2] = a;
                dstPixels[off + 3] = 0xff;
            }
        }

        public void copyRgb(ImagePixels src)
        {
            int nExpectedBpp = 4;
            ensureSameFormat(src, nExpectedBpp);

            int nPixel = src.w * src.h;
            byte[] srcPixels = src.pixels;
            byte[] dstPixels = pixels;
            int off = 0;
            for (int i = 0; i < nPixel; i++, off += nExpectedBpp)
            {
                dstPixels[off + 0] = srcPixels[off + 0];
                dstPixels[off + 1] = srcPixels[off + 1];
                dstPixels[off + 2] = srcPixels[off + 2];
                dstPixels[off + 3] = 0xff;
            }
        }

        internal void copyChannelAsAlpha(ImagePixels src, int iChannel)
        {
            int nPixel = src.w * src.h;
            byte[] srcPixels = src.pixels;
            byte[] dstPixels = pixels;
            int off = 0;
            for (int i = 0; i < nPixel; i++, off += 4)
            {
                dstPixels[off + 3] = srcPixels[off + iChannel];
            }
        }

    } // end - struct ImagePixels
}
