using System;
using System.IO;
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
using NationalInstruments;
using REMS.classes;
using System.Windows.Threading;
using NationalInstruments.Controls;

namespace REMS
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private enum state : int
        {
            Initial,
            Ready,
            Calibration,
            Stopped,
            Scanning,
            Done
        }

        private enum direction : int
        {
            North,
            South,
            East,
            West
        }

        private Point[] imageCalibrationPoints = new Point[2]; // where actually on the image is the calibration points
        private Point[] canvasCalibrationPoints = new Point[2]; // which calibration points have been collected
        private Point[] canvasScanArea = new Point[2];
        private Point[] motorScanAreaPoints = new Point[2]; // the bounds for the motor to travel

        private int motorXTravelDistance = 300;
        private int motorYTravelDistance = 300;
        private int motorZTravelDistance = 100;
        

        private DispatcherTimer nextPositionTimer;

        private double canvasXStepSize, canvasYStepSize;

        private int numOfCalibrationPoints = 0;

        private Boolean imageLoaded = false;
        public Boolean scanAreaSet = false;
        private state currentState = state.Initial;
        private direction currentScanDirection = direction.South;
        private ImageSource loadedImage;

        CountdownTimer CDTimer;

        bool mouseDown = false; // Set to 'true' when mouse is held down.
        Point canvasMouseDownPos; // The point where the mouse button was clicked down on the image.
        Point canvasMouseUpPos;
        int motorXPos, motorYPos, motorZPos;

        AnalogWaveform<double> analogWaveform = new AnalogWaveform<double>(0);
        string mFileName;

        public MainWindow()
        {
            InitializeComponent();
            analyzeGrid.ItemsSource = LoadCollectionData();
            //setupHeatMap(50, 50);

            //lblStatus.Text = status.Initial;

            // Load user/application settings
            imageCalibrationPoints[0] = (Point)Properties.Settings.Default["TableBL"];
            imageCalibrationPoints[1] = (Point)Properties.Settings.Default["TableTR"];


            
        }

        // TODO: Show current x,y,z location on main panel
        // TODO: Implement selection area
        // TODO: Add "Take Picture" button somewhere

        // select image to set scan area and zoom in on picture
        //http://www.codeproject.com/Articles/148503/Simple-Drag-Selection-in-WPF

        private List<AnalyzeSynopsisData> LoadCollectionData()
        {

            List<AnalyzeSynopsisData> authors = new List<AnalyzeSynopsisData>();

            authors.Add(new AnalyzeSynopsisData()
            {
                threshold = "Test 1",
                state = "passed"
            });

            authors.Add(new AnalyzeSynopsisData()
            {
                threshold = "Test 2",
                state = "failed"
            });

            authors.Add(new AnalyzeSynopsisData()
            {
                threshold = "Test 3",
                state = "passed"
            });


            return authors;
        }

        // saving user settings (preferences)
        // http://stackoverflow.com/questions/3784477/c-sharp-approach-for-saving-user-settings-in-a-wpf-application


        // charting/plotting information
        // http://zone.ni.com/reference/en-XX/help/372636F-01/mstudiowebhelp/html/wpfgraphplotting/
        private void button1_Click(object sender, RoutedEventArgs e)
        {
            Random random = new Random();

            int numberOfPoints = 100;
            AnalogWaveform<double> analogWaveform = new AnalogWaveform<double>(numberOfPoints);

            for (int i = 0; i < numberOfPoints; i++)
            {
                analogWaveform.Samples[i].Value = random.NextDouble();
            }

            graph1.DataSource = analogWaveform;

            // generate intensity graph
            // http://zone.ni.com/reference/en-XX/help/372636F-01/mstudiowebhelp/html/wpfintensitygraphplotting/


            //intensityGraph1.DataSource = GetData(10);


            colorHeatMap(50, 50);
        }

        //http://www.c-sharpcorner.com/uploadfile/mahesh/grid-in-wpf/
        private void setupHeatMap(double x, double y)
        {
            RowDefinition rowDef;
            ColumnDefinition colDef;

            for (int lRow = 0; lRow <= y; lRow++)
            {
                rowDef = new RowDefinition();
                heat_map.RowDefinitions.Insert(heat_map.RowDefinitions.Count, rowDef);
                //Console.WriteLine(heat_map.RowDefinitions.IndexOf(rowDef).ToString());
            }

            for (int lCol = 0; lCol <= x; lCol++)
            {
                colDef = new ColumnDefinition();
                heat_map.ColumnDefinitions.Insert(heat_map.ColumnDefinitions.Count, colDef);
                //Console.WriteLine(heat_map.ColumnDefinitions.IndexOf(colDef).ToString());
            }

        }

        private void colorHeatMap(int x, int y)
        {
            heat_map.Children.Clear();

            Random random = new Random();
            for (int lRow = 0; lRow < x; lRow++)
            {
                for (int lCol = 0; lCol < y; lCol++)
                {

                    Rectangle pixel = new Rectangle();
                    Color temp = new Color();

                    int sel = random.Next(10) % 3;

                    switch (sel)
                    {
                        case 0:
                            temp = Colors.Red;
                            break;
                        case 1:
                            temp = Colors.Blue;
                            break;
                        case 2:
                            temp = Colors.Yellow;
                            break;
                    }

                    SolidColorBrush pixelFill = new SolidColorBrush(temp);
                    pixel.Fill = pixelFill;
                    pixel.Opacity = 0.8;

                    Grid.SetColumn(pixel, lCol);
                    Grid.SetRow(pixel, lRow);

                    heat_map.Children.Add(pixel);
                    
                }
            }
        }


        
        private void read_csv()
        {
            //var reader = new StreamReader(File.OpenRead(@"C:\Users\delongja\Documents\Visual Studio 2013\Projects\ReadCSV\SampleData.csv"));
            var reader = new StreamReader(File.OpenRead(mFileName));
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

        private void open_image()
        {
            loadedImage = null;
            BitmapImage image = new BitmapImage(new Uri(mFileName));
            loadedImage = ConvertBitmapTo96DPI(image);
            imageCaptured.Source = loadedImage;
            imageLoaded = true;

            canvasDrawing.Visibility = Visibility.Visible;
        }

        private void updateComponentsByState(state aState)
        {
            switch (aState)
            {
                case state.Initial:
                    btnCancel.Content = "Cancel";
                    btnCancel.Background = null;
                    btnCancel.IsEnabled = false;

                    btnAccept.Content = "Accept";
                    btnAccept.Background = null;
                    btnAccept.IsEnabled = false;

                    lblStatus.Text = status.Initial;
                    break;

                case state.Ready:
                    btnCancel.Content = "Cancel";
                    btnCancel.Background = null;
                    btnCancel.IsEnabled = true;

                    btnAccept.Content = "Start";
                    btnAccept.Background = Brushes.Green;
                    btnAccept.IsEnabled = true;

                    lblStatus.Text = status.Ready;
                    break;

                case state.Scanning:
                    btnCancel.Content = "Stop";
                    btnCancel.Background = Brushes.Red;
                    btnCancel.IsEnabled = true;

                    btnAccept.Content = "Start";
                    btnAccept.Background = Brushes.Green;
                    btnAccept.IsEnabled = false;

                    lblStatus.Text = status.Scanning;
                    break;

                case state.Stopped:
                    btnCancel.Content = "Reset";
                    btnCancel.Background = null;
                    btnCancel.IsEnabled = true;

                    btnAccept.Content = "Resume";
                    btnAccept.Background = Brushes.Green;
                    btnAccept.IsEnabled = true;

                    lblStatus.Text = status.Stopped;
                    break;
            }
        }

        /// <summary>
        /// Assumes has top left and bottom right corners
        /// </summary>
        /// <param name="aImage"></param>
        /// <param name="aMouseDown"></param>
        /// <param name="aMouseUp"></param>
        /// <returns></returns>
        public ImageSource cropImage(Image aImage, Point aMouseDown, Point aMouseUp)
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



        /**
         * 
         * 
         * Button Functions
         * 
         * 
         **/

        /// <summary>
        /// Called on "Calibrate" button click
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void click_calibrate(object sender, RoutedEventArgs e)
        {
            currentState = state.Calibration;
            lblStatus.Text = "Click on the bottom left corner of the table";
            numOfCalibrationPoints = 0;
            canvasDrawing.Children.Clear();
        }

        /// <summary>
        /// Called on "Cancel" button click
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void click_cancel(object sender, RoutedEventArgs e)
        {
            if (currentState == state.Ready)
            {
                currentState = state.Initial;
                updateComponentsByState(currentState);
                imageCaptured.Source = loadedImage;
                canvasDrawing.Visibility = Visibility.Visible;
            }
            else if (currentState == state.Scanning)
            {
                currentState = state.Stopped;
                updateComponentsByState(currentState);
                nextPositionTimer.Stop();
                CDTimer.stop();
            }
            else if (currentState == state.Stopped)
            {
                currentState = state.Initial;
                updateComponentsByState(currentState);
                imageCaptured.Source = null;
                heat_map.Children.Clear();
                heat_map.RowDefinitions.Clear();
                heat_map.ColumnDefinitions.Clear();
                canvasDrawing.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// Called on "Accept" button click
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void click_accept(object sender, RoutedEventArgs e)
        {
            // transitionning to READY state
            if (currentState == state.Initial && scanAreaSet)
            {
                currentState = state.Ready;
                updateComponentsByState(currentState);

                canvasDrawing.Visibility = Visibility.Collapsed;
                setScanArea();
            }
            // transitioning to SCANNING state
            else if (currentState == state.Ready)
            {
                currentState = state.Scanning;
                updateComponentsByState(currentState);
                nextPositionTimer = new DispatcherTimer();
                nextPositionTimer.Tick += new EventHandler(moveMotors_Tick);
                nextPositionTimer.Interval = new TimeSpan(0, 0, 1);
                nextPositionTimer.Start();

                Console.WriteLine("Scan Area:: Width " + (motorScanAreaPoints[1].X - motorScanAreaPoints[0].X) +
                                        "Height " + (motorScanAreaPoints[1].Y - motorScanAreaPoints[0].Y));

                Console.WriteLine("Top Left: " + motorScanAreaPoints[0].X + "," + motorScanAreaPoints[0].Y + " Bottom Right " +
                                            motorScanAreaPoints[1].X + "," + motorScanAreaPoints[1].Y);

                double xScanPoints = (motorScanAreaPoints[1].X - motorScanAreaPoints[0].X) / nsXStepSize.Value;
                double yScanPoints = (motorScanAreaPoints[1].Y - motorScanAreaPoints[0].Y) / nsYStepSize.Value;

                setupHeatMap(xScanPoints, yScanPoints);

                CDTimer.start();
            }
            else if (currentState == state.Stopped)
            {
                currentState = state.Scanning;
                updateComponentsByState(currentState);
                nextPositionTimer.Start();
                CDTimer.start();
            }
            // transitioning to INITIAL state
            else if (currentState == state.Calibration)
            {
                currentState = state.Initial;
                updateComponentsByState(currentState);

                // Delete everything in the selectorCanvas
                canvasDrawing.Children.Clear();

                // Draw the table area
                DrawAreaRectangle(canvasDrawing, canvasCalibrationPoints[0].X, canvasCalibrationPoints[0].Y, canvasCalibrationPoints[1].X - canvasCalibrationPoints[0].X, canvasCalibrationPoints[1].Y - canvasCalibrationPoints[0].Y, Brushes.Blue);
            }
        }

        /// <summary>
        /// Called on "Open" menu item click
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void click_open(object sender, RoutedEventArgs e)
        {
            // Create OpenFileDialog 
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();

            // Set filter for file extension and default file extension 
            //dlg.DefaultExt = ".csv";
            //dlg.Filter = "CSV Files (*.csv)|*.csv|JPEG Files (*.jpeg)|*.jpeg|PNG Files (*.png)|*.png|JPG Files (*.jpg)|*.jpg|GIF Files (*.gif)|*.gif";

            // Display OpenFileDialog by calling ShowDialog method 
            Nullable<bool> result = dlg.ShowDialog();

            // Get the selected file name and display in a TextBox 
            if (result == true)
            {
                // Open document 
                mFileName = dlg.FileName;
                //textBox1.Text = mFilename;

                String[] tokens = mFileName.Split('.');

                if (tokens[1] == "csv")
                {
                    read_csv();
                }

                if (tokens[1] == "jpg" || tokens[1] == "png" || tokens[1] == "jpeg")
                {
                    open_image();
                }
            }
        }



        /**
         * 
         * 
         * GUI functions
         * 
         * 
         **/

        // test

        /// <summary>
        /// Called when the mouse button is pressed over the Results Tab grid
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Grid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            canvasMouseDownPos = e.GetPosition(canvasDrawing);

            //Console.WriteLine("X: " + canvasMouseDownPos.X + " Y: " + canvasMouseDownPos.Y);

            if (imageLoaded && currentState == state.Initial)
            {
                // Only draw the scan area if we are within the table
                // calibration area
                if (canvasMouseDownPos.X >= canvasCalibrationPoints[0].X &&
                    canvasMouseDownPos.Y >= canvasCalibrationPoints[0].Y &&
                    canvasMouseDownPos.X <= canvasCalibrationPoints[1].X &&
                    canvasMouseDownPos.Y <= canvasCalibrationPoints[1].Y)
                {
                    mouseDown = true;
                    //DrawScanArea();

                    // The area selection will always be in position 1
                    if (canvasDrawing.Children.Count > 1)
                    {
                        canvasDrawing.Children.RemoveAt(1);
                    }

                    DrawAreaRectangle(canvasDrawing, 0, 0, 0, 0, Brushes.Red, true);
                }
            }
            else if (currentState == state.Calibration)
            {
                numOfCalibrationPoints++;

                if (numOfCalibrationPoints <= 2)
                {
                    DrawCircle(canvasDrawing, e.GetPosition(canvasDrawing));
                }

                if (numOfCalibrationPoints == 1)
                {
                    canvasCalibrationPoints[0] = e.GetPosition(imageCaptured);

                    lblStatus.Text = "Now click on the top right corner of the table";

                    btnCancel.IsEnabled = true;
                    btnAccept.IsEnabled = false;
                }
                else if (numOfCalibrationPoints == 2)
                {
                    canvasCalibrationPoints[1] = e.GetPosition(imageCaptured);

                    // Make sure that we always have the bottom left and top right corners
                    canvasCalibrationPoints = determineOpositeCorners(canvasCalibrationPoints[0], canvasCalibrationPoints[1]);

                    imageCalibrationPoints[0] = getAspectRatioPosition(canvasCalibrationPoints[0], imageCaptured.ActualWidth, imageCaptured.ActualHeight,
                        imageCaptured.Source.Width, imageCaptured.Source.Height);

                    imageCalibrationPoints[1] = getAspectRatioPosition(canvasCalibrationPoints[1], imageCaptured.ActualWidth, imageCaptured.ActualHeight,
                        imageCaptured.Source.Width, imageCaptured.Source.Height);

                    lblStatus.Text = "Click 'Accept' if rectangle matches table";
                    btnAccept.IsEnabled = true;
                }
            }
        }

        /// <summary>
        /// Called when the mouse button is released on the Results Tab grid
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Grid_MouseUp(object sender, MouseButtonEventArgs e)
        {
            canvasMouseUpPos = e.GetPosition(imageCaptured);

            if (imageLoaded && currentState == state.Initial)
            {
                mouseDown = false;

                if (canvasMouseDownPos.X != canvasMouseUpPos.X && canvasMouseDownPos.Y != canvasMouseUpPos.Y)
                {
                    scanAreaSet = true;
                    btnAccept.IsEnabled = true;
                }
                else
                {
                    scanAreaSet = false;
                    btnAccept.IsEnabled = false;
                }

            }

        }

        /// <summary>
        /// Called when the mouse is moved over the Results Tab grid
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Grid_MouseMove(object sender, MouseEventArgs e)
        {
            if (mouseDown)
            {
                // When the mouse is held down, reposition the drag selection box.
                Point mousePos = e.GetPosition(canvasDrawing);

                Rectangle selectionBox = (Rectangle)canvasDrawing.Children[1];

                // Restrict movement to image of PCB
                if (mousePos.X < canvasCalibrationPoints[0].X) mousePos.X = canvasCalibrationPoints[0].X;
                if (mousePos.X > canvasCalibrationPoints[1].X) mousePos.X = canvasCalibrationPoints[1].X;
                if (mousePos.Y < canvasCalibrationPoints[0].Y) mousePos.Y = canvasCalibrationPoints[0].Y;
                if (mousePos.Y > canvasCalibrationPoints[1].Y) mousePos.Y = canvasCalibrationPoints[1].Y;

                // Position box
                if (canvasMouseDownPos.X < mousePos.X)
                {
                    Canvas.SetLeft(canvasDrawing.Children[1], canvasMouseDownPos.X);
                    selectionBox.Width = mousePos.X - canvasMouseDownPos.X;
                }
                else
                {
                    Canvas.SetLeft(selectionBox, mousePos.X);
                    selectionBox.Width = canvasMouseDownPos.X - mousePos.X;
                }

                if (canvasMouseDownPos.Y < mousePos.Y)
                {
                    Canvas.SetTop(selectionBox, canvasMouseDownPos.Y);
                    selectionBox.Height = mousePos.Y - canvasMouseDownPos.Y;
                }
                else
                {
                    Canvas.SetTop(selectionBox, mousePos.Y);
                    selectionBox.Height = canvasMouseDownPos.Y - mousePos.Y;
                }
            }
        }

        /// <summary>
        /// Resizes selection items (i.e. table calibration/scan area) when the window resizes
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void redrawSelectionObjects(object sender, SizeChangedEventArgs e)
        {
            // Only resize stuff if an image has been loaded
            if (imageCaptured.Source != null)
            {
                // Convert from image location to canvas location
                canvasCalibrationPoints[0] = getAspectRatioPosition(imageCalibrationPoints[0], 
                        imageCaptured.Source.Width, imageCaptured.Source.Height, imageCaptured.ActualWidth, imageCaptured.ActualHeight);

                canvasCalibrationPoints[1] = getAspectRatioPosition(imageCalibrationPoints[1], 
                        imageCaptured.Source.Width, imageCaptured.Source.Height, imageCaptured.ActualWidth, imageCaptured.ActualHeight);

                // Delete everything in the selectorCanvas
                canvasDrawing.Children.Clear();

                // Draw the table area
                DrawAreaRectangle(canvasDrawing, canvasCalibrationPoints[0].X, canvasCalibrationPoints[0].Y, canvasCalibrationPoints[1].X - canvasCalibrationPoints[0].X, canvasCalibrationPoints[1].Y - canvasCalibrationPoints[0].Y, Brushes.Blue);
            }
        }

        /// <summary>
        /// Sets the area for where the probe will need to scan
        /// </summary>
        private void setScanArea()
        {
            // Crop down the image to the selected region
            canvasScanArea = determineOpositeCorners(canvasMouseDownPos, canvasMouseUpPos);

            // Zoom in on the scan area
            imageCaptured.Source = cropImage(imageCaptured, canvasScanArea[0], canvasScanArea[1]);

            canvasXStepSize = (canvasCalibrationPoints[1].X - canvasCalibrationPoints[0].X) / motorXTravelDistance;
            canvasYStepSize = (canvasCalibrationPoints[1].Y - canvasCalibrationPoints[0].Y) / motorYTravelDistance;

            // Convert the bounds to an actual motor positions
            motorScanAreaPoints[0].X = Math.Round((canvasScanArea[0].X - canvasCalibrationPoints[0].X) / canvasXStepSize, 0, MidpointRounding.ToEven);
            motorScanAreaPoints[0].Y = Math.Round((canvasScanArea[0].Y - canvasCalibrationPoints[0].Y) / canvasYStepSize, 0, MidpointRounding.ToEven);
            motorScanAreaPoints[1].X = Math.Round((canvasScanArea[1].X - canvasCalibrationPoints[0].X) / canvasXStepSize, 0, MidpointRounding.ToEven);
            motorScanAreaPoints[1].Y = Math.Round((canvasScanArea[1].Y - canvasCalibrationPoints[0].Y) / canvasYStepSize, 0, MidpointRounding.ToEven);

            // Give the motors a starting position
            motorXPos = Convert.ToInt32(motorScanAreaPoints[0].X);
            motorYPos = Convert.ToInt32(motorScanAreaPoints[0].Y);

            // Calculate how long it will take
            double xScanPoints = (motorScanAreaPoints[1].X - motorScanAreaPoints[0].X) / nsXStepSize.Value;
            double yScanPoints = (motorScanAreaPoints[1].Y - motorScanAreaPoints[0].Y) / nsYStepSize.Value;
            int scanPoints = Convert.ToInt32( xScanPoints * yScanPoints );
            CDTimer = new CountdownTimer(lblCDTimer, scanPoints);

            lblXPosition.Text = motorXPos.ToString();
            lblYPosition.Text = motorYPos.ToString();
        }


        /// <summary>
        /// Saves all of the application settings for furture use when application closes
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void onClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Properties.Settings.Default["TableBL"] = imageCalibrationPoints[0];
            Properties.Settings.Default["TableTR"] = imageCalibrationPoints[1];

            Properties.Settings.Default.Save();
        }


        /**
         * 
         * 
         * Helper Functions
         * 
         * 
         **/

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
        /// Draws a rectange with a boarder and clear background 
        /// </summary>
        /// <param name="aDrawingCanvas"></param>
        /// <param name="aX"></param>
        /// <param name="aY"></param>
        /// <param name="aWidth"></param>
        /// <param name="aHeight"></param>
        /// <param name="aColor"></param>
        /// <param name="aDashedLine"></param>
        private void DrawAreaRectangle(Canvas aDrawingCanvas, double aX, double aY, double aWidth, double aHeight, Brush aColor, Boolean aDashedLine = false)
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
        private void DrawCircle(Canvas aDrawingCanvas, Point aLocation)
        {
            Shape Rendershape = new Ellipse() { Height = 20, Width = 20 };
            Rendershape.Fill = Brushes.Blue;
            Canvas.SetLeft(Rendershape, aLocation.X - 10);
            Canvas.SetTop(Rendershape, aLocation.Y - 10);
            aDrawingCanvas.Children.Add(Rendershape);
        }

        /// <summary>
        /// From a specific location, determines location on image/canvas using aspect ration
        /// </summary>
        /// <param name="aImage"></param>
        /// <param name="aLocation"></param>
        /// <returns></returns>
        private Point getAspectRatioPosition(Point aLocation, double aFromWidth, double aFromHeight, double aToWidth, double aToHeight)
        {
            // Take current location and convert to actual image location
            Double dblXPos = (aLocation.X * aToWidth) / aFromWidth;
            Double dblYPos = (aLocation.Y * aToHeight) / aFromHeight;

            // Convert coordinates to integers
            //double xPos = Math.Round(dblXPos, 0, MidpointRounding.ToEven);
            //double yPos = Math.Round(dblYPos, 0, MidpointRounding.ToEven);

            return new Point(dblXPos, dblYPos);
        }

        /// <summary>
        /// Given to points, this function returns the top left and bottom right corner
        /// of the rectangle
        /// </summary>
        /// <param name="aPoint1"></param>
        /// <param name="aPoint2"></param>
        /// <returns></returns>
        private Point[] determineOpositeCorners(Point aPoint1, Point aPoint2)
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

        private void moveMotors_Tick(object sender, EventArgs e)
        {
            // Move the motors to the next location
            int nextX = motorXPos;
            int nextY = motorYPos;

            // Update the motor position text
            lblXPosition.Text = nextX.ToString();
            lblYPosition.Text = nextY.ToString();

            Console.WriteLine("Pixel Pos:: X " + (motorXPos - Convert.ToInt32(motorScanAreaPoints[0].X)) +
                                    " Y " + (motorYPos - Convert.ToInt32(motorScanAreaPoints[0].Y)));

            Console.WriteLine("Motor Pos:: X " + motorXPos +
                                    " Y " + motorYPos);

            int row = (motorXPos - (int)motorScanAreaPoints[0].X) / (int)nsXStepSize.Value;
            int col = (motorYPos - (int)motorScanAreaPoints[0].Y) / (int)nsYStepSize.Value;

            // draw heat map pixel
            drawHeatMapPixel(row, col, Colors.Blue);

            // Determine the next scan position
            if(currentScanDirection == direction.South)
            {
                nextY = nextY + nsYStepSize.Value;
                if (nextY > motorScanAreaPoints[1].Y)
                {
                    currentScanDirection = reverseDirection(currentScanDirection);
                    nextY = nextY - nsYStepSize.Value;
                    nextX = nextX + nsXStepSize.Value;
                }
            }
            else if(currentScanDirection == direction.North)
            {
                nextY = nextY - nsYStepSize.Value;
                if (nextY < motorScanAreaPoints[0].Y)
                {
                    currentScanDirection = reverseDirection(currentScanDirection);
                    nextY = nextY + nsYStepSize.Value;
                    nextX = nextX + nsXStepSize.Value;
                }
            }
            
            if (nextX > motorScanAreaPoints[1].X)
                nextPositionTimer.Stop();

            motorXPos = nextX;
            motorYPos = nextY;
        }

        private void drawHeatMapPixel(int aCol, int aRow, Color aColor)
        {
            Rectangle pixel = new Rectangle();

            SolidColorBrush pixelFill = new SolidColorBrush(aColor);
            pixel.Fill = pixelFill;
            pixel.Opacity = 0.8;

            int tempX = motorXPos - Convert.ToInt32(motorScanAreaPoints[0].X);
            int tempY = motorYPos - Convert.ToInt32(motorScanAreaPoints[0].Y);

            Grid.SetColumn(pixel, aCol);
            Grid.SetRow(pixel, aRow);

            heat_map.Children.Add(pixel);
        }

        private direction reverseDirection(direction aDirection)
        {
            if (aDirection == direction.North)
                return direction.South;
            else if (aDirection == direction.South)
                return direction.North;
            else if (aDirection == direction.East)
                return direction.West;
            else
                return direction.East;
        }

        private void nsValidator(object sender, ValueChangedEventArgs<int> e)
        {
            if (e.NewValue < 1)
            {
                NumericTextBoxDouble numericStepper = sender as NumericTextBoxDouble;
                numericStepper.Value = 1;
            }
        }
    }
}
