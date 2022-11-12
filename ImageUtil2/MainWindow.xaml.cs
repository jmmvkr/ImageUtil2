using jvk.util;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ImageUtil2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        FileInfo _masterFile;
        WriteableBitmap _wrAlpha = null;
        WriteableBitmap _wrRgb = null;
        WriteableBitmap _wrMerge = null;


        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += MainWindow_Loaded;
        }

        private void Current_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            // track UnhandledException in release build 
            showError(e.Exception);
            e.Handled = true;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Application.Current.DispatcherUnhandledException += Current_DispatcherUnhandledException;

            xImageOrig.DisplayText = "PNG Source";
            xImageAlpha.DisplayText = "Alpha";
            xImageRgb.DisplayText = "RGB";
            xImageMerge.DisplayText = "PNG Result";
        }

        bool convertDds(string pth, ref string refPngOut)
        {
            ProcessRun pr = new ProcessRun();
            var ab = ProcessRun.ArgBuilder.makeOne();
            string dirOut = "temp";
            string userDirOut = "tempDir.txt";
            if (File.Exists(userDirOut))
            {
                dirOut = File.ReadAllText(userDirOut);
            }
            else
            {
                File.WriteAllText(userDirOut, dirOut, Encoding.UTF8);
            }

            if (!Directory.Exists(dirOut))
            {
                Directory.CreateDirectory(dirOut);
            }
            pr.args = ab.append("-ft PNG -y -o").append(dirOut).appendQuoted(pth).export();
            pr.fileName = "texconv.exe";
            pr.redirectStandardOutput = true;
            pr.run();

            if (0 == pr.ExitCode)
            {
                FileInfo fi = new FileInfo(pth);
                string nm = fi.Name;
                string tmpDds = String.Format(@"{0}\{1}", dirOut, fi.Name);
                if (tmpDds.EndsWith(".dds", StringComparison.InvariantCultureIgnoreCase))
                {
                    string tmpPng = tmpDds.Substring(0, tmpDds.Length - 4) + ".png";
                    if (File.Exists(tmpPng))
                    {
                        var buf = File.ReadAllBytes(fi.FullName);
                        File.WriteAllBytes(tmpDds, buf);
                        File.SetAttributes(tmpDds, fi.Attributes);
                        refPngOut = tmpPng;
                        return true;
                    }
                }
            }
            return false;
        }

        void runCatched<TEvent>(object sender, TEvent e, Action act)
        {
            try
            {
                act();
            }
            catch (Exception ex)
            {
                showError(ex);
            }
        }

        void showError(string msg, string caption = "")
        {
            MessageBox.Show(msg, caption, MessageBoxButton.OK, MessageBoxImage.Error);
        }

        void showError(Exception ex)
        {
            StringBuilder sb = new StringBuilder();
            string tp = ex.GetType().ToString();

            sb.AppendLine(ex.Message);
            sb.AppendLine();
            sb.AppendLine(ex.StackTrace);
            showError(sb.ToString(), tp);
        }

        private void XImageOrig_OnImageDrop(object sender, RoutedEventArgs e)
        {
            runCatched(sender, e, () => {
                ImageDrop d = sender as ImageDrop;
                var fileList = d.listFiles();

                string dirSingle;
                if ((1 == fileList.Count) && Directory.Exists(dirSingle = fileList[0]))
                {
                    txtFolder.Text = dirSingle;
                    onSingleFolder(dirSingle);
                    return;
                }

                foreach (string s in fileList)
                {
                    processFirstImage(s);
                    break;
                }

                if (fileList.Count > 1)
                {
                    onFileListing(fileList);
                }
                if (0 == fileList.Count)
                {
                    showError("No supported file");
                }
            });
        }

        void onFileListing(IList<string> fileList)
        {
            List<ImageItem> lst = new List<ImageItem>();
            StringComparison cmp = StringComparison.InvariantCultureIgnoreCase;

            foreach (var pth in fileList)
            {
                FileInfo fi = new FileInfo(pth);
                ImageItem st;
                if (pth.EndsWith(".dds", cmp) || pth.EndsWith(".png", cmp))
                {
                    st.imagePath = pth;
                    st.imageName = fi.Name;
                    lst.Add(st);
                }
            }
            xListFiles.ItemsSource = lst;
        }


        void processFirstImage(string pthIn)
        {
            string s = pthIn;
            if (s.EndsWith(".dds", StringComparison.InvariantCultureIgnoreCase))
            {
                string pthPng = "";
                if (convertDds(pthIn, ref pthPng))
                {
                    s = pthPng;
                }
            }

            BitmapImage bi = ImageUtil.loadImage(s);
            var pixelsAlpha = ImageUtil.createPixels(bi, 32);
            var pixelsRgb = ImageUtil.createPixels(bi, 32);
            var src = ImageUtil.getPixelsCopy(bi);
            if (4 != src.bpp)
            {
                throw new ArgumentException("Not a ARGB image");
            }

            _masterFile = new FileInfo(s);
            xImageOrig.ImageHolder.Source = bi;

            var wrAlpha = getOrCreateWriteable(ref _wrAlpha, bi);
            var wrRgb = getOrCreateWriteable(ref _wrRgb, bi);
            if (null != wrAlpha)
            {
                pixelsAlpha.copyAlphaAsRgb(src);
                showImagePixels(pixelsAlpha, wrAlpha, xImageAlpha.ImageHolder);
            }
            if (null != wrAlpha)
            {
                pixelsRgb.copyRgb(src);
                showImagePixels(pixelsRgb, wrRgb, xImageRgb.ImageHolder);
            }
            updateMergedImage();
        }

        void updateMergedImage()
        {
            var wrAlpha = _wrAlpha;
            var wrRgb = _wrRgb;

            if (null != wrAlpha && null != wrRgb)
            {
                var pixelsAlpha = ImageUtil.getPixelsCopy(wrAlpha);
                var pixelsRgb = ImageUtil.getPixelsCopy(wrRgb);

                var h1 = ImagePixels.headerOnly(pixelsAlpha);
                var h2 = ImagePixels.headerOnly(pixelsRgb);
                if (!h1.Equals(h2))
                {
                    _wrAlpha = null;
                    _wrRgb = null;
                    throw new ArgumentException("image mismatch on size / format");
                }

                var wrMerge = getOrCreateWriteable(ref _wrMerge, wrAlpha);
                pixelsRgb.copyChannelAsAlpha(pixelsAlpha, 0);
                showImagePixels(pixelsRgb, wrMerge, xImageMerge.ImageHolder);
            }
        }

        WriteableBitmap getOrCreateWriteable(ref WriteableBitmap rBmp, BitmapSource bi)
        {
            WriteableBitmap bm = rBmp;
            if (null != bm)
            {
                if ((bm.PixelWidth != bi.PixelWidth) || (bm.PixelHeight != bi.PixelHeight))
                {
                    bm = null;
                }
            }
            if (null == bm)
            {
                bm = ImageUtil.createWriteableBitmap(bi, PixelFormats.Bgra32);
                rBmp = bm;
            }
            return bm;
        }

        void showImagePixels(ImagePixels pixels, WriteableBitmap wr, Image img)
        {
            img.Source = null;
            ImageUtil.assignPixels(wr, pixels);
            img.Source = wr;
        }

        string getOneDrop(object sender)
        {
            ImageDrop d = sender as ImageDrop;
            var fileList = d.listFiles();
            int cnt = fileList.Count;
            if (cnt > 1)
            {
                throw new ArgumentException("too many files");
            }
            if (0 == cnt)
            {
                throw new ArgumentException("file not supported");
            }
            return fileList[0];
        }

        private void XImageAlpha_OnImageDrop(object sender, RoutedEventArgs e)
        {
            runCatched(sender, e, () =>
            {
                useDropImage(sender, ref _wrAlpha);
            });
        }

        private void XImageRgb_OnImageDrop(object sender, RoutedEventArgs e)
        {
            runCatched(sender, e, () =>
            {
                useDropImage(sender, ref _wrRgb);
            });
        }

        void useDropImage(object sender, ref WriteableBitmap wrRef)
        {
            var drop = sender as ImageDrop;
            string pth = getOneDrop(sender);
            BitmapImage bi = ImageUtil.loadImage(pth);
            var pixels = ImageUtil.getPixelsCopy(bi);

            var wr = getOrCreateWriteable(ref wrRef, bi);
            showImagePixels(pixels, wr, drop.ImageHolder);
            updateMergedImage();
        }


        private void XImageOrig_MouseDown(object sender, MouseButtonEventArgs e)
        {
            runCatched(sender, e, () => {
                showOverlayImage(e, xImageOrig.ImageHolder.Source as BitmapSource);
            });
        }

        private void XImageRgb_MouseDown(object sender, MouseButtonEventArgs e)
        {
            runCatched(sender, e, () =>
            {
                saveImageAs(_wrRgb, e, ".rgb.png");
            });
        }
        private void XImageAlpha_MouseDown(object sender, MouseButtonEventArgs e)
        {
            runCatched(sender, e, () =>
            {
                saveImageAs(_wrAlpha, e, ".alpha.png");
            });
        }
        private void XImageMerge_MouseDown(object sender, MouseButtonEventArgs e)
        {
            runCatched(sender, e, () =>
            {
                saveImageAs(_wrMerge, e, ".merge.png");
            });
        }

        string getFileName(string strReplace)
        {
            var f = _masterFile;
            if (null != f)
            {
                string nm = f.Name;
                if (nm.EndsWith(".png", StringComparison.InvariantCultureIgnoreCase))
                {
                    if (!nm.EndsWith(".merge.png", StringComparison.InvariantCultureIgnoreCase))
                    {
                        nm = nm.Substring(0, nm.Length - 4) + strReplace;
                    }
                }
                return nm;
            }
            return "out.png";
        }

        bool showOverlayImage(MouseButtonEventArgs e, BitmapSource bm)
        {
            if (e.ChangedButton != MouseButton.Left)
            {
                switch (e.ChangedButton)
                {
                    case MouseButton.Right:
                        spOverlay.Background = Brushes.White;
                        break;
                    case MouseButton.Middle:
                        spOverlay.Background = Brushes.Gray;
                        break;
                }
                spOverlay.Visibility = Visibility;
                imgOverlay.Source = bm;
                return true;
            }
            return false;
        }

        void saveImageAs(BitmapSource bm, MouseButtonEventArgs e, string strReplace)
        {
            if (null == bm) return;
            if (showOverlayImage(e, bm)) return;

            SaveFileDialog dlg = new SaveFileDialog();
            dlg.FileName = getFileName(strReplace);
            dlg.Filter = "PNG files (*.png)|*.png|All files (*.*)|*.*";
            if (true == dlg.ShowDialog())
            {
                string nm = dlg.FileName;
                ImageUtil.saveToPng(bm, nm);
            }
        }

        private void XListFiles_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var sel = (sender as ListView).SelectedItem as ImageItem?;
            if (null != sel)
            {
                processFirstImage(sel.Value.Path);
            }
        }

        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Return:
                case Key.F5:
                    break;
                default:
                    return;
            }

            var tb = sender as TextBox;
            string dir = tb.Text;
            onSingleFolder(dir);
        }

        private void onSingleFolder(string dir)
        {
            if (Directory.Exists(dir))
            {
                var arr = Directory.GetFiles(dir);
                onFileListing(arr);
            }
        }

        private void SpOverlay_MouseDown(object sender, MouseButtonEventArgs e)
        {
            spOverlay.Visibility = Visibility.Hidden;
        }

        public struct ImageItem
        {
            public string imagePath;
            public string imageName;

            public string Name { get { return imageName; } }
            public string Path { get { return imagePath; } }
        }

    } // end - class MainWindow
}
