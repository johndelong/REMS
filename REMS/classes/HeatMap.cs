using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace REMS.classes
{
    public enum GraphType : int
    {
        Intensity,
        Threshold
    }

    public class HeatMap : Grid
    {
        private int mRows = 0;
        private int mColumns = 0;
        private double mPixelOpacity;
        private int mIntensityMax = 0;
        private int mIntensityMin = 0;
        private Boolean mIntensityUpdates = false;
        private List<Pixel> mPixels;
        private GraphType mType = GraphType.Threshold;

        public int IntensityMax { get; set; }
        public int IntensityMin { get; set; }

        public HeatMap()
        {
            // constructor
        }

        public void Create(int Columns, int Rows)
        {
            mRows = Rows;
            mColumns = Columns;

            mPixels = new List<Pixel>();

            RowDefinition rowDef;
            ColumnDefinition colDef;

            for (int lRow = 0; lRow < mRows; lRow++)
            {
                rowDef = new RowDefinition();
                this.RowDefinitions.Insert(this.RowDefinitions.Count, rowDef);
            }

            for (int lCol = 0; lCol < mColumns; lCol++)
            {
                colDef = new ColumnDefinition();
                this.ColumnDefinitions.Insert(this.ColumnDefinitions.Count, colDef);
            }
        }

        public void Clear()
        {
            this.Children.Clear();
            this.RowDefinitions.Clear();
            this.ColumnDefinitions.Clear();
        }

        public void ClearPixels()
        {
            this.Children.Clear();

            if(mPixels != null)
                mPixels.Clear();
        }

        public Point getClickedCell(object sender, MouseButtonEventArgs e)
        {
            var point = Mouse.GetPosition(this);

            int row = 0;
            int col = 0;
            double accumulatedHeight = 0.0;
            double accumulatedWidth = 0.0;

            // calc row mouse was over
            foreach (var rowDefinition in this.RowDefinitions)
            {
                accumulatedHeight += rowDefinition.ActualHeight;
                if (accumulatedHeight >= point.Y)
                    break;
                row++;
            }

            // calc col mouse was over
            foreach (var columnDefinition in this.ColumnDefinitions)
            {
                accumulatedWidth += columnDefinition.ActualWidth;
                if (accumulatedWidth >= point.X)
                    break;
                col++;
            }

            // row and col now correspond Grid's RowDefinition and ColumnDefinition mouse was 
            // over when double clicked!

            return new Point(col, row); // (x, y)
        }

        public void setPixelOpacity(double Opacity)
        {
            if (Opacity >= 0 && Opacity <= 100)
            {
                mPixelOpacity = Opacity / 100;
            }

            foreach (Rectangle lPixel in Children)
            {
                lPixel.Fill.Opacity = mPixelOpacity;
            }
        }

        public void drawPixel(int aCol, int aRow, int aValue)
        {
            mPixels.Add(new Pixel(aCol, aRow, aValue));

            if (this.Children.Count == 0)
            {
                mIntensityMax = mIntensityMin = aValue;
            }

            if (aValue > mIntensityMax)
            {
                mIntensityMax = aValue;
                mIntensityUpdates = true;
            }
            else if (aValue < mIntensityMin)
            {
                mIntensityMin = aValue;
                mIntensityUpdates = true;
            }

            Color lColor = GetColor(mIntensityMin, mIntensityMax, aValue);

            drawPixel(aCol, aRow, lColor);
        }

        public void drawPixel(int aCol, int aRow, Color aColor)
        {
            Rectangle pixel = new Rectangle();

            SolidColorBrush pixelFill = new SolidColorBrush(aColor);
            pixel.Fill = pixelFill;
            pixel.Opacity = 1;

            Grid.SetColumn(pixel, aCol);
            Grid.SetRow(pixel, aRow);
            pixel.Fill.Opacity = mPixelOpacity;

            this.Children.Add(pixel);
        }

        public void refresh()
        {
            if (mIntensityUpdates)
            {

                this.Children.Clear();
                foreach (Pixel lPixel in mPixels)
                {
                    Color lColor = GetColor(mIntensityMin, mIntensityMax, lPixel.Value);
                    drawPixel(lPixel.Column, lPixel.Row, lColor);
                }
                mIntensityUpdates = false;
            }
        }

        private Color GetColor(int aMin /*Complete Blue*/, int aMax /*Complete Red*/, int aValue)
        {
            if (aMin >= aMax) return Colors.Black;

            int lMaxColorValue = 255 * 4;

            double lRange = aMax - aMin; // make the scale start from 0
            double lActualValue = aValue - aMin; // adjust the value accordingly

            double lPercentage = lActualValue / lRange;
            int lColorValue = Convert.ToInt32(lMaxColorValue * lPercentage);

            int blue = lColorValue <= 255 ? 255 : lColorValue > 255 * 2 ? 0 : 255 * 1 - lColorValue;
            int green = lColorValue <= 255 ? lColorValue : lColorValue > 255 * 3 ? 255 * 4 - lColorValue : 255;
            int red = lColorValue <= 255 * 2 ? 0 : lColorValue > 255 * 2 ? 255 : 255 * 3 - lColorValue;

            // R = 255; G = 0;      B = 0;      255 * 4
            // R = 255; G = 255;    B = 0;      255 * 3
            // R = 0;   G = 255;    B = 0       255 * 2
            // R = 0;   G = 255;    B = 255     255
            // R = 0;   G = 0;      B = 255     0

            return Color.FromRgb((Byte)red, (Byte)green, (Byte)blue);
        }
    }

    public class Pixel
    {
        private int _column;
        private int _row;
        private int _value;

        public Pixel(int Column, int Row, int Value)
        {
            _column = Column;
            _row = Row;
            _value = Value;
        }

        public int Column 
        {
            get { return _column; }
            set { _column = value; }
        }
        
        public int Row 
        {
            get { return _row; }
            set { _row = value; }
        }

        public int Value 
        {
            get { return _value; }
            set { _value = value; }
        }
    }
}
