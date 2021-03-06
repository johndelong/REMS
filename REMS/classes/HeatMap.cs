﻿using System;
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
        private double mIntensityMax = 0;
        private double mIntensityMin = 0;
        private Boolean mIntensityUpdates = false;
        private List<Pixel> mPixels;

        public HeatMap()
        {
            // constructor
        }

        public void Create(int Columns, int Rows, Grid ColorKey)
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

            // Create Grid For intensity Key
            for (int lCol = 0; lCol < 2; lCol++)
            {
                colDef = new ColumnDefinition();
                ColorKey.ColumnDefinitions.Insert(ColorKey.ColumnDefinitions.Count, colDef);
            }

            for (int lRow = 0; lRow < 6; lRow++)
            {
                rowDef = new RowDefinition();
                ColorKey.RowDefinitions.Insert(ColorKey.RowDefinitions.Count, rowDef);
                
                int lValue = (5 - lRow) * 20;
                Color lColor = GetColor(0, 100, lValue);
                Rectangle lPixel = drawPixel(lColor, 1);
                addToGrid(ColorKey, 0, lRow, lPixel);

                Label lLabel = new Label();
                lLabel.VerticalAlignment = VerticalAlignment.Center;
                lLabel.Content = Convert.ToString(Math.Round(Convert.ToDouble(lValue), 2));
                addToGrid(ColorKey, 1, lRow, lLabel);
            }
        }

        public void Clear(Grid aColorKey)
        {
            this.Children.Clear();
            this.RowDefinitions.Clear();
            this.ColumnDefinitions.Clear();

            aColorKey.Children.Clear();
            aColorKey.RowDefinitions.Clear();
            aColorKey.ColumnDefinitions.Clear();
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

        public void addIntensityPixel(int aCol, int aRow, double aValue)
        {
            mPixels.Add(new Pixel(aCol, aRow, aValue));

            // Set the min and max intensity values to the first
            // pixel value
            if (this.Children.Count == 0)
            {
                mIntensityMax = mIntensityMin = aValue;
            }

            // Update the min and max intensity values
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
            Rectangle lPixel = drawPixel(lColor, mPixelOpacity);
            addToGrid(this, aCol, aRow, lPixel); 

            // Re-evaluate all of the pixels
            this.refresh();
        }

        public void addThresholdPixel(int aCol, int aRow, Color aColor)
        {
            Rectangle lPixel = drawPixel(aColor, mPixelOpacity);
            addToGrid(this, aCol, aRow, lPixel);
        }

        private Rectangle drawPixel(Color aColor, Double aFillOpacity)
        {
            Rectangle pixel = new Rectangle();

            SolidColorBrush pixelFill = new SolidColorBrush(aColor);
            pixel.Fill = pixelFill;
            pixel.Opacity = 1;
            pixel.Fill.Opacity = aFillOpacity;

            return pixel;
        }

        private void addToGrid(Grid aGrid, int aCol, int aRow, UIElement aUIElement)
        {
            Grid.SetColumn(aUIElement, aCol);
            Grid.SetRow(aUIElement, aRow);

            aGrid.Children.Add(aUIElement);
        }

        private void refresh()
        {
            // No need to redraw the pixels if the maximum
            // and minimum intensity values haven't changed
            if (mIntensityUpdates)
            {
                this.Children.Clear();
                foreach (Pixel lPixelData in mPixels)
                {
                    Color lColor = GetColor(mIntensityMin, mIntensityMax, lPixelData.Value);
                    Rectangle lPixel = drawPixel(lColor, mPixelOpacity);
                    addToGrid(this, lPixelData.Column, lPixelData.Row, lPixel);
                }
                mIntensityUpdates = false;
            }
        }

        private Color GetColor(double aMin /*Complete Blue*/, double aMax /*Complete Red*/, double aValue)
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

        public void updateIntensityKey(Grid ColorKey)
        {
            double lRange = mIntensityMax - mIntensityMin;
            double lStep = lRange / 5;
            double lCurrent = mIntensityMax;

            foreach (Object lObj in ColorKey.Children)
            {
                if(lObj.GetType().Name == "Label")
                {
                    Label lLabel = ((Label)lObj);
                    lLabel.Content = Convert.ToString(Math.Round(lCurrent, 2));
                    lCurrent -= lStep;
                }
            }

            // Resize the label column
            ColorKey.ColumnDefinitions[1].Width = new GridLength(1, GridUnitType.Auto);
        }

        private class Pixel
        {
            private int _column;
            private int _row;
            private double _value;

            public Pixel(int Column, int Row, double Value)
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

            public double Value
            {
                get { return _value; }
                set { _value = value; }
            }
        }
    }
}
