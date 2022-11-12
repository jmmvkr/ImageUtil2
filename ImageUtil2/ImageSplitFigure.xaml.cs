using System;
using System.Collections.Generic;
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
    /// Interaction logic for ImageSplitFigure.xaml
    /// </summary>
    public partial class ImageSplitFigure : UserControl
    {
        bool _bMerge = false;
        Brush _lineColor;
        Pen _linePen;

        public Brush LineColor { get { return _lineColor; } set { setLineColor(value); InvalidateVisual(); } }
        public bool Merge { get { return _bMerge; } set { _bMerge = value; InvalidateVisual(); } }


        public ImageSplitFigure()
        {
            InitializeComponent();
        }

        void setLineColor(Brush br)
        {
            _lineColor = br;
            _linePen = new Pen(br, 2.0);
        }

        protected override void OnRender(DrawingContext ctx)
        {
            Point p1 = Point.Parse("0,0");
            Point p2 = p1;
            double aw = this.ActualWidth;
            double ah = this.ActualHeight;

            double h1 = 0.0;
            double h2 = aw;
            bool bMerge = _bMerge;

            if (null == _linePen)
            {
                setLineColor(Brushes.Black);
            }
            double thick = _linePen.Thickness;

            p1.X = bMerge ? h2 : h1;
            p2.X = bMerge ? h1 : h2;
            p1.Y = 0.5 * ah;
            ctx.DrawLine(_linePen, p1, p2);
            if (bMerge) { ctx.DrawEllipse(null, _linePen, p2, thick, thick); }

            p2.Y = ah;
            ctx.DrawLine(_linePen, p1, p2);
            if (bMerge) { ctx.DrawEllipse(null, _linePen, p2, thick, thick); }

            if (!bMerge)
            {
                ctx.DrawEllipse(null, _linePen, p1, thick, thick);
            }
        }

    } // end - class ImageSplitFigure
}
