using System;
using System.Collections;
using System.Threading;
using System.Collections.Generic;
//using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows;
using System.Drawing.Imaging;
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
            Overview,
            ProbeChange
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
        public static Point[] determineOpositeCorners(Point aPoint1, Point aPoint2)
        {
            Point topLeft = new Point();
            Point bottomRight = new Point();
            Point[] corners = new Point[2];

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

        public static void analyzeScannedData(Point[] aCollectedData, ObservableCollection<ThresholdViewModel> aThresholds,
            ThresholdViewModel aSelectedThreshold, double aDistance, out Boolean aPassed, out double aReturnValue)
        {
            aReturnValue = 0;
            aPassed = true;
            double lValue = 0;

            foreach (ThresholdViewModel lThreshold in aThresholds)
            {
                ThresholdViewModel lDerivedThreshold = Utilities.getDerivedThreshold(lThreshold, aDistance);
                Nullable<double> lMinDifference = null;

                if (lDerivedThreshold != null)
                {
                    // Loop through all of the amplitudes of the collected data
                    foreach (Point lPoint in aCollectedData) // number of data points collected
                    {
                        ThresholdLimitViewModel prevLimit = null;

                        // Loop through all of the limits of the threshold
                        foreach (ThresholdLimitViewModel lLimit in lDerivedThreshold.Limits) // limits within this threshold
                        {
                            if (prevLimit == null)
                                prevLimit = lLimit;

                            if (lPoint.X > Convert.ToDouble(lLimit.Frequency))
                            {
                                prevLimit = lLimit;
                                continue;
                            }

                            // If we have gotten this far, we know we are comparing 
                            //the data to the correct threshold
                            if (Convert.ToDouble(prevLimit.Amplitude) + (lPoint.Y * -1) < lMinDifference || lMinDifference == null)
                            {
                                lMinDifference = Convert.ToDouble(prevLimit.Amplitude) + (lPoint.Y * -1);
                                lValue = (lMinDifference.Value * -1);
                            }

                            if (lPoint.Y > Convert.ToInt32(prevLimit.Amplitude))
                            {
                                lThreshold.State = "Failed";
                                break;
                            }
                        } // end of limits loop

                        if (lThreshold.State != "Failed")
                        {
                            lThreshold.State = "Passed";
                        }
                        else
                        {
                            aPassed = false;
                        }

                        // Only care to return the distance from the selected threashold
                        if (lThreshold == aSelectedThreshold)
                        {
                            aReturnValue = lValue;
                        }
                    }
                }
            }
        }

        public static ThresholdViewModel getDerivedThreshold(ThresholdViewModel aThreshold, double aDistance)
        {
            ThresholdViewModel lNewThreshold = new ThresholdViewModel();
            double lAmplitude;
            foreach (ThresholdLimitViewModel lLimit in aThreshold.Limits)
            {
                lAmplitude = lLimit.Amplitude + 20 * Math.Log10(3 / (aDistance / 1000));
                lNewThreshold.Limits.Add(new ThresholdLimitViewModel(lLimit.Frequency, lAmplitude)); 
            }

            return lNewThreshold;
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

    public static class ImagingMinipulation
    {
        public static ImageSource openImageSource(string aFileName)
        {
            BitmapImage lImage = new BitmapImage(new Uri(aFileName));
            return ConvertBitmapTo96DPI(lImage);
        }

        public static ImageSource openBitmap(System.Drawing.Bitmap aBitmap)
        {
            using (MemoryStream memory = new MemoryStream())
            {
                aBitmap.Save(memory, ImageFormat.Png);
                memory.Position = 0;
                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();

                return ConvertBitmapTo96DPI(bitmapImage);
            }
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
        public static ImageSource cropImage(System.Windows.Controls.Image aImage, Point aMouseDown, Point aMouseUp)
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
        public static Point getAspectRatioPosition(Point aLocation, double aFromWidth, double aFromHeight, double aToWidth, double aToHeight)
        {
            // Take current location and convert to actual image location
            Double dblXPos = (aLocation.X * aToWidth) / aFromWidth;
            Double dblYPos = (aLocation.Y * aToHeight) / aFromHeight;

            return new Point(dblXPos, dblYPos);
        }

        public static void SaveToJPG(FrameworkElement visual, string fileName)
        {
            var encoder = new JpegBitmapEncoder();
            visual.UpdateLayout();
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
        public static void openReport(string aFileName, HeatMap aHeatMap, Grid ColorKey, out ObservableCollection<ScanLevel> aScanLevels)
        {
            string[] lLine = null;
            aScanLevels = new ObservableCollection<ScanLevel>();
            int lRows = 0;
            int lCols = 0;
            Boolean lFirstLine = true;
            try
            {
                using (StreamReader srLog = new StreamReader(aFileName))
                {
                    while (srLog.Peek() >= 0)
                    {
                        lLine = srLog.ReadLine().Split(',');

                        if (lFirstLine)
                        {
                            lCols = Convert.ToInt32(lLine[0]);
                            lRows = Convert.ToInt32(lLine[1]);
                            lFirstLine = false;
                            continue;
                        }

                        if (aScanLevels.Count() > 0)
                        {
                            if (aScanLevels.ElementAt<ScanLevel>(aScanLevels.Count() - 1).ZPos != Convert.ToInt32(lLine[2]))
                                aScanLevels.Add(new ScanLevel(Convert.ToInt32(lLine[2]), "Complete"));
                        }
                        else
                        {
                            aScanLevels.Add(new ScanLevel(Convert.ToInt32(lLine[2]), "Complete"));
                        }
                    }
                }
                aHeatMap.Clear(ColorKey);
                aHeatMap.Create(lCols, lRows, ColorKey);
                //aScanLevels = lScanLevels;
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
        /// 

        public static void initializeFile(string aFileName, int aCols, int aRows, int aXYPlanes)
        {
            try
            {
                System.IO.File.WriteAllText(aFileName, string.Empty);

                //To append to file, set second argument of StreamWriter to true
                using (StreamWriter swLog = new StreamWriter(aFileName, true))
                {
                    swLog.Write(aCols.ToString() + ",");
                    swLog.Write(aRows.ToString() + ",");
                    swLog.Write(aXYPlanes.ToString() + ",");
                    swLog.Write("\r\n");
                }
            }
            catch (IOException)
            {
            }
        }

        public static void writeToFile(string aFileName, int x, int y, int z, int motorX, int motorY, Point[] data)
        {
            Boolean lFileOpened = false;

            while (!lFileOpened)
            {
                try
                {
                    //To append to file, set second argument of StreamWriter to true
                    using (StreamWriter swLog = new StreamWriter(aFileName, true))
                    {
                        lFileOpened = true;
                        swLog.Write(x.ToString() + ",");
                        swLog.Write(y.ToString() + ",");
                        swLog.Write(z.ToString() + ",");

                        for (int i = 0; i < data.Length; i++)
                        {
                            swLog.Write(data[i].X.ToString() + "|" + data[i].Y.ToString());

                            if (i + 1 < data.Length)
                                swLog.Write(",");
                        }

                        swLog.Write("\r\n");
                    }
                }
                catch (IOException)
                {
                    // Wait before trying again
                    Thread.Sleep(100);
                }
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
            Boolean lFileOpened = false;
            string[] lResult = null;

            while (!lFileOpened)
            {
                try
                {
                    using (StreamReader srLog = new StreamReader(aFileName))
                    {
                        lFileOpened = true;
                        while (srLog.Peek() >= 0)
                        {
                            lResult = srLog.ReadLine().Split(',');
                            if (lResult[0] == aRow && lResult[1] == aCol && lResult[2] == aZPos)
                                break;
                            else
                                lResult = null;
                        }
                    }
                    
                }
                catch (IOException)
                {
                    // Wait before trying again
                    Thread.Sleep(100);
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
        }
    }

    /*public class Motor
    {
        private Boolean _connected = false;
        private Boolean _wasHomed = false;
        private int _driverStatus = 0;
        public ClsVxmDriver Vxm;

        public Motor()
        {
            Vxm = new ClsVxmDriver();
        }

        public Boolean connected
        {
            get { return _connected; }
            set { _connected = value; }
        }

        public Boolean wasHomed
        {
            get { return _wasHomed; }
            set { _wasHomed = value; }
        }

        public void connect()
        {
            int lStatus = 0;

            if (_driverStatus == 0)
            {
                _driverStatus = Vxm.LoadDriver("VxmDriver.dll");
                Console.WriteLine("Loading Motor Driver: " + (_driverStatus != 0 ? "Success" : "Failed"));
            }

            if (Vxm.PortIsOpen() == 1)
            {
                Vxm.PortClear();
                Vxm.PortClose();
            }

            lStatus = Vxm.PortOpen(Properties.Settings.Default.MotorCommPort, 9600);
            Console.WriteLine("Connecting to Motors: " + (lStatus == 1 ? "Success" : "Failed"));

            if (lStatus == 1)
            {
                _connected = true;
            }
            else
            {
                _connected = false;
            }

            //Console.WriteLine("Speed of motors set to 4000 steps per second (20mm/s): " + (lStatus == 1 ? "Success" : "Failed"));
        }

        public void disconnect()
        {
            _connected = false;

            // Close port and release driver
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
            int lStatus = 0;
            lStatus = Vxm.PortSendCommands("*");
            
            //Console.WriteLine(Vxm.PortWaitForCharWithMotorPosition());
            Vxm.
            Console.WriteLine("Motor 2 Pos: " + Vxm.PortReadReply());
            //
            //Vxm.PortWaitForChar("^", 0);
            Vxm.PortSendCommands("Y");
            //System.Threading.Thread.Sleep(5000);
            //Vxm.PortWaitForChar("", 0);
            string lReply = Vxm.PortReadReply();
            Console.WriteLine("Reply: " + lReply);
        }

        public void move(int aXPos, int aYPos, int aZPos)
        {
            // DEBUGGING ONLY!
            //_connected = true;

            if (_connected)
            {
                // if the motors have never been homed, do that now!
                if (!_wasHomed)
                    this.home();

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
                _wasHomed = false;

                int lStatus = 0;
                //lStatus = Vxm.PortSendCommands("F,C,S1M4000,S2M4000,R");
                lStatus = Vxm.PortSendCommands("F,C,I2M-0,R");
                Console.WriteLine("Homing Z: " + (lStatus == 1 ? "Success" : "Failed"));
                lStatus = Vxm.PortWaitForChar("^", 0);

                lStatus = Vxm.PortSendCommands("F,C,(I3M-0,I1M-0,)R");
                Console.WriteLine("Homing X & Y: " + (lStatus == 1 ? "Success" : "Failed"));
                lStatus = Vxm.PortWaitForChar("^", 0);

                lStatus = Vxm.PortSendCommands("N");
                Console.WriteLine("Update Home Position: " + (lStatus == 1 ? "Success" : "Failed"));
                lStatus = Vxm.PortWaitForChar("^", 0);

                _wasHomed = true;
            }
        }
    }*/

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
        public static void Circle(Canvas aDrawingCanvas, Point aLocation)
        {
            Shape Rendershape = new Ellipse() { Height = 10, Width = 10 };
            Rendershape.Fill = System.Windows.Media.Brushes.Blue;
            Canvas.SetLeft(Rendershape, aLocation.X - 5);
            Canvas.SetTop(Rendershape, aLocation.Y - 5);
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

        public System.Drawing.Bitmap RemoveFisheye(ref System.Drawing.Bitmap aImage, double aFocalLinPixels)
        {
            System.Drawing.Bitmap correctedImage = new System.Drawing.Bitmap(aImage.Width, aImage.Height);
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
                    double Rd = aFocalLinPixels * (double)mFisheyeCorrect[index] / mScaleFESize;
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