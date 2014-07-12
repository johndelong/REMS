﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows;
using System.Windows.Shapes;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Controls;
using System.Collections.ObjectModel;
using REMS.data;

namespace REMS.classes
{
    public static class Constants
    {
        public static readonly string StatusInitial1 = "Click 'Tools>Capture Image' to define the scan area";
        public static readonly string StatusInitial2 = "Click and drag to determine scan area";
        public static readonly string StatusInitial3 = "Click accept when desired scan area has been selected";
        public static readonly string StatusReady = "Ready to start scanning";
        public static readonly string StatusStopped = "Scanning paused";
        public static readonly string StatusScanning = "Scanning...";
        public static readonly string StatusOverview = "To start a new scan, click 'File>New Scan'";
        public static readonly string StatusDone = "Scan has finished";
        public static readonly string StatusCalibration1 = "Accurately click OPPOSITE corners of servo travel area";
        public static readonly string StatusCalibration2 = "Click 'Accept' if satisfied with calibration";

        public enum state : int
        {
            Initial,
            Ready,
            Calibration,
            Stopped,
            Scanning,
            Overview
        }

        public enum direction : int
        {
            North,
            South,
            East,
            West
        }
    }

    public static class Utilities
    {
        public static Constants.direction reverseDirection(Constants.direction aDirection)
        {
            if (aDirection == Constants.direction.North)
                return Constants.direction.South;
            else if (aDirection == Constants.direction.South)
                return Constants.direction.North;
            else if (aDirection == Constants.direction.East)
                return Constants.direction.West;
            else
                return Constants.direction.East;
        }

        public static void Sort<T>(this ObservableCollection<T> collection) where T : IComparable
        {
            List<T> sorted = collection.OrderBy(x => x).ToList();
            for (int i = 0; i < sorted.Count(); i++)
                collection.Move(collection.IndexOf(sorted[i]), i);
        }


        /// <summary>
        /// Given to points, this function returns the top left and bottom right corner
        /// of the rectangle
        /// </summary>
        /// <param name="aPoint1"></param>
        /// <param name="aPoint2"></param>
        /// <returns></returns>
        public static System.Windows.Point[] determineOpositeCorners(System.Windows.Point aPoint1, System.Windows.Point aPoint2)
        {
            System.Windows.Point topLeft = new System.Windows.Point();
            System.Windows.Point bottomRight = new System.Windows.Point();
            System.Windows.Point[] corners = new System.Windows.Point[2];

            if (aPoint1.X < aPoint2.X && aPoint1.Y < aPoint2.Y) // top left to bottom right
            {
                topLeft = aPoint1;
                bottomRight = aPoint2;
            }
            else if (aPoint1.X > aPoint2.X && aPoint1.Y > aPoint2.Y) // bottom right to top left
            {
                topLeft = aPoint2;
                bottomRight = aPoint1;
            }
            else if (aPoint1.X > aPoint2.X && aPoint1.Y < aPoint2.Y) // top right to bottom left
            {
                topLeft.X = aPoint2.X;
                topLeft.Y = aPoint1.Y;
                bottomRight.X = aPoint1.X;
                bottomRight.Y = aPoint2.Y;
            }
            else if (aPoint1.X < aPoint2.X && aPoint1.Y > aPoint2.Y) // bottom left to top right
            {
                topLeft.X = aPoint1.X;
                topLeft.Y = aPoint2.Y;
                bottomRight.X = aPoint2.X;
                bottomRight.Y = aPoint1.Y;
            }

            corners[0] = topLeft;
            corners[1] = bottomRight;
            return corners;
        }

        public static void analyzeScannedData(double[] aCollectedData, double aMinFreq, double aMaxFreq, ThresholdViewModel aThreshold,
            out Boolean aPassed, out double aValue)
        {
            aPassed = true; // Default to Passed;
            aValue = 0;

            Nullable<double> lMinDifference = null;
            double lStepSize = (aMaxFreq - aMinFreq) / aCollectedData.Count();
            double lCurrFreq = aMinFreq;

            if (aThreshold != null)
            {
                // Loop through all of the amplitudes of the collected data
                foreach (double lCollectedAmplitude in aCollectedData) // number of data points collected
                {
                    ThresholdLimitViewModel prevLimit = null;

                    // Loop through all of the limits of the threshold
                    foreach (ThresholdLimitViewModel lLimit in aThreshold.Limits) // limits within this threshold
                    {
                        if (prevLimit == null)
                            prevLimit = lLimit;

                        if (lCurrFreq > Convert.ToDouble(lLimit.Frequency))
                        {
                            prevLimit = lLimit;
                            continue;
                        }

                        // If we have gotten this far, we know we are comparing 
                        //the data to the correct threshold
                        if (Convert.ToDouble(prevLimit.Amplitude) + (lCollectedAmplitude * -1) < lMinDifference || lMinDifference == null)
                        {
                            lMinDifference = Convert.ToDouble(prevLimit.Amplitude) + (lCollectedAmplitude * -1);
                            aValue = (lMinDifference.Value * -1);
                        }

                        if (lCollectedAmplitude > Convert.ToInt32(prevLimit.Amplitude))
                        {
                            aThreshold.State = "Failed";
                            aPassed = false;
                            break;
                        }
                    }

                    lCurrFreq += lStepSize;
                }
            }
        }
    }

    public static class DialogBox
    {
        public static Boolean save(out string aFilePath, string aType, string aFileName = "")
        {
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
            string lFileName = aFileName;
            // Name the log file
            dlg.FileName = lFileName;
            switch (aType.ToUpper())
            {
                case "LOG":
                    dlg.DefaultExt = ".csv";
                    dlg.Filter = "CSV Files (*.csv)|*.csv";
                    break;

                case "IMAGE":
                    dlg.DefaultExt = ".jpg";
                    dlg.Filter = "JPG Files (*.jpg)|*.jpg|JPEG Files (*.jpeg)|*.jpeg";
                    break;
            }

            // Show save file dialog box
            Nullable<bool> result = dlg.ShowDialog();

            // Process save file dialog box results
            if (result != true) result = false;

            // Save document
            aFilePath = dlg.FileName;

            return (Boolean)result;
        }

        public static Boolean open(out string aFilePath, string aType = "LOG")
        {
            // Create OpenFileDialog 
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();

            // Set filter for file extension and default file extension 
            switch (aType.ToUpper())
            {
                case "LOG":
                    dlg.DefaultExt = ".csv";
                    dlg.Filter = "CSV Files (*.csv)|*.csv";
                    break;

                case "IMAGE":
                    dlg.DefaultExt = ".jpg";
                    dlg.Filter = "JPG Files (*.jpg)|*.jpg|JPEG Files (*.jpeg)|*.jpeg";
                    break;
            }

            // Display OpenFileDialog by calling ShowDialog method 
            Nullable<bool> result = dlg.ShowDialog();

            // Get the selected file name and display in a TextBox 
            if (result != true) result = false;

            aFilePath = dlg.FileName;

            return (Boolean)result;
        }
    }

    public static class Imaging
    {
        public static ImageSource openImageSource(string aFileName)
        {
            //aImgSource = null;

            BitmapImage lImage = new BitmapImage(new Uri(aFileName));
            return ConvertBitmapTo96DPI(lImage);
            //imageCaptured.Source = loadedImage;
            //imageLoaded = true;

            //canvasDrawing.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// Converts bitmap images to 96DPI.
        /// 
        /// This function is required so that all images are of the same resolution
        /// Original: http://social.msdn.microsoft.com/Forums/vstudio/en-US/35db45e3-ebd6-4981-be57-2efd623ea439/wpf-bitmapsource-dpi-change
        /// </summary>
        /// <param name="bitmapImage"></param>
        /// <returns></returns>
        public static BitmapSource ConvertBitmapTo96DPI(BitmapImage bitmapImage)
        {
            double dpi = 96;
            int width = bitmapImage.PixelWidth;
            int height = bitmapImage.PixelHeight;

            int stride = width * 4; // 4 bytes per pixel
            byte[] pixelData = new byte[stride * height];
            bitmapImage.CopyPixels(pixelData, stride, 0);

            return BitmapSource.Create(width, height, dpi, dpi, PixelFormats.Bgra32, null, pixelData, stride);
        }

        /// <summary>
        /// Assumes has top left and bottom right corners
        /// </summary>
        /// <param name="aImage"></param>
        /// <param name="aMouseDown"></param>
        /// <param name="aMouseUp"></param>
        /// <returns></returns>
        public static ImageSource cropImage(System.Windows.Controls.Image aImage, System.Windows.Point aMouseDown, System.Windows.Point aMouseUp)
        {

            // Convert selected coordinates to actual image coordinates
            Double Xbegin = (aMouseDown.X * aImage.Source.Width) / aImage.ActualWidth;
            Double Ybegin = (aMouseDown.Y * aImage.Source.Height) / aImage.ActualHeight;
            Double Xend = (aMouseUp.X * aImage.Source.Width) / aImage.ActualWidth;
            Double Yend = (aMouseUp.Y * aImage.Source.Height) / aImage.ActualHeight;

            // Convert coordinates to integers
            int xPos = Convert.ToInt32(Math.Round(Xbegin, 0, MidpointRounding.ToEven));
            int yPos = Convert.ToInt32(Math.Round(Ybegin, 0, MidpointRounding.ToEven));
            int width = Convert.ToInt32(Math.Round(Xend - Xbegin, 0, MidpointRounding.ToEven));
            int height = Convert.ToInt32(Math.Round(Yend - Ybegin, 0, MidpointRounding.ToEven));

            // Create the cropped image
            var fullImage = aImage.Source;
            var croppedImage = new CroppedBitmap((BitmapSource)fullImage, new Int32Rect(xPos, yPos, width, height));

            return croppedImage;
        }

        /// <summary>
        /// From a specific location, determines location on image/canvas using aspect ration
        /// </summary>
        /// <param name="aImage"></param>
        /// <param name="aLocation"></param>
        /// <returns></returns>
        public static System.Windows.Point getAspectRatioPosition(System.Windows.Point aLocation, double aFromWidth, double aFromHeight, double aToWidth, double aToHeight)
        {
            // Take current location and convert to actual image location
            Double dblXPos = (aLocation.X * aToWidth) / aFromWidth;
            Double dblYPos = (aLocation.Y * aToHeight) / aFromHeight;

            return new System.Windows.Point(dblXPos, dblYPos);
        }

        public static void SaveToJPG(FrameworkElement visual, string fileName)
        {
            var encoder = new JpegBitmapEncoder();
            SaveUsingEncoder(visual, fileName, encoder);
        }

        public static void SaveToJPG(BitmapSource bitmap, string fileName)
        {
            var encoder = new JpegBitmapEncoder();
            SaveUsingEncoder(bitmap, fileName, encoder);
        }

        public static void SaveToPng(FrameworkElement visual, string fileName)
        {
            var encoder = new PngBitmapEncoder();
            SaveUsingEncoder(visual, fileName, encoder);
        }

        public static void SaveToPng(BitmapSource bitmap, string fileName)
        {
            var encoder = new PngBitmapEncoder();
            SaveUsingEncoder(bitmap, fileName, encoder);
        }

        private static void SaveUsingEncoder(BitmapSource bitmap, string fileName, BitmapEncoder encoder)
        {
            BitmapFrame frame = BitmapFrame.Create(bitmap);
            encoder.Frames.Add(frame);

            using (var stream = File.Create(fileName))
            {
                encoder.Save(stream);
            }
        }

        private static void SaveUsingEncoder(FrameworkElement visual, string fileName, BitmapEncoder encoder)
        {
            RenderTargetBitmap bitmap = new RenderTargetBitmap(
                (int)visual.ActualWidth,
                (int)visual.ActualHeight,
                96,
                96,
                PixelFormats.Pbgra32);

            bitmap.Render(visual);
            BitmapFrame frame = BitmapFrame.Create(bitmap);
            encoder.Frames.Add(frame);

            using (var stream = File.Create(fileName))
            {
                encoder.Save(stream);
            }
        }
    }

    public static class LogReader
    {
        public static void openReport(string aFileName, HeatMap aHeatMap, DataGrid aScanLevels, Grid ColorKey)
        {
            string[] lLine = null;
            ObservableCollection<ScanLevel> lScanLevels = new ObservableCollection<ScanLevel>();
            int lRows = 0;
            int lCols = 0;
            try
            {
                using (StreamReader srLog = new StreamReader(aFileName))
                {
                    while (srLog.Peek() >= 0)
                    {
                        lLine = srLog.ReadLine().Split(',');
                        if (Convert.ToInt32(lLine[1]) > lRows)
                            lRows = Convert.ToInt32(lLine[1]);

                        if (Convert.ToInt32(lLine[0]) > lCols)
                            lCols = Convert.ToInt32(lLine[0]);

                        if (lScanLevels.Count() > 0)
                        {
                            if (lScanLevels.ElementAt<ScanLevel>(lScanLevels.Count() - 1).ZPos != Convert.ToInt32(lLine[2]))
                                lScanLevels.Add(new ScanLevel(Convert.ToInt32(lLine[2]), "Complete"));
                        }
                        else
                        {
                            lScanLevels.Add(new ScanLevel(Convert.ToInt32(lLine[2]), "Complete"));
                        }
                    }
                }
                aHeatMap.Clear(ColorKey);
                aHeatMap.Create(lCols + 1, lRows + 1, ColorKey);
                aScanLevels.ItemsSource = lScanLevels;
            }
            catch (Exception)
            {
                Console.WriteLine("Error occured when trying to read in file");
            }
        }
    }

    public static class Logger
    {
        /// <summary>
        /// Concats a line to the end of a file
        /// </summary>
        /// <param name="aFileName"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <param name="data"></param>
        public static void writeToFile(string aFileName, int x, int y, int z, double[] data)
        {
            //To append to file, set second argument of StreamWriter to true
            using (StreamWriter swLog = new StreamWriter(aFileName, true))
            {
                swLog.Write(x.ToString() + ",");
                swLog.Write(y.ToString() + ",");
                swLog.Write(z.ToString() + ",");

                for (int i = 0; i < data.Length; i++)
                {
                    swLog.Write(data[i].ToString());

                    if (i + 1 < data.Length)
                        swLog.Write(",");
                }

                swLog.Write("\r\n");
            }
        }

        /// <summary>
        /// Returns a specific line from a file
        /// </summary>
        /// <param name="aFileName"></param>
        /// <param name="aLine"></param>
        /// <returns></returns>
        public static string[] getLineFromFile(string aFileName, String aRow, String aCol, String aZPos)
        {
            string[] lResult = null;
            using (StreamReader srLog = new StreamReader(aFileName))
            {
                while (srLog.Peek() >= 0)
                {
                    lResult = srLog.ReadLine().Split(',');
                    if (lResult[0] == aRow && lResult[1] == aCol && lResult[2] == aZPos)
                        break;
                }
            }
            return lResult;
        }

        /// <summary>
        /// A useless function that will eventually be deleted
        /// </summary>
        /// <param name="aFileName"></param>
        public static void read_csv(string aFileName)
        {
            //var reader = new StreamReader(File.OpenRead(@"C:\Users\delongja\Documents\Visual Studio 2013\Projects\ReadCSV\SampleData.csv"));
            var reader = new StreamReader(File.OpenRead(aFileName));
            List<List<string>> Matrix = new List<List<string>>();

            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                var values = line.Split(',');

                Matrix.Add(values.ToList());
            }

            //Console.WriteLine("Here it comes!");
            //Console.WriteLine(Matrix[1][1]);

            foreach (var row in Matrix)
            {
                foreach (var val in row)
                {
                    Console.Write(val);
                }
                Console.Write('\n');
            }

            /*if (openFileDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                pictureBox1.Load(openFileDialog1.FileName);
            }*/
        }

        /*static string ReadLines(Stream source, Encoding encoding)
        {
            using (StreamReader reader = new StreamReader(source, encoding))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    return line;
                }
            }
            return "";
        }*/
    }

    public class Motor
    {
        private Boolean _connected = false;
        private ClsVxmDriver Vxm;

        public Motor()
        {
            Vxm = new ClsVxmDriver();
        }

        public Boolean connected
        {
            get { return _connected; }
            set { _connected = value; }
        }

        public void connect()
        {
            int lStatus = 0;

            lStatus = Vxm.LoadDriver("VxmDriver.dll");
            Console.WriteLine("Loading Motor Driver: " + (lStatus == 1 ? "Success" : "Failed"));

            lStatus = Vxm.PortOpen(Properties.Settings.Default.MotorCommPort, 9600);
            Console.WriteLine("Connecting to Motors: " + (lStatus == 1 ? "Success" : "Failed"));

            if (lStatus == 1)
            {
                _connected = true;
                //Vxm.DriverTerminalShowState(1, 0);

            }

            
            Console.WriteLine("Speed of motors set to 4000 steps per second (20mm/s): " + (lStatus == 1 ? "Success" : "Failed"));
        }

        public void disconnect()
        {
            Vxm.PortClose();

            _connected = false;

            Vxm.ReleaseDriver();
        }

        public void sendCommand(string aCommand)
        {
            if (_connected)
            {
                Vxm.PortSendCommands(aCommand);
            }
        }

        public void decelerate()
        {
            if (_connected)
            {
                Vxm.PortSendCommands("D");
            }
        }

        public void getPosition()
        {
            /*int lStatus = 0;
            lStatus = Vxm.PortSendCommands("*");
            
            //Console.WriteLine(Vxm.PortWaitForCharWithMotorPosition());
            Vxm.
            Console.WriteLine("Motor 2 Pos: " + Vxm.PortReadReply());*/
            //
            //Vxm.PortWaitForChar("^", 0);
            //Vxm.PortSendCommands("Y");
            //System.Threading.Thread.Sleep(5000);
            //Vxm.PortWaitForChar("", 0);
            //string lReply = Vxm.PortReadReply();
            //Console.WriteLine("Reply: " + lReply);
        }

        public void move(int aXPos, int aYPos, int aZPos)
        {
            if (_connected)
            {
                // Since our motor step size is 0.005mm, we have to
                // multiply our steps by 200
                string lXPos = Convert.ToString(aXPos * 200);
                string lYPos = Convert.ToString(aYPos * 200);
                string lZPos = Convert.ToString(aZPos * 200);

                int lStatus = 0;
                String lCommand = "F,C,(IA3M" + lXPos + ",IA1M" + lYPos + ",IA2M" + lZPos + ",)R";
                lStatus = Vxm.PortSendCommands(lCommand);
                Console.WriteLine("Moving Motors: " + (lStatus == 1 ? "Success" : "Failed"));

                Vxm.PortWaitForChar("^", 0);
            }
        }

        public void stop()
        {
            Vxm.PortSendCommands("K");
        }

        public void home()
        {
            if (_connected)
            {
                int lStatus = 0;
                //lStatus = Vxm.PortSendCommands("F,C,S1M4000,S2M4000,R");
                lStatus = Vxm.PortSendCommands("F,C,I2M-0,R");
                Console.WriteLine("Homing Z: " + (lStatus == 1 ? "Success" : "Failed"));
                Vxm.PortWaitForChar("^", 0);

                lStatus = Vxm.PortSendCommands("F,C,(I3M-0,I1M-0,)R");
                Console.WriteLine("Homing X & Y: " + (lStatus == 1 ? "Success" : "Failed"));
                Vxm.PortWaitForChar("^", 0);

                lStatus = Vxm.PortSendCommands("N");
                Console.WriteLine("Update Home Position: " + (lStatus == 1 ? "Success" : "Failed"));
                Vxm.PortWaitForChar("^", 0);

                //lStatus = Vxm.PortSendCommands("F,C,S1M2000,S2M2000,R");
            }
        }
    }

    public static class ExternalData
    {
        public static ObservableCollection<ThresholdViewModel> GetThresholds(String aFile = "Thresholds.csv")
        {
            ObservableCollection<ThresholdViewModel> lThresholds = new ObservableCollection<ThresholdViewModel>();
            
            try
            {
                if (!File.Exists(aFile))
                {
                    File.Create(aFile);
                }

                using (StreamReader sr = new StreamReader(aFile))
                {
                    string line;

                    while ((line = sr.ReadLine()) != null)
                    {
                        //Console.WriteLine(line);
                        string[] lPairs = line.Split(',');
                        ThresholdViewModel lThreshold = new ThresholdViewModel();
                        lThreshold.Name = lPairs[0];

                        for (int i = 1; i < lPairs.Length; i++)
                        {
                            string[] lTemp = lPairs[i].Split('|');
                            lThreshold.Limits.Add(new ThresholdLimitViewModel(Convert.ToDouble(lTemp[0]), Convert.ToDouble(lTemp[1])));
                        }

                        lThresholds.Add(lThreshold);
                    }
                }
                
            }
            catch (Exception) { }

            return lThresholds;
        }

        public static void SaveThresholds(ObservableCollection<ThresholdViewModel> aThresholds, String aFile = "Thresholds.csv")
        {
            try
            {
                if (!File.Exists(aFile))
                {
                    File.Create(aFile);
                }

                using (StreamWriter sw = new StreamWriter(aFile))
                {
                    foreach (ThresholdViewModel lThreshold in aThresholds)
                    {
                        sw.Write(lThreshold.Name);
                        foreach (ThresholdLimitViewModel lLimits in lThreshold.Limits)
                        {
                            sw.Write(",");
                            sw.Write(lLimits.Frequency + "|" + lLimits.Amplitude);
                        }
                        sw.Write("\r\n");
                    }
                }

            }
            catch (Exception) { }
        }
    }

    public static class Draw
    {
        /// <summary>
        /// Draws a rectange with a boarder and clear background 
        /// </summary>
        /// <param name="aDrawingCanvas"></param>
        /// <param name="aX"></param>
        /// <param name="aY"></param>
        /// <param name="aWidth"></param>
        /// <param name="aHeight"></param>
        /// <param name="aColor"></param>
        /// <param name="aDashedLine"></param>
        public static void Rectangle(Canvas aDrawingCanvas, double aX, double aY, double aWidth, double aHeight, System.Windows.Media.Brush aColor, Boolean aDashedLine = false)
        {
            try
            {
                Shape Rendershape = new System.Windows.Shapes.Rectangle();
                Rendershape.Stroke = aColor;
                Rendershape.StrokeThickness = 3;

                if (aDashedLine)
                {
                    Rendershape.StrokeDashArray = new DoubleCollection() { 2, 1 };
                }

                Canvas.SetLeft(Rendershape, aX);
                Canvas.SetTop(Rendershape, aY);
                Rendershape.Height = aHeight;
                Rendershape.Width = aWidth;

                aDrawingCanvas.Children.Add(Rendershape);
            }
            catch (Exception) { }
        }

        /// <summary>
        /// Draws a circle at a specific location
        /// </summary>
        /// <param name="aLocation"></param>
        public static void Circle(Canvas aDrawingCanvas, System.Windows.Point aLocation)
        {
            Shape Rendershape = new Ellipse() { Height = 20, Width = 20 };
            Rendershape.Fill = System.Windows.Media.Brushes.Blue;
            Canvas.SetLeft(Rendershape, aLocation.X - 10);
            Canvas.SetTop(Rendershape, aLocation.Y - 10);
            aDrawingCanvas.Children.Add(Rendershape);
        }
    }

    public class Analyzer
    {
        private ArrayList mFisheyeCorrect;
        private int mFELimit = 1500;
        private double mScaleFESize = 0.9;

        public Analyzer()
        {
            //A lookup table so we don't have to calculate Rdistorted over and over
            //The values will be multiplied by focal length in pixels to 
            //get the Rdistorted
            mFisheyeCorrect = new ArrayList(mFELimit);
            //i corresponds to Rundist/focalLengthInPixels * 1000 (to get integers)
            for (int i = 0; i < mFELimit; i++)
            {
                double result = Math.Sqrt(1 - 1 / Math.Sqrt(1.0 + (double)i * i / 1000000.0)) * 1.4142136;
                mFisheyeCorrect.Add(result);
            }
        }

        public Bitmap RemoveFisheye(ref Bitmap aImage, double aFocalLinPixels)
        {
            Bitmap correctedImage = new Bitmap(aImage.Width, aImage.Height);
            //The center points of the image
            double xc = aImage.Width / 2.0;
            double yc = aImage.Height / 2.0;
            Boolean xpos, ypos;
            //Move through the pixels in the corrected image; 
            //set to corresponding pixels in distorted image
            for (int i = 0; i < correctedImage.Width; i++)
            {
                for (int j = 0; j < correctedImage.Height; j++)
                {
                    //which quadrant are we in?
                    xpos = i > xc;
                    ypos = j > yc;
                    //Find the distance from the center
                    double xdif = i - xc;
                    double ydif = j - yc;
                    //The distance squared
                    double Rusquare = xdif * xdif + ydif * ydif;
                    //the angle from the center
                    double theta = Math.Atan2(ydif, xdif);
                    //find index for lookup table
                    int index = (int)(Math.Sqrt(Rusquare) / aFocalLinPixels * 1000);
                    if (index >= mFELimit) index = mFELimit - 1;
                    //calculated Rdistorted
                    double Rd = aFocalLinPixels * (double)mFisheyeCorrect[index]
                                          / mScaleFESize;
                    //calculate x and y distances
                    double xdelta = Math.Abs(Rd * Math.Cos(theta));
                    double ydelta = Math.Abs(Rd * Math.Sin(theta));
                    //convert to pixel coordinates
                    int xd = (int)(xc + (xpos ? xdelta : -xdelta));
                    int yd = (int)(yc + (ypos ? ydelta : -ydelta));
                    xd = Math.Max(0, Math.Min(xd, aImage.Width - 1));
                    yd = Math.Max(0, Math.Min(yd, aImage.Height - 1));
                    //set the corrected pixel value from the distorted image
                    correctedImage.SetPixel(i, j, aImage.GetPixel(xd, yd));
                }
            }
            return correctedImage;
        }
    }


    /*
     * References
     * 
     * Saving user settings (preferences)
     * http://stackoverflow.com/questions/3784477/c-sharp-approach-for-saving-user-settings-in-a-wpf-application
     * 
     * Charting/plotting information
     * http://zone.ni.com/reference/en-XX/help/372636F-01/mstudiowebhelp/html/wpfgraphplotting/
     * 
     * 
     */
}