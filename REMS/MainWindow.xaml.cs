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
using NationalInstruments;
using REMS.classes;
using REMS.popups;
using REMS.data;
using System.Windows.Threading;
using NationalInstruments.Controls;
using System.Collections.ObjectModel;

namespace REMS
{
    //Binding datagrids
    //http://www.codeproject.com/Articles/165368/WPF-MVVM-Quick-Start-Tutorial

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window //maybe should try 'implement' REMSProperties
    {

        // Calibration Variables
        private Point[] imageCalibrationPoints = new Point[2]; // where actually on the image is the calibration points
        private Point[] canvasCalibrationPoints = new Point[2]; // which calibration points have been collected
        private Boolean mCalibrationSet = false;

        private Point[] canvasScanArea = new Point[2];
        private Point[] motorScanAreaPoints = new Point[2]; // the bounds for the motor to travel

        public double motorXTravelDistance;
        public double motorYTravelDistance;
        public double motorZTravelDistance;
        //private int motorZTravelDistance = 100;

        // Timers
        private DispatcherTimer simulatorTimer;
        private CountdownTimer CDTimer;
        
        private double canvasXStepSize, canvasYStepSize;

        //private double heatMapOpacity;
        
        private int numOfCalibrationPoints = 0;

        //private Boolean imageLoaded = false;
        public Boolean scanAreaSet = false;
        private Constants.state currentState = Constants.state.Initial;
        private Constants.state previousState;
        private Constants.direction currentScanDirection;
        
        private ImageSource mLoadedImage;

        bool mouseDown = false; // Set to 'true' when mouse is held down.
        Point canvasMouseDownPos; // The point where the mouse button was clicked down on the image.
        Point canvasMouseUpPos;
        int motorXPos, motorYPos, motorZPos;
        
        private int xScanPoints = 0;
        private int yScanPoints = 0;
        //private int[] zScanPoints;
        //private int numXYPlanes = 0;
        private int totalScanPoints = 0;

        private int currentZScanIndex = 0;
        
        // File Names
        private string mFileName = "";
        private string mLogFileName;
        
        private Boolean mHeatMapPixelClicked = false;

        // Connected Devices
        ClsVxmDriver Vxm = new ClsVxmDriver();

        ObservableCollection<ThresholdViewModel> _thresholds;
        ObservableCollection<ScanLevel> _scans;

        public MainWindow()
        {
            InitializeComponent();

            Vxm.LoadDriver("VxmDriver.dll");

            loadUserPreferences();

            _thresholds = new ObservableCollection<ThresholdViewModel>(ExternalData.GetThresholds());
            _scans = new ObservableCollection<ScanLevel>();

            analyzeGrid.ItemsSource = _thresholds;

            transitionToState(Constants.state.Initial);
        }

        // select image to set scan area and zoom in on picture
        //http://www.codeproject.com/Articles/148503/Simple-Drag-Selection-in-WPF

        private void populateGraph(int x, int y, int rows, int columns)
        {
            int numberOfPoints = 461;
            int lineNum = (x * rows) + y + 1;
                
            string line = Logger.getLineFromFile(mLogFileName, lineNum);
            if (line != null)
            {
                int[] data = new int[numberOfPoints];
                var temp = line.Split(',');
                for (int i = 3; i < temp.Length; i++)
                {
                    data[i - 3] = Convert.ToInt32(temp[i]);
                }
                
                graph1.Data[1] = data;
            }
            else
            {
                MessageBox.Show("No Data at " + x + " " + y);
            }
        }

        private void drawThreshold(ThresholdViewModel aThreshold)
        {
            Point[] lThresholdLine = new Point[aThreshold.Limits.Count * 2 - 1];
            int freq = Convert.ToInt32(aThreshold.Limits[0].Frequency);
            int amp = Convert.ToInt32(aThreshold.Limits[0].Amplitude);
            int pos = 0;
            for (int i = 1; i < aThreshold.Limits.Count * 2; i++)
            {
                if(i % 2 == 0)
                    freq = Convert.ToInt32(aThreshold.Limits[i / 2].Frequency);
                else
                    amp = Convert.ToInt32(aThreshold.Limits[i / 2].Amplitude);

                lThresholdLine[pos] = new Point(freq, amp);
                pos++;
            }
            graph1.Data[0] = lThresholdLine;
        }

        private void transitionToState(Constants.state aState)
        {
            previousState = currentState;
            currentState = aState;

            switch (aState)
            {
                case Constants.state.Initial:
                    // Update Buttons
                    btnCancel.Content = "Cancel";
                    btnCancel.Background = null;
                    btnCancel.IsEnabled = false;

                    btnAccept.Content = "Accept";
                    btnAccept.Background = null;
                    btnAccept.IsEnabled = false;

                    btnCalibrate.IsEnabled = true;

                    // Enable all GUI Controls
                    setGUIControlIsEnabled(true);

                    currentScanDirection = Constants.direction.South;

                    if (previousState == Constants.state.Stopped)
                    {
                        // Remmove captured image and heat map
                        mLoadedImage = null;
                    }

                    mHeatMap.Clear();

                    // Update Status
                    lblStatus.Text = Constants.StatusInitial;

                    // Show captured image
                    imageCaptured.Source = mLoadedImage;

                    // Redraw the table area on the image after calibration
                    redrawSelectionObjects();

                    // Only show the canvas if we have an image loaded
                    if (imageCaptured.Source == null)
                    {
                        canvasDrawing.Visibility = Visibility.Collapsed;
                        btnCalibrate.IsEnabled = false;
                    }
                    else
                    {
                        canvasDrawing.Visibility = Visibility.Visible;
                        btnCalibrate.IsEnabled = true;
                    }
                    break;

                case Constants.state.Calibration:
                    // Update Status
                    lblStatus.Text = Constants.StatusCalibration1;

                    // Reset the number of calibration points clicked
                    numOfCalibrationPoints = 0;

                    // Delete everything in the selectorCanvas
                    canvasDrawing.Children.Clear();

                    if (mCalibrationSet)
                    {
                        // Draw the table area
                        drawMotorTravelArea();
                    }
                    break;

                case Constants.state.Ready:
                    // Update Buttons
                    btnCancel.Content = "Cancel";
                    btnCancel.Background = null;
                    btnCancel.IsEnabled = true;

                    btnAccept.Content = "Start";
                    btnAccept.Background = Brushes.Green;
                    btnAccept.IsEnabled = true;

                    btnCalibrate.IsEnabled = false;

                    // Update Status
                    lblStatus.Text = Constants.StatusReady;

                    // Hide drawing canvas
                    canvasDrawing.Visibility = Visibility.Collapsed;

                    // Set the area of where to scan
                    setScanArea();
                    break;

                case Constants.state.Scanning:
                    // Update Buttons
                    btnCancel.Content = "Stop";
                    btnCancel.Background = Brushes.Red;
                    btnCancel.IsEnabled = true;

                    btnAccept.Content = "Start";
                    btnAccept.Background = Brushes.Green;
                    btnAccept.IsEnabled = false;

                    // Disable all GUI Controls
                    setGUIControlIsEnabled(false);

                    // Update Status
                    lblStatus.Text = Constants.StatusScanning;

                    // Hide drawing canvas
                    canvasDrawing.Visibility = Visibility.Collapsed;
                    
                    // Name the log file
                    mLogFileName = string.Format("Scan_{0:yyyy-MM-dd_hh-mm-ss}.csv", DateTime.Now);

                    // Start the simulator
                    startSimulator();
                    break;

                case Constants.state.Stopped:
                    // Update Buttons
                    btnCancel.Content = "Reset";
                    btnCancel.Background = null;
                    btnCancel.IsEnabled = true;

                    btnAccept.Content = "Resume";
                    btnAccept.Background = Brushes.Green;
                    btnAccept.IsEnabled = true;

                    // Update Status
                    lblStatus.Text = Constants.StatusStopped;

                    // Stop the simulator
                    simulatorTimer.Stop();

                    // Stop the count down timer
                    CDTimer.Stop();

               
                    
                    break;

                case Constants.state.Overview:
                    // Update Buttons
                    btnCancel.Content = "Reset";
                    btnCancel.Background = null;
                    btnCancel.IsEnabled = true;

                    btnAccept.Content = "Start";
                    btnAccept.Background = null;
                    btnAccept.IsEnabled = false;
                    break;
            }
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
            transitionToState(Constants.state.Calibration);
        }

        /// <summary>
        /// Called on "Cancel" button click
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void click_cancel(object sender, RoutedEventArgs e)
        {
            if (currentState == Constants.state.Calibration)
            {
                transitionToState(Constants.state.Initial);
            }
            else if (currentState == Constants.state.Ready)
            {
                transitionToState(Constants.state.Initial);
            }
            else if (currentState == Constants.state.Scanning)
            {
                transitionToState(Constants.state.Stopped);
            }
            else if (currentState == Constants.state.Stopped)
            {
                transitionToState(Constants.state.Initial);
            }
        }

        /// <summary>
        /// Called on "Accept" button click
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void click_accept(object sender, RoutedEventArgs e)
        {
            if (currentState == Constants.state.Initial && scanAreaSet)
            {
                transitionToState(Constants.state.Ready);
            }
            else if (currentState == Constants.state.Ready)
            {
                transitionToState(Constants.state.Scanning);
            }
            else if (currentState == Constants.state.Stopped)
            {
                transitionToState(Constants.state.Scanning);
            }
            else if (currentState == Constants.state.Calibration)
            {
                // Only want to save our calibration values if the user clicks
                // the 'Accept' button in the calibration state
                canvasCalibrationPoints = Utilities.determineOpositeCorners(canvasCalibrationPoints[0], canvasCalibrationPoints[1]);

                imageCalibrationPoints[0] = Imaging.getAspectRatioPosition(canvasCalibrationPoints[0], imageCaptured.ActualWidth, imageCaptured.ActualHeight,
                    imageCaptured.Source.Width, imageCaptured.Source.Height);

                imageCalibrationPoints[1] = Imaging.getAspectRatioPosition(canvasCalibrationPoints[1], imageCaptured.ActualWidth, imageCaptured.ActualHeight,
                    imageCaptured.Source.Width, imageCaptured.Source.Height);

                transitionToState(Constants.state.Initial);
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
                    Logger.read_csv("Test.csv");
                }

                //if (tokens[1] == "jpg" || tokens[1] == "png" || tokens[1] == "jpeg")
                //{
                //    open_image();
                //}
            }
        }

        private void click_capture_image(object sender, RoutedEventArgs e)
        {
            // Create OpenFileDialog 
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();

            // Set filter for file extension and default file extension 
            dlg.DefaultExt = ".jpg";
            dlg.Filter = "JPG Files (*.jpg)|*.jpg|JPEG Files (*.jpeg)|*.jpeg|PNG Files (*.png)|*.png";

            // Display OpenFileDialog by calling ShowDialog method 
            Nullable<bool> result = dlg.ShowDialog();

            // Get the selected file name and display in a TextBox 
            if (result == true)
            {
                // Open document 
                mFileName = dlg.FileName;
                //textBox1.Text = mFilename;

                //String[] tokens = mFileName.Split('.');

                //if (tokens[1] == "jpg" || tokens[1] == "png" || tokens[1] == "jpeg")
                //{


                mLoadedImage = Imaging.openImageSource(mFileName);
                imageCaptured.Source = mLoadedImage;
                //imageLoaded = true;
                canvasDrawing.Visibility = Visibility.Visible;
                btnCalibrate.IsEnabled = true;
                //}
            }
        }

        private void click_preferences(object sender, RoutedEventArgs e)
        {
            PrefPopup popup = new PrefPopup();
            popup.ShowDialog();
            loadUserPreferences();
            
            _thresholds = new ObservableCollection<ThresholdViewModel>(ExternalData.GetThresholds());
            analyzeGrid.ItemsSource = _thresholds;
        }

        private void click_heatMapMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2) // for double-click, remove this condition if only want single click
            {
                Point cell = mHeatMap.getClickedCell(sender, e);
                populateGraph((int)cell.X, (int)cell.Y, (int)xScanPoints, (int)yScanPoints);
                mHeatMapPixelClicked = true;
            }
        }

        private void click_heatMapMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (mHeatMapPixelClicked)
            {
                AnalyzeTab.IsSelected = true;
                mHeatMapPixelClicked = false;
            }
        }

        private void click_saveHeatMapImage(object sender, RoutedEventArgs e)
        {
            RenderTargetBitmap rtb = new RenderTargetBitmap((int)gridResultsTab.RenderSize.Width,
                                                            (int)gridResultsTab.RenderSize.Height, 
                                                            96d, 96d, System.Windows.Media.PixelFormats.Default);
            rtb.Render(gridResultsTab);

            //var crop = new CroppedBitmap(rtb, new Int32Rect(50, 50, 250, 250));

            BitmapEncoder pngEncoder = new PngBitmapEncoder();
            pngEncoder.Frames.Add(BitmapFrame.Create(rtb));

            using (var fs = System.IO.File.OpenWrite("logo.png"))
            {
                pngEncoder.Save(fs);
            }
        }

        private void motorJogUp_Click(object sender, RoutedEventArgs e)
        {
            Vxm.DriverTerminalShowState(1, 0);
            Vxm.PortOpen(1, 9600);
            Vxm.PortSendCommands("F,C,I1M1000,I1M-1000,R");
            Vxm.PortClose();
            Vxm.DriverTerminalShowState(0, 0);
        }

        private void motorJogDown_Click(object sender, RoutedEventArgs e)
        {
            Vxm.DriverTerminalShowState(1, 0);
            Vxm.PortOpen(1, 9600);
            Vxm.PortSendCommands("F,C,I1M1000,I1M-1000,R");
            Vxm.PortWaitForChar("^", 0);
            Vxm.PortClose();
            Vxm.DriverTerminalShowState(0, 0);
        }

        private void motorJogLeft_Click(object sender, RoutedEventArgs e)
        {

        }

        private void motorJogRight_Click(object sender, RoutedEventArgs e)
        {

        }

        private void dataGridThreshold_MouseUp(object sender, MouseButtonEventArgs e)
        {
            drawThreshold(((ThresholdViewModel)analyzeGrid.SelectedItem));
        }

        private void dataGridScanLevel_MouseUp(object sender, MouseButtonEventArgs e)
        {

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
            if(imageCaptured.Source != null)
            {
                if (currentState == Constants.state.Initial)
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

                        Draw.Rectangle(canvasDrawing, 0, 0, 0, 0, Brushes.Red, true);
                    }
                }
                else if (currentState == Constants.state.Calibration)
                {
                    numOfCalibrationPoints++;

                    if (numOfCalibrationPoints <= 2)
                    {
                        Draw.Circle(canvasDrawing, e.GetPosition(canvasDrawing));
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

                        lblStatus.Text = "Click 'Accept' if rectangle matches table";
                        btnAccept.IsEnabled = true;
                    }
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
            // Only do stuff if the user clicked down in the
            // canvas area first.
            if (mouseDown)
            {
                // Get the mouse up location
                canvasMouseUpPos = e.GetPosition(imageCaptured);

                // Get the selection area
                if (imageCaptured.Source != null && currentState == Constants.state.Initial)
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
        private void redrawSelectionObjects()
        {
            // Only resize stuff if an image has been loaded
            if (imageCaptured.Source != null)
            {
                // Convert from image location to canvas location
                canvasCalibrationPoints[0] = Imaging.getAspectRatioPosition(imageCalibrationPoints[0],
                        imageCaptured.Source.Width, imageCaptured.Source.Height, imageCaptured.ActualWidth, imageCaptured.ActualHeight);

                canvasCalibrationPoints[1] = Imaging.getAspectRatioPosition(imageCalibrationPoints[1],
                        imageCaptured.Source.Width, imageCaptured.Source.Height, imageCaptured.ActualWidth, imageCaptured.ActualHeight);

                // Delete everything in the selectorCanvas
                canvasDrawing.Children.Clear();

                // Draw the table area
                drawMotorTravelArea();
            }
        }

        private void imageResized(object sender, SizeChangedEventArgs e)
        {
            redrawSelectionObjects();
        }

        /// <summary>
        /// Sets the area for where the probe will need to scan
        /// </summary>
        private void setScanArea()
        {
            // Crop down the image to the selected region
            canvasScanArea = Utilities.determineOpositeCorners(canvasMouseDownPos, canvasMouseUpPos);

            // Zoom in on the scan area
            imageCaptured.Source = Imaging.cropImage(imageCaptured, canvasScanArea[0], canvasScanArea[1]);

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
            motorZPos = nsZMin.Value;

            // Update the scan area width and length
            xScanPoints = (int)(motorScanAreaPoints[1].X - motorScanAreaPoints[0].X) / nsXStepSize.Value;
            yScanPoints = (int)(motorScanAreaPoints[1].Y - motorScanAreaPoints[0].Y) / nsYStepSize.Value;

            // Calculate how long it will take
            int numXYPlanes = (int)Math.Ceiling((double)(nsZMax.Value - nsZMin.Value) / (double)nsZStepSize.Value) + 1;
            //zScanPoints = new int[numXYPlanes];

            _scans.Clear();
            for (int i = 0; i < numXYPlanes; i++)
            {
                int temp = (int)(nsZMin.Value + (nsZStepSize.Value * i));
                if(temp > (int)nsZMax.Value) temp = (int)nsZMax.Value;
                ScanLevel lScan = new ScanLevel(temp);
                _scans.Add(lScan);
                //zScanPoints[i] = temp;
            }
            dgZScanPoints.ItemsSource = _scans;

            totalScanPoints = Convert.ToInt32(xScanPoints * yScanPoints * numXYPlanes);

            CDTimer = new CountdownTimer(lblCDTimer, totalScanPoints);

            // Initialize the heatmap
            mHeatMap.Create((int)yScanPoints, (int)xScanPoints);

            lblXPosition.Text = motorXPos.ToString();
            lblYPosition.Text = motorYPos.ToString();
            lblZPosition.Text = motorZPos.ToString();
        }


        /// <summary>
        /// Saves all of the application settings for furture use when application closes
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void onClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //srLog.Close(); // close stream to log file
            
            // Release Connected Devices
            Vxm.ReleaseDriver();

            // Save User Preferences
            saveUserPreferences();

            
        }

       


        /**
         * 
         * 
         * Helper Functions
         * 
         * 
         **/

        private void drawMotorTravelArea()
        {
            // Draw the table area
            Draw.Rectangle(canvasDrawing,
                canvasCalibrationPoints[0].X,
                canvasCalibrationPoints[0].Y,
                canvasCalibrationPoints[1].X - canvasCalibrationPoints[0].X,
                canvasCalibrationPoints[1].Y - canvasCalibrationPoints[0].Y,
                Brushes.Blue);
        }

        private void moveMotors_Tick(object sender, EventArgs e)
        {
            _scans.ElementAt<ScanLevel>(currentZScanIndex).State = "In Progress";

            // Start the count down timer if it is
            // not already started
            if(!CDTimer.Enabled)
                CDTimer.Start();

            // Move the motors to the next location
            int nextX = motorXPos;
            int nextY = motorYPos;
            int nextZ = motorZPos;

            Logger.writeToFile(mLogFileName, nextX, nextY, 0, getScannedData(461));

            // Update the motor position text
            lblXPosition.Text = nextX.ToString();
            lblYPosition.Text = nextY.ToString();
            lblZPosition.Text = nextZ.ToString();

            // draw heat map pixel
            int row = (motorXPos - (int)motorScanAreaPoints[0].X) / (int)nsXStepSize.Value;
            int col = (motorYPos - (int)motorScanAreaPoints[0].Y) / (int)nsYStepSize.Value;
            mHeatMap.drawPixel(row, col, Colors.Blue);

            // Determine the next scan position
            if (currentScanDirection == Constants.direction.South)
            {
                nextY = nextY + nsYStepSize.Value;
                if (nextY > motorScanAreaPoints[1].Y)
                {
                    currentScanDirection = Utilities.reverseDirection(currentScanDirection);
                    nextY = nextY - nsYStepSize.Value;
                    nextX = nextX + nsXStepSize.Value;
                }
            }
            else if (currentScanDirection == Constants.direction.North)
            {
                nextY = nextY - nsYStepSize.Value;
                if (nextY < motorScanAreaPoints[0].Y)
                {
                    currentScanDirection = Utilities.reverseDirection(currentScanDirection);
                    nextY = nextY + nsYStepSize.Value;
                    nextX = nextX + nsXStepSize.Value;
                }
            }

            if (nextX > motorScanAreaPoints[1].X)
            {
                if (nextZ + nsZStepSize.Value > nsZMax.Value)
                {
                    simulatorTimer.Stop();
                    transitionToState(Constants.state.Overview);

                    string messageBoxText = "Scan Finished!";
                    string caption = "Information";
                    MessageBoxButton button = MessageBoxButton.OK;
                    MessageBoxImage icon = MessageBoxImage.Information;
                    MessageBox.Show(messageBoxText, caption, button, icon);
                }
                else
                {
                    nextZ += nsZStepSize.Value;

                    _scans.ElementAt<ScanLevel>(currentZScanIndex).State = "Complete";
                    
                    currentZScanIndex++;

                    // Put the motors back to their starting positions
                    nextX = Convert.ToInt32(motorScanAreaPoints[0].X);
                    nextY = Convert.ToInt32(motorScanAreaPoints[0].Y);
                    currentScanDirection = Constants.direction.South;
                    


                    mHeatMap.ClearPixels();
                }
            }
                
            motorXPos = nextX;
            motorYPos = nextY;
            motorZPos = nextZ;
        }

        /// <summary>
        /// Ensures that the values entered into the numeric steppers are valid
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void nsValidator(object sender, ValueChangedEventArgs<int> e)
        {
            // Step size cannot be less than 1
            if (e.NewValue < 1)
            {
                NumericTextBoxInt32 numericStepper = sender as NumericTextBoxInt32;
                numericStepper.Value = 1;
            }
        }

        private int[] getScannedData(int aPoints)
        {
            Random rNum = new Random();
            int[] data = new int[aPoints];

            for (int i = 0; i < aPoints; i++)
            {
                data[i] = rNum.Next(110) - rNum.Next(40);
                if (i > 82)
                    data[i] += rNum.Next(60) - rNum.Next(20);
                if(i > 216)
                    data[i] += rNum.Next(60) - rNum.Next(20);
            }

            return data;
        }

        private Boolean analyzeScannedData(int[] aData, ThresholdViewModel aThreshold)
        {

            return true;
        }

        private void startSimulator()
        {
            simulatorTimer = new DispatcherTimer();
            simulatorTimer.Tick += new EventHandler(moveMotors_Tick);
            simulatorTimer.Interval = new TimeSpan(0, 0, 1);
            simulatorTimer.Start();
        }

        private void loadUserPreferences()
        {
            // Load user/application settings
            imageCalibrationPoints[0] = (Point)Properties.Settings.Default["TableBL"];
            imageCalibrationPoints[1] = (Point)Properties.Settings.Default["TableTR"];

            motorXTravelDistance = Properties.Settings.Default.motorXTravelDistance;
            motorYTravelDistance = Properties.Settings.Default.motorYTravelDistance;
            motorZTravelDistance = Properties.Settings.Default.motorZTravelDistance;

            sHeatMapOpacity.Value = Properties.Settings.Default.heatMapOpacity;

            nsXStepSize.Value = (int)Properties.Settings.Default["nsXStepSize"];
            nsYStepSize.Value = (int)Properties.Settings.Default["nsYStepSize"];
            nsZStepSize.Value = (int)Properties.Settings.Default.nsZStepSize;
            nsZMin.Value = (int)Properties.Settings.Default.nsZMin;
            nsZMax.Value = (int)Properties.Settings.Default.nsZMax;
        }

        private void saveUserPreferences()
        {
            Properties.Settings.Default["TableBL"] = imageCalibrationPoints[0];
            Properties.Settings.Default["TableTR"] = imageCalibrationPoints[1];

            Properties.Settings.Default.motorXTravelDistance = motorXTravelDistance;
            Properties.Settings.Default.motorYTravelDistance = motorYTravelDistance;
            Properties.Settings.Default.motorZTravelDistance = motorZTravelDistance;

            Properties.Settings.Default["nsXStepSize"] = nsXStepSize.Value;
            Properties.Settings.Default["nsYStepSize"] = nsYStepSize.Value;
            Properties.Settings.Default.nsZStepSize = nsZStepSize.Value;
            Properties.Settings.Default.nsZMin = nsZMin.Value;
            Properties.Settings.Default.nsZMax = nsZMax.Value;

            Properties.Settings.Default.heatMapOpacity = sHeatMapOpacity.Value;
            
            Properties.Settings.Default.Save();
        }

        private void heatMapOpacityChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            var slider = sender as Slider;
            mHeatMap.setPixelOpacity(slider.Value);
        }

        private void setGUIControlIsEnabled(Boolean aIsEnabled)
        {
            btnMotorJogUp.IsEnabled = aIsEnabled;
            btnMotorJogDown.IsEnabled = aIsEnabled;
            btnMotorJogLeft.IsEnabled = aIsEnabled;
            btnMotorJogRight.IsEnabled = aIsEnabled;

            nsXStepSize.IsEnabled = aIsEnabled;
            nsYStepSize.IsEnabled = aIsEnabled;
            nsZStepSize.IsEnabled = aIsEnabled;
            nsZMax.IsEnabled = aIsEnabled;
            nsZMin.IsEnabled = aIsEnabled;
        }
        
    }
}
