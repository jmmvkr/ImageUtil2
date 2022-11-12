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
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ImageUtil2
{
    /// <summary>
    /// Interaction logic for ImageDrop.xaml
    /// </summary>
    public partial class ImageDrop : UserControl
    {
        public event RoutedEventHandler OnImageDrop;

        public Brush LineColor { get { return xBorder.BorderBrush; } set { xBorder.BorderBrush = value; } }
        List<string> _fileList = new List<string>();
        public Image ImageHolder { get { return xImageDisplay; }  }
        public string DisplayText { get { return txtDisplayText.Text; } set { txtDisplayText.Text = value; } }


        public ImageDrop()
        {
            InitializeComponent();
        }


        public IList<string> listFiles()
        {
            return _fileList.ToArray();
        }

        private void SpImageDrop_PreviewDragOver(object sender, DragEventArgs e)
        {
            var obj = getDropInfo(e);
            if (null != obj)
            {
                spImageDrop.Background = LineColor;
            }
        }

        private void SpImageDrop_Drop(object sender, DragEventArgs e)
        {
            string[] arr = getDropInfo(e) as string[];

            var lst = _fileList;
            try
            {
                if (null != arr)
                {
                    lst.Clear();
                    bool bScanFiles = true;
                    if (1 == arr.Length)
                    {
                        string path = arr[0];
                        if (Directory.Exists(path))
                        {
                            lst.Add(path);
                            bScanFiles = false;
                        }
                    }
                    if (bScanFiles)
                    {
                        foreach (var path in arr)
                        {
                            if (File.Exists(path))
                            {
                                lst.Add(path);
                            }
                        }
                    }
                    OnImageDrop?.Invoke(this, e);
                }
            }
            finally
            {
                restoreBackground();
            }
        }

        private void SpImageDrop_DragLeave(object sender, DragEventArgs e)
        {
            restoreBackground();
        }

        void restoreBackground()
        {
            spImageDrop.Background = FindResource("bgDropNone") as Brush;
        }

        object getDropInfo(DragEventArgs ev)
        {
            return getDropInfo(ev, DataFormats.FileDrop);
        }

        object getDropInfo(DragEventArgs ev, string fmt)
        {
            object v = null;
            if (ev.Data.GetDataPresent(fmt))
            {
                v = ev.Data.GetData(fmt);
            }
            else
            {
                rejectDrop(ev);
            }
            return v;
        }

        void rejectDrop(DragEventArgs ev)
        {
            ev.Effects = DragDropEffects.None;
            ev.Handled = true;
        }

    } // end - class ImageDrop
}
