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
    public class HeatMap : Grid
    {
        private int mRows = 0;
        private int mColumns = 0;
        private double mPixelOpacity;


        public HeatMap()
        {
            // constructor
        }

        public void Create(int Rows, int Columns)
        {
            mRows = Rows;
            mColumns = Columns;

            RowDefinition rowDef;
            ColumnDefinition colDef;

            for (int lRow = 0; lRow < mRows; lRow++)
            {
                rowDef = new RowDefinition();
                this.RowDefinitions.Insert(this.RowDefinitions.Count, rowDef);
                //Console.WriteLine(heat_map.RowDefinitions.IndexOf(rowDef).ToString());
            }

            for (int lCol = 0; lCol < mColumns; lCol++)
            {
                colDef = new ColumnDefinition();
                this.ColumnDefinitions.Insert(this.ColumnDefinitions.Count, colDef);
                //Console.WriteLine(heat_map.ColumnDefinitions.IndexOf(colDef).ToString());
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
    }
}
