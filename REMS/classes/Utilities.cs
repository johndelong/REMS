using System;
using System.Collections.Generic;
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
        public static readonly string StatusInitial         = "Click and drag to determine scan area";
        public static readonly string StatusReady           = "Ready to start scanning";
        public static readonly string StatusStopped         = "Scanning paused";
        public static readonly string StatusScanning        = "Scanning...";
        public static readonly string StatusDone            = "Scan has finished";
        public static readonly string StatusCalibration1    = "Accurately click OPPOSITE corners of servo travel area";
        public static readonly string StatusCalibration2    = "Click 'Accept' if satisfied with calibration";

        public enum state : int
        {
            Initial,
            Ready,
            Calibration,
            Stopped,
            Scanning,
            Done
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
        public static ImageSource cropImage(Image aImage, Point aMouseDown, Point aMouseUp)
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
        public static void writeToFile(string aFileName, int x, int y, int z, int[] data)
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
        public static string getLineFromFile(string aFileName, int aLine)
        {
            string lResult;
            using (StreamReader srLog = new StreamReader(aFileName))
            {
                for (int i = 1; i < aLine; i++)
                    srLog.ReadLine();

                lResult = srLog.ReadLine();
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

            Console.WriteLine("Here it comes!");
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

    public static class ExternalData
    {
        public static ObservableCollection<ThresholdViewModel> GetThresholds()
        {
            ObservableCollection<ThresholdViewModel> lThresholds = new ObservableCollection<ThresholdViewModel>();
           
            using (StreamReader sr = new StreamReader("Thresholds.csv"))
            {
                string line;

                while ((line = sr.ReadLine()) != null)
                {
                    Console.WriteLine(line);
                    string[] lPairs = line.Split(',');
                    ThresholdViewModel lThreshold = new ThresholdViewModel();
                    lThreshold.Name = lPairs[0];

                    for (int i = 1; i < lPairs.Length; i++)
                    {
                        string[] lTemp = lPairs[i].Split('|');
                        lThreshold.Limits.Add(new ThresholdLimitViewModel(lTemp[0], lTemp[1]));
                    }

                    lThresholds.Add(lThreshold);
                }
            }

            return lThresholds;
        }

        public static void SaveThresholds(ObservableCollection<ThresholdViewModel> aThresholds)
        {
            using (StreamWriter sw = new StreamWriter("Thresholds.csv"))
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
        public static void Rectangle(Canvas aDrawingCanvas, double aX, double aY, double aWidth, double aHeight, Brush aColor, Boolean aDashedLine = false)
        {
            Shape Rendershape = new Rectangle();
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

        /// <summary>
        /// Draws a circle at a specific location
        /// </summary>
        /// <param name="aLocation"></param>
        public static void Circle(Canvas aDrawingCanvas, Point aLocation)
        {
            Shape Rendershape = new Ellipse() { Height = 20, Width = 20 };
            Rendershape.Fill = Brushes.Blue;
            Canvas.SetLeft(Rendershape, aLocation.X - 10);
            Canvas.SetTop(Rendershape, aLocation.Y - 10);
            aDrawingCanvas.Children.Add(Rendershape);
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